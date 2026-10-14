using System;
using UnityEngine;

namespace SDKHub
{
    public sealed class NullIapService : IIapService
    {
        public event Action Initialized;
        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;

        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Debug.Log("[SDK Hub] IAP is running in mock mode (In-App Purchasing is not installed).");
            Initialized?.Invoke();
        }

        public void Purchase(string productId)
        {
            if (Application.isEditor)
            {
                IapOwnership.MarkIfNonConsumable(productId);
                PurchaseSucceeded?.Invoke(productId);
            }
            else PurchaseFailed?.Invoke(productId, "In-App Purchasing is not installed.");
        }

        public bool IsOwned(string productId)
        {
            return IapOwnership.IsOwned(productId);
        }

        public string GetPrice(string productId)
        {
            return Application.isEditor ? "$0.99" : "";
        }

        public void RestorePurchases(Action<bool> onFinished)
        {
            onFinished?.Invoke(Application.isEditor);
        }
    }
}
