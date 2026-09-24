using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Tek bir booster'ın tanımı ve sayısal değerleri. Etkiyi uygulayan taraf ilgili
    /// BoosterBehaviour'dır; bu asset yalnızca veri taşır.
    /// Varsayılanların kaynağı: Memory-bank/06-Sabitler-ve-Kararlar.md
    /// </summary>
    [CreateAssetMenu(fileName = "Booster_", menuName = "MatchPack/Booster Data")]
    public class BoosterData : ScriptableObject
    {
        [Header("Tanım")]
        [Tooltip("Bu asset'in tanımladığı booster. Katalogda her tip bir kez bulunur.")]
        [SerializeField] private BoosterType _type;

        [Tooltip("Booster adının localization key'i. Örnek: ui.booster.timebonus.title")]
        [SerializeField] private string _titleKey;

        [Tooltip("Booster açıklamasının localization key'i. Örnek: ui.booster.timebonus.info")]
        [SerializeField] private string _infoKey;

        [Tooltip("Buton ve satın alma panelinde gösterilen ikon.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Booster'ın açıldığı bölüm numarası. Oyuncunun bölümü bunun altındayken buton kilitlidir.")]
        [SerializeField, Min(1)] private int _unlockLevel = 1;

        [Header("Satın alma")]
        [Tooltip("Bir paketin gold fiyatı.")]
        [SerializeField, Min(0)] private int _goldPrice = 40;

        [Tooltip("Bir pakette verilen booster adedi.")]
        [SerializeField, Min(1)] private int _packAmount = 3;

        [Header("Efekt")]
        [Tooltip("Booster kullanılınca ankraj noktasında oynatılacak efekt prefab'ı. Boşsa efekt oynatılmaz.")]
        [SerializeField] private GameObject _effectPrefab;

        [Tooltip("Efektin ekranda kalma süresi (saniye).")]
        [SerializeField, Min(0f)] private float _effectDuration = 1f;

        [Header("Ek Süre")]
        [Tooltip("Sayaca eklenecek süre (saniye).")]
        [SerializeField, Min(0f)] private float _bonusSeconds = 5f;

        [Tooltip("\"+X sn\" yazısının butondan süre yazısına uçma süresi (saniye). Süre, yazı vardığında eklenir.")]
        [SerializeField, Min(0f)] private float _bonusFlyDuration = 0.5f;

        [Header("Shuffle")]
        [Tooltip("Objelerin yeni yerlerine kayma süresi (saniye).")]
        [SerializeField, Min(0f)] private float _shuffleDuration = 0.5f;

        [Header("Auto-Match")]
        [Tooltip("Kutunun eksik objelerini toplayıp kutuyu tamamlayan UFO prefab'ı. AutoMatchUfo bileşeni taşımalı.")]
        [SerializeField] private GameObject _ufoPrefab;

        [Tooltip("İki Auto-Match kullanımı arasında geçmesi gereken en kısa süre (saniye). Aynı anda çok fazla UFO uçmasın diye.")]
        [SerializeField, Min(0f)] private float _autoMatchCooldown = 1f;

        [Header("Joker Box")]
        [Tooltip("Tek kullanımda banta gönderilecek joker kutu adedi.")]
        [SerializeField, Min(1)] private int _jokerBoxCount = 1;

        public BoosterType Type => _type;
        public string TitleKey => _titleKey;
        public string InfoKey => _infoKey;
        public Sprite Icon => _icon;
        public int UnlockLevel => _unlockLevel;
        public int GoldPrice => _goldPrice;
        public int PackAmount => _packAmount;
        public GameObject EffectPrefab => _effectPrefab;
        public float EffectDuration => _effectDuration;
        public float BonusSeconds => _bonusSeconds;
        public float BonusFlyDuration => _bonusFlyDuration;
        public float ShuffleDuration => _shuffleDuration;
        public GameObject UfoPrefab => _ufoPrefab;
        public float AutoMatchCooldown => _autoMatchCooldown;
        public int JokerBoxCount => _jokerBoxCount;
    }
}
