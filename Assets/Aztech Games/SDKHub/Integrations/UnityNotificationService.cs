#if SDKHUB_NOTIFICATIONS
using System;
using Unity.Notifications;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class UnityNotificationService : INotificationService
    {
        bool initialized;
        int mockId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterNotifications(new UnityNotificationService());
        }

        public void Initialize()
        {
            if (initialized) return;
#if UNITY_ANDROID || UNITY_IOS
            var config = SdkHubConfig.Instance;
            var args = NotificationCenterArgs.Default;
            args.AndroidChannelId = Read(config, "channelId", "default");
            args.AndroidChannelName = Read(config, "channelName", "Notifications");
            var description = Read(config, "channelDescription", "");
            if (!string.IsNullOrEmpty(description)) args.AndroidChannelDescription = description;
            NotificationCenter.Initialize(args);
#endif
            initialized = true;
        }

        public void RequestPermission()
        {
            Initialize();
#if UNITY_ANDROID || UNITY_IOS
            NotificationCenter.RequestPermission();
#else
            Debug.Log("[SDK Hub] Notification permission is only requested on Android and iOS.");
#endif
        }

        public int Schedule(string title, string text, TimeSpan delay)
        {
            Initialize();
#if UNITY_ANDROID || UNITY_IOS
            var notification = new Notification { Title = title, Text = text };
            return NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule(delay));
#else
            Debug.Log($"[SDK Hub] Notification '{title}' would show in {delay.TotalSeconds:0}s on a device.");
            return ++mockId;
#endif
        }

        public int ScheduleAt(string title, string text, DateTime localTime)
        {
            Initialize();
#if UNITY_ANDROID || UNITY_IOS
            var notification = new Notification { Title = title, Text = text };
            return NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(localTime));
#else
            Debug.Log($"[SDK Hub] Notification '{title}' would show at {localTime} on a device.");
            return ++mockId;
#endif
        }

        public void Cancel(int id)
        {
#if UNITY_ANDROID || UNITY_IOS
            NotificationCenter.CancelScheduledNotification(id);
            NotificationCenter.CancelDeliveredNotification(id);
#endif
        }

        public void CancelAll()
        {
#if UNITY_ANDROID || UNITY_IOS
            NotificationCenter.CancelAllScheduledNotifications();
            NotificationCenter.CancelAllDeliveredNotifications();
#endif
        }

        static string Read(SdkHubConfig config, string key, string fallback)
        {
            var value = config != null ? config.Get("notifications", key) : "";
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
#endif
