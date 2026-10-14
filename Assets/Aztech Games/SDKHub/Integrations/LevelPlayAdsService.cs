#if SDKHUB_LEVELPLAY
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class LevelPlayAdsService : IAdsService
    {
        sealed class Slot
        {
            public AdPlacementEntry Entry;
            public string AdUnitId;
            public LevelPlayRewardedAd Rewarded;
            public LevelPlayInterstitialAd Interstitial;
            public LevelPlayBannerAd Banner;
            public BannerPosition BannerPosition;
            public Action<RewardedResult> RewardCallback;
            public Action ClosedCallback;
            public bool RewardEarned;
            public int Failures;
        }

        readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>();
        AdCatalog catalog;
        bool initializing;
        bool subscribed;
        bool waitingForConsent;

        public event Action Initialized;

        public bool IsInitialized { get; private set; }

        AdCatalog Catalog => catalog ?? (catalog = AdCatalog.Load());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
#if !UNITY_EDITOR
            SdkManager.RegisterAds(new LevelPlayAdsService());
#endif
        }

        public void Initialize()
        {
            if (IsInitialized || initializing || waitingForConsent) return;
            waitingForConsent = true;
            ConsentGate.WhenReady(() =>
            {
                waitingForConsent = false;
                InitializeCore();
            });
        }

        void InitializeCore()
        {
            if (IsInitialized || initializing) return;

            var config = SdkHubConfig.Instance;
            if (config == null)
            {
                Debug.LogError("[SDK Hub] SdkHubConfig asset not found. Open Tools > Aztech Games > SDK Hub.");
                return;
            }

            var platform = SdkPlatformInfo.Current;
            string appKey;
            switch (platform)
            {
                case SdkPlatform.Android: appKey = config.Get("levelplay", "androidAppKey"); break;
                case SdkPlatform.iOS: appKey = config.Get("levelplay", "iosAppKey"); break;
                default: appKey = ""; break;
            }

            if (string.IsNullOrEmpty(appKey))
            {
                Debug.LogWarning("[SDK Hub] LevelPlay app key is empty for " + platform + ". Ads are off.");
                return;
            }

            if (!subscribed)
            {
                subscribed = true;
                LevelPlay.OnInitSuccess += _ => OnInitSuccess();
                LevelPlay.OnInitFailed += error =>
                {
                    initializing = false;
                    Debug.LogError("[SDK Hub] LevelPlay init failed: " + error);
                };
            }

            ApplyConsent();
            initializing = true;
            LevelPlay.Init(appKey);
        }

        static void ApplyConsent()
        {
            var status = SdkManager.Consent.Status;
            if (status == ConsentStatus.Unknown) return;
            var agreed = status == ConsentStatus.Agreed;
            CallStatic(new[] { "SetConsent", "setConsent" }, new[] { typeof(bool) }, agreed);
            CallStatic(new[] { "SetMetaData", "setMetaData" }, new[] { typeof(string), typeof(string) }, "do_not_sell", agreed ? "false" : "true");
        }

        // The consent calls are named differently between LevelPlay versions, so they are looked up by name.
        static void CallStatic(string[] names, Type[] parameters, params object[] arguments)
        {
            foreach (var name in names)
            {
                var method = typeof(LevelPlay).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
                if (method == null) continue;
                try
                {
                    method.Invoke(null, arguments);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[SDK Hub] Could not pass consent to LevelPlay: " + exception.Message);
                }
                return;
            }
        }

        public bool IsAvailable(AdFormat format, string placement)
        {
            var platform = SdkPlatformInfo.Current;
            var entry = Catalog.Resolve(format, placement, platform);
            return entry != null && entry.IsAvailableOn(platform);
        }

        public bool IsReady(AdFormat format, string placement)
        {
            if (!IsInitialized || !IsAvailable(format, placement)) return false;
            if (format == AdFormat.Banner) return true;

            var slot = FindSlot(format, placement);
            if (slot == null) return false;
            return format == AdFormat.Rewarded
                ? slot.Rewarded != null && slot.Rewarded.IsAdReady()
                : slot.Interstitial != null && slot.Interstitial.IsAdReady();
        }

        public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            var slot = FindSlot(AdFormat.Rewarded, placement);
            if (slot == null || slot.Rewarded == null || !slot.Rewarded.IsAdReady())
            {
                slot?.Rewarded?.LoadAd();
                onFinished?.Invoke(RewardedResult.Failed);
                return;
            }
            slot.RewardCallback = onFinished;
            slot.RewardEarned = false;
            slot.Rewarded.ShowAd();
        }

        public void ShowInterstitial(string placement, Action onClosed = null)
        {
            var slot = FindSlot(AdFormat.Interstitial, placement);
            if (slot == null || slot.Interstitial == null || !slot.Interstitial.IsAdReady())
            {
                slot?.Interstitial?.LoadAd();
                onClosed?.Invoke();
                return;
            }
            slot.ClosedCallback = onClosed;
            slot.Interstitial.ShowAd();
        }

        public void ShowBanner(string placement, BannerPosition position = BannerPosition.Bottom)
        {
            if (!IsInitialized) return;

            var platform = SdkPlatformInfo.Current;
            var entry = Catalog.Resolve(AdFormat.Banner, placement, platform);
            if (entry == null || !entry.IsAvailableOn(platform)) return;

            var key = Key(AdFormat.Banner, entry.name);
            Slot slot;
            if (slots.TryGetValue(key, out slot) && slot.Banner != null)
            {
                if (slot.BannerPosition == position)
                {
                    slot.Banner.ShowAd();
                    return;
                }
                slot.Banner.DestroyAd();
            }

            slot = new Slot { Entry = entry, AdUnitId = entry.IdFor(platform), BannerPosition = position };
            var config = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.CreateAdaptiveAdSize())
                .SetPosition(position == BannerPosition.Top ? LevelPlayBannerPosition.TopCenter : LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(true)
                .SetRespectSafeArea(true)
                .Build();
            slot.Banner = new LevelPlayBannerAd(slot.AdUnitId, config);
            slots[key] = slot;
            slot.Banner.LoadAd();
        }

        public void HideBanner(string placement)
        {
            var slot = FindBannerSlot(placement);
            slot?.Banner?.HideAd();
        }

        public void DestroyBanner(string placement)
        {
            var slot = FindBannerSlot(placement);
            if (slot == null) return;
            slot.Banner?.DestroyAd();
            slots.Remove(Key(AdFormat.Banner, slot.Entry.name));
        }

        void OnInitSuccess()
        {
            initializing = false;
            IsInitialized = true;

            var platform = SdkPlatformInfo.Current;
            foreach (var entry in Catalog.placements)
            {
                if (entry.format == AdFormat.Banner || !entry.IsAvailableOn(platform)) continue;
                var slot = new Slot { Entry = entry, AdUnitId = entry.IdFor(platform) };
                slots[Key(entry.format, entry.name)] = slot;
                if (entry.format == AdFormat.Rewarded) CreateRewarded(slot);
                else CreateInterstitial(slot);
            }

            Debug.Log($"[SDK Hub] LevelPlay initialized on {platform}. Loading {slots.Count} ad(s).");
            Initialized?.Invoke();
        }

        void CreateRewarded(Slot slot)
        {
            var ad = new LevelPlayRewardedAd(slot.AdUnitId);
            slot.Rewarded = ad;
            ad.OnAdLoaded += info =>
            {
                slot.Failures = 0;
                Debug.Log($"[SDK Hub] Rewarded '{slot.Entry.name}' is ready.");
            };
            ad.OnAdLoadFailed += error =>
            {
                Debug.LogWarning($"[SDK Hub] Rewarded '{slot.Entry.name}' failed to load: {error}");
                RetryLater(slot, () => ad.LoadAd());
            };
            ad.OnAdRewarded += (info, reward) => slot.RewardEarned = true;
            ad.OnAdClosed += info => FinishRewarded(slot, false);
            ad.OnAdDisplayFailed += (info, error) =>
            {
                slot.RewardEarned = false;
                FinishRewarded(slot, true);
            };
            ad.LoadAd();
        }

        void CreateInterstitial(Slot slot)
        {
            var ad = new LevelPlayInterstitialAd(slot.AdUnitId);
            slot.Interstitial = ad;
            ad.OnAdLoaded += info =>
            {
                slot.Failures = 0;
                Debug.Log($"[SDK Hub] Interstitial '{slot.Entry.name}' is ready.");
            };
            ad.OnAdLoadFailed += error =>
            {
                Debug.LogWarning($"[SDK Hub] Interstitial '{slot.Entry.name}' failed to load: {error}");
                RetryLater(slot, () => ad.LoadAd());
            };
            ad.OnAdClosed += info => FinishInterstitial(slot);
            ad.OnAdDisplayFailed += (info, error) => FinishInterstitial(slot);
            ad.LoadAd();
        }

        void FinishRewarded(Slot slot, bool displayFailed)
        {
            var callback = slot.RewardCallback;
            slot.RewardCallback = null;

            RewardedResult result;
            if (displayFailed) result = RewardedResult.Failed;
            else result = slot.RewardEarned ? RewardedResult.Success : RewardedResult.Cancelled;

            callback?.Invoke(result);
            slot.Rewarded.LoadAd();
        }

        void FinishInterstitial(Slot slot)
        {
            var callback = slot.ClosedCallback;
            slot.ClosedCallback = null;
            callback?.Invoke();
            slot.Interstitial.LoadAd();
        }

        async void RetryLater(Slot slot, Action load)
        {
            slot.Failures++;
            var seconds = Math.Min(60, 5 * (1 << Math.Min(slot.Failures - 1, 4)));
            await Task.Delay(seconds * 1000);
            load();
        }

        Slot FindSlot(AdFormat format, string placement)
        {
            var entry = Catalog.Resolve(format, placement, SdkPlatformInfo.Current);
            if (entry == null) return null;
            Slot slot;
            return slots.TryGetValue(Key(format, entry.name), out slot) ? slot : null;
        }

        Slot FindBannerSlot(string placement)
        {
            return FindSlot(AdFormat.Banner, placement);
        }

        static string Key(AdFormat format, string name)
        {
            return format + ":" + name;
        }
    }
}
#endif
