using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Kameranın yatay görüş alanını referans çözünürlükteki değerinde sabitler. Unity dikey
    /// FOV'u saklayıp yatayı aspect'ten türettiği için, referanstan daha dar ekranlarda yanlardan
    /// alan kaybedilir; bu bileşen dikey FOV'u büyüterek bunu telafi eder.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraAspectFitter : MonoBehaviour
    {
        [Tooltip("Çerçevenin tasarlandığı referans çözünürlük. 02-Mimari.md: 1080x1920.")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1080f, 1920f);

        [Tooltip("Referans çözünürlükte geçerli olan dikey FOV. Kameradaki değerle aynı olmalı.")]
        [SerializeField, Range(1f, 179f)] private float _referenceVerticalFov = 60f;

        private Camera _camera;
        private float _referenceHalfTangent;
        private float _horizontalHalfTangent;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            _referenceHalfTangent = Mathf.Tan(_referenceVerticalFov * 0.5f * Mathf.Deg2Rad);
            _horizontalHalfTangent = _referenceHalfTangent * (_referenceResolution.x / _referenceResolution.y);
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // Ekran döndüğünde veya bölünmüş ekranda çözünürlük değişebilir; sadece o zaman hesapla.
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) { return; }

            Apply();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (_camera.orthographic) { return; }

            float aspect = _camera.aspect;
            if (aspect <= 0f) { return; }

            // Referanstan genişse dikeyi kısmak kareyi yukarıdan kırpardı; o yüzden asla küçültme.
            float requiredHalfTangent = Mathf.Max(_referenceHalfTangent, _horizontalHalfTangent / aspect);

            _camera.fieldOfView = 2f * Mathf.Atan(requiredHalfTangent) * Mathf.Rad2Deg;
        }
    }
}
