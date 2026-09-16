using System.Text;
using TMPro;
using UnityEngine;

namespace MatchPack.UI
{
    /// <summary>
    /// Level hazırlanırken açılan yükleme ekranı. Ayarlanabilir bir sahte bekleme süresi işletir ve
    /// yazının sonundaki noktaları döngüyle artırıp sıfırlar. Hazırlık bu süreden uzun sürerse ekran
    /// açık kalmaya devam eder; kapatma kararı GameManager'a aittir.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("Ekranın kökü. Açılıp kapanan obje budur; bu bileşenle aynı obje de olabilir.")]
        [SerializeField] private GameObject _root;

        [Tooltip("\"Yükleniyor\" yazısının alanı.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Noktalardan önce yazılacak metin.")]
        [SerializeField] private string _message = "Yükleniyor";

        [Tooltip("Sahte yükleme süresi (saniye). Hazırlık erken bitse de ekran bu kadar açık kalır.")]
        [SerializeField, Min(0f)] private float _fakeDurationSeconds = 1.5f;

        [Tooltip("Yazının sonundaki noktaların çıkacağı en yüksek adet.")]
        [SerializeField, Min(1)] private int _maxDotCount = 3;

        [Tooltip("Bir noktanın eklenme aralığı (saniye).")]
        [SerializeField, Min(0.02f)] private float _dotIntervalSeconds = 0.35f;

        private readonly StringBuilder _labelBuilder = new StringBuilder();

        private float _elapsedSeconds;
        private float _dotTimer;
        private int _dotCount;

        /// <summary>Sahte bekleme süresi doldu mu? Ekranın kapanabilmesi için bunun true olması gerekir.</summary>
        public bool IsFakeDelayComplete => _elapsedSeconds >= _fakeDurationSeconds;

        /// <summary>Ekran şu an açık mı?</summary>
        public bool IsVisible => _root != null && _root.activeSelf;

        private void Awake()
        {
            if (_root == null)
            {
                Debug.LogError("LoadingScreen has no root assigned; the screen will never show.", this);
            }
        }

        /// <summary>Ekranı açar ve sahte süreyi baştan başlatır.</summary>
        public void Show()
        {
            _elapsedSeconds = 0f;
            _dotTimer = 0f;
            _dotCount = 0;

            SetRootActive(true);
            WriteLabel();
        }

        /// <summary>Ekranı kapatır. Sahte süre dolmadan çağrılırsa bekleme iptal olur.</summary>
        public void Hide()
        {
            SetRootActive(false);
        }

        private void Update()
        {
            if (!IsVisible) { return; }

            // Yükleme sırasında Time.timeScale sıfırlanabildiği için ölçeksiz zaman kullanılır.
            _elapsedSeconds += Time.unscaledDeltaTime;
            _dotTimer += Time.unscaledDeltaTime;

            if (_dotTimer < _dotIntervalSeconds) { return; }

            _dotTimer -= _dotIntervalSeconds;
            _dotCount = (_dotCount + 1) % (_maxDotCount + 1);
            WriteLabel();
        }

        private void WriteLabel()
        {
            if (_label == null) { return; }

            _labelBuilder.Clear();
            _labelBuilder.Append(_message);

            for (int i = 0; i < _dotCount; i++) { _labelBuilder.Append('.'); }

            _label.SetText(_labelBuilder);
        }

        private void SetRootActive(bool isActive)
        {
            if (_root == null || _root.activeSelf == isActive) { return; }

            _root.SetActive(isActive);
        }
    }
}
