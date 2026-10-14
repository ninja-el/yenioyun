using System;
using UnityEngine;

namespace SDKHub
{
    // The easy way to use ads from any script. Everything starts and loads by itself.
    public static class SdkAds
    {
        public static event Action BannerStateChanged;

        internal static void RaiseBannerStateChanged()
        {
            BannerStateChanged?.Invoke();
        }

        public static bool IsBannerVisible
        {
            get
            {
                var facade = SdkManager.Ads as AdsFacade;
                return facade != null && facade.IsBannerVisible;
            }
        }

        public static BannerPosition CurrentBannerPosition
        {
            get
            {
                var facade = SdkManager.Ads as AdsFacade;
                return facade != null ? facade.CurrentBannerPosition : AdRules.DefaultBannerPosition;
            }
        }

        // ---- Rewarded ----------------------------------------------------------

        public static bool IsRewardedReady(string placement = null)
        {
            return SdkManager.Ads.IsReady(AdFormat.Rewarded, placement);
        }

        public static void ShowRewarded(Action onSuccess, Action onFailed = null, Action onCancelled = null)
        {
            ShowRewarded(null, onSuccess, onFailed, onCancelled);
        }

        public static void ShowRewarded(string placement, Action onSuccess, Action onFailed = null, Action onCancelled = null)
        {
            SdkManager.Ads.ShowRewarded(placement, result =>
            {
                switch (result)
                {
                    case RewardedResult.Success:
                        onSuccess?.Invoke();
                        break;
                    case RewardedResult.Cancelled:
                        onCancelled?.Invoke();
                        break;
                    default:
                        onFailed?.Invoke();
                        break;
                }
            });
        }

        // ---- Interstitial ------------------------------------------------------

        public static void ShowInterstitial(Action onClosed = null)
        {
            SdkManager.Ads.ShowInterstitial(null, onClosed);
        }

        public static void ShowInterstitial(string placement, Action onClosed = null)
        {
            SdkManager.Ads.ShowInterstitial(placement, onClosed);
        }

        // ---- Banner ------------------------------------------------------------

        // Uses the banner position you chose in the hub.
        public static void ShowBanner()
        {
            SdkManager.Ads.ShowBanner(null, AdRules.DefaultBannerPosition);
        }

        public static void ShowBanner(BannerPosition position, string placement = null)
        {
            SdkManager.Ads.ShowBanner(placement, position);
        }

        public static void HideBanner(string placement = null)
        {
            SdkManager.Ads.HideBanner(placement);
        }

        // ---- Remove ads --------------------------------------------------------

        // Turns off the chosen ads for good. Call it after the player buys "Remove Ads".
        public static void RemoveAds(AdTypes types = AdTypes.All)
        {
            AdRemoval.Remove(types);
        }

        // Turns the chosen ads back on.
        public static void RestoreAds(AdTypes types = AdTypes.All)
        {
            AdRemoval.Restore(types);
        }

        public static bool AreAdsRemoved(AdTypes types = AdTypes.All)
        {
            return AdRemoval.AreRemoved(types);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            BannerStateChanged = null;
        }
    }
}
