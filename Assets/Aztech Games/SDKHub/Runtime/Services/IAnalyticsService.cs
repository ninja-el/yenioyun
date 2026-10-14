using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public interface IAnalyticsService
    {
        bool IsStarted { get; }

        // Starts data collection. Does nothing until the player has agreed in the privacy panel.
        void Initialize();
        void LogEvent(string eventName);
        void LogEvent(string eventName, IDictionary<string, object> parameters);
    }

    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public bool IsStarted { get; private set; }

        public void Initialize()
        {
            if (SdkManager.Consent.Status != ConsentStatus.Agreed) return;
            IsStarted = true;
            Debug.Log("[SDK Hub] Analytics is running in mock mode (Unity Analytics is not installed).");
        }

        public void LogEvent(string eventName)
        {
            Debug.Log("[SDK Hub] Analytics event: " + eventName);
        }

        public void LogEvent(string eventName, IDictionary<string, object> parameters)
        {
            Debug.Log($"[SDK Hub] Analytics event: {eventName} ({(parameters != null ? parameters.Count : 0)} parameter(s))");
        }
    }
}
