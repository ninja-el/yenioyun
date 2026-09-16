using System;
using System.Collections.Generic;
using MatchPack.Core;

namespace MatchPack.Meta
{
    /// <summary>
    /// Unity IAP paketi projeye eklenene kadar kullanılan sahte store. Satın almayı anında
    /// başarılı sayar, böylece ödül ve kayıt akışı editörde uçtan uca test edilebilir.
    /// Gerçek servis yazıldığında ShopManager'daki servis seçimi değişir, bu dosya silinir.
    /// </summary>
    public class StubPurchaseService : IPurchaseService
    {
        private readonly HashSet<string> _ownedProductIds = new HashSet<string>();
        private ShopCatalog _catalog;

        public bool IsInitialized { get; private set; }

        public event Action OnInitialized;
        public event Action<PurchaseFailure> OnInitializeFailed;
        public event Action<string> OnPurchaseSucceeded;
        public event Action<string, PurchaseFailure> OnPurchaseFailed;
        public event Action<bool> OnRestoreCompleted;

        public void Initialize(ShopCatalog catalog)
        {
            _catalog = catalog;

            if (_catalog == null)
            {
                OnInitializeFailed?.Invoke(PurchaseFailure.ProductUnavailable);
                return;
            }

            string[] owned = SaveManager.Instance.Data.OwnedProductIds;
            for (int i = 0; i < owned.Length; i++) { _ownedProductIds.Add(owned[i]); }

            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        public void Purchase(string productId)
        {
            if (!IsInitialized)
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailure.NotInitialized);
                return;
            }

            ShopProduct product = _catalog.Find(productId);
            if (product == null)
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailure.ProductUnavailable);
                return;
            }

            if (product.ProductType != ShopProductType.Consumable && _ownedProductIds.Contains(productId))
            {
                OnPurchaseFailed?.Invoke(productId, PurchaseFailure.DuplicateTransaction);
                return;
            }

            if (product.ProductType != ShopProductType.Consumable) { _ownedProductIds.Add(productId); }

            OnPurchaseSucceeded?.Invoke(productId);
        }

        public void RestorePurchases()
        {
            OnRestoreCompleted?.Invoke(IsInitialized);
        }

        public string GetLocalizedPrice(string productId)
        {
            ShopProduct product = _catalog != null ? _catalog.Find(productId) : null;
            return product != null ? product.FallbackPriceText : string.Empty;
        }

        public bool IsOwned(string productId)
        {
            return _ownedProductIds.Contains(productId);
        }
    }
}
