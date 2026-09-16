using System;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Oyuncunun kalıcı verisi. JsonUtility tek bir JSON string'e serialize eder; property'ler
    /// serialize edilmediği için değerler [SerializeField] alanlarda tutulur.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Tooltip("Oyuncunun oynayacağı sıradaki bölüm numarası.")]
        [SerializeField] private int _currentLevel = 1;

        [Tooltip("Biriktirilen soft para.")]
        [SerializeField] private int _gold;

        [Tooltip("Eldeki can sayısı. İlk açılışta EconomyManager GameConfig'teki maksimuma çeker.")]
        [SerializeField] private int _currentLives;

        [Tooltip("Son can yenilenmesinin UTC tick değeri.")]
        [SerializeField] private long _lastLifeRegenTime;

        [Tooltip("Sınırsız canın biteceği UTC tick değeri. 0 ise sınırsız can yok.")]
        [SerializeField] private long _infiniteLivesUntilTime;

        [Tooltip("Booster envanteri. Index eşlemesi booster kartında tanımlanacak.")]
        [SerializeField] private int[] _boosterCounts = Array.Empty<int>();

        [Tooltip("Reklam kaldırma satın alındı mı?")]
        [SerializeField] private bool _hasRemovedAds;

        [Tooltip("Satın alınmış kalıcı (non-consumable) ürünlerin id listesi.")]
        [SerializeField] private string[] _ownedProductIds = Array.Empty<string>();

        [Tooltip("Ses efektleri açık mı?")]
        [SerializeField] private bool _isSoundEnabled = true;

        [Tooltip("Müzik açık mı?")]
        [SerializeField] private bool _isMusicEnabled = true;

        [Tooltip("Titreşim (taptic) açık mı?")]
        [SerializeField] private bool _isHapticsEnabled = true;

        public int CurrentLevel { get => _currentLevel; set => _currentLevel = value; }
        public int Gold { get => _gold; set => _gold = value; }
        public int CurrentLives { get => _currentLives; set => _currentLives = value; }

        /// <summary>Son can yenilenmesinin UTC tick değeri. Oyun kapalıyken geçen süre bununla hesaplanır.</summary>
        public long LastLifeRegenTime { get => _lastLifeRegenTime; set => _lastLifeRegenTime = value; }

        /// <summary>Sınırsız canın biteceği UTC tick değeri. 0 ise sınırsız can yok.</summary>
        public long InfiniteLivesUntilTime { get => _infiniteLivesUntilTime; set => _infiniteLivesUntilTime = value; }

        public int[] BoosterCounts { get => _boosterCounts; set => _boosterCounts = value; }
        public bool HasRemovedAds { get => _hasRemovedAds; set => _hasRemovedAds = value; }
        public string[] OwnedProductIds { get => _ownedProductIds; set => _ownedProductIds = value; }
        public bool IsSoundEnabled { get => _isSoundEnabled; set => _isSoundEnabled = value; }
        public bool IsMusicEnabled { get => _isMusicEnabled; set => _isMusicEnabled = value; }
        public bool IsHapticsEnabled { get => _isHapticsEnabled; set => _isHapticsEnabled = value; }
    }
}
