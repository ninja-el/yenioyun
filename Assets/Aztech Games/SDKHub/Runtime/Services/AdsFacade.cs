using System;
using UnityEngine;

namespace SDKHub
{
    // Wraps the real ad service. It starts ads automatically, applies "remove ads"
    // and the interstitial rules, and keeps the banner in the right state.
    public sealed class AdsFacade : IAdsService, IDisposable
    {
        readonly IAdsService inner;
        readonly InterstitialPolicy policy;
        AdCatalog catalog;
        bool bannerWanted;
        string bannerPlacement;
        BannerPosition bannerPosition;

        public event Action Initialized;

        public bool IsInitialized => inner.IsInitialized;
        public bool IsBannerVisible { get; private set; }
        public BannerPosition CurrentBannerPosition => bannerPosition;

        AdCatalog Catalog => catalog ?? (catalog = AdCatalog.Load());

        public AdsFacade(IAdsService inner)
        {
            this.inner = inner;
            policy = new InterstitialPolicy(AdRules.GraceSeconds, AdRules.MinSecondsBetween);
            inner.Initialized += OnInnerInitialized;
            AdRemoval.Changed += OnRemovalChanged;
        }

        public void Dispose()
        {
            inner.Initialized -= OnInnerInitialized;
            AdRemoval.Changed -= OnRemovalChanged;
        }

        public void Initialize()
        {
            inner.Initialize();
        }

        public bool IsAvailable(AdFormat format, string placement)
        {
            if (AdRemoval.IsRemoved(format)) return GivesRewardWithoutAd(format);
            return inner.IsAvailable(format, placement);
        }

        public bool IsReady(AdFormat format, string placement)
        {
            if (AdRemoval.IsRemoved(format)) return GivesRewardWithoutAd(format);
            return inner.IsReady(format, placement);
        }

        public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            if (AdRemoval.IsRemoved(AdFormat.Rewarded))
            {
                onFinished?.Invoke(GivesRewardWithoutAd(AdFormat.Rewarded) ? RewardedResult.Success : RewardedResult.Failed);
                return;
            }
            inner.Initialize();
            inner.ShowRewarded(placement, onFinished);
        }

        public void ShowInterstitial(string placement, Action onClosed = null)
        {
            if (AdRemoval.IsRemoved(AdFormat.Interstitial))
            {
                onClosed?.Invoke();
                return;
            }

            inner.Initialize();

            var entry = Catalog.Resolve(AdFormat.Interstitial, placement, SdkPlatformInfo.Current);
            var key = entry != null ? entry.name : placement;
            var now = Time.realtimeSinceStartup;

            var reason = policy.Check(key, entry != null ? entry.everyNth : 1, now);
            if (reason != null)
            {
                Debug.Log("[SDK Hub] Interstitial skipped: " + reason + ".");
                onClosed?.Invoke();
                return;
            }

            if (!inner.IsReady(AdFormat.Interstitial, placement))
            {
                Debug.Log("[SDK Hub] Interstitial skipped: no ad is loaded yet. It loads automatically for the next request.");
                inner.ShowInterstitial(placement, onClosed);
                return;
            }

            policy.MarkShown(key, now);
            inner.ShowInterstitial(placement, onClosed);
        }

        public void ShowBanner(string placement, BannerPosition position = BannerPosition.Bottom)
        {
            bannerWanted = true;
            bannerPlacement = placement;
            bannerPosition = position;

            if (AdRemoval.IsRemoved(AdFormat.Banner))
            {
                SetBannerVisible(false);
                return;
            }

            inner.Initialize();
            inner.ShowBanner(placement, position);
            SetBannerVisible(true);
        }

        public void HideBanner(string placement)
        {
            bannerWanted = false;
            inner.HideBanner(placement);
            SetBannerVisible(false);
        }

        public void DestroyBanner(string placement)
        {
            bannerWanted = false;
            inner.DestroyBanner(placement);
            SetBannerVisible(false);
        }

        bool GivesRewardWithoutAd(AdFormat format)
        {
            return format == AdFormat.Rewarded && AdRules.RewardedWhenRemoved == RewardedWhenRemoved.GiveReward;
        }

        void OnInnerInitialized()
        {
            Initialized?.Invoke();
            if (bannerWanted && !AdRemoval.IsRemoved(AdFormat.Banner)) inner.ShowBanner(bannerPlacement, bannerPosition);
        }

        void OnRemovalChanged()
        {
            if (AdRemoval.IsRemoved(AdFormat.Banner))
            {
                if (bannerWanted || IsBannerVisible) inner.DestroyBanner(bannerPlacement);
                SetBannerVisible(false);
            }
            else if (bannerWanted)
            {
                inner.Initialize();
                inner.ShowBanner(bannerPlacement, bannerPosition);
                SetBannerVisible(true);
            }
        }

        void SetBannerVisible(bool visible)
        {
            IsBannerVisible = visible;
            SdkAds.RaiseBannerStateChanged();
        }
    }
}
