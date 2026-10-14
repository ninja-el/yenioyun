using System;

namespace SDKHub
{
    public interface IIapService
    {
        event Action Initialized;
        event Action<string> PurchaseSucceeded;
        event Action<string, string> PurchaseFailed;

        bool IsInitialized { get; }

        void Initialize();
        void Purchase(string productId);
        string GetPrice(string productId);

        // True for one-time purchases the player has bought. Remembered on the device.
        bool IsOwned(string productId);
        void RestorePurchases(Action<bool> onFinished);
    }
}
