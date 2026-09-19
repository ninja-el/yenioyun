using MatchPack.Core;
using MatchPack.Localization;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Menüdeki gold ve can göstergesi. EconomyManager event'lerini dinler, değerleri yazar ve
    /// "+" butonlarını market paneline bağlar. Hiçbir değeri Update içinde yoklamaz.
    /// </summary>
    public class CurrencyView : MonoBehaviour
    {
        [Tooltip("Gold miktarının yazıldığı alan.")]
        [SerializeField] private TMP_Text _goldText;

        [Tooltip("Can sayısının yazıldığı alan.")]
        [SerializeField] private TMP_Text _livesText;

        [Tooltip("Can sayacının yazıldığı alan. Sınırsız can aktifken onun bitişine kalan süreyi gösterir.")]
        [SerializeField] private TMP_Text _lifeTimerText;

        [Tooltip("Market panelini açan gold + butonu.")]
        [SerializeField] private Button _goldAddButton;

        [Tooltip("Market panelini açan can + butonu.")]
        [SerializeField] private Button _heartAddButton;

        [Tooltip("Sınırsız can aktifken can sayısı yerine yazılacak metin.")]
        [SerializeField] private string _infiniteLivesLabel = "∞";

        [Tooltip("Canlar doluyken sayaç alanına yazılacak metnin localization key'i.")]
        [SerializeField] private string _fullLivesKey = "ui.currency.full";

        private void Awake()
        {
            _goldAddButton.onClick.AddListener(OpenMarket);
            _heartAddButton.onClick.AddListener(OpenMarket);
        }

        private void Start()
        {
            EconomyManager.Instance.OnGoldChanged += SetGold;
            EconomyManager.Instance.OnLivesChanged += SetLives;
            EconomyManager.Instance.OnLifeTimerTicked += SetLifeTimer;
            Loc.OnLanguageChanged += HandleLanguageChanged;

            SetGold(EconomyManager.Instance.Gold);
            SetLives(EconomyManager.Instance.Lives);
            SetLifeTimer(EconomyManager.Instance.LifeTimerSeconds);
        }

        private void OnDestroy()
        {
            _goldAddButton.onClick.RemoveListener(OpenMarket);
            _heartAddButton.onClick.RemoveListener(OpenMarket);

            Loc.OnLanguageChanged -= HandleLanguageChanged;

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnGoldChanged -= SetGold;
                EconomyManager.Instance.OnLivesChanged -= SetLives;
                EconomyManager.Instance.OnLifeTimerTicked -= SetLifeTimer;
            }
        }

        /// <summary>Market panelini açar.</summary>
        public void OpenMarket()
        {
            UIManager.Instance.ShowMarket();
        }

        private void SetGold(int gold)
        {
            if (_goldText != null) { _goldText.text = gold.ToString(); }
        }

        private void SetLives(int lives)
        {
            if (_livesText == null) { return; }

            if (EconomyManager.Instance.HasInfiniteLives)
            {
                _livesText.text = _infiniteLivesLabel;
                return;
            }

            _livesText.text = lives.ToString();
        }

        private void SetLifeTimer(float secondsRemaining)
        {
            if (_lifeTimerText == null) { return; }

            if (secondsRemaining <= 0f)
            {
                _lifeTimerText.text = Loc.Get(_fullLivesKey);
                return;
            }

            _lifeTimerText.text = FormatDuration(secondsRemaining);
        }

        // "FULL" ve sonsuz can işareti dışındaki metinler LocalizedText ile gelir; bu ikisini
        // kod yazdığı için dil değişiminde elle yenilenmeleri gerekir.
        private void HandleLanguageChanged()
        {
            SetLives(EconomyManager.Instance.Lives);
            SetLifeTimer(EconomyManager.Instance.LifeTimerSeconds);
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));

            // Sınırsız can paketleri saat bazlı; 48 saat mm:ss ile "2880:00" görünüyordu.
            if (totalSeconds >= 3600)
            {
                return $"{totalSeconds / 3600}:{totalSeconds / 60 % 60:00}:{totalSeconds % 60:00}";
            }

            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
