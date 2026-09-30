using System;
using System.Collections;
using MatchPack.Data;
using MatchPack.Meta;
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

        // Kayıpta gerçekten can düşüldüyse true; devam edilirse can geri verilir. Sınırsız canda düşülmez.
        private bool _isLifeChargedForLoss;

        public GameState State { get; private set; } = GameState.Menu;
        public LevelData CurrentLevel { get; private set; }

        /// <summary>
        /// Aktif bölümün oyuncuya gösterilen numarası. Katalog döngüye girdikten sonra aynı LevelData
        /// farklı numaralarla oynanır; gösterge ve ilerleme bu numarayı kullanır. Bölüm yokken 0.
        /// </summary>
        public int CurrentLevelNumber { get; private set; }

        /// <summary>Oyuncunun kayıtlı ilerlemesine karşılık gelen bölüm; katalog boşsa null.</summary>
        public LevelData ProgressLevel => _catalog != null
            ? _catalog.GetByNumber(SaveManager.Instance.Data.CurrentLevel)
            : null;

        /// <summary>Aktif bölümden sonra oynanacak bölüm var mı? Katalog döngüye girdiği için bölüm varsa hep true.</summary>
        public bool HasNextLevel => _catalog != null && _catalog.Count > 0 && CurrentLevelNumber > 0;

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

        /// <summary>
        /// Gösterilen numaraya karşılık gelen bölümü kurar; katalogdan büyük numaralar döngüdeki bölüme
        /// eşlenir. Yükleme ekranı açılır, hazırlık bitince durum Playing olur.
        /// </summary>
        public void StartLevel(int levelNumber)
        {
            LevelData level = _catalog != null ? _catalog.GetByNumber(levelNumber) : null;

            if (level == null)
            {
                Debug.LogError($"GameManager.StartLevel found no level for number {levelNumber}.", this);
                return;
            }

            // Açılıştaki GameScene yüklemesi de meşgul sayılır; o bitmeden level kurulamaz.
            if (_buildRoutine != null || SceneLoader.Instance.IsBusy) { return; }

            _buildRoutine = StartCoroutine(BuildLevelRoutine(level, levelNumber));
        }

        /// <summary>Aktif bölümü baştan kurar. Sahne değişmez, içerik yeniden üretilir.</summary>
        public void RetryLevel()
        {
            if (CurrentLevel == null)
            {
                Debug.LogError("GameManager.RetryLevel was called with no active level.", this);
                return;
            }

            StartLevel(CurrentLevelNumber);
        }

        /// <summary>Sıradaki bölümü kurar. Sıradaki bölüm yoksa menüye döner.</summary>
        public void StartNextLevel()
        {
            if (!HasNextLevel)
            {
                ReturnToMenu();
                return;
            }

            StartLevel(CurrentLevelNumber + 1);
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
            ChargeLifeForLoss();
            OnLevelFailed?.Invoke();
        }

        /// <summary>
        /// Kaybedilmiş leveli yerinde devam ettirir. Bedelin (gold/reklam) ödendiğini çağıran taraf
        /// doğrular. Level kaybedilmemiş sayıldığı için kayıpta düşülen can geri verilir.
        /// </summary>
        public void ResumeLevel()
        {
            if (State != GameState.Lose) { return; }

            RefundLifeForLoss();
            SetState(GameState.Playing);
            OnLevelResumed?.Invoke();
        }

        /// <summary>
        /// Aktif leveli söküp menüye döner. Game sahnesi yüklü kalır. Oynanırken çıkılırsa level
        /// kaybedilmiş sayılır ve can düşülür; aksi halde oyuncu kaybetmeden çıkıp cezadan kaçabilirdi.
        /// </summary>
        public void ReturnToMenu()
        {
            if (_buildRoutine != null) { return; }

            if (State == GameState.Playing) { ChargeLifeForLoss(); }

            SceneLoader.Instance.TeardownLevel();
            CurrentLevel = null;
            CurrentLevelNumber = 0;
            SetState(GameState.Menu);
        }

        private IEnumerator BuildLevelRoutine(LevelData level, int levelNumber)
        {
            SetState(GameState.Loading);
            UIManager.Instance.ShowLoadingScreen();

            CurrentLevel = level;
            CurrentLevelNumber = levelNumber;

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
            int completedNumber = CurrentLevelNumber;
            if (completedNumber < 1) { return; }

            PlayerData data = SaveManager.Instance.Data;
            if (data.CurrentLevel > completedNumber) { return; }

            // Numara katalogla sınırlanmaz; son bölümden sonra döngüdeki bölümler artan numarayla oynanır.
            data.CurrentLevel = completedNumber + 1;
            SaveManager.Instance.Save();
        }

        private void ChargeLifeForLoss()
        {
            _isLifeChargedForLoss = EconomyManager.Instance != null
                && !EconomyManager.Instance.HasInfiniteLives
                && EconomyManager.Instance.TrySpendLife();
        }

        private void RefundLifeForLoss()
        {
            if (!_isLifeChargedForLoss) { return; }

            _isLifeChargedForLoss = false;
            EconomyManager.Instance.AddLives(1);
        }

        private void SetState(GameState state)
        {
            if (State == state) { return; }

            State = state;
            OnGameStateChanged?.Invoke(state);
        }
    }
}
