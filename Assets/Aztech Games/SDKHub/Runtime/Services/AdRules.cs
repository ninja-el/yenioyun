using System;
using System.Globalization;
using UnityEngine;

namespace SDKHub
{
    // Remembers which ads the player has removed. Saved on the device, so it survives restarts.
    public static class AdRemoval
    {
        const string Key = "SDKHub.AdsRemoved";

        static int? cached;

        public static event Action Changed;

        public static AdTypes Removed
        {
            get
            {
                if (!cached.HasValue) cached = PlayerPrefs.GetInt(Key, 0) & (int)AdTypes.All;
                return (AdTypes)cached.Value;
            }
        }

        public static bool IsRemoved(AdFormat format)
        {
            return (Removed & format.ToType()) != 0;
        }

        // True when every ad type in the selection is removed.
        public static bool AreRemoved(AdTypes types)
        {
            return types != AdTypes.None && (Removed & types) == types;
        }

        public static void Remove(AdTypes types)
        {
            Set(Removed | types);
        }

        public static void Restore(AdTypes types)
        {
            Set(Removed & ~types);
        }

        static void Set(AdTypes value)
        {
            value &= AdTypes.All;
            if (value == Removed) return;
            cached = (int)value;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            cached = null;
            Changed = null;
        }
    }

    // The ad rules you set in the hub (LevelPlay page).
    public static class AdRules
    {
        public static float GraceSeconds => ReadFloat("graceSeconds", 0f);
        public static float MinSecondsBetween => ReadFloat("cooldownSeconds", 30f);
        public static BannerPosition DefaultBannerPosition
        {
            get
            {
                var platform = SdkPlatformInfo.Current;
                foreach (var placement in AdCatalog.Current.placements)
                {
                    if (placement.format == AdFormat.Banner && placement.IsAvailableOn(platform)) return placement.position;
                }
                return BannerPosition.Bottom;
            }
        }
        public static RewardedWhenRemoved RewardedWhenRemoved => Read("rewardedWhenRemoved", "give") == "fail" ? SDKHub.RewardedWhenRemoved.Fail : SDKHub.RewardedWhenRemoved.GiveReward;
        static string Read(string key, string fallback)
        {
            var config = SdkHubConfig.Instance;
            var value = config != null ? config.Get("levelplay", key) : "";
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        static float ReadFloat(string key, float fallback)
        {
            float value;
            return float.TryParse(Read(key, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }
    }
}
