using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>Ana menü paneli. Start butonunu GameManager'a bağlar, panel görünürlüğünü state'e göre ayarlar.</summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Tooltip("Leveli başlatan Start butonu.")]
        [SerializeField] private Button _startButton;

        [Tooltip("Level açıldığında kapatılacak menü paneli.")]
        [SerializeField] private GameObject _menuPanel;

        [Tooltip("Başlatılacak bölüm. İlerleme sistemi gelene kadar elle bağlanır.")]
        [SerializeField] private LevelData _level;

        private void Awake()
        {
            _startButton.onClick.AddListener(StartLevel);
        }

        private void Start()
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(StartLevel);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void StartLevel()
        {
            GameManager.Instance.StartLevel(_level);
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool isInMenu = state == GameState.Menu;

            _menuPanel.SetActive(isInMenu);
            _startButton.interactable = isInMenu;
        }
    }
}
