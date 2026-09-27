using System.Text;
using MatchPack.Localization;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Bağlı olduğu TMP yazısının sonundaki noktaları en az ve en çok adet arasında döngüyle
    /// artırır (varsayılan ".." → "..." → ".."). Herhangi bir sahnede tek başına çalışır; obje
    /// aktif olduğu sürece oynar.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LoadingDotsText : MonoBehaviour
    {
        [Tooltip("Noktalardan önce yazılacak metnin localization key'i.")]
        [SerializeField] private string _messageKey = "ui.loading.message";

        [Tooltip("Yazının sonundaki en az nokta adedi.")]
        [SerializeField, Min(0)] private int _minDotCount = 2;

        [Tooltip("Yazının sonundaki en çok nokta adedi.")]
        [SerializeField, Min(1)] private int _maxDotCount = 3;

        [Tooltip("Nokta adedinin değişme aralığı (saniye).")]
        [SerializeField, Min(0.02f)] private float _dotIntervalSeconds = 0.35f;

        private readonly StringBuilder _labelBuilder = new StringBuilder();

        private TMP_Text _label;
        private float _dotTimer;
        private int _dotCount;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            _dotTimer = 0f;
            _dotCount = _minDotCount;
            Loc.OnLanguageChanged += WriteLabel;
            WriteLabel();
        }

        private void OnDisable()
        {
            Loc.OnLanguageChanged -= WriteLabel;
        }

        private void Update()
        {
            // Yükleme sırasında Time.timeScale sıfırlanabildiği için ölçeksiz zaman kullanılır.
            _dotTimer += Time.unscaledDeltaTime;
            if (_dotTimer < _dotIntervalSeconds) { return; }

            _dotTimer -= _dotIntervalSeconds;
            _dotCount = _dotCount >= _maxDotCount ? _minDotCount : _dotCount + 1;
            WriteLabel();
        }

        private void WriteLabel()
        {
            _labelBuilder.Clear();
            _labelBuilder.Append(Loc.Get(_messageKey));

            for (int i = 0; i < _dotCount; i++) { _labelBuilder.Append('.'); }

            _label.SetText(_labelBuilder);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_maxDotCount < _minDotCount) { _maxDotCount = _minDotCount; }
        }
#endif
    }
}
