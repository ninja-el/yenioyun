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

        public int BoxCapacity => _boxCapacity;
        public float ItemFlyDuration => _itemFlyDuration;
        public bool IsMissPenaltyEnabled => _isMissPenaltyEnabled;
        public int MissesBeforePenalty => _missesBeforePenalty;
        public float MissPenaltySeconds => _missPenaltySeconds;
        public int MaxLives => _maxLives;
        public float LifeRegenSeconds => _lifeRegenSeconds;
        public int LevelCompleteGold => _levelCompleteGold;
    }
}
