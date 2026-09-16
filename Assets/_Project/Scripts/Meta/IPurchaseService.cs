using System;

namespace MatchPack.Meta
{
    /// <summary>Satın almanın neden tamamlanamadığı. Unity IAP PurchaseFailureReason ile eşlenir.</summary>
    public enum PurchaseFailure
    {
        Unknown,
        NotInitialized,
        ProductUnavailable,
        UserCancelled,
        PaymentDeclined,
        NetworkError,
        DuplicateTransaction,
        SignatureInvalid
    }

    /// <summary>
    /// Store katmanının oyuna açtığı yüzey. Unity IAP, bu arayüzü uygulayan tek bir sınıfın
    /// içinde kalır; oyunun geri kalanı store SDK'sını hiç görmez (06-Sabitler karar #5).
    /// </summary>
    public interface IPurchaseService
    {
        /// <summary>Store bağlantısı kurulup ürün listesi alındığında true olur.</summary>
        bool IsInitialized { get; }

        /// <summary>Store hazır olduğunda yayınlanır.</summary>
        event Action OnInitialized;

        /// <summary>Store'a bağlanılamadığında yayınlanır; parametre kullanıcıya gösterilecek sebeptir.</summary>
        event Action<PurchaseFailure> OnInitializeFailed;

        /// <summary>Satın alma doğrulandığında yayınlanır. Parametre store ürün kimliğidir.</summary>
        event Action<string> OnPurchaseSucceeded;

        /// <summary>Satın alma tamamlanamadığında yayınlanır.</summary>
        event Action<string, PurchaseFailure> OnPurchaseFailed;

        /// <summary>Kalıcı ürünler geri yüklendiğinde yayınlanır (iOS Restore).</summary>
        event Action<bool> OnRestoreCompleted;

        /// <summary>Katalogdaki ürünlerle store'a bağlanır. Uygulama açılışında bir kez çağrılır.</summary>
        void Initialize(ShopCatalog catalog);

        /// <summary>Satın alma akışını başlatır. Sonuç event ile döner.</summary>
        void Purchase(string productId);

        /// <summary>Kalıcı ürünleri geri yükler. iOS'ta zorunlu, Android'de otomatiktir.</summary>
        void RestorePurchases();

        /// <summary>Store'dan gelen yerelleştirilmiş fiyat metni. Store hazır değilse boş string döner.</summary>
        string GetLocalizedPrice(string productId);

        /// <summary>Kalıcı bir ürün store tarafında sahiplenilmiş mi?</summary>
        bool IsOwned(string productId);
    }
}
