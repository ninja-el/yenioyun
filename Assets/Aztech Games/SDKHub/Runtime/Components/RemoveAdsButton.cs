using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("A ready-made 'Remove Ads' purchase button. The product and the kinds of ads it removes are set in the hub (LevelPlay page, Remove ads).\n\n" +
                "After the purchase those ads stay off, also after restarting, and the button hides itself.\n\n" +
                "Without a button, from any script:\n" +
                "SdkAds.RemoveAds();   // or SdkAds.RemoveAds(AdTypes.Banner);\n" +
                "SdkAds.RestoreAds();  // turn ads back on")]
    [AddComponentMenu("SDK Hub/Remove Ads Button")]
    public class RemoveAdsButton : SdkHubButton
    {
        [HideInInspector]
        [SerializeField] string placement;

        [HideInInspector]
        [SerializeField] TMP_Text priceLabel;

        [Header("Results")]
        [SerializeField] UnityEvent onAdsRemoved = new UnityEvent();
        [SerializeField] UnityEvent<string> onPurchaseFailed = new UnityEvent<string>();

        IIapService iap;

        RemoveAdsEntry Entry => AdCatalog.Current.FindRemoveAds(placement);

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
            AdRemoval.Changed += Refresh;
            iap.Initialize();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (iap != null)
            {
                iap.Initialized -= Refresh;
                iap.PurchaseSucceeded -= OnSucceeded;
                iap.PurchaseFailed -= OnFailed;
            }
            AdRemoval.Changed -= Refresh;
        }

        protected override void OnClicked()
        {
            var entry = Entry;
            if (entry == null || string.IsNullOrEmpty(entry.productId))
            {
                Debug.LogWarning("[SDK Hub] Remove Ads has no product. Pick one in the hub (LevelPlay page, Remove ads).", this);
                return;
            }
            iap.Purchase(entry.productId);
        }

        void Refresh()
        {
            var entry = Entry;
            if (entry == null)
            {
                SetVisible(false);
                return;
            }

            var removed = AdRemoval.AreRemoved(entry.types);
            SetVisible(!removed);
            Button.interactable = iap.IsInitialized && !removed;

            if (priceLabel == null || !iap.IsInitialized) return;
            var price = iap.GetPrice(entry.productId);
            if (!string.IsNullOrEmpty(price)) priceLabel.text = price;
        }

        void OnSucceeded(string id)
        {
            var entry = Entry;
            if (entry == null || id != entry.productId) return;
            AdRemoval.Remove(entry.types);
            onAdsRemoved.Invoke();
        }

        void OnFailed(string id, string reason)
        {
            var entry = Entry;
            if (entry != null && (id == entry.productId || string.IsNullOrEmpty(id))) onPurchaseFailed.Invoke(reason);
        }
    }
}
