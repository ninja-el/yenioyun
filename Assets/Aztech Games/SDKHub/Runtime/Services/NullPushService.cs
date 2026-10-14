using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public sealed class NullPushService : IPushService
    {
        public event Action Initialized;
        public event Action<string> TokenReceived;
        public event Action<IDictionary<string, object>> MessageReceived;

        public bool IsInitialized { get; private set; }
        public string Token => "";

        public void Initialize(bool analyticsConsentGiven)
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Debug.Log("[SDK Hub] Push is running in mock mode (Push Notifications package is not installed).");
            Initialized?.Invoke();
        }
    }
}
