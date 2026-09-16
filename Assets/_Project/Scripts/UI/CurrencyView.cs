using MatchPack.Core;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Menüdeki gold ve can göstergesi. EconomyManager event'lerini dinler, değerleri yazar ve
    /// "+" butonlarını ilgili popup'a bağlar. Hiçbir değeri Update içinde yoklamaz.
    /// </summary>
    public class CurrencyView : MonoBehaviour
    {
        [Tooltip("Gold miktarının yazıldığı alan.")]
        [SerializeField] private TMP_Text _goldText;

        [Tooltip("Can sayısının yazıldığı alan.")]
        [SerializeField] private TMP_Text _livesText;

        [Tooltip("Bir sonraki cana kalan sürenin yazıldığı alan.")]
        [SerializeField] private TMP_Text _lifeTimerText;

        [Tooltip("Gold popup'ını açan + butonu.")]
        [SerializeField] private Button _goldAddButton;

        [Tooltip("Can popup'ını açan + butonu.")]
        [SerializeField] private Button _heartAddButton;

        [Tooltip("Sınırsız can aktifken can sayısı yerine yazılacak metin.")]
        [SerializeField] private string _infiniteLivesLabel = "∞";

        [Tooltip("Canlar doluyken sayaç alanına yazılacak metin.")]
        [SerializeField] private string _fullLivesLabel = "FULL";

        private void Awake()
        {
            _goldAddButton.onClick.AddListener(OpenGoldPopup);
            _heartAddButton.onClick.AddListener(OpenHeartPopup);
        }

        private void Start()
        {
            EconomyManager.Instance.OnGoldChanged += SetGold;
            EconomyManager.Instance.OnLivesChanged += SetLives;
            EconomyManager.Instance.OnLifeTimerTicked += SetLifeTimer;

            SetGold(EconomyManager.Instance.Gold);
            SetLives(EconomyManager.Instance.Lives);
            SetLifeTimer(EconomyManager.Instance.GetSecondsUntilNextLife());
        }

        private void OnDestroy()
        {
            _goldAddButton.onClick.RemoveListener(OpenGoldPopup);
            _heartAddButton.onClick.RemoveListener(OpenHeartPopup);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnGoldChanged -= SetGold;
                EconomyManager.Instance.OnLivesChanged -= SetLives;
                EconomyManager.Instance.OnLifeTimerTicked -= SetLifeTimer;
            }
        }

        /// <summary>Gold popup'ını açar.</summary>
        public void OpenGoldPopup()
        {
            UIManager.Instance.ShowGoldPopup();
        }

        /// <summary>Can popup'ını açar.</summary>
        public void OpenHeartPopup()
        {
            UIManager.Instance.ShowHeartPopup();
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
                _lifeTimerText.text = _fullLivesLabel;
                return;
            }

            int totalSeconds = Mathf.CeilToInt(secondsRemaining);
            _lifeTimerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
