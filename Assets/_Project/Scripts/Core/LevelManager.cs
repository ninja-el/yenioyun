using MatchPack.Data;
using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Level sahnesi hazır olduğunda bandı, yığını ve sayacı aktif LevelData ile kurar; sahne
    /// boşaltılmadan önce abonelikleri bırakıp havuzu boşaltır.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Tooltip("Devam etme süresinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [SerializeField] private Conveyor _conveyor;
        [SerializeField] private ItemStack _itemStack;
        [SerializeField] private LevelTimer _timer;

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
            SceneLoader.Instance.OnLevelSceneReady += HandleLevelSceneReady;
            SceneLoader.Instance.OnBeforeLevelUnload += HandleBeforeLevelUnload;
            GameManager.Instance.OnLevelResumed += HandleLevelResumed;
        }

        private void OnDestroy()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnLevelSceneReady -= HandleLevelSceneReady;
                SceneLoader.Instance.OnBeforeLevelUnload -= HandleBeforeLevelUnload;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelResumed -= HandleLevelResumed;
            }

            if (Instance == this) { Instance = null; }
        }

        private void HandleLevelSceneReady(LevelContext context)
        {
            LevelData level = GameManager.Instance.CurrentLevel;

            if (level == null)
            {
                Debug.LogError("LevelManager was asked to build a level but no level data is active.", this);
                return;
            }

            _conveyor.OnAllBoxesCompleted += HandleAllBoxesCompleted;
            _itemStack.OnStackSettled += HandleStackSettled;

            _conveyor.Build(level, context.ConveyorPath);
            _itemStack.Build(level, context.StackRoot);
        }

        private void HandleLevelResumed()
        {
            _timer.Resume(_config.ContinueExtraSeconds);
        }

        private void HandleStackSettled()
        {
            _timer.StartTimer(GameManager.Instance.CurrentLevel.Duration);
        }

        private void HandleAllBoxesCompleted()
        {
            _timer.Stop();
            GameManager.Instance.CompleteLevel();
        }

        private void HandleBeforeLevelUnload()
        {
            _timer.Stop();

            _conveyor.OnAllBoxesCompleted -= HandleAllBoxesCompleted;
            _itemStack.OnStackSettled -= HandleStackSettled;

            _conveyor.Clear();
            _itemStack.Clear();
            PoolManager.Instance.ReleaseAll();
        }
    }
}
