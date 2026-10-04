using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using MatchPack.Meta;

[Serializable]
public enum IAPProductKey
{
    
    SupriseBox, BeginnerBox, MegaBox, GoldenBox, 
    Box1K, Box5K, Box10K,
    Box25K, Box50K, Box100K,
    RemoveAds
}

[Serializable]
public class IAPPayData
{
    public string Payload;
    public string Store;
    public string TransactionID;
}

[Serializable]
public class IAPPayload
{
    public string json;
    public string signature;
    public IAPPayData payloadData;
}

[Serializable]
public class IAPPayloadData
{
    public string orderId;
    public string packageName;
    public string productId;
    public long purchaseTime;
    public int purchaseState;
    public string purchaseToken;
    public int quantity;
    public bool acknowledged;
}
// ---------------------------------

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;
    
    private CrossPlatformValidator validator;
    
    public string supriseBox = "suprisebox";
    public string beginnerBox = "beginnerbox";
    public string megaBox = "megabox";
    public string goldenBox = "goldenbox";
    
    public string box1k = "box1k";
    public string box5k = "box5k";
    public string box10k = "box10k";
    public string box25k = "box25k";
    public string box50k = "box50k";
    public string box100k = "box100k";
    
    public string boxremoveAds  = "boxremoveads";

    public static bool IsInitialized { get; private set; } = false;
    private static StoreController _storeController;

    /// <summary>Store'dan fiyatlar geldiğinde yayınlanır; satın alma butonları fiyat yazılarını bu event'te tazeler.</summary>
    public event Action OnPricesUpdated;

    private readonly Dictionary<string, string> _localizedPrices = new Dictionary<string, string>();

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

    public async Task InitializeIAPAsync()
    {
        try
        {
            Debug.Log("IAP Modülü Başlatılıyor");

            _storeController = UnityIAPServices.StoreController();
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed += OnPurchasesFetchedFailed;
            
            _storeController.OnPurchasePending += OnPurchasesPending; 
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

            RegisterEntitlementCallback();
            
            await _storeController.Connect();
            
            var initialProductToFetch = BuildProductDefinitios();
            _storeController.FetchProducts(initialProductToFetch);
            
            try {
                validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            } catch (Exception e) {
                Debug.LogWarning("Validator kurulamadı (IAP Receipt Validation Obfuscator aracını çalıştırmamış olabilirsin): " + e.Message);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Initialization failed with: {e}");
        }
    }

    private void RegisterEntitlementCallback()
    {
        _storeController.OnCheckEntitlement += (result) =>
        {
            try
            {
                Product product = result.Product;
                var status = result.Status;

                Debug.Log($"Product is {product}, Entitle Status is {status}");

                bool isEntitled = status == EntitlementStatus.FullyEntitled;
                if (isEntitled && product != null && product.definition != null)
                {
                    ShopProduct shopProduct = FindShopProduct(product.definition.id);
                    if (shopProduct != null && shopProduct.ProductType != ShopProductType.Consumable)
                    {
                        ShopManager.Instance.RestoreOwnership(shopProduct);
                    }

                    if(product.definition.id == boxremoveAds){
                        /// <summary>
                        /// 
                        /// </summary>
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OnCheckEntitlement: {e}");
            }
        };
    }

    private void UpdateButtonPrices()
    {
        if (_storeController != null)
        {
            foreach (var product in _storeController.GetProducts())
            {
                string price = product.metadata.localizedPrice + " " + product.metadata.isoCurrencyCode;
                _localizedPrices[product.definition.id] = price;
            }
        }

        OnPricesUpdated?.Invoke();
    }

    /// <summary>Store'dan gelen fiyat metni. Fiyat henüz gelmediyse boş döner.</summary>
    public string GetLocalizedPrice(string productId)
    {
        return _localizedPrices.TryGetValue(productId, out string price) ? price : string.Empty;
    }

    /// <summary>Paket anahtarının store'daki ürün kimliği.</summary>
    public string GetProductId(IAPProductKey productKey)
    {
        switch (productKey)
        {
            case IAPProductKey.SupriseBox : return supriseBox;
            case IAPProductKey.BeginnerBox : return beginnerBox;
            case IAPProductKey.MegaBox : return megaBox;
            case IAPProductKey.GoldenBox : return goldenBox;
            case IAPProductKey.Box1K : return box1k;
            case IAPProductKey.Box5K : return box5k;
            case IAPProductKey.Box10K : return box10k;
            case IAPProductKey.Box25K : return box25k;
            case IAPProductKey.Box50K : return box50k;
            case IAPProductKey.Box100K : return box100k;
            case IAPProductKey.RemoveAds : return boxremoveAds;
            default : return string.Empty;
        }
    }

    private static ShopProduct FindShopProduct(string productId)
    {
        if (ShopManager.Instance == null || ShopManager.Instance.Catalog == null) { return null; }

        return ShopManager.Instance.Catalog.Find(productId);
    }

    private List<ProductDefinition> BuildProductDefinitios()
    {
        var initialProductToFetch = new List<ProductDefinition>();

        AddProductDefinition(initialProductToFetch, supriseBox);
        AddProductDefinition(initialProductToFetch, beginnerBox);
        AddProductDefinition(initialProductToFetch, megaBox);
        AddProductDefinition(initialProductToFetch, goldenBox);

        AddProductDefinition(initialProductToFetch, box1k);
        AddProductDefinition(initialProductToFetch, box5k);
        AddProductDefinition(initialProductToFetch, box10k);
        AddProductDefinition(initialProductToFetch, box25k);
        AddProductDefinition(initialProductToFetch, box50k);
        AddProductDefinition(initialProductToFetch, box100k);

        initialProductToFetch.Add(new ProductDefinition(boxremoveAds, ProductType.NonConsumable));

        return initialProductToFetch;
    }

    private void AddProductDefinition(List<ProductDefinition> definitions, string productId)
    {
        ShopProduct shopProduct = FindShopProduct(productId);
        if (shopProduct == null)
        {
            Debug.LogError($"No ShopProduct in the catalog for store product id: {productId}");
            return;
        }

        definitions.Add(new ProductDefinition(productId, ToStoreProductType(shopProduct.ProductType)));
    }

    private static ProductType ToStoreProductType(ShopProductType type)
    {
        switch (type)
        {
            case ShopProductType.NonConsumable : return ProductType.NonConsumable;
            case ShopProductType.Subscription : return ProductType.Subscription;
            default : return ProductType.Consumable;
        }
    }

    private void OnProductsFetched(List<Product> products)
    {
        _storeController.FetchPurchases();
        UpdateButtonPrices();
    }

    private void OnProductsFetchFailed(ProductFetchFailed reason)
    {
        Debug.Log($"Product fetch failed with: {reason}");
    }
    
    private void OnPurchasesFetched(Orders orders)
    {
        IsInitialized = true;
        foreach (var product in _storeController.GetProducts())
        {
            _storeController.CheckEntitlement(product);
        }
    }

    private void OnPurchasesFetchedFailed(PurchasesFetchFailureDescription reason)
    {
        Debug.Log($"Purchases fetch failed: {reason}");
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription reason)
    {
        Debug.Log($"Initialization/Connection failed: {reason.message}");
    }

    public void BuyProduct(IAPProductKey productKey)
    {
        if (!IsInitialized)
        {
            Debug.Log("IAP Module is not initialized");
            return;
        }

        _storeController.PurchaseProduct(GetProductId(productKey));
    }
    
    private void OnPurchasesPending(PendingOrder order)
    {
        Debug.Log($"Purchases pending, yerel makbuz doğrulaması başlıyor: {order}");

        if (order?.Info?.PurchasedProductInfo == null || order.Info.PurchasedProductInfo.Count == 0)
        {
            Debug.LogError("PendingOrder içinde ürün bilgisi bulunamadı!");
            return;
        }

        bool isReceiptValid = true;
        string productId = order.Info.PurchasedProductInfo[0].productId;

#if !UNITY_EDITOR
        if (validator != null && !string.IsNullOrEmpty(order?.Info?.Receipt))
        {
            try
            {
                // Makbuzun şifresini çöz ve Google/Apple imzası taşıyor mu kontrol et
                var result = validator.Validate(order.Info.Receipt);
                Debug.Log($"MAKBUZ DOĞRULANDI! Hile yok. Ürün: {productId}");
            }
            catch (IAPSecurityException)
            {
                // Hırsız Kapanı! Lucky Patcher veya sahte makbuz yakalandı.
                Debug.LogError($"SAHTE MAKBUZ YAKALANDI! Hile programı kullanılıyor. Ürün: {productId}");
                isReceiptValid = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Makbuz doğrulanırken bilinmeyen hata: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("CrossPlatformValidator NULL veya makbuz boş! Güvenlik doğrulaması atlanıyor.");
        }
#endif

        if (isReceiptValid)
        {
            _storeController.ConfirmPurchase(order);
        }
        else
        {
            Debug.LogWarning("Satın alma işlemi güvenlik nedeniyle iptal edildi!");
        }
    }

    private void OnPurchaseDeferred(DeferredOrder deferredOrder)
    {
        Debug.Log($"Purchase Deferred for Product: {deferredOrder?.Info}");
    }

    private void OnPurchaseConfirmed(Order order)
    {
        try
        {
            Debug.Log($"Purchase confirmed: {order}");

            if (order?.Info?.PurchasedProductInfo != null && order.Info.PurchasedProductInfo.Count > 0)
            {
                int quantity = GetPurchaseQuantity(order);
                string productId = order.Info.PurchasedProductInfo[0].productId;
                
                if(productId == boxremoveAds){
                    //
                }
                else
                {
                    GrantPurchasedProduct(productId, quantity);
                }

                Product purchasedProduct = null;
                if (_storeController != null)
                {
                    foreach (var p in _storeController.GetProducts())
                    {
                        if (p.definition.id == productId)
                        {
                            purchasedProduct = p;
                            break;
                        }
                    }
                }
                if (purchasedProduct != null && FacebookManager.Instance != null)
                {
                    string currency = !string.IsNullOrEmpty(purchasedProduct.metadata.isoCurrencyCode) ? purchasedProduct.metadata.isoCurrencyCode : "USD";
                    FacebookManager.Instance.LogIAPurchase((float)purchasedProduct.metadata.localizedPrice, currency);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OnPurchaseConfirmed: {e}");
        }
    }

    private void GrantPurchasedProduct(string productId, int quantity)
    {
        ShopProduct shopProduct = FindShopProduct(productId);
        if (shopProduct == null)
        {
            Debug.LogError($"Purchase confirmed but no ShopProduct found for id: {productId}");
            return;
        }

        // Çoklu adet yalnızca tekrar alınabilen paketlerde olur; tek alımlık paket bir kez verilir.
        int grantCount = shopProduct.ProductType == ShopProductType.Consumable ? quantity : 1;
        for (int i = 0; i < grantCount; i++)
        {
            ShopManager.Instance.GrantProduct(shopProduct);
        }
    }

    private int GetPurchaseQuantity(Order order)
    {
        int quantity = 1;
        try
        {
            string receipt = order?.Info?.Receipt;
            if (!string.IsNullOrEmpty(receipt))
            {
                var payData = JsonUtility.FromJson<IAPPayData>(receipt);
                if (payData != null && payData.Store == "GooglePlay" && !string.IsNullOrEmpty(payData.Payload))
                {
                    IAPPayload payload = JsonUtility.FromJson<IAPPayload>(payData.Payload);
                    if (payload != null && !string.IsNullOrEmpty(payload.json))
                    {
                        IAPPayloadData payloadData = JsonUtility.FromJson<IAPPayloadData>(payload.json);
                        if (payloadData != null && payloadData.quantity > 0)
                        {
                            quantity = payloadData.quantity;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not parse purchase quantity, defaulting to 1: {e.Message}");
        }
        return quantity;
    }

    private void OnPurchaseFailed(FailedOrder failedOrder)
    {
        if (failedOrder?.Info?.PurchasedProductInfo == null || failedOrder.Info.PurchasedProductInfo.Count == 0)
        {
            Debug.Log($"Purchase failed but no product info available");
            return;
        }
        var productId = failedOrder.Info.PurchasedProductInfo[0].productId;
        var reason = failedOrder.FailureReason;
        var message = failedOrder.Details;
        
        Debug.Log($"Purchase failed. Product is {productId}. Reason is {reason}. Here is message {message}");
    }

    public void RestoreAllPurchases()
    {
        _storeController.RestoreTransactions ((sucess, error) =>
        {
            if (sucess)
            {
                Debug.Log("All Previous purchase restored");
            }
            else
            {
                Debug.LogWarning($"Restore failed: " + error);
            }
        });
    }
}
