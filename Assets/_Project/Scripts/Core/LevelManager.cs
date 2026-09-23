using MatchPack.Data;
using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Level hazır olduğunda bandı, yığını ve sayacı aktif LevelData ile kurar; level sökülmeden
    /// önce abonelikleri bırakıp havuzu boşaltır. Hedef kutu sayısına ulaşıldığında leveli kazandırır.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Tooltip("Devam etme süresinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [SerializeField] private Conveyor _conveyor;
        [SerializeField] private ItemStack _itemStack;
        [SerializeField] private LevelTimer _timer;

        private int _filledBoxCount;
        private bool _isStackSettled;
        private bool _isLevelStarted;

        /// <summary>Bu levelde şimdiye kadar dolan kutu sayısı.</summary>
        public int FilledBoxCount => _filledBoxCount;

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
            SceneLoader.Instance.OnBeforeLevelTeardown += HandleBeforeLevelTeardown;
            GameManager.Instance.OnLevelResumed += HandleLevelResumed;
            GameManager.Instance.OnLevelStarted += HandleLevelStarted;
        }

        private void OnDestroy()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnLevelSceneReady -= HandleLevelSceneReady;
                SceneLoader.Instance.OnBeforeLevelTeardown -= HandleBeforeLevelTeardown;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelResumed -= HandleLevelResumed;
                GameManager.Instance.OnLevelStarted -= HandleLevelStarted;
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

            _filledBoxCount = 0;
            _isStackSettled = false;
            _isLevelStarted = false;

            _conveyor.OnBoxFilled += HandleBoxFilled;
            _conveyor.OnAllBoxesCompleted += HandleAllBoxesCompleted;
            _itemStack.OnStackSettled += HandleStackSettled;

            _conveyor.Build(level, context.ConveyorPath, _itemStack);
            _itemStack.Build(level, context.StackArea);
        }

        private void HandleLevelResumed()
        {
            _timer.Resume(_config.ContinueExtraSeconds);
        }

        private void HandleStackSettled()
        {
            _isStackSettled = true;
            TryStartTimer();
        }

        private void HandleLevelStarted(LevelData level)
        {
            _isLevelStarted = true;
            TryStartTimer();
        }

        // Yığın yükleme ekranının arkasında oturabiliyor; sayacın orada işlemeye başlamaması için
        // hem oturmanın hem de levelin gerçekten başlamış olması beklenir.
        private void TryStartTimer()
        {
            if (!_isStackSettled || !_isLevelStarted || _timer.IsRunning) { return; }

            _timer.StartTimer(GameManager.Instance.CurrentLevel.Duration);
        }

        private void HandleBoxFilled(Box box)
        {
            _filledBoxCount++;

            LevelData level = GameManager.Instance.CurrentLevel;
            if (level == null || _filledBoxCount < level.TargetBoxCount) { return; }

            CompleteLevel();
        }

        // Hedefe ulaşılmadan bandın kutuları biterse level yine de kapanır; aksi halde oyuncu
        // yapacak hamlesi kalmadan sayacın bitmesini beklerdi.
        private void HandleAllBoxesCompleted()
        {
            CompleteLevel();
        }

        private void CompleteLevel()
        {
            _timer.Stop();
            GameManager.Instance.CompleteLevel();
        }

        private void HandleBeforeLevelTeardown()
        {
            _timer.Stop();

            _conveyor.OnBoxFilled -= HandleBoxFilled;
            _conveyor.OnAllBoxesCompleted -= HandleAllBoxesCompleted;
            _itemStack.OnStackSettled -= HandleStackSettled;

            _conveyor.Clear();
            _itemStack.Clear();
            PoolManager.Instance.ReleaseAll();
        }
    }
}
