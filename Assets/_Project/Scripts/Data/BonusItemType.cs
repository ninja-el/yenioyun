using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Yığına kutuya gitmeyen, dokununca toplanan özel obje tipi. Toplanınca yalnızca burada
    /// açılan ödülleri verir; hiçbiri açık değilse obje sadece toplanır. Yığında normal obje gibi
    /// doğup fizikle durduğu için <see cref="ItemType"/>'ın prefab alanını kullanır; ikon alanı kullanılmaz.
    /// </summary>
    [CreateAssetMenu(fileName = "Bonus_", menuName = "MatchPack/Bonus Item Type")]
    public class BonusItemType : ItemType
    {
        [Header("Ek süre")]
        [Tooltip("Toplanınca sayaca eklenecek saniye. 0 = süre vermez.")]
        [SerializeField, Min(0f)] private float _bonusSeconds;

        [Header("Booster")]
        [Tooltip("Toplanınca booster verilsin mi?")]
        [SerializeField] private bool _grantsBooster;

        [Tooltip("Verilecek booster.")]
        [SerializeField] private BoosterType _boosterType;

        [Tooltip("Verilecek booster adedi.")]
        [SerializeField, Min(1)] private int _boosterAmount = 1;

        [Tooltip("Açıksa booster'ın biri stoktan düşülmeden hemen kullanılır, kalan adet envantere eklenir. " +
            "Booster o an kullanılamıyorsa (ör. Shuffle sürüyor) o da envantere eklenir. Kapalıysa tamamı envantere eklenir.")]
        [SerializeField] private bool _useBoosterInstantly;

        [Header("Etkinlik")]
        [Tooltip("Toplanınca sayacı artacak etkinliğin id'si. Boş = etkinliğe sayılmaz.")]
        [SerializeField] private string _eventId;

        [Tooltip("Etkinlik sayacına eklenecek adet.")]
        [SerializeField, Min(1)] private int _eventAmount = 1;

        public float BonusSeconds => _bonusSeconds;
        public bool GrantsBooster => _grantsBooster;
        public BoosterType BoosterType => _boosterType;
        public int BoosterAmount => _boosterAmount;
        public bool UseBoosterInstantly => _useBoosterInstantly;
        public string EventId => _eventId;
        public int EventAmount => _eventAmount;
        public bool HasEvent => !string.IsNullOrEmpty(_eventId);
    }
}
