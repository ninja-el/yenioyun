using MatchPack.Core;
using MatchPack.Data;
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

        [Tooltip("Başlatılacak bölüm. İlerleme sistemi gelene kadar elle bağlanır.")]
        [SerializeField] private LevelData _level;

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

        /// <summary>Can varsa 1 can tüketip leveli başlatır; can yoksa can popup'ını açar.</summary>
        public void StartLevel()
        {
            if (GameManager.Instance.State != GameState.Menu || SceneLoader.Instance.IsBusy) { return; }

            // Can, level gerçekten başlatılabiliyorsa harcanır; aksi halde tüketilip boşa gidiyordu.
            if (_level == null)
            {
                Debug.LogError("MainMenuScreen has no level assigned.", this);
                return;
            }

            if (!EconomyManager.Instance.TrySpendLife())
            {
                UIManager.Instance.ShowHeartPopup();
                return;
            }

            GameManager.Instance.StartLevel(_level);
        }

        private void HandleGameStateChanged(GameState state)
        {
            _startButton.interactable = state == GameState.Menu;
        }
    }
}
