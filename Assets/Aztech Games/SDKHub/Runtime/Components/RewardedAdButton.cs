using UnityEngine;
using UnityEngine.Events;

namespace SDKHub.Components
{
    [SdkHubHelp("A rewarded ad button. Which ad it shows, and what it does when there is no ad on this platform, are set in the hub (LevelPlay page).\n\n" +
                "On Success: the player watched the ad. Give the reward here.\n" +
                "On Cancelled: the player closed the ad early.\n" +
                "On Failed: no ad was ready.\n\n" +
                "Without a button, from any script:\n" +
                "SdkAds.ShowRewarded(() => GiveReward(), () => ShowNoAd(), () => { });")]
    [AddComponentMenu("SDK Hub/Rewarded Ad Button")]
    public class RewardedAdButton : SdkHubButton
    {
        [HideInInspector]
        [SerializeField] string placement;

        [Header("Results")]
        [Tooltip("The player watched the ad to the end. Give the reward here.")]
        [SerializeField] UnityEvent onSuccess = new UnityEvent();

        [Tooltip("The player closed the ad before it finished.")]
        [SerializeField] UnityEvent onCancelled = new UnityEvent();

        [Tooltip("No ad was ready, or it could not be shown.")]
        [SerializeField] UnityEvent onFailed = new UnityEvent();

        float timer;

        AdUnavailableBehavior Behavior
        {
            get
            {
                var entry = AdCatalog.Current.Resolve(AdFormat.Rewarded, placement, SdkPlatformInfo.Current);
                return entry != null ? entry.whenUnavailable : AdUnavailableBehavior.Hide;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            AdRemoval.Changed += Refresh;
            SdkManager.Ads.Initialize();
            timer = 0f;
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            AdRemoval.Changed -= Refresh;
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = 0.5f;
            Refresh();
        }

        protected override void OnClicked()
        {
            var ads = SdkManager.Ads;
            if (!ads.IsAvailable(AdFormat.Rewarded, placement))
            {
                // "Give the reward anyway" never overrides a player who removed rewarded ads on purpose.
                if (Behavior == AdUnavailableBehavior.Skip && !AdRemoval.IsRemoved(AdFormat.Rewarded)) onSuccess.Invoke();
                return;
            }

            ads.ShowRewarded(placement, result =>
            {
                switch (result)
                {
                    case RewardedResult.Success:
                        onSuccess.Invoke();
                        break;
                    case RewardedResult.Cancelled:
                        onCancelled.Invoke();
                        break;
                    default:
                        onFailed.Invoke();
                        break;
                }
            });
        }

        void Refresh()
        {
            var ads = SdkManager.Ads;
            if (!ads.IsAvailable(AdFormat.Rewarded, placement))
            {
                var behavior = Behavior;
                SetVisible(behavior != AdUnavailableBehavior.Hide);
                Button.interactable = behavior == AdUnavailableBehavior.Skip && !AdRemoval.IsRemoved(AdFormat.Rewarded);
                return;
            }

            SetVisible(true);
            Button.interactable = ads.IsReady(AdFormat.Rewarded, placement);
        }
    }
}
