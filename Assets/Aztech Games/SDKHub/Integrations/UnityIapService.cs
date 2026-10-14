#if SDKHUB_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace SDKHub.Integrations
{
    public sealed class UnityIapService : IIapService
    {
        StoreController store;
        bool initializing;

        public event Action Initialized;
        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;

        public bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterIap(new UnityIapService());
        }

        public async void Initialize()
        {
            if (IsInitialized || initializing) return;

            var config = SdkHubConfig.Instance;
            if (config == null)
            {
                Debug.LogError("[SDK Hub] SdkHubConfig asset not found. Open Tools > SDK Hub.");
                return;
            }

            var catalog = IapCatalog.Parse(config.Get("iap", "catalog"));
            if (catalog.products.Count == 0)
            {
                Debug.LogWarning("[SDK Hub] No IAP products defined. Add them in Tools > SDK Hub.");
                return;
            }

            initializing = true;
            store = UnityIAPServices.StoreController();
            Subscribe();

            try
            {
                await store.Connect();
            }
            catch (Exception exception)
            {
                initializing = false;
                Debug.LogError("[SDK Hub] IAP store connection failed: " + exception.Message);
                return;
            }

            store.FetchProducts(catalog.products.Select(ToDefinition).ToList());
        }

        public void Purchase(string productId)
        {
            if (!IsInitialized)
            {
                PurchaseFailed?.Invoke(productId, "IAP is not initialized.");
                return;
            }
            store.PurchaseProduct(productId);
        }

        public bool IsOwned(string productId)
        {
            return IapOwnership.IsOwned(productId);
        }

        public string GetPrice(string productId)
        {
            if (!IsInitialized) return "";
            var product = store.GetProductById(productId);
            return product != null ? product.metadata.localizedPriceString : "";
        }

        public void RestorePurchases(Action<bool> onFinished)
        {
            if (!IsInitialized)
            {
                onFinished?.Invoke(false);
                return;
            }
            store.RestoreTransactions((success, message) => onFinished?.Invoke(success));
        }

        void Subscribe()
        {
            store.OnProductsFetched += OnProductsFetched;
            store.OnProductsFetchFailed += failed =>
            {
                initializing = false;
                Debug.LogError("[SDK Hub] IAP product fetch failed: " + failed.FailureReason);
            };
            store.OnPurchasePending += HandlePending;
            store.OnPurchaseFailed += failed =>
                PurchaseFailed?.Invoke(ProductId(failed), failed.FailureReason + ": " + failed.Details);
            store.OnPurchaseDeferred += order =>
                Debug.Log("[SDK Hub] IAP purchase deferred: " + ProductId(order));
            store.OnPurchaseConfirmed += order =>
            {
                if (order is FailedOrder failed) Debug.LogWarning("[SDK Hub] IAP confirmation failed: " + failed.FailureReason);
            };
            store.OnPurchasesFetched += orders => { };
            store.OnPurchasesFetchFailed += failed =>
                Debug.LogWarning("[SDK Hub] IAP purchase history fetch failed: " + failed.FailureReason);
            store.OnStoreDisconnected += description =>
                Debug.LogWarning("[SDK Hub] IAP store disconnected.");
        }

        async void HandlePending(PendingOrder order)
        {
            var productId = ProductId(order);
            if (PurchaseValidator.IsEnabled)
            {
                var receipt = order.Info != null ? order.Info.Receipt : "";
                if (!await PurchaseValidator.ValidateAsync(productId, receipt))
                {
                    PurchaseFailed?.Invoke(productId, "The purchase could not be verified.");
                    return;
                }
            }
            IapOwnership.MarkIfNonConsumable(productId);
            PurchaseSucceeded?.Invoke(productId);
            store.ConfirmPurchase(order);
        }

        void OnProductsFetched(List<Product> products)
        {
            if (IsInitialized) return;
            initializing = false;
            IsInitialized = true;
            store.FetchPurchases();
            Initialized?.Invoke();
        }

        static ProductDefinition ToDefinition(IapProductEntry entry)
        {
            ProductType type;
            switch (entry.kind)
            {
                case IapProductKind.NonConsumable: type = ProductType.NonConsumable; break;
                case IapProductKind.Subscription: type = ProductType.Subscription; break;
                default: type = ProductType.Consumable; break;
            }
            return new ProductDefinition(entry.id, type);
        }

        static string ProductId(Order order)
        {
            var item = order.CartOrdered.Items().FirstOrDefault();
            return item != null && item.Product != null ? item.Product.definition.id : "";
        }
    }
}
#endif
