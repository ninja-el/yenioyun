using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Localization;
using MatchPack.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Ana menünün Start butonu. Can varsa 1 can tüketip leveli başlatır, yoksa can popup'ını
    /// açar. Panel görünürlüğü UIManager'a aittir.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Tooltip("Leveli başlatan Start butonu.")]
        [SerializeField] private Button _startButton;

        [Tooltip("Buton üzerindeki bölüm yazısı. Metin ui.menu.level key'inden, numara buradan gelir.")]
        [SerializeField] private LocalizedText _levelLabel;

        private void Awake()
        {
            _startButton.onClick.AddListener(StartLevel);
        }

        private void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            HandleGameStateChanged(GameManager.Instance.State);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(StartLevel);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        /// <summary>Can varsa leveli başlatır; can yoksa can popup'ını açar. Can yalnızca kayıpta düşer.</summary>
        public void StartLevel()
        {
            if (GameManager.Instance.State != GameState.Menu || SceneLoader.Instance.IsBusy) { return; }

            LevelData level = GameManager.Instance.ProgressLevel;

            if (level == null)
            {
                Debug.LogError("LevelCatalog has no level for the saved progress number.", this);
                return;
            }

            if (!EconomyManager.Instance.HasEnoughLives)
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            GameManager.Instance.StartLevel(SaveManager.Instance.Data.CurrentLevel);
        }

        private void HandleGameStateChanged(GameState state)
        {
            _startButton.interactable = state == GameState.Menu;

            if (state == GameState.Menu) { RefreshLevelLabel(); }
        }

        /// <summary>Bölüm yazısını kayıttaki bölüm numarasıyla günceller; döngüde de numara artmaya devam eder.</summary>
        private void RefreshLevelLabel()
        {
            if (_levelLabel == null) { return; }

            _levelLabel.SetFormatArgs(SaveManager.Instance.Data.CurrentLevel);
        }
    }
}
