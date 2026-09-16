using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Kazanma ve kaybetme panellerinin butonlarını işletir. Panelleri açma işi UIManager'a,
    /// ödül ve bakiye işi EconomyManager'a aittir; buradaki her method yalnızca o iki sınıfı çağırır.
    /// Paneller kapalıyken de bağlı kalması için bileşen UI_Canvas üzerinde durur.
    /// </summary>
    public class LevelResultScreen : MonoBehaviour
    {
        [Tooltip("Ödül ve devam etme maliyetlerinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [Header("Kazanma paneli")]
        [Tooltip("Ödülü alıp menüye dönen buton.")]
        [SerializeField] private Button _winClaimButton;

        [Tooltip("Reklam izleyip ödülü katlayan buton.")]
        [SerializeField] private Button _winDoubleRewardButton;

        [Tooltip("Paneli kapatıp menüye dönen buton.")]
        [SerializeField] private Button _winCloseButton;

        [Tooltip("Kazanılan gold miktarının yazıldığı alan.")]
        [SerializeField] private TMP_Text _winRewardText;

        [Header("Kaybetme paneli")]
        [Tooltip("Gold ödeyip levele devam eden buton.")]
        [SerializeField] private Button _loseContinueWithGoldButton;

        [Tooltip("Reklam izleyip levele devam eden buton.")]
        [SerializeField] private Button _loseContinueWithAdButton;

        [Tooltip("Paneli kapatıp menüye dönen buton.")]
        [SerializeField] private Button _loseCloseButton;

        [Tooltip("Devam etme maliyetinin yazıldığı alan.")]
        [SerializeField] private TMP_Text _loseContinueCostText;

        [Tooltip("Reklam SDK'sı eklenene kadar reklam ödülleri doğrudan verilir. SDK gelince kapatılır.")]
        [SerializeField] private bool _grantAdRewardsWithoutAds = true;

        private void Awake()
        {
            _winClaimButton.onClick.AddListener(ClaimAndReturnToMenu);
            _winDoubleRewardButton.onClick.AddListener(DoubleRewardWithAd);
            _winCloseButton.onClick.AddListener(ClaimAndReturnToMenu);

            _loseContinueWithGoldButton.onClick.AddListener(ContinueWithGold);
            _loseContinueWithAdButton.onClick.AddListener(ContinueWithAd);
            _loseCloseButton.onClick.AddListener(ReturnToMenu);
        }

        private void Start()
        {
            GameManager.Instance.OnLevelCompleted += RefreshWinPanel;
            GameManager.Instance.OnLevelFailed += RefreshLosePanel;

            RefreshWinPanel();
            RefreshLosePanel();
        }

        private void OnDestroy()
        {
            _winClaimButton.onClick.RemoveListener(ClaimAndReturnToMenu);
            _winDoubleRewardButton.onClick.RemoveListener(DoubleRewardWithAd);
            _winCloseButton.onClick.RemoveListener(ClaimAndReturnToMenu);

            _loseContinueWithGoldButton.onClick.RemoveListener(ContinueWithGold);
            _loseContinueWithAdButton.onClick.RemoveListener(ContinueWithAd);
            _loseCloseButton.onClick.RemoveListener(ReturnToMenu);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelCompleted -= RefreshWinPanel;
                GameManager.Instance.OnLevelFailed -= RefreshLosePanel;
            }
        }

        /// <summary>Kazanma panelini kapatıp menüye döner. Level ödülü kazanma anında zaten verilmiştir.</summary>
        public void ClaimAndReturnToMenu()
        {
            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ReturnToMenu();
        }

        /// <summary>Reklam izleyerek level ödülünü katlar ve menüye döner.</summary>
        public void DoubleRewardWithAd()
        {
            if (!_grantAdRewardsWithoutAds) { return; }

            int bonus = _config.LevelCompleteGold * (_config.RewardedRewardMultiplier - 1);
            EconomyManager.Instance.AddGold(bonus);

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayRewardGranted(); }

            ClaimAndReturnToMenu();
        }

        /// <summary>Gold ödeyerek kaybedilen levele devam eder. Gold yetmezse gold popup'ı açılır.</summary>
        public void ContinueWithGold()
        {
            if (!EconomyManager.Instance.TrySpendGold(_config.ContinueCostGold))
            {
                UIManager.Instance.ShowGoldPopup();
                return;
            }

            ResumeLevel();
        }

        /// <summary>Reklam izleyerek kaybedilen levele devam eder.</summary>
        public void ContinueWithAd()
        {
            if (!_grantAdRewardsWithoutAds) { return; }

            ResumeLevel();
        }

        /// <summary>Paneli kapatıp menüye döner.</summary>
        public void ReturnToMenu()
        {
            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ReturnToMenu();
        }

        /// <summary>Bir can tüketip aynı leveli baştan başlatır. Can yoksa can popup'ı açılır.</summary>
        public void Retry()
        {
            if (!EconomyManager.Instance.TrySpendLife())
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.RetryLevel();
        }

        private void ResumeLevel()
        {
            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ResumeLevel();
        }

        private void RefreshWinPanel()
        {
            if (_winRewardText != null) { _winRewardText.text = _config.LevelCompleteGold.ToString(); }
        }

        private void RefreshLosePanel()
        {
            if (_loseContinueCostText != null) { _loseContinueCostText.text = _config.ContinueCostGold.ToString(); }
        }
    }
}
