using MatchPack.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>Ana menüdeki Start butonunu game sahnesinin yüklenmesine bağlar.</summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [Tooltip("Game sahnesinin yüklenmesini başlatan Start butonu.")]
        [SerializeField] private Button _startButton;

        [Tooltip("Level açıldığında kapatılacak menü paneli.")]
        [SerializeField] private GameObject _menuPanel;

        private void Awake()
        {
            _startButton.onClick.AddListener(StartLevel);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(StartLevel);
        }

        private void StartLevel()
        {
            _startButton.interactable = false;
            SceneLoader.Instance.LoadLevel(OnLevelSceneReady);
        }

        private void OnLevelSceneReady(LevelContext context)
        {
            _startButton.interactable = true;

            if (_menuPanel != null)
            {
                _menuPanel.SetActive(false);
            }
        }
    }
}
