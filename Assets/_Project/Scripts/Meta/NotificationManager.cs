using System;
using System.Collections;
using MatchPack.Localization;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    private const string CHANNEL_ID = "game_channel";

    private void Awake()
    {
        // Singleton yapısı (Sahneden sahneye geçişte yok olmaması için)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeNotifications();
        
        // Oyuna girdiğimizde daha önceden kurulan bildirimleri temizleyelim (çünkü artık oyundayız)
        CancelAllNotifications();
    }

    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        // Android 13 ve üzeri için bildirim izni isteme
        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }

        // Android Kanalı Oluşturma
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "Oyun İçi Bildirimler",
            Importance = Importance.High,
            Description = "Can durumu ve geri dönüş hatırlatıcıları"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#elif UNITY_IOS
        // iOS için bildirim izni isteme
        StartCoroutine(RequestIOSPermissions());
#endif
    }

#if UNITY_IOS
    private IEnumerator RequestIOSPermissions()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }
        }
    }
#endif

    // Oyundan çıkıldığında (veya arka plana atıldığında) bildirimleri zamanla
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            // 1. 2 Günlük İnaktiflik Bildirimini Kur
            ScheduleInactivityNotification();

            // 2. Can (Enerji) Bildirimini Kur
            int minutesUntilFullHealth = 200; 
            
            if (minutesUntilFullHealth > 0)
            {
                ScheduleHealthNotification(minutesUntilFullHealth);
            }
        }
        else
        {
            // Oyuna geri dönüldüğünde bildirimleri sil
            CancelAllNotifications();
        }
    }

    private void ScheduleHealthNotification(int minutesUntilFull)
    {
        string title = Loc.Get("notification.lives_full.title");
        string text = Loc.Get("notification.lives_full.body");
        DateTime fireTime = DateTime.Now.AddMinutes(minutesUntilFull);

        SendNotification(title, text, fireTime, "health_id");
    }

    private void ScheduleInactivityNotification()
    {
        string title = Loc.Get("notification.inactivity.title");
        string text = Loc.Get("notification.inactivity.body");
        DateTime fireTime = DateTime.Now.AddDays(2); // 48 saat sonrası

        SendNotification(title, text, fireTime, "inactivity_id");
    }

    private void SendNotification(string title, string text, DateTime fireTime, string idString)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = fireTime,
            SmallIcon = "icon_small", 
            LargeIcon = "icon_small" 
        };
        
        // idString.GetHashCode() kullanarak string'i int id'ye çeviriyoruz
        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, CHANNEL_ID, idString.GetHashCode());

#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = fireTime - DateTime.Now,
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = idString,
            Title = title,
            Body = text,
            ShowInForeground = false, 
            Badge = 1,
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    private void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.ApplicationBadge = 0; 
#endif
    }
}