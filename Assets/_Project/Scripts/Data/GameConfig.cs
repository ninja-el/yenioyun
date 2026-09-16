using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Level'den bağımsız, oyunun tamamında geçerli sayısal ayarlar.
    /// Varsayılanların kaynağı: Memory-bank/06-Sabitler-ve-Kararlar.md
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MatchPack/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Gameplay")]
        [Tooltip("Bir kutunun dolması için gereken obje sayısı.")]
        [SerializeField, Min(1)] private int _boxCapacity = 3;

        [Tooltip("Objenin yığından kutuya uçuş süresi (saniye).")]
        [SerializeField, Min(0f)] private float _itemFlyDuration = 0.35f;

        [Header("Bant")]
        [Tooltip("Bandın hızı: saniyede kaç slot ilerlediği.")]
        [SerializeField, Min(0f)] private float _beltSpeed = 0.8f;

        [Tooltip("Bir kutu gönderildikten sonra bir sonrakinin beklediği süre (saniye).")]
        [SerializeField, Min(0f)] private float _boxEntryDelay = 0.5f;

        [Tooltip("Kutunun bant dışından slotuna yerleşme süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _boxEntryDuration = 0.4f;

        [Tooltip("Dolan kutunun banttan ayrılıp kaybolma süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float _boxExitDuration = 0.35f;

        [Header("Hatalı hamle cezası")]
        [Tooltip("Hatalı hamlede süre cezası uygulansın mı? Varsayılan kapalı.")]
        [SerializeField] private bool _isMissPenaltyEnabled;

        [Tooltip("Ceza uygulanmadan önce izin verilen hata sayısı.")]
        [SerializeField, Min(1)] private int _missesBeforePenalty = 3;

        [Tooltip("Ceza tetiklendiğinde süreden düşülecek saniye.")]
        [SerializeField, Min(0f)] private float _missPenaltySeconds = 5f;

        [Header("Ekonomi")]
        [Tooltip("Oyuncunun sahip olabileceği maksimum can.")]
        [SerializeField, Min(1)] private int _maxLives = 5;

        [Tooltip("Bir canın yenilenmesi için geçmesi gereken süre (saniye).")]
        [SerializeField, Min(1f)] private float _lifeRegenSeconds = 900f;

        [Tooltip("Level tamamlandığında verilen gold.")]
        [SerializeField, Min(0)] private int _levelCompleteGold = 50;

        [Tooltip("Kaybedilen levele gold ile devam etmenin maliyeti.")]
        [SerializeField, Min(0)] private int _continueCostGold = 800;

        [Tooltip("Canları gold ile doldurmanın maliyeti.")]
        [SerializeField, Min(0)] private int _lifeRefillCostGold = 2000;

        [Tooltip("Rewarded reklam izlenince ödülün kaçla çarpılacağı.")]
        [SerializeField, Min(1)] private int _rewardedRewardMultiplier = 2;

        [Tooltip("Kaybedilen levele devam edildiğinde sayaca eklenecek süre (saniye).")]
        [SerializeField, Min(1f)] private float _continueExtraSeconds = 15f;

        public int BoxCapacity => _boxCapacity;
        public float ItemFlyDuration => _itemFlyDuration;
        public float BeltSpeed => _beltSpeed;
        public float BoxEntryDelay => _boxEntryDelay;
        public float BoxEntryDuration => _boxEntryDuration;
        public float BoxExitDuration => _boxExitDuration;
        public bool IsMissPenaltyEnabled => _isMissPenaltyEnabled;
        public int MissesBeforePenalty => _missesBeforePenalty;
        public float MissPenaltySeconds => _missPenaltySeconds;
        public int MaxLives => _maxLives;
        public float LifeRegenSeconds => _lifeRegenSeconds;
        public int LevelCompleteGold => _levelCompleteGold;
        public int ContinueCostGold => _continueCostGold;
        public int LifeRefillCostGold => _lifeRefillCostGold;
        public int RewardedRewardMultiplier => _rewardedRewardMultiplier;
        public float ContinueExtraSeconds => _continueExtraSeconds;
    }
}
