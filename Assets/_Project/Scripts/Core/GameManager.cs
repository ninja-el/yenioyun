using System;
using System.Collections;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Oyun durumunun tek sahibi. Level kurma isteğini alır, yükleme ekranını açıp hazırlığı
    /// bekletir, kazanma ve kaybetme kararlarını yayınlar. Levellar arası geçişte sahne
    /// değiştirilmez; aynı GameScene yeni LevelData ile yeniden kurulur.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<LevelData> OnLevelStarted;
        public event Action OnLevelCompleted;
        public event Action OnLevelFailed;

        /// <summary>Kaybedilen level devam ettirildiğinde yayınlanır. Sayacı LevelManager yeniden başlatır.</summary>
        public event Action OnLevelResumed;

        [Tooltip("Bölüm sırasının okunduğu katalog.")]
        [SerializeField] private LevelCatalog _catalog;

        private Coroutine _buildRoutine;

        public GameState State { get; private set; } = GameState.Menu;
        public LevelData CurrentLevel { get; private set; }

        /// <summary>Oyuncunun kayıtlı ilerlemesine karşılık gelen bölüm; katalog boşsa null.</summary>
        public LevelData ProgressLevel => _catalog != null
            ? _catalog.GetByNumber(SaveManager.Instance.Data.CurrentLevel)
            : null;

        /// <summary>Aktif bölümden sonra oynanacak bölüm var mı?</summary>
        public bool HasNextLevel => _catalog != null && _catalog.TryGetNext(CurrentLevel, out _);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_catalog == null)
            {
                Debug.LogError("GameManager has no LevelCatalog assigned; no level can be started.", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        /// <summary>Verilen bölümü kurar. Yükleme ekranı açılır, hazırlık bitince durum Playing olur.</summary>
        public void StartLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("GameManager.StartLevel was called with a null level.", this);
                return;
            }

            // Açılıştaki GameScene yüklemesi de meşgul sayılır; o bitmeden level kurulamaz.
            if (_buildRoutine != null || SceneLoader.Instance.IsBusy) { return; }

            _buildRoutine = StartCoroutine(BuildLevelRoutine(level));
        }

        /// <summary>Aktif bölümü baştan kurar. Sahne değişmez, içerik yeniden üretilir.</summary>
        public void RetryLevel()
        {
            if (CurrentLevel == null)
            {
                Debug.LogError("GameManager.RetryLevel was called with no active level.", this);
                return;
            }

            StartLevel(CurrentLevel);
        }

        /// <summary>Sıradaki bölümü kurar. Katalogda sıradaki bölüm yoksa menüye döner.</summary>
        public void StartNextLevel()
        {
            if (_catalog == null || !_catalog.TryGetNext(CurrentLevel, out LevelData next))
            {
                ReturnToMenu();
                return;
            }

            StartLevel(next);
        }

        /// <summary>Hedef kutu sayısına ulaşıldığında çağrılır.</summary>
        public void CompleteLevel()
        {
            if (State != GameState.Playing) { return; }

            SetState(GameState.Win);
            AdvanceProgress();
            OnLevelCompleted?.Invoke();
        }

        /// <summary>Süre dolduğunda çağrılır.</summary>
        public void FailLevel()
        {
            if (State != GameState.Playing) { return; }

            SetState(GameState.Lose);
            OnLevelFailed?.Invoke();
        }

        /// <summary>
        /// Kaybedilmiş leveli yerinde devam ettirir. Bedelin ödendiğini çağıran taraf doğrular;
        /// GameManager yalnızca durumu geri alır.
        /// </summary>
        public void ResumeLevel()
        {
            if (State != GameState.Lose) { return; }

            SetState(GameState.Playing);
            OnLevelResumed?.Invoke();
        }

        /// <summary>Aktif leveli söküp menüye döner. Game sahnesi yüklü kalır.</summary>
        public void ReturnToMenu()
        {
            if (_buildRoutine != null) { return; }

            SceneLoader.Instance.TeardownLevel();
            CurrentLevel = null;
            SetState(GameState.Menu);
        }

        private IEnumerator BuildLevelRoutine(LevelData level)
        {
            SetState(GameState.Loading);
            UIManager.Instance.ShowLoadingScreen();

            CurrentLevel = level;

            yield return SceneLoader.Instance.BuildLevelRoutine();

            // Hazırlık sahte süreden uzun sürerse ekran zaten açık kaldı; kısa sürdüyse burada beklenir.
            yield return UIManager.Instance.WaitForLoadingScreenRoutine();

            UIManager.Instance.HideLoadingScreen();

            _buildRoutine = null;
            SetState(GameState.Playing);
            OnLevelStarted?.Invoke(CurrentLevel);
        }

        private void AdvanceProgress()
        {
            if (_catalog == null) { return; }

            int completedNumber = _catalog.GetNumber(CurrentLevel);
            if (completedNumber < 1) { return; }

            PlayerData data = SaveManager.Instance.Data;
            if (data.CurrentLevel > completedNumber) { return; }

            data.CurrentLevel = Mathf.Min(completedNumber + 1, _catalog.Count);
            SaveManager.Instance.Save();
        }

        private void SetState(GameState state)
        {
            if (State == state) { return; }

            State = state;
            OnGameStateChanged?.Invoke(state);
        }
    }
}
