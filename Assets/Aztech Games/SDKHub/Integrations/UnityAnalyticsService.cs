#if SDKHUB_ANALYTICS
using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class UnityAnalyticsService : IAnalyticsService
    {
        bool starting;
        bool waiting;

        public bool IsStarted { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterAnalytics(new UnityAnalyticsService());
        }

        public async void Initialize()
        {
            if (IsStarted || starting) return;

            var consent = SdkManager.Consent;
            if (consent.Status != ConsentStatus.Agreed)
            {
                if (!waiting)
                {
                    waiting = true;
                    consent.Changed += OnConsentChanged;
                }
                return;
            }

            starting = true;
            try
            {
                await UgsBootstrap.InitializeAsync();
                AnalyticsService.Instance.StartDataCollection();
                IsStarted = true;
                Debug.Log("[SDK Hub] Analytics started.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SDK Hub] Analytics failed to start: " + exception.Message);
            }
            finally
            {
                starting = false;
            }
        }

        public void LogEvent(string eventName)
        {
            if (!IsStarted) return;
            AnalyticsService.Instance.RecordEvent(eventName);
        }

        public void LogEvent(string eventName, IDictionary<string, object> parameters)
        {
            if (!IsStarted) return;
            var customEvent = new CustomEvent(eventName);
            if (parameters != null)
            {
                foreach (var pair in parameters) customEvent.Add(pair.Key, pair.Value);
            }
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        void OnConsentChanged(ConsentStatus status)
        {
            if (status != ConsentStatus.Agreed) return;
            SdkManager.Consent.Changed -= OnConsentChanged;
            waiting = false;
            Initialize();
        }
    }
}
#endif
