using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("Restores the player's earlier purchases, for example after reinstalling. Apple requires this button in games with one-time purchases.\n\n" +
                "Restored items arrive like normal purchases, so 'Remove Ads' turns itself on again.\n\n" +
                "Code: SdkManager.Iap.RestorePurchases(success => { ... });")]
    [AddComponentMenu("SDK Hub/Restore Purchases Button")]
    public class RestorePurchasesButton : SdkHubButton
    {
        [SerializeField] bool initializeIfNeeded = true;
        [SerializeField] UnityEvent onRestored = new UnityEvent();
        [SerializeField] UnityEvent onFailed = new UnityEvent();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (initializeIfNeeded) SdkManager.Iap.Initialize();
        }

        protected override void OnClicked()
        {
            SdkManager.Iap.RestorePurchases(success =>
            {
                if (success) onRestored.Invoke();
                else onFailed.Invoke();
            });
        }
    }
}
