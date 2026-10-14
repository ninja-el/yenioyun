using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("Buys one product when clicked and shows its store price. The product is set from the hub (In-App Purchases page, Add to scene).\n\n" +
                "On Purchase Succeeded: give the item here. A one-time item hides the button once owned.\n\n" +
                "Without a button, from any script:\n" +
                "SdkManager.Iap.Purchase(\"product_id\");")]
    [AddComponentMenu("SDK Hub/IAP Buy Button")]
    public class IapBuyButton : SdkHubButton
    {
        [HideInInspector]
        [SerializeField] string productId;

        [HideInInspector]
        [SerializeField] TMP_Text priceLabel;

        [Header("Results")]
        [SerializeField] UnityEvent<string> onPurchaseSucceeded = new UnityEvent<string>();
        [SerializeField] UnityEvent<string> onPurchaseFailed = new UnityEvent<string>();

        IIapService iap;

        protected override void Awake()
        {
            base.Awake();
            if (priceLabel == null)
            {
                var child = transform.Find("Price");
                if (child != null) priceLabel = child.GetComponent<TMP_Text>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            iap = SdkManager.Iap;
            iap.Initialized += Refresh;
            iap.PurchaseSucceeded += OnSucceeded;
            iap.PurchaseFailed += OnFailed;
            iap.Initialize();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (iap == null) return;
            iap.Initialized -= Refresh;
            iap.PurchaseSucceeded -= OnSucceeded;
            iap.PurchaseFailed -= OnFailed;
        }

        protected override void OnClicked()
        {
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogWarning("[SDK Hub] This buy button has no product. Add it from the hub (In-App Purchases page).", this);
                return;
            }
            iap.Purchase(productId);
        }

        void Refresh()
        {
            var owned = iap.IsOwned(productId);
            SetVisible(!owned);
            Button.interactable = iap.IsInitialized && !owned;

            if (priceLabel == null || !iap.IsInitialized) return;
            var price = iap.GetPrice(productId);
            if (!string.IsNullOrEmpty(price)) priceLabel.text = price;
        }

        void OnSucceeded(string id)
        {
            if (id != productId) return;
            onPurchaseSucceeded.Invoke(id);
            Refresh();
        }

        void OnFailed(string id, string reason)
        {
            if (id == productId || string.IsNullOrEmpty(id)) onPurchaseFailed.Invoke(reason);
        }
    }
}
