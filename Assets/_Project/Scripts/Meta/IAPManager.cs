using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security; 

[Serializable]
public enum IAPProductKey
{
    StarterPack, BeginnerPack, GoldenBirdPack, SpaceBirdPack, 
    ThousandGold, FiveThousandGold, TenThousandGold,
    TwentyFiveThousandGold, FiftyThousandGold, HundredThousandGold,
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
    
    public string starterPack = "starter_pack";
    public string beginnerPack = "beginnerpack";
    public string goldenBirdPack = "goldenbirdpack";
    public string spaceBirdPack = "spacebirdpack";
    
    public string thousandgold = "1kgold";
    public string fivethousandgold = "5kgold";
    public string tenthousandgold = "10kgold";
    public string twentyFivethousandgold = "25kgold";
    public string fiftyThousandgold = "50kgold";
    public string hundredThousandgold = "100kgold";
    
    public string removeAds  = "removeads";

    public static bool IsInitialized { get; private set; } = false;
    private static StoreController _storeController;

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
                    // Add consume transaction for consumable products like coins and one time buy for non-consumable products like remove ads.
                    // if product.definition.id == starterPack
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in OnCheckEntitlement: {e}");
            }
        };
    }

    //make function to update button prices
    private void UpdateButtonPrices()
    {
        if (_storeController != null)
        {
            foreach (var product in _storeController.GetProducts())
            {
                string price = product.metadata.localizedPrice + " " + product.metadata.isoCurrencyCode;
                // Update button price
            }
        }
    }
    
    private List<ProductDefinition> BuildProductDefinitios()
    {
        var initialProductToFetch = new List<ProductDefinition>();
        
        initialProductToFetch.Add(new ProductDefinition(starterPack, ProductType.NonConsumable));
        initialProductToFetch.Add(new ProductDefinition(beginnerPack, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(goldenBirdPack, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(spaceBirdPack, ProductType.Consumable));

        initialProductToFetch.Add(new ProductDefinition(thousandgold, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(fivethousandgold, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(tenthousandgold, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(twentyFivethousandgold, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(fiftyThousandgold, ProductType.Consumable));
        initialProductToFetch.Add(new ProductDefinition(hundredThousandgold, ProductType.Consumable));

        initialProductToFetch.Add(new ProductDefinition(removeAds, ProductType.NonConsumable));

        return initialProductToFetch;
    }

    private void OnProductsFetched(List<Product> products)
    {
        _storeController.FetchPurchases();
        foreach (var product in products)
        {
         string price = product.metadata.localizedPrice + " " + product.metadata.isoCurrencyCode;
         // Update button price
        } 
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

        switch (productKey)
        {
            case IAPProductKey.StarterPack : _storeController.PurchaseProduct(starterPack); break;
            case IAPProductKey.BeginnerPack : _storeController.PurchaseProduct(beginnerPack); break;
            case IAPProductKey.GoldenBirdPack : _storeController.PurchaseProduct(goldenBirdPack); break;
            case IAPProductKey.SpaceBirdPack : _storeController.PurchaseProduct(spaceBirdPack); break;
            case IAPProductKey.ThousandGold : _storeController.PurchaseProduct(thousandgold); break;
            case IAPProductKey.FiveThousandGold : _storeController.PurchaseProduct(fivethousandgold); break;
            case IAPProductKey.TenThousandGold : _storeController.PurchaseProduct(tenthousandgold); break;
            case IAPProductKey.TwentyFiveThousandGold : _storeController.PurchaseProduct(twentyFivethousandgold); break;
            case IAPProductKey.FiftyThousandGold : _storeController.PurchaseProduct(fiftyThousandgold); break;
            case IAPProductKey.HundredThousandGold : _storeController.PurchaseProduct(hundredThousandgold); break;
            case IAPProductKey.RemoveAds : _storeController.PurchaseProduct(removeAds); break;
        }
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
                
                // if productıd == "id" make transaction for that product
                // Then save transaction to cloud save
            
                
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
