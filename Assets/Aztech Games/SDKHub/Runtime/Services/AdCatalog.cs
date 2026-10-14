using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public enum AdFormat { Rewarded, Interstitial, Banner }

    public enum BannerPosition { Bottom, Top }

    public enum SdkPlatform { Android, iOS, Other }

    // What a button does when there is no ad for it on this platform.
    public enum AdUnavailableBehavior { Hide, Disable, Skip }

    public static class SdkPlatformInfo
    {
        public static SdkPlatform Current
        {
            get
            {
#if UNITY_EDITOR
                // The hub's Settings page can make the Editor pretend to be Android or iOS, to test ads that exist on one platform only.
                var config = SdkHubConfig.Instance;
                var preview = config != null ? config.Get("general", "editorPlatform") : "";
                if (preview == "Android") return SdkPlatform.Android;
                if (preview == "iOS") return SdkPlatform.iOS;
#endif
#if UNITY_ANDROID
                return SdkPlatform.Android;
#elif UNITY_IOS
                return SdkPlatform.iOS;
#else
                return SdkPlatform.Other;
#endif
            }
        }
    }

    [Serializable]
    public class AdPlacementEntry
    {
        public string name;
        public AdFormat format;
        public string androidId;
        public string iosId;

        // Interstitials only: show once every N requests (1 = every request).
        public int everyNth = 1;

        // Rewarded and interstitial: what the button does when there is no ad here.
        public AdUnavailableBehavior whenUnavailable = AdUnavailableBehavior.Hide;

        // Banner only.
        public BannerPosition position = BannerPosition.Bottom;
        public bool showAtLaunch = true;

        public string IdFor(SdkPlatform platform)
        {
            switch (platform)
            {
                case SdkPlatform.Android: return androidId;
                case SdkPlatform.iOS: return iosId;
                default: return "";
            }
        }

        public bool IsAvailableOn(SdkPlatform platform)
        {
            if (platform == SdkPlatform.Other) return !string.IsNullOrEmpty(androidId) || !string.IsNullOrEmpty(iosId);
            return !string.IsNullOrEmpty(IdFor(platform));
        }

        public string PlatformSummary
        {
            get
            {
                var android = !string.IsNullOrEmpty(androidId);
                var ios = !string.IsNullOrEmpty(iosId);
                if (android && ios) return "Android + iOS";
                if (android) return "Android only";
                if (ios) return "iOS only";
                return "No platform";
            }
        }
    }

    // One "Remove Ads" product and the kinds of ads it turns off.
    [Serializable]
    public class RemoveAdsEntry
    {
        public string name;
        public string productId;
        public AdTypes types = AdTypes.All;
    }

    [Serializable]
    public class AdCatalog
    {
        public List<AdPlacementEntry> placements = new List<AdPlacementEntry>();
        public List<RemoveAdsEntry> removeAds = new List<RemoveAdsEntry>();

        static AdCatalog current;

        // The catalog from the hub, read once per play session.
        public static AdCatalog Current
        {
            get
            {
                if (current == null) current = Load();
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCurrent()
        {
            current = null;
        }

        public RemoveAdsEntry FindRemoveAds(string name)
        {
            foreach (var entry in removeAds)
            {
                if (string.IsNullOrEmpty(name) || entry.name == name) return entry;
            }
            return null;
        }

        public static AdCatalog Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new AdCatalog();
            try
            {
                return JsonUtility.FromJson<AdCatalog>(json) ?? new AdCatalog();
            }
            catch (ArgumentException)
            {
                return new AdCatalog();
            }
        }

        public static AdCatalog Load()
        {
            var config = SdkHubConfig.Instance;
            return Parse(config != null ? config.Get("levelplay", "adPlacements") : "");
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public AdPlacementEntry Resolve(AdFormat format, string name, SdkPlatform platform)
        {
            AdPlacementEntry fallback = null;
            foreach (var placement in placements)
            {
                if (placement.format != format) continue;
                if (!string.IsNullOrEmpty(name))
                {
                    if (placement.name == name) return placement;
                    continue;
                }
                if (placement.IsAvailableOn(platform)) return placement;
                if (fallback == null) fallback = placement;
            }
            return fallback;
        }
    }
}
