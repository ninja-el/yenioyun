using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>
    /// Mağazadaki tüm ürünlerin sıralı listesi. IAP katmanı store'a bu listeyi bildirir,
    /// UI butonları ürününü buradan bulur.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "MatchPack/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        [Tooltip("Store'a bildirilecek ürünler. Boş satır bırakılmaz.")]
        [SerializeField] private ShopProduct[] _products = new ShopProduct[0];

        public ShopProduct[] Products => _products;

        /// <summary>Verilen kimliğe sahip ürünü döner; bulunamazsa null.</summary>
        public ShopProduct Find(string productId)
        {
            for (int i = 0; i < _products.Length; i++)
            {
                if (_products[i] != null && _products[i].ProductId == productId) { return _products[i]; }
            }

            return null;
        }
    }
}
