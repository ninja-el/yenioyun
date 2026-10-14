using System;

namespace SDKHub
{
    // Which kinds of ads an action applies to. Combine them to pick several.
    [Flags]
    public enum AdTypes
    {
        None = 0,
        Interstitial = 1,
        Banner = 2,
        Rewarded = 4,
        All = 7
    }

    // The three ways a rewarded ad can end.
    public enum RewardedResult
    {
        Success,
        Cancelled,
        Failed
    }

    public enum RewardedWhenRemoved
    {
        GiveReward,
        Fail
    }

    public static class AdTypesExtensions
    {
        public static AdTypes ToType(this AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Interstitial: return AdTypes.Interstitial;
                case AdFormat.Banner: return AdTypes.Banner;
                default: return AdTypes.Rewarded;
            }
        }
    }
}
