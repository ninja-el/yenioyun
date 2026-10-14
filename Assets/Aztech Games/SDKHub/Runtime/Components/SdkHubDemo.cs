using System;
using UnityEngine;

namespace SDKHub.Components
{
    // The demo scene's buttons call these methods.
    // Read them as examples of how to use the SDK Hub from your own scripts.
    [SdkHubHelp("Used by the demo scene. Each method shows how to call the SDK Hub from your own scripts.")]
    public class SdkHubDemo : SdkHubBehaviour
    {
        [HideInInspector]
        [SerializeField] SdkHubDemoLog log;

        public void ShowBanner()
        {
            SdkAds.ShowBanner();
            Log("Banner: show requested");
        }

        public void HideBanner()
        {
            SdkAds.HideBanner();
            Log("Banner: hide requested");
        }

        public void RemoveAllAds()
        {
            SdkAds.RemoveAds();
            Log("Ads removed. This is saved on the device.");
        }

        public void RestoreAds()
        {
            SdkAds.RestoreAds();
            Log("Ads restored.");
        }

        public void NotifyIn10Seconds()
        {
            SdkManager.Notifications.RequestPermission();
            SdkManager.Notifications.Schedule("SDK Hub demo", "Scheduled from the demo scene.", TimeSpan.FromSeconds(10));
            Log("Notification scheduled for 10 seconds from now");
        }

        public void SendAnalyticsEvent()
        {
            SdkManager.Analytics.LogEvent("demo_button");
            Log("Analytics event 'demo_button' sent (only after the player agrees)");
        }

        public void SubmitScore()
        {
            var config = SdkHubConfig.Instance;
            var catalog = LeaderboardCatalog.Parse(config != null ? config.Get("leaderboards", "ids") : "");
            var board = catalog.ids.Count > 0 ? catalog.ids[0] : "demo";
            SdkManager.Leaderboards.Submit(board, 100, success => Log(success ? "Score 100 submitted" : "Score could not be submitted"));
        }

        public void CheckForUpdate()
        {
            SdkManager.Updates.CheckAndUpdate(AppUpdateMode.Flexible, result => Log("Update check: " + result));
        }

        void Log(string message)
        {
            if (log != null) log.LogMessage(message);
        }
    }
}
