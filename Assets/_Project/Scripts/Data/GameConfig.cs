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

        [Tooltip("Kutunun makinenin içindeki (EntryPoint üzerindeki) en küçük ölçeği. 1 = küçülme yok.")]
        [SerializeField, Range(0.1f, 1f)] private float _boxMachineMinScale = 0.5f;

        [Tooltip("Makineden çıkan kutunun EntryPoint'ten kaç slot ileride tam boyuta ulaşacağı.")]
        [SerializeField, Min(0.01f)] private float _boxMachineGrowSlots = 0.75f;

        [Tooltip("Makineye giren kutunun EntryPoint'e kaç slot kala küçülmeye başlayacağı.")]
        [SerializeField, Min(0.01f)] private float _boxMachineShrinkSlots = 0.5f;

        [Tooltip("Ölçek geçişinin hızlı kısmının mesafedeki payı: makineden en uzak uçtaki dilim. 0.25 = küçülmenin " +
            "ilk %25'i, büyümenin son %25'i.")]
        [SerializeField, Range(0.01f, 0.99f)] private float _boxMachineFastDistanceShare = 0.25f;

        [Tooltip("Ölçek değişiminin ne kadarının hızlı dilimde gerçekleşeceği. 0.75 = değişimin %75'i. Mesafe payıyla " +
            "aynı değer verilirse geçiş doğrusal olur.")]
        [SerializeField, Range(0.01f, 0.99f)] private float _boxMachineFastScaleShare = 0.75f;

        [Tooltip("Bir önceki kutuyla aynı tipte kutunun (art arda 2.) seçilme ağırlığının çarpanı. 1 = normal, " +
            "0.5 = yarı yarıya daha düşük ihtimal, 0 = başka tip kaldıkça hiç gelmez.")]
        [SerializeField, Range(0f, 1f)] private float _secondInRowBoxWeight = 0.5f;

        [Tooltip("Son iki kutuyla aynı tipte kutunun (art arda 3. ve sonrası) seçilme ağırlığının çarpanı. " +
            "0 = kuyrukta başka tip kaldıkça hiç gelmez; yalnızca başka tip kalmadığında gelir.")]
        [SerializeField, Range(0f, 1f)] private float _thirdInRowBoxWeight = 0f;

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

        [Tooltip("Level tamamlandığında verilen gold aralığının alt sınırı (dahil).")]
        [SerializeField, Min(0)] private int _levelCompleteGoldMin = 40;

        [Tooltip("Level tamamlandığında verilen gold aralığının üst sınırı (dahil).")]
        [SerializeField, Min(0)] private int _levelCompleteGoldMax = 60;

        [Tooltip("Kaybedilen levele gold ile devam etmenin maliyeti.")]
        [SerializeField, Min(0)] private int _continueCostGold = 800;

        [Tooltip("Canları gold ile doldurmanın maliyeti.")]
        [SerializeField, Min(0)] private int _lifeRefillCostGold = 2000;

        [Tooltip("Rewarded reklam izlenince ödülün kaçla çarpılacağı.")]
        [SerializeField, Min(1)] private int _rewardedRewardMultiplier = 2;

        [Tooltip("Kaybedilen levele devam edildiğinde sayaca eklenecek süre (saniye).")]
        [SerializeField, Min(1f)] private float _continueExtraSeconds = 30f;

        [Tooltip("Bölüm sonunda (kazanma veya kaybetme paneli açılırken) interstitial reklam gösterilme olasılığı (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _interstitialChance = 0.3f;

        [Tooltip("Art arda en fazla kaç bölüm sonunda interstitial gösterilebilir. Bu sayıya ulaşılınca sonraki bölüm sonunda reklam çıkmaz.")]
        [SerializeField, Min(1)] private int _maxConsecutiveInterstitials = 2;

        public int BoxCapacity => _boxCapacity;
        public float ItemFlyDuration => _itemFlyDuration;
        public float BeltSpeed => _beltSpeed;
        public float BoxEntryDelay => _boxEntryDelay;
        public float BoxEntryDuration => _boxEntryDuration;
        public float BoxExitDuration => _boxExitDuration;
        public float BoxMachineMinScale => _boxMachineMinScale;
        public float BoxMachineGrowSlots => _boxMachineGrowSlots;
        public float BoxMachineShrinkSlots => _boxMachineShrinkSlots;
        public float BoxMachineFastDistanceShare => _boxMachineFastDistanceShare;
        public float BoxMachineFastScaleShare => _boxMachineFastScaleShare;
        public float SecondInRowBoxWeight => _secondInRowBoxWeight;
        public float ThirdInRowBoxWeight => _thirdInRowBoxWeight;
        public bool IsMissPenaltyEnabled => _isMissPenaltyEnabled;
        public int MissesBeforePenalty => _missesBeforePenalty;
        public float MissPenaltySeconds => _missPenaltySeconds;
        public int MaxLives => _maxLives;
        public float LifeRegenSeconds => _lifeRegenSeconds;
        public int LevelCompleteGoldMin => _levelCompleteGoldMin;
        public int LevelCompleteGoldMax => _levelCompleteGoldMax;
        public int ContinueCostGold => _continueCostGold;
        public int LifeRefillCostGold => _lifeRefillCostGold;
        public int RewardedRewardMultiplier => _rewardedRewardMultiplier;
        public float ContinueExtraSeconds => _continueExtraSeconds;
        public float InterstitialChance => _interstitialChance;
        public int MaxConsecutiveInterstitials => _maxConsecutiveInterstitials;

        /// <summary>Level tamamlama gold aralığından (iki uç dahil) rastgele bir değer seçer.</summary>
        public int RollLevelCompleteGold()
        {
            return Random.Range(_levelCompleteGoldMin, _levelCompleteGoldMax + 1);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_levelCompleteGoldMax < _levelCompleteGoldMin) { _levelCompleteGoldMax = _levelCompleteGoldMin; }
        }
#endif
    }
}
