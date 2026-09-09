using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Bir obje tipinin tanımı. Eşleşme referans karşılaştırmasıyla yapılır; Id yalnızca kayıt
    /// ve hata ayıklama içindir.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "MatchPack/Item Type")]
    public class ItemType : ScriptableObject
    {
        [Tooltip("Kayıt ve hata ayıklamada kullanılan sabit kimlik. Asset yeniden adlandırılsa bile değişmez.")]
        [SerializeField] private string _id;

        [Tooltip("Objenin oyuncuya gösterilen adı.")]
        [SerializeField] private string _displayName;

        [Tooltip("Yığında oluşturulacak 3D obje prefab'ı.")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Kutunun üzerinde görünecek ikon.")]
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;
    }
}
