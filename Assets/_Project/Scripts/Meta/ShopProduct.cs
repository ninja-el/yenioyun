using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>Mağaza ürününün store tarafındaki tipi. Unity IAP ProductType ile birebir eşleşir.</summary>
    public enum ShopProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    /// <summary>
    /// Tek bir mağaza ürününün tanımı. Store'daki ürün kimliği ve satın alma başarılı olduğunda
    /// oyuncuya verilecek içerik burada durur; ödülü uygulayan taraf ShopManager'dır.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopProduct", menuName = "MatchPack/Shop Product")]
    public class ShopProduct : ScriptableObject
    {
        [Header("Store")]
        [Tooltip("App Store ve Google Play'de tanımlı ürün kimliği. İki mağazada da aynı olmalı.")]
        [SerializeField] private string _productId;

        [Tooltip("Ürünün store tipi.")]
        [SerializeField] private ShopProductType _productType = ShopProductType.Consumable;

        [Tooltip("Store'dan fiyat çekilemediğinde gösterilecek yedek fiyat metni.")]
        [SerializeField] private string _fallbackPriceText = "--";

        [Header("İçerik")]
        [Tooltip("Satın alma sonrası eklenecek gold.")]
        [SerializeField, Min(0)] private int _goldReward;

        [Tooltip("Satın alma sonrası eklenecek can.")]
        [SerializeField, Min(0)] private int _livesReward;

        [Tooltip("Satın alma sonrası verilecek sınırsız can süresi (saat). 0 ise verilmez.")]
        [SerializeField, Min(0f)] private float _infiniteLivesHours;

        [Tooltip("Booster envanterine eklenecek adetler. Index, booster listesindeki sırayla eşleşir.")]
        [SerializeField] private int[] _boosterRewards = new int[0];

        [Tooltip("Satın alındığında reklamlar kaldırılsın mı?")]
        [SerializeField] private bool _removesAds;

        public string ProductId => _productId;
        public ShopProductType ProductType => _productType;
        public string FallbackPriceText => _fallbackPriceText;
        public int GoldReward => _goldReward;
        public int LivesReward => _livesReward;
        public float InfiniteLivesHours => _infiniteLivesHours;
        public int[] BoosterRewards => _boosterRewards;
        public bool RemovesAds => _removesAds;
    }
}
