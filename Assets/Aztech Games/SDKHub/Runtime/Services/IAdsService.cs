using System;

namespace SDKHub
{
    public interface IAdsService
    {
        event Action Initialized;

        bool IsInitialized { get; }

        void Initialize();

        // True when the placement exists on this platform and its ads have not been removed.
        bool IsAvailable(AdFormat format, string placement);

        // True when an ad can be shown right now.
        bool IsReady(AdFormat format, string placement);

        void ShowRewarded(string placement, Action<RewardedResult> onFinished);
        void ShowInterstitial(string placement, Action onClosed = null);
        void ShowBanner(string placement, BannerPosition position = BannerPosition.Bottom);
        void HideBanner(string placement);
        void DestroyBanner(string placement);
    }
}
