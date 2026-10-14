using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("An interstitial ad button. Which ad it shows, how often, and what it does when there is no ad on this platform are set in the hub (LevelPlay page).\n\n" +
                "On Closed is called when the ad closes, and at once when a rule says wait, so your game never gets stuck.\n\n" +
                "Without a button, from any script:\n" +
                "SdkAds.ShowInterstitial(\"level_end\", () => LoadNextLevel());")]
    [AddComponentMenu("SDK Hub/Interstitial Ad Button")]
    public class InterstitialAdButton : SdkHubButton
    {
        [HideInInspector]
        [SerializeField] string placement;

        [Tooltip("Called when the ad closes, or at once when no ad is shown.")]
        [SerializeField] UnityEvent onClosed = new UnityEvent();

        AdUnavailableBehavior Behavior
        {
            get
            {
                var entry = AdCatalog.Current.Resolve(AdFormat.Interstitial, placement, SdkPlatformInfo.Current);
                return entry != null ? entry.whenUnavailable : AdUnavailableBehavior.Hide;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            AdRemoval.Changed += Refresh;
            SdkManager.Ads.Initialize();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            AdRemoval.Changed -= Refresh;
        }

        protected override void OnClicked()
        {
            var ads = SdkManager.Ads;
            if (!ads.IsAvailable(AdFormat.Interstitial, placement))
            {
                if (Behavior == AdUnavailableBehavior.Skip) onClosed.Invoke();
                return;
            }
            ads.ShowInterstitial(placement, () => onClosed.Invoke());
        }

        void Refresh()
        {
            if (!SdkManager.Ads.IsAvailable(AdFormat.Interstitial, placement))
            {
                var behavior = Behavior;
                SetVisible(behavior != AdUnavailableBehavior.Hide);
                Button.interactable = behavior == AdUnavailableBehavior.Skip;
                return;
            }

            SetVisible(true);
            Button.interactable = true;
        }
    }
}
