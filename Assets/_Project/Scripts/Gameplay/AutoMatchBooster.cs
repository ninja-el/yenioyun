using System.Collections;
using System.Collections.Generic;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Auto-Match. BoosterData'daki moda göre ya belirli sayıda objeyi uygun kutulara gönderir
    /// (Items) ya da belirli sayıda kutuyu tamamen doldurur (Boxes). Hamleleri dokunuşla aynı
    /// yoldan, aralarında kısa bir bekleme bırakarak işletir.
    /// </summary>
    public class AutoMatchBooster : BoosterBehaviour
    {
        [Tooltip("Hedef kutuların arandığı bant.")]
        [SerializeField] private Conveyor _conveyor;

        [Tooltip("Gönderilecek objelerin alındığı yığın.")]
        [SerializeField] private ItemStack _itemStack;

        [Tooltip("Hamleyi işleten çözümleyici.")]
        [SerializeField] private MatchResolver _matchResolver;

        private readonly List<Box> _boxBuffer = new List<Box>();

        private Coroutine _routine;

        public override BoosterType Type => BoosterType.AutoMatch;

        public override bool TryActivate(BoosterData data)
        {
            if (data == null || _routine != null || !_itemStack.IsSettled) { return false; }
            if (!HasAnyTarget(data.AutoMatchMode)) { return false; }

            _routine = StartCoroutine(RunRoutine(data));
            BoosterManager.Instance.PlayEffect(data.EffectPrefab, data.EffectDuration);
            return true;
        }

        public override void Cancel()
        {
            if (_routine == null) { return; }

            StopCoroutine(_routine);
            _routine = null;
            _boxBuffer.Clear();
        }

        private IEnumerator RunRoutine(BoosterData data)
        {
            WaitForSeconds wait = new WaitForSeconds(data.AutoMatchInterval);

            if (data.AutoMatchMode == AutoMatchMode.Items)
            {
                yield return MatchItemsRoutine(data.AutoMatchCount, wait);
            }
            else
            {
                yield return FillBoxesRoutine(data.AutoMatchCount, wait);
            }

            _routine = null;
        }

        private IEnumerator MatchItemsRoutine(int itemCount, WaitForSeconds wait)
        {
            for (int i = 0; i < itemCount; i++)
            {
                StackItem item = FindMatchableItem();
                if (item == null || !_matchResolver.TryMatch(item)) { yield break; }

                yield return wait;
            }
        }

        private IEnumerator FillBoxesRoutine(int boxCount, WaitForSeconds wait)
        {
            _conveyor.CollectFillableBoxes(_boxBuffer);
            int filledBoxCount = 0;

            for (int i = 0; i < _boxBuffer.Count && filledBoxCount < boxCount; i++)
            {
                Box box = _boxBuffer[i];

                while (!box.IsFilled)
                {
                    StackItem item = _itemStack.FindItem(box.Type);
                    if (item == null || !_matchResolver.TryMatchInto(item, box)) { break; }

                    yield return wait;
                }

                if (box.IsFilled) { filledBoxCount++; }
            }

            _boxBuffer.Clear();
        }

        private bool HasAnyTarget(AutoMatchMode mode)
        {
            if (mode == AutoMatchMode.Items) { return FindMatchableItem() != null; }

            _conveyor.CollectFillableBoxes(_boxBuffer);

            for (int i = 0; i < _boxBuffer.Count; i++)
            {
                if (_itemStack.FindItem(_boxBuffer[i].Type) != null)
                {
                    _boxBuffer.Clear();
                    return true;
                }
            }

            _boxBuffer.Clear();
            return false;
        }

        private StackItem FindMatchableItem()
        {
            IReadOnlyList<StackItem> items = _itemStack.Items;

            for (int i = 0; i < items.Count; i++)
            {
                if (_conveyor.TryGetBoxFor(items[i].Type, out _)) { return items[i]; }
            }

            return null;
        }
    }
}
