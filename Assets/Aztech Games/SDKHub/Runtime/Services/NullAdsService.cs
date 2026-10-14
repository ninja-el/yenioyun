using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public sealed class NullAdsService : IAdsService
    {
        const float ReloadSeconds = 1.5f;

        readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
        AdCatalog catalog;

        public event Action Initialized;

        public bool IsInitialized { get; private set; }

        AdCatalog Catalog => catalog ?? (catalog = AdCatalog.Load());

        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Debug.Log("[SDK Hub] Ads are running in mock mode. Platform: " + SdkPlatformInfo.Current);
            Initialized?.Invoke();
        }

        public bool IsAvailable(AdFormat format, string placement)
        {
            if (Catalog.placements.Count == 0) return true;
            var platform = SdkPlatformInfo.Current;
            var entry = Catalog.Resolve(format, placement, platform);
            return entry != null && entry.IsAvailableOn(platform);
        }

        public bool IsReady(AdFormat format, string placement)
        {
            if (!IsInitialized || !IsAvailable(format, placement)) return false;
            float at;
            return !readyAt.TryGetValue(Key(format, placement), out at) || Time.unscaledTime >= at;
        }

        public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            if (!IsReady(AdFormat.Rewarded, placement))
            {
                onFinished?.Invoke(RewardedResult.Failed);
                return;
            }
            readyAt[Key(AdFormat.Rewarded, placement)] = Time.unscaledTime + ReloadSeconds;
            Debug.Log("[SDK Hub] Mock rewarded ad shown. The reward is granted in the Editor only.");
            onFinished?.Invoke(Application.isEditor ? RewardedResult.Success : RewardedResult.Failed);
        }

        public void ShowInterstitial(string placement, Action onClosed = null)
        {
            if (IsReady(AdFormat.Interstitial, placement))
            {
                readyAt[Key(AdFormat.Interstitial, placement)] = Time.unscaledTime + ReloadSeconds;
                Debug.Log("[SDK Hub] Mock interstitial ad shown.");
            }
            onClosed?.Invoke();
        }

        public void ShowBanner(string placement, BannerPosition position = BannerPosition.Bottom)
        {
            if (!IsAvailable(AdFormat.Banner, placement)) return;
            Debug.Log($"[SDK Hub] Mock banner shown ({position}).");
        }

        public void HideBanner(string placement)
        {
            Debug.Log("[SDK Hub] Mock banner hidden.");
        }

        public void DestroyBanner(string placement)
        {
            Debug.Log("[SDK Hub] Mock banner removed.");
        }

        static string Key(AdFormat format, string placement)
        {
            return format + ":" + (placement ?? "");
        }
    }
}
