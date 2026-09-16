using DG.Tweening;
using MatchPack.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Hatalı hamlede ekranın kenarlarını kırmızı bir vinyet ile parlatır. Efekt tam ekran bir
    /// Image üzerinde MatchPack/UI/ScreenEdgeFlash shader'ı ile çizilir; kural işletmez, yalnızca
    /// hatalı hamle event'ini dinler.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ScreenEdgeFlash : MonoBehaviour
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        [Tooltip("Hatalı hamle event'inin dinlendiği çözümleyici.")]
        [SerializeField] private MatchResolver _matchResolver;

        [Tooltip("Efektin çizildiği tam ekran görsel.")]
        [SerializeField] private Image _image;

        [Tooltip("Parlamanın ulaşacağı en yüksek yoğunluk.")]
        [SerializeField, Range(0f, 1f)] private float _peakIntensity = 0.85f;

        [Tooltip("Yoğunluğun tepeye çıkma süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _fadeInSeconds = 0.06f;

        [Tooltip("Yoğunluğun sıfıra dönme süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _fadeOutSeconds = 0.35f;

        private Material _materialInstance;
        private RectTransform _rectTransform;
        private Sequence _flashSequence;
        private float _intensity;

        private void Awake()
        {
            if (_image == null) { _image = GetComponent<Image>(); }

            _rectTransform = (RectTransform)transform;

            // Materyal asset'i paylaşıldığı için yoğunluk doğrudan üzerine yazılamaz; örnek çıkarılır.
            _materialInstance = new Material(_image.material);
            _image.material = _materialInstance;
            _image.raycastTarget = false;

            SetIntensity(0f);
            ApplyAspect();
        }

        private void OnRectTransformDimensionsChange()
        {
            // Ekran döndüğünde veya çözünürlük değiştiğinde kenar kalınlığı bozulmasın diye.
            if (_materialInstance == null) { return; }

            ApplyAspect();
        }

        private void Start()
        {
            if (_matchResolver != null)
            {
                _matchResolver.OnItemMissed += HandleItemMissed;
            }
        }

        private void OnDestroy()
        {
            if (_matchResolver != null)
            {
                _matchResolver.OnItemMissed -= HandleItemMissed;
            }

            _flashSequence?.Kill();

            if (_materialInstance != null) { Destroy(_materialInstance); }
        }

        /// <summary>Kenar parlamasını baştan tetikler. Üst üste çağrılırsa önceki tween durdurulur.</summary>
        public void Flash()
        {
            _flashSequence?.Kill();

            _flashSequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_intensity, _peakIntensity, _fadeInSeconds, SetIntensity))
                .Append(DOVirtual.Float(_peakIntensity, 0f, _fadeOutSeconds, SetIntensity))
                .SetUpdate(true);
        }

        private void ApplyAspect()
        {
            Rect rect = _rectTransform.rect;
            float aspect = rect.height > 0f ? rect.width / rect.height : 1f;

            _materialInstance.SetFloat(AspectId, aspect);
        }

        private void SetIntensity(float value)
        {
            _intensity = value;
            _materialInstance.SetFloat(IntensityId, value);
        }

        private void HandleItemMissed(StackItem item)
        {
            Flash();
        }
    }
}
