using System;
using System.Globalization;
using UnityEngine;

namespace SDKHub
{
    // Starts what you set up in the hub when the game launches, so nothing has to be started by hand.
    static class SdkHubAutoStart
    {
        static bool reminderHooked;
        static int reminderId = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            reminderHooked = false;
            reminderId = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            var catalog = AdCatalog.Current;
            if (catalog.placements.Count > 0)
            {
                SdkManager.Ads.Initialize();
                StartBanner(catalog);
            }

            if (IapCatalog.Parse(Read("iap", "catalog")).products.Count > 0) SdkManager.Iap.Initialize();
            if (IsReal(SdkManager.Analytics)) SdkManager.Analytics.Initialize();
            if (IsReal(SdkManager.Facebook)) SdkManager.Facebook.Initialize();

            StartReminder();
            StartUpdateCheck();
        }

        static void StartBanner(AdCatalog catalog)
        {
            var platform = SdkPlatformInfo.Current;
            foreach (var placement in catalog.placements)
            {
                if (placement.format != AdFormat.Banner || !placement.showAtLaunch || !placement.IsAvailableOn(platform)) continue;
                SdkAds.ShowBanner(placement.position, placement.name);
                return;
            }
        }

        static void StartReminder()
        {
            if (reminderHooked || Read("notifications", "reminderEnabled") != "1") return;
            reminderHooked = true;

            SdkHubRunner.EnsureCreated();
            SdkHubRunner.AppPaused += OnAppPaused;
            SdkManager.Notifications.RequestPermission();
        }

        static void OnAppPaused(bool paused)
        {
            var notifications = SdkManager.Notifications;
            if (paused)
            {
                float hours;
                if (!float.TryParse(Read("notifications", "reminderHours"), NumberStyles.Float, CultureInfo.InvariantCulture, out hours)) hours = 24f;
                var title = ReadOr("notifications", "reminderTitle", "We miss you!");
                var text = ReadOr("notifications", "reminderText", "Come back and play.");
                reminderId = notifications.Schedule(title, text, TimeSpan.FromHours(Mathf.Max(0.01f, hours)));
            }
            else if (reminderId >= 0)
            {
                notifications.Cancel(reminderId);
                reminderId = -1;
            }
        }

        static void StartUpdateCheck()
        {
            var mode = Read("updates", "startupCheck");
            if (mode == "flexible") SdkManager.Updates.CheckAndUpdate(AppUpdateMode.Flexible, null);
            else if (mode == "immediate") SdkManager.Updates.CheckAndUpdate(AppUpdateMode.Immediate, null);
        }

        // Mock services are named Null...; only real SDKs are started by themselves.
        static bool IsReal(object service)
        {
            return !service.GetType().Name.StartsWith("Null", StringComparison.Ordinal);
        }

        static string Read(string module, string key)
        {
            var config = SdkHubConfig.Instance;
            return config != null ? config.Get(module, key) : "";
        }

        static string ReadOr(string module, string key, string fallback)
        {
            var value = Read(module, key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
