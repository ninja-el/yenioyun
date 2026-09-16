using System;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>
    /// Gold ve canın tek sahibi. Değerleri PlayerData üzerinde tutar, her değişiklikte
    /// SaveManager'a yazdırır ve event yayınlar. UI hiçbir değeri Update içinde yoklamaz.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        /// <summary>Gold değiştiğinde yayınlanır. Değer yeni gold miktarıdır.</summary>
        public event Action<int> OnGoldChanged;

        /// <summary>Can değiştiğinde yayınlanır. Değer yeni can sayısıdır.</summary>
        public event Action<int> OnLivesChanged;

        /// <summary>Bir sonraki cana kalan süre her saniye yayınlanır. Canlar doluysa 0 gelir.</summary>
        public event Action<float> OnLifeTimerTicked;

        [Tooltip("Can ve gold değerlerinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        private float _tickAccumulator;
        private float _lastBroadcastRemaining = -1f;
        private bool _wasInfiniteLastTick;

        public int Gold => SaveManager.Instance.Data.Gold;
        public int MaxLives => _config.MaxLives;

        /// <summary>Sınırsız can aktifken MaxLives döner; can tüketimi de yapılmaz.</summary>
        public int Lives => HasInfiniteLives ? _config.MaxLives : SaveManager.Instance.Data.CurrentLives;

        /// <summary>Sınırsız can süresi devam ediyor mu?</summary>
        public bool HasInfiniteLives => SaveManager.Instance.Data.InfiniteLivesUntilTime > DateTime.UtcNow.Ticks;

        /// <summary>Level başlatmaya yetecek can var mı?</summary>
        public bool HasEnoughLives => HasInfiniteLives || SaveManager.Instance.Data.CurrentLives > 0;

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

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
            }

            if (Instance == this) { Instance = null; }
        }

        private void Start()
        {
            InitializeLives();
            GameManager.Instance.OnLevelCompleted += HandleLevelCompleted;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused) { RegenerateOfflineLives(); }
        }

        private void Update()
        {
            _tickAccumulator += Time.unscaledDeltaTime;
            if (_tickAccumulator < 1f) { return; }

            _tickAccumulator = 0f;
            RefreshInfiniteLivesState();
            RegenerateOfflineLives();
            BroadcastLifeTimer();
        }

        /// <summary>Gold ekler ve kaydeder. Negatif değer kabul edilmez.</summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) { return; }

            PlayerData data = SaveManager.Instance.Data;
            data.Gold += amount;
            SaveManager.Instance.Save();
            OnGoldChanged?.Invoke(data.Gold);
        }

        /// <summary>Yeterli gold varsa harcar ve true döner.</summary>
        public bool TrySpendGold(int amount)
        {
            PlayerData data = SaveManager.Instance.Data;
            if (amount < 0 || data.Gold < amount) { return false; }

            data.Gold -= amount;
            SaveManager.Instance.Save();
            OnGoldChanged?.Invoke(data.Gold);
            return true;
        }

        /// <summary>Can ekler; MaxLives değerini geçmez.</summary>
        public void AddLives(int amount)
        {
            if (amount <= 0) { return; }

            PlayerData data = SaveManager.Instance.Data;
            data.CurrentLives = Mathf.Min(_config.MaxLives, data.CurrentLives + amount);
            SaveManager.Instance.Save();
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
        }

        /// <summary>Canları maksimuma çeker ve yenilenme sayacını sıfırlar.</summary>
        public void RefillLives()
        {
            PlayerData data = SaveManager.Instance.Data;
            data.CurrentLives = _config.MaxLives;
            data.LastLifeRegenTime = DateTime.UtcNow.Ticks;
            SaveManager.Instance.Save();
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
        }

        /// <summary>Level girişinde 1 can düşer. Can yoksa false döner ve level başlatılmaz.</summary>
        public bool TrySpendLife()
        {
            if (HasInfiniteLives) { return true; }

            PlayerData data = SaveManager.Instance.Data;
            if (data.CurrentLives <= 0) { return false; }

            // Canlar doluyken sayaç ilerlemiyor; ilk eksilmede yenilenme buradan başlatılır.
            if (data.CurrentLives == _config.MaxLives) { data.LastLifeRegenTime = DateTime.UtcNow.Ticks; }

            data.CurrentLives--;
            SaveManager.Instance.Save();
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
            return true;
        }

        /// <summary>Booster envanterine ekleme yapar. Dizi kısa ise gereken boyuta büyütülür.</summary>
        public void AddBooster(int index, int amount)
        {
            if (index < 0 || amount <= 0) { return; }

            PlayerData data = SaveManager.Instance.Data;

            if (data.BoosterCounts.Length <= index)
            {
                int[] resized = new int[index + 1];
                Array.Copy(data.BoosterCounts, resized, data.BoosterCounts.Length);
                data.BoosterCounts = resized;
            }

            data.BoosterCounts[index] += amount;
            SaveManager.Instance.Save();
        }

        /// <summary>Verilen saat kadar sınırsız can verir. Süre birikir, üzerine yazılmaz.</summary>
        public void GrantInfiniteLives(float hours)
        {
            if (hours <= 0f) { return; }

            PlayerData data = SaveManager.Instance.Data;
            long now = DateTime.UtcNow.Ticks;
            long start = data.InfiniteLivesUntilTime > now ? data.InfiniteLivesUntilTime : now;

            data.InfiniteLivesUntilTime = start + TimeSpan.FromHours(hours).Ticks;
            SaveManager.Instance.Save();
            _wasInfiniteLastTick = true;
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
        }

        /// <summary>Sınırsız canı iptal eder. Yalnızca test ve debug içindir.</summary>
        public void ClearInfiniteLives()
        {
            SaveManager.Instance.Data.InfiniteLivesUntilTime = 0;
            SaveManager.Instance.Save();
            _wasInfiniteLastTick = false;
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
        }

        /// <summary>Bir sonraki cana kalan saniye. Canlar doluysa veya sınırsız can varsa 0 döner.</summary>
        public float GetSecondsUntilNextLife()
        {
            PlayerData data = SaveManager.Instance.Data;
            if (HasInfiniteLives || data.CurrentLives >= _config.MaxLives) { return 0f; }

            double elapsed = (DateTime.UtcNow - new DateTime(data.LastLifeRegenTime, DateTimeKind.Utc)).TotalSeconds;
            if (elapsed < 0d) { return _config.LifeRegenSeconds; }

            return Mathf.Max(0f, _config.LifeRegenSeconds - (float)(elapsed % _config.LifeRegenSeconds));
        }

        private void InitializeLives()
        {
            PlayerData data = SaveManager.Instance.Data;

            if (!SaveManager.Instance.HasSave || data.LastLifeRegenTime == 0)
            {
                data.CurrentLives = _config.MaxLives;
                data.LastLifeRegenTime = DateTime.UtcNow.Ticks;
                SaveManager.Instance.Save();
            }

            _wasInfiniteLastTick = HasInfiniteLives;

            RegenerateOfflineLives();
            OnGoldChanged?.Invoke(data.Gold);
            OnLivesChanged?.Invoke(Lives);
            BroadcastLifeTimer();
        }

        /// <summary>
        /// Sınırsız can süresi dolduğu an kimse bir şey çağırmadığı için UI sonsuz işaretinde
        /// kalıyordu; süre bitişi burada yakalanıp bir kez yayınlanır.
        /// </summary>
        private void RefreshInfiniteLivesState()
        {
            bool isInfinite = HasInfiniteLives;
            if (isInfinite == _wasInfiniteLastTick) { return; }

            // Sınırsız can açıkken normal yenilenme arka planda işlemeye devam ettiği için
            // ankraja dokunulmaz; süre bitince eldeki can zaten güncel olur.
            _wasInfiniteLastTick = isInfinite;
            OnLivesChanged?.Invoke(Lives);
        }

        private void RegenerateOfflineLives()
        {
            PlayerData data = SaveManager.Instance.Data;
            long now = DateTime.UtcNow.Ticks;

            // Cihaz saati geriye alındığında sayaç ileride kalır; can üretmek yerine ankraj tazelenir.
            if (data.LastLifeRegenTime > now)
            {
                data.LastLifeRegenTime = now;
                SaveManager.Instance.Save();
                return;
            }

            if (data.CurrentLives >= _config.MaxLives)
            {
                data.LastLifeRegenTime = now;
                return;
            }

            double elapsedSeconds = (now - data.LastLifeRegenTime) / (double)TimeSpan.TicksPerSecond;
            int earned = (int)(elapsedSeconds / _config.LifeRegenSeconds);
            if (earned <= 0) { return; }

            int missing = _config.MaxLives - data.CurrentLives;
            int granted = Mathf.Min(earned, missing);

            data.CurrentLives += granted;
            data.LastLifeRegenTime = data.CurrentLives >= _config.MaxLives
                ? now
                : data.LastLifeRegenTime + (long)(granted * (double)_config.LifeRegenSeconds * TimeSpan.TicksPerSecond);

            SaveManager.Instance.Save();
            OnLivesChanged?.Invoke(Lives);
        }

        private void BroadcastLifeTimer()
        {
            float remaining = Mathf.Ceil(GetSecondsUntilNextLife());
            if (Mathf.Approximately(remaining, _lastBroadcastRemaining)) { return; }

            _lastBroadcastRemaining = remaining;
            OnLifeTimerTicked?.Invoke(remaining);
        }

        private void HandleLevelCompleted()
        {
            AddGold(_config.LevelCompleteGold);
        }
    }
}
