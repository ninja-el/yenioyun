using System;
using System.Collections;
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
        [Tooltip("Ödülü alıp sıradaki bölümü başlatan buton.")]
        [SerializeField] private Button _winNextLevelButton;

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

        [Header("Buton gecikmeleri")]
        [Tooltip("Restart butonuna basıldıktan sonra level yeniden kurulmadan önce beklenecek süre (saniye). Efektler bu sırada oynar.")]
        [SerializeField, Min(0f)] private float _retryDelaySeconds = 3f;

        [Tooltip("Sonraki level butonuna basıldıktan sonra beklenecek süre (saniye). Efektler bu sırada oynar.")]
        [SerializeField, Min(0f)] private float _nextLevelDelaySeconds = 3f;

        [Tooltip("Reklamla katlanan ödül verildikten sonra sıradaki level kurulmadan önce beklenecek süre (saniye). Gold animasyonu bu sırada oynar.")]
        [SerializeField, Min(0f)] private float _doubleRewardDelaySeconds = 3f;

        private Coroutine _delayRoutine;

        /// <summary>Bir buton gecikmesi işliyor mu? İşlerken diğer butonlar cevap vermez.</summary>
        public bool IsBusy => _delayRoutine != null;

        private void Awake()
        {
            _winNextLevelButton.onClick.AddListener(NextLevel);
            _winDoubleRewardButton.onClick.AddListener(DoubleRewardWithAd);
            _winCloseButton.onClick.AddListener(ClaimAndReturnToMenu);

            _loseContinueWithGoldButton.onClick.AddListener(ContinueWithGold);
            _loseContinueWithAdButton.onClick.AddListener(ContinueWithAd);
            _loseCloseButton.onClick.AddListener(ReturnToMenu);
        }

        private void Start()
        {
            // Ödül EconomyManager'da rastgele seçildiği için panel OnLevelCompleted'i değil, verilen miktarı dinler.
            EconomyManager.Instance.OnLevelRewardGranted += RefreshWinPanel;
            GameManager.Instance.OnLevelFailed += RefreshLosePanel;

            RefreshWinPanel(EconomyManager.Instance.LastLevelReward);
            RefreshLosePanel();
        }

        private void OnDestroy()
        {
            _winNextLevelButton.onClick.RemoveListener(NextLevel);
            _winDoubleRewardButton.onClick.RemoveListener(DoubleRewardWithAd);
            _winCloseButton.onClick.RemoveListener(ClaimAndReturnToMenu);

            _loseContinueWithGoldButton.onClick.RemoveListener(ContinueWithGold);
            _loseContinueWithAdButton.onClick.RemoveListener(ContinueWithAd);
            _loseCloseButton.onClick.RemoveListener(ReturnToMenu);

            if (EconomyManager.Instance != null) { EconomyManager.Instance.OnLevelRewardGranted -= RefreshWinPanel; }
            if (GameManager.Instance != null) { GameManager.Instance.OnLevelFailed -= RefreshLosePanel; }
        }

        /// <summary>Kazanma panelini kapatıp menüye döner. Level ödülü kazanma anında zaten verilmiştir.</summary>
        public void ClaimAndReturnToMenu()
        {
            if (IsBusy) { return; }

            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ReturnToMenu();
        }

        /// <summary>
        /// Reklam izleyerek level ödülünü katlar. Ödül yazısı katlanmış değere güncellenir, ayarlanan
        /// gecikme kadar beklenir (gold animasyonu bu sırada oynar) ve sıradaki level kurulur.
        /// </summary>
        public void DoubleRewardWithAd()
        {
            if (IsBusy || !IsAdsAvailable()) { return; }

            AdsManager.Instance.ShowRewardedAd(GrantDoubleReward);
        }

        private void GrantDoubleReward()
        {
            int baseReward = EconomyManager.Instance.LastLevelReward;
            EconomyManager.Instance.AddGold(baseReward * (_config.RewardedRewardMultiplier - 1));
            RefreshWinPanel(baseReward * _config.RewardedRewardMultiplier);

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayRewardGranted(); }

            // Reklam açıkken başka bir butonun gecikmesi başladıysa ödül verilir ama ikinci geçiş başlatılmaz.
            if (IsBusy) { return; }

            if (!EconomyManager.Instance.HasEnoughLives)
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            _delayRoutine = StartCoroutine(DelayedActionRoutine(_doubleRewardDelaySeconds, GoToNextLevel));
        }

        /// <summary>Gold ödeyerek kaybedilen levele devam eder. Gold yetmezse gold popup'ı açılır.</summary>
        public void ContinueWithGold()
        {
            if (IsBusy) { return; }

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
            if (IsBusy || !IsAdsAvailable()) { return; }

            AdsManager.Instance.ShowRewardedAd(ResumeLevel);
        }

        /// <summary>Paneli kapatıp menüye döner.</summary>
        public void ReturnToMenu()
        {
            if (IsBusy) { return; }

            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ReturnToMenu();
        }

        /// <summary>
        /// Kaybetme panelindeki restart butonuna bağlanır. Kayıbın canı zaten düşüldüğü için can
        /// harcamaz; ayarlanan gecikme kadar bekler (efektler bu sırada oynar) ve aynı leveli baştan
        /// kurar. Can kalmadıysa can popup'ı açılır ve beklenmez.
        /// </summary>
        public void Retry()
        {
            if (IsBusy) { return; }

            if (!EconomyManager.Instance.HasEnoughLives)
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            _delayRoutine = StartCoroutine(DelayedActionRoutine(_retryDelaySeconds, RestartLevel));
        }

        /// <summary>
        /// Kazanma panelindeki sonraki level butonuna bağlanır. Can harcamaz; ayarlanan gecikme
        /// kadar bekler (efektler bu sırada oynar) ve sıradaki leveli kurar. Can kalmadıysa can
        /// popup'ı açılır ve beklenmez.
        /// </summary>
        public void NextLevel()
        {
            if (IsBusy) { return; }

            if (!EconomyManager.Instance.HasEnoughLives)
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            _delayRoutine = StartCoroutine(DelayedActionRoutine(_nextLevelDelaySeconds, GoToNextLevel));
        }

        private IEnumerator DelayedActionRoutine(float delaySeconds, Action action)
        {
            // Bekleme boyunca panel açık kalır ve hiçbir şey yapılmaz; efektler bu aralıkta oynatılır.
            if (delaySeconds > 0f) { yield return new WaitForSecondsRealtime(delaySeconds); }

            _delayRoutine = null;

            UIManager.Instance.HideLevelResultPanels();
            action();
        }

        private static void RestartLevel()
        {
            GameManager.Instance.RetryLevel();
        }

        private static void GoToNextLevel()
        {
            GameManager.Instance.StartNextLevel();
        }
        private void ResumeLevel()
        {
            UIManager.Instance.HideLevelResultPanels();
            GameManager.Instance.ResumeLevel();
        }

        private static bool IsAdsAvailable()
        {
            if (AdsManager.Instance != null) { return true; }

            Debug.LogWarning("AdsManager is not available, rewarded ad skipped.");
            return false;
        }

        private void RefreshWinPanel(int reward)
        {
            if (_winRewardText != null) { _winRewardText.text = reward.ToString(); }
        }

        private void RefreshLosePanel()
        {
            if (_loseContinueCostText != null) { _loseContinueCostText.text = _config.ContinueCostGold.ToString(); }
        }
    }
}
