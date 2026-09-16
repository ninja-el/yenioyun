using System.Collections;
using MatchPack.UI;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Panellerin tek aç/kapa noktası. Oyun durumunu dinler ve doğru paneli gösterir; kural
    /// işletmez, ödül vermez, para harcamaz. Panellerin kendi butonları ilgili manager'ı çağırır.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Menü")]
        [Tooltip("Ana menü kökü. Level oynanırken kapanır.")]
        [SerializeField] private GameObject _mainMenuRoot;

        [Tooltip("Ayarlar paneli.")]
        [SerializeField] private GameObject _settingsPanel;

        [Tooltip("Market (mağaza) paneli.")]
        [SerializeField] private GameObject _marketPanel;

        [Tooltip("Gold satın alma popup'ı.")]
        [SerializeField] private GameObject _goldPopup;

        [Tooltip("Can satın alma popup'ı.")]
        [SerializeField] private GameObject _heartPopup;

        [Header("Yükleme")]
        [Tooltip("Level hazırlanırken açılan yükleme ekranı.")]
        [SerializeField] private LoadingScreen _loadingScreen;

        [Header("Oyun içi")]
        [Tooltip("Oyun içi panellerin kökü.")]
        [SerializeField] private GameObject _inGameRoot;

        [Tooltip("Level kazanma paneli.")]
        [SerializeField] private GameObject _winPanel;

        [Tooltip("Level kaybetme paneli.")]
        [SerializeField] private GameObject _losePanel;

        [Tooltip("Booster satın alma paneli.")]
        [SerializeField] private GameObject _boosterPanel;

        /// <summary>Ekranda oyunun üstünü kapatan bir panel açık mı?</summary>
        public bool IsAnyPopupOpen =>
            IsOpen(_settingsPanel) || IsOpen(_marketPanel) || IsOpen(_goldPopup) ||
            IsOpen(_heartPopup) || IsOpen(_winPanel) || IsOpen(_losePanel) || IsOpen(_boosterPanel);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnLevelCompleted += ShowWinPanel;
            GameManager.Instance.OnLevelFailed += ShowLosePanel;

            CloseAllPopups();
            HandleGameStateChanged(GameManager.Instance.State);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnLevelCompleted -= ShowWinPanel;
                GameManager.Instance.OnLevelFailed -= ShowLosePanel;
            }

            if (Instance == this) { Instance = null; }
        }

        /// <summary>Yükleme ekranını açar ve sahte bekleme süresini başlatır.</summary>
        public void ShowLoadingScreen()
        {
            if (_loadingScreen == null) { return; }

            _loadingScreen.Show();
        }

        /// <summary>Yükleme ekranını kapatır.</summary>
        public void HideLoadingScreen()
        {
            if (_loadingScreen == null) { return; }

            _loadingScreen.Hide();
        }

        /// <summary>
        /// Yükleme ekranının sahte süresi dolana kadar bekler. Ekran bağlanmamışsa hemen döner.
        /// </summary>
        public IEnumerator WaitForLoadingScreenRoutine()
        {
            if (_loadingScreen == null) { yield break; }

            while (!_loadingScreen.IsFakeDelayComplete) { yield return null; }
        }

        /// <summary>Ayarlar panelini açar.</summary>
        public void ShowSettings() { SetOpen(_settingsPanel, true); }

        /// <summary>Ayarlar panelini kapatır.</summary>
        public void HideSettings() { SetOpen(_settingsPanel, false); }

        /// <summary>Ayarlar panelinin durumunu tersine çevirir.</summary>
        public void ToggleSettings() { SetOpen(_settingsPanel, !IsOpen(_settingsPanel)); }

        /// <summary>Market panelini açar.</summary>
        public void ShowMarket() { SetOpen(_marketPanel, true); }

        /// <summary>Market panelini kapatır.</summary>
        public void HideMarket() { SetOpen(_marketPanel, false); }

        /// <summary>Gold satın alma popup'ını açar.</summary>
        public void ShowGoldPopup() { SetOpen(_goldPopup, true); }

        /// <summary>Gold satın alma popup'ını kapatır.</summary>
        public void HideGoldPopup() { SetOpen(_goldPopup, false); }

        /// <summary>Can satın alma popup'ını açar.</summary>
        public void ShowHeartPopup() { SetOpen(_heartPopup, true); }

        /// <summary>Can satın alma popup'ını kapatır.</summary>
        public void HideHeartPopup() { SetOpen(_heartPopup, false); }

        /// <summary>Booster satın alma panelini açar.</summary>
        public void ShowBoosterPanel() { SetOpen(_boosterPanel, true); }

        /// <summary>Booster satın alma panelini kapatır.</summary>
        public void HideBoosterPanel() { SetOpen(_boosterPanel, false); }

        /// <summary>Kazanma panelini açar. GameManager.OnLevelCompleted bunu kendiliğinden çağırır.</summary>
        public void ShowWinPanel()
        {
            SetOpen(_losePanel, false);
            SetOpen(_winPanel, true);
        }

        /// <summary>Kaybetme panelini açar. GameManager.OnLevelFailed bunu kendiliğinden çağırır.</summary>
        public void ShowLosePanel()
        {
            SetOpen(_winPanel, false);
            SetOpen(_losePanel, true);
        }

        /// <summary>Level sonu panellerini kapatır.</summary>
        public void HideLevelResultPanels()
        {
            SetOpen(_winPanel, false);
            SetOpen(_losePanel, false);
        }

        /// <summary>Açık olan tüm panel ve popup'ları kapatır.</summary>
        public void CloseAllPopups()
        {
            SetOpen(_settingsPanel, false);
            SetOpen(_marketPanel, false);
            SetOpen(_goldPopup, false);
            SetOpen(_heartPopup, false);
            SetOpen(_boosterPanel, false);
            HideLevelResultPanels();
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool isInMenu = state == GameState.Menu;

            SetOpen(_mainMenuRoot, isInMenu);
            SetOpen(_inGameRoot, !isInMenu);

            if (state == GameState.Playing || isInMenu) { HideLevelResultPanels(); }
            if (isInMenu) { SetOpen(_boosterPanel, false); }
        }

        private static void SetOpen(GameObject panel, bool isOpen)
        {
            if (panel == null || panel.activeSelf == isOpen) { return; }

            panel.SetActive(isOpen);
        }

        private static bool IsOpen(GameObject panel)
        {
            return panel != null && panel.activeSelf;
        }
    }
}
