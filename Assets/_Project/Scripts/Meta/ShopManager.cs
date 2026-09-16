using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>
    /// Mağazanın oyun tarafındaki tek kapısı. Store katmanını (IPurchaseService) başlatır, satın
    /// alma isteğini ona iletir ve başarıya dönen ürünün içeriğini EconomyManager üzerinden
    /// oyuncuya yazıp kaydeder. UI store SDK'sını hiç görmez, yalnızca bu sınıfı çağırır.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        /// <summary>Ürün içeriği oyuncuya yazıldıktan sonra yayınlanır. UI bunu dinleyip kutlama gösterir.</summary>
        public event Action<ShopProduct> OnProductGranted;

        /// <summary>Satın alma tamamlanamadığında yayınlanır.</summary>
        public event Action<string, PurchaseFailure> OnPurchaseFailed;

        /// <summary>Store hazır olduğunda yayınlanır; fiyat etiketleri bu event'te tazelenir.</summary>
        public event Action OnStoreReady;

        [Tooltip("Store'a bildirilecek ürün kataloğu.")]
        [SerializeField] private ShopCatalog _catalog;

        private IPurchaseService _service;

        public ShopCatalog Catalog => _catalog;
        public bool IsStoreReady => _service != null && _service.IsInitialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Unity IAP eklenene kadar sahte servis kullanılır; gerçek servis yazılınca burası değişir.
            SetService(new StubPurchaseService());
        }

        private void OnDestroy()
        {
            DetachService();

            if (Instance == this) { Instance = null; }
        }

        /// <summary>
        /// Store katmanını değiştirir ve başlatır. Unity IAP servisi yazıldığında yalnızca bu
        /// çağrının parametresi değişir, başka hiçbir sınıf etkilenmez.
        /// </summary>
        public void SetService(IPurchaseService service)
        {
            DetachService();

            _service = service;
            if (_service == null) { return; }

            _service.OnInitialized += HandleInitialized;
            _service.OnPurchaseSucceeded += HandlePurchaseSucceeded;
            _service.OnPurchaseFailed += HandlePurchaseFailed;
            _service.Initialize(_catalog);
        }

        /// <summary>Satın alma akışını başlatır. Sonuç OnProductGranted veya OnPurchaseFailed ile döner.</summary>
        public void Purchase(string productId)
        {
            if (_service == null)
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailure.NotInitialized);
                return;
            }

            _service.Purchase(productId);
        }

        /// <summary>Kalıcı ürünleri geri yükler. Mağaza panelindeki "Restore" butonu bunu çağırır.</summary>
        public void RestorePurchases()
        {
            _service?.RestorePurchases();
        }

        /// <summary>Store'dan gelen yerelleştirilmiş fiyat. Store hazır değilse ürünün yedek metni döner.</summary>
        public string GetLocalizedPrice(string productId)
        {
            string price = _service != null ? _service.GetLocalizedPrice(productId) : string.Empty;
            if (!string.IsNullOrEmpty(price)) { return price; }

            ShopProduct product = _catalog != null ? _catalog.Find(productId) : null;
            return product != null ? product.FallbackPriceText : string.Empty;
        }

        /// <summary>Kalıcı ürün daha önce alınmış mı?</summary>
        public bool IsOwned(string productId)
        {
            if (_service != null && _service.IsOwned(productId)) { return true; }

            string[] owned = SaveManager.Instance.Data.OwnedProductIds;
            for (int i = 0; i < owned.Length; i++)
            {
                if (owned[i] == productId) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Ürünün içeriğini oyuncuya yazar ve kaydeder. Store doğrulaması tamamlandıktan sonra
        /// çağrılır; reklam ödülü veya hediye kodu gibi akışlar da buraya bağlanır.
        /// </summary>
        public void GrantProduct(ShopProduct product)
        {
            if (product == null) { return; }

            if (product.GoldReward > 0) { EconomyManager.Instance.AddGold(product.GoldReward); }
            if (product.LivesReward > 0) { EconomyManager.Instance.AddLives(product.LivesReward); }
            if (product.InfiniteLivesHours > 0f) { EconomyManager.Instance.GrantInfiniteLives(product.InfiniteLivesHours); }

            int[] boosters = product.BoosterRewards;
            for (int i = 0; i < boosters.Length; i++)
            {
                EconomyManager.Instance.AddBooster(i, boosters[i]);
            }

            if (product.RemovesAds) { SaveManager.Instance.Data.HasRemovedAds = true; }
            if (product.ProductType != ShopProductType.Consumable) { RememberOwnedProduct(product.ProductId); }

            SaveManager.Instance.Save();

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayPurchaseSucceeded(); }

            OnProductGranted?.Invoke(product);
        }

        private void RememberOwnedProduct(string productId)
        {
            PlayerData data = SaveManager.Instance.Data;
            List<string> owned = new List<string>(data.OwnedProductIds);

            if (owned.Contains(productId)) { return; }

            owned.Add(productId);
            data.OwnedProductIds = owned.ToArray();
        }

        private void DetachService()
        {
            if (_service == null) { return; }

            _service.OnInitialized -= HandleInitialized;
            _service.OnPurchaseSucceeded -= HandlePurchaseSucceeded;
            _service.OnPurchaseFailed -= HandlePurchaseFailed;
            _service = null;
        }

        private void HandleInitialized()
        {
            OnStoreReady?.Invoke();
        }

        private void HandlePurchaseSucceeded(string productId)
        {
            ShopProduct product = _catalog != null ? _catalog.Find(productId) : null;

            if (product == null)
            {
                Debug.LogError($"Purchase succeeded for an unknown product id: {productId}", this);
                OnPurchaseFailed?.Invoke(productId, PurchaseFailure.ProductUnavailable);
                return;
            }

            GrantProduct(product);
        }

        private void HandlePurchaseFailed(string productId, PurchaseFailure reason)
        {
            OnPurchaseFailed?.Invoke(productId, reason);
        }
    }
}
