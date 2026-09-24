using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Auto-Match. Bantta en boş kutuyu seçer, eksik objelerini yığından ayırtır ve bir
    /// <see cref="AutoMatchUfo"/> gönderir; UFO objeleri toplayıp kutuyu tamamlar. Art arda
    /// kullanılabilir, iki kullanım arasında BoosterData'daki bekleme süresi kadar zaman geçmelidir.
    /// </summary>
    public class AutoMatchBooster : BoosterBehaviour
    {
        private static readonly Comparison<Box> ByItemCount = (left, right) => left.ItemCount.CompareTo(right.ItemCount);

        [Tooltip("Hedef kutuların arandığı bant.")]
        [SerializeField] private Conveyor _conveyor;

        [Tooltip("Toplanacak objelerin alındığı yığın.")]
        [SerializeField] private ItemStack _itemStack;

        private readonly List<Box> _boxBuffer = new List<Box>();
        private readonly List<StackItem> _itemBuffer = new List<StackItem>();
        private readonly List<AutoMatchUfo> _activeUfos = new List<AutoMatchUfo>();

        private Action<AutoMatchUfo> _handleUfoFinished;
        private float _nextActivationTime;

        public override BoosterType Type => BoosterType.AutoMatch;

        private void Awake()
        {
            _handleUfoFinished = HandleUfoFinished;
        }

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || !_itemStack.IsSettled) { return false; }

            if (data.UfoPrefab == null)
            {
                Debug.LogError("Auto-Match has no UFO prefab; run MatchPack > Build Auto-Match UFO.", data);
                return false;
            }

            if (Time.time < _nextActivationTime || !TryFindTarget(out Box box)) { return false; }

            AutoMatchUfo ufo = SpawnUfo(data.UfoPrefab);
            if (ufo == null) { return false; }

            ReserveItems(box);
            ufo.Play(box, _itemBuffer, GetCamera(), _handleUfoFinished);
            _itemBuffer.Clear();

            _nextActivationTime = Time.time + data.AutoMatchCooldown;
            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            return true;
        }

        public override void Cancel()
        {
            for (int i = 0; i < _activeUfos.Count; i++)
            {
                PoolManager.Instance.Release(_activeUfos[i].gameObject);
            }

            _activeUfos.Clear();
            _boxBuffer.Clear();
            _itemBuffer.Clear();
            _nextActivationTime = 0f;
        }

        // En boş kutu önce denenir; eksik objelerinin hepsi yığında değilse (bir kısmı henüz doğmamış
        // olabilir) sıradaki kutuya geçilir. Bulunan kutunun objeleri _itemBuffer'da kalır.
        private bool TryFindTarget(out Box box)
        {
            box = null;
            _conveyor.CollectFillableBoxes(_boxBuffer);
            _boxBuffer.Sort(ByItemCount);

            for (int i = 0; i < _boxBuffer.Count; i++)
            {
                if (!CollectItems(_boxBuffer[i].Type, _boxBuffer[i].FreeSlotCount)) { continue; }

                box = _boxBuffer[i];
                break;
            }

            _boxBuffer.Clear();
            return box != null;
        }

        private bool CollectItems(ItemType type, int count)
        {
            _itemBuffer.Clear();
            IReadOnlyList<StackItem> items = _itemStack.Items;

            for (int i = 0; i < items.Count && _itemBuffer.Count < count; i++)
            {
                if (items[i].Type == type) { _itemBuffer.Add(items[i]); }
            }

            return _itemBuffer.Count == count;
        }

        // Yuvalar UFO yola çıkmadan ayrılır: kutu dolu sayılır, oyuncu ve diğer UFO'lar o kutuya obje gönderemez.
        private void ReserveItems(Box box)
        {
            for (int i = 0; i < _itemBuffer.Count; i++)
            {
                StackItem item = _itemBuffer[i];
                _itemStack.Remove(item);
                item.SetSimulated(false);
                box.TryAddItem(item, out _);
            }
        }

        private AutoMatchUfo SpawnUfo(GameObject prefab)
        {
            GameObject instance = PoolManager.Instance.Get(prefab);
            if (instance == null) { return null; }

            AutoMatchUfo ufo = instance.GetComponent<AutoMatchUfo>();

            if (ufo == null)
            {
                Debug.LogError($"Auto-Match UFO prefab '{prefab.name}' has no AutoMatchUfo component.", prefab);
                PoolManager.Instance.Release(instance);
                return null;
            }

            _activeUfos.Add(ufo);
            return ufo;
        }

        private void HandleUfoFinished(AutoMatchUfo ufo)
        {
            _activeUfos.Remove(ufo);
            PoolManager.Instance.Release(ufo.gameObject);
        }

        private static Camera GetCamera()
        {
            if (SceneLoader.Instance == null || SceneLoader.Instance.ActiveLevel == null) { return null; }

            return SceneLoader.Instance.ActiveLevel.Camera;
        }
    }
}
