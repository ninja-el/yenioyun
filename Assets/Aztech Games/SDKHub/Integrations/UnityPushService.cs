#if SDKHUB_PUSH
using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.PushNotifications;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class UnityPushService : IPushService
    {
        bool initializing;

        public event Action Initialized;
        public event Action<string> TokenReceived;
        public event Action<IDictionary<string, object>> MessageReceived;

        public bool IsInitialized { get; private set; }
        public string Token { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterPush(new UnityPushService());
        }

        public void Initialize(bool analyticsConsentGiven)
        {
            if (IsInitialized || initializing) return;
            initializing = true;
            ConsentGate.WhenReady(() => Start(analyticsConsentGiven));
        }

        async void Start(bool analyticsConsentGiven)
        {
            var status = SdkManager.Consent.Status;
            if (ConsentGate.Required && status != ConsentStatus.Agreed)
            {
                initializing = false;
                Debug.Log("[SDK Hub] Push is off because the player did not agree to privacy terms.");
                return;
            }

            try
            {
                await UgsBootstrap.InitializeAsync();

                if (analyticsConsentGiven || status == ConsentStatus.Agreed) AnalyticsService.Instance.StartDataCollection();
                else Debug.LogWarning("[SDK Hub] Analytics consent was not given. Unity Push may not work without Analytics data collection.");

                PushNotificationsService.Instance.OnRemoteNotificationReceived += data =>
                    MessageReceived?.Invoke((object)data as IDictionary<string, object>);

                Token = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
                IsInitialized = true;
                TokenReceived?.Invoke(Token);
                Initialized?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SDK Hub] Push registration failed: " + exception.Message);
            }
            finally
            {
                initializing = false;
            }
        }
    }
}
#endif
