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

        [Tooltip("Objenin kutuya uçarken çizdiği kavisin yüksekliği.")]
        [SerializeField, Min(0f)] private float _flightArcHeight = 1.5f;

        [Tooltip("Hatalı hamlede kameranın sarsılma süresi.")]
        [SerializeField, Min(0f)] private float _shakeDuration = 0.2f;

        [Tooltip("Hatalı hamlede kameranın sarsılma şiddeti.")]
        [SerializeField, Min(0f)] private float _shakeStrength = 0.15f;

        private void Start()
        {
            InputManager.Instance.OnItemTapped += HandleItemTapped;
            SceneLoader.Instance.OnBeforeLevelTeardown += HandleBeforeLevelTeardown;
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnItemTapped -= HandleItemTapped;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnBeforeLevelTeardown -= HandleBeforeLevelTeardown;
            }
        }

        private void HandleBeforeLevelTeardown()
        {
            Transform cameraTransform = GetCameraTransform();
            if (cameraTransform != null) { cameraTransform.DOKill(true); }
        }

        // Kamera GameScene'de durduğu için Inspector'dan bağlanamaz; LevelContext üzerinden okunur.
        private static Transform GetCameraTransform()
        {
            if (SceneLoader.Instance == null || SceneLoader.Instance.ActiveLevel == null) { return null; }

            Camera camera = SceneLoader.Instance.ActiveLevel.Camera;
            return camera != null ? camera.transform : null;
        }

        /// <summary>
        /// Objeyi bantta uygun bir kutuya gönderir. Dokunuşla aynı yolu işletir ama hatalı hamle
        /// geri bildirimi vermez; boosterlar dokunuş olmadan bunu çağırır.
        /// </summary>
        public bool TryMatch(StackItem item)
        {
            if (item == null || !_conveyor.TryReserveBox(item, out Box box, out Transform slot)) { return false; }

            Match(item, box, slot);
            return true;
        }

        /// <summary>
        /// Objeyi belirli bir kutuya gönderir. Auto-Match'in kutu modu hedefi kendisi seçtiği için
        /// bunu kullanır; kutu doluysa veya tip uymuyorsa false döner.
        /// </summary>
        public bool TryMatchInto(StackItem item, Box box)
        {
            if (item == null || box == null || !box.TryAddItem(item, out Transform slot)) { return false; }

            Match(item, box, slot);
            return true;
        }

        private void HandleItemTapped(StackItem item)
        {
            if (GameManager.Instance.State != GameState.Playing || !_itemStack.IsSettled) { return; }

            if (!TryMatch(item)) { Miss(item); }
        }

        private void Match(StackItem item, Box box, Transform slot)
        {
            _itemStack.Remove(item);
            item.SetSimulated(false);

            // Kutu bant üzerinde ilerlediği için hedef sabit değil; obje yuvaya parent edilip
            // yerel uzayda uçurulur, böylece uçuş boyunca kutuyla birlikte hareket eder.
            item.transform.SetParent(slot, true);

            item.transform
                .DOLocalJump(Vector3.zero, _flightArcHeight, 1, _config.ItemFlyDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => box.ConfirmItem(item));

            item.transform.DOLocalRotateQuaternion(Quaternion.identity, _config.ItemFlyDuration);

            OnItemMatched?.Invoke(item);
        }

        private void Miss(StackItem item)
        {
            Transform cameraTransform = GetCameraTransform();

            if (cameraTransform != null)
            {
                cameraTransform.DOKill(true);
                cameraTransform.DOShakePosition(_shakeDuration, _shakeStrength);
            }

            OnItemMissed?.Invoke(item);
        }
    }
}
