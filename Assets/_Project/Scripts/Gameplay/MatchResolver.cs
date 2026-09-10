using System;
using DG.Tweening;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Dokunulan objeye bantta uygun kutu arar. Bulursa objeyi fizik dışına alıp kavisli bir uçuşla
    /// kutuya gönderir, bulamazsa hatalı hamle geri bildirimini tetikler.
    /// </summary>
    public class MatchResolver : MonoBehaviour
    {
        /// <summary>Obje uygun bir kutuya gönderildiğinde yayınlanır.</summary>
        public event Action<StackItem> OnItemMatched;

        /// <summary>Objeye uygun kutu bulunamadığında yayınlanır. Ses ve VFX bu event'i dinler.</summary>
        public event Action<StackItem> OnItemMissed;

        [Tooltip("Uçuş süresinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [SerializeField] private Conveyor _conveyor;
        [SerializeField] private ItemStack _itemStack;

        [Tooltip("Hatalı hamlede sarsılacak kalıcı kamera.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Objenin kutuya uçarken çizdiği kavisin yüksekliği.")]
        [SerializeField, Min(0f)] private float _flightArcHeight = 1.5f;

        [Tooltip("Hatalı hamlede kameranın sarsılma süresi.")]
        [SerializeField, Min(0f)] private float _shakeDuration = 0.2f;

        [Tooltip("Hatalı hamlede kameranın sarsılma şiddeti.")]
        [SerializeField, Min(0f)] private float _shakeStrength = 0.15f;

        private void Start()
        {
            InputManager.Instance.OnItemTapped += HandleItemTapped;
            SceneLoader.Instance.OnBeforeLevelUnload += HandleBeforeLevelUnload;
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnItemTapped -= HandleItemTapped;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnBeforeLevelUnload -= HandleBeforeLevelUnload;
            }
        }

        private void HandleBeforeLevelUnload()
        {
            _cameraTransform.DOKill(true);
        }

        private void HandleItemTapped(StackItem item)
        {
            if (GameManager.Instance.State != GameState.Playing || !_itemStack.IsSettled) { return; }

            if (!_conveyor.TryGetBoxFor(item.Type, out Box box) || !box.TryAddItem(item, out Transform slot))
            {
                Miss(item);
                return;
            }

            Match(item, box, slot);
        }

        private void Match(StackItem item, Box box, Transform slot)
        {
            _itemStack.Remove(item);
            item.SetSimulated(false);

            item.transform
                .DOJump(slot.position, _flightArcHeight, 1, _config.ItemFlyDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => box.ConfirmItem(item));

            OnItemMatched?.Invoke(item);
        }

        private void Miss(StackItem item)
        {
            _cameraTransform.DOKill(true);
            _cameraTransform.DOShakePosition(_shakeDuration, _shakeStrength);

            OnItemMissed?.Invoke(item);
        }
    }
}
