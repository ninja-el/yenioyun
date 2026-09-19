using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>Auto-Match booster'ının çalışma biçimi. Aynı anda yalnızca biri geçerlidir.</summary>
    public enum AutoMatchMode
    {
        Items,
        Boxes
    }

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

        [Tooltip("Oyuncuya gösterilen ad.")]
        [SerializeField] private string _displayName;

        [Tooltip("Buton ikonu.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Booster'ın açıldığı bölüm numarası. Oyuncunun bölümü bunun altındayken buton kilitlidir.")]
        [SerializeField, Min(1)] private int _unlockLevel = 1;

        [Header("Efekt")]
        [Tooltip("Booster kullanılınca ankraj noktasında oynatılacak efekt prefab'ı. Boşsa efekt oynatılmaz.")]
        [SerializeField] private GameObject _effectPrefab;

        [Tooltip("Efektin ekranda kalma süresi (saniye). Time Freeze'de bunun yerine donma süresi kullanılır.")]
        [SerializeField, Min(0f)] private float _effectDuration = 1f;

        [Header("Time Freeze")]
        [Tooltip("Sayacın duracağı süre (saniye).")]
        [SerializeField, Min(0f)] private float _freezeDuration = 5f;

        [Tooltip("Donma süresince bant da dursun mu?")]
        [SerializeField] private bool _isBeltFrozen = true;

        [Header("Auto-Match")]
        [Tooltip("Items: belirli sayıda obje kutulara gönderilir. Boxes: belirli sayıda kutu tamamen doldurulur.")]
        [SerializeField] private AutoMatchMode _autoMatchMode = AutoMatchMode.Items;

        [Tooltip("Seçilen moda göre gönderilecek obje veya doldurulacak kutu adedi.")]
        [SerializeField, Min(1)] private int _autoMatchCount = 1;

        [Tooltip("Art arda gönderilen objeler arasındaki bekleme (saniye). Hepsi aynı karede uçmasın diye.")]
        [SerializeField, Min(0f)] private float _autoMatchInterval = 0.12f;

        [Header("Joker Box")]
        [Tooltip("Banta gönderilecek joker kutu prefab'ı. Kutunun alacağı obje adedi prefab'taki yuva sayısıdır.")]
        [SerializeField] private GameObject _jokerBoxPrefab;

        [Tooltip("Tek kullanımda banta gönderilecek joker kutu adedi.")]
        [SerializeField, Min(1)] private int _jokerBoxCount = 1;

        public BoosterType Type => _type;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public int UnlockLevel => _unlockLevel;
        public GameObject EffectPrefab => _effectPrefab;
        public float EffectDuration => _effectDuration;
        public float FreezeDuration => _freezeDuration;
        public bool IsBeltFrozen => _isBeltFrozen;
        public AutoMatchMode AutoMatchMode => _autoMatchMode;
        public int AutoMatchCount => _autoMatchCount;
        public float AutoMatchInterval => _autoMatchInterval;
        public GameObject JokerBoxPrefab => _jokerBoxPrefab;
        public int JokerBoxCount => _jokerBoxCount;
    }
}
