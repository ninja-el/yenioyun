using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Can bittiğinde açılan popup. Reklam butonu bir can, gold butonu canları tamamen doldurur.
    /// Bakiye işini EconomyManager yapar; burada yalnızca buton bağlantıları ve metinler vardır.
    /// Panel kapalıyken de bağlı kalması için bileşen UI_Canvas üzerinde durur.
    /// </summary>
    public class HeartPopup : MonoBehaviour
    {
        [Tooltip("Can doldurma maliyetinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [Tooltip("Popup'ı kapatan buton.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Reklam izleyip bir can veren buton.")]
        [SerializeField] private Button _adButton;

        [Tooltip("Gold ödeyip canları dolduran buton.")]
        [SerializeField] private Button _goldButton;

        [Tooltip("Mevcut can sayısının yazıldığı alan.")]
        [SerializeField] private TMP_Text _livesText;

        [Tooltip("Bir sonraki cana kalan sürenin yazıldığı alan.")]
        [SerializeField] private TMP_Text _timerText;

        [Tooltip("Gold maliyetinin yazıldığı alan.")]
        [SerializeField] private TMP_Text _goldCostText;

        [Tooltip("Reklam SDK'sı eklenene kadar reklam ödülleri doğrudan verilir. SDK gelince kapatılır.")]
        [SerializeField] private bool _grantAdRewardsWithoutAds = true;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _adButton.onClick.AddListener(GrantLifeWithAd);
            _goldButton.onClick.AddListener(RefillLivesWithGold);
        }

        private void Start()
        {
            EconomyManager.Instance.OnLivesChanged += SetLives;
            EconomyManager.Instance.OnLifeTimerTicked += SetTimer;

            if (_goldCostText != null) { _goldCostText.text = _config.LifeRefillCostGold.ToString(); }

            SetLives(EconomyManager.Instance.Lives);
            SetTimer(EconomyManager.Instance.GetSecondsUntilNextLife());
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(Close);
            _adButton.onClick.RemoveListener(GrantLifeWithAd);
            _goldButton.onClick.RemoveListener(RefillLivesWithGold);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLivesChanged -= SetLives;
                EconomyManager.Instance.OnLifeTimerTicked -= SetTimer;
            }
        }

        /// <summary>Popup'ı kapatır.</summary>
        public void Close()
        {
            UIManager.Instance.HideHeartPopup();
        }

        /// <summary>Reklam izleyerek bir can verir ve popup'ı kapatır.</summary>
        public void GrantLifeWithAd()
        {
            if (!_grantAdRewardsWithoutAds) { return; }

            EconomyManager.Instance.AddLives(1);

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayRewardGranted(); }

            Close();
        }

        /// <summary>Gold ödeyerek canları doldurur ve popup'ı kapatır. Gold yetmezse gold popup'ı açılır.</summary>
        public void RefillLivesWithGold()
        {
            if (!EconomyManager.Instance.TrySpendGold(_config.LifeRefillCostGold))
            {
                UIManager.Instance.ShowGoldPopup();
                return;
            }

            EconomyManager.Instance.RefillLives();

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayRewardGranted(); }

            Close();
        }

        private void SetLives(int lives)
        {
            if (_livesText == null) { return; }

            _livesText.text = EconomyManager.Instance.HasInfiniteLives ? "∞" : lives.ToString();
        }

        private void SetTimer(float secondsRemaining)
        {
            if (_timerText == null) { return; }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, secondsRemaining));
            _timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
