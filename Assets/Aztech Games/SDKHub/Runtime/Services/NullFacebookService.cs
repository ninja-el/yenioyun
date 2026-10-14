using System;
using UnityEngine;

namespace SDKHub
{
    public sealed class NullFacebookService : IFacebookService
    {
        public event Action Initialized;

        public bool IsInitialized { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public string UserId => IsLoggedIn ? "mock-user" : "";
        public string AccessTokenString => IsLoggedIn ? "mock-token" : "";

        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            Debug.Log("[SDK Hub] Facebook is running in mock mode (Facebook SDK is not installed).");
            Initialized?.Invoke();
        }

        public void Login(Action<bool> onFinished, string[] permissions = null)
        {
            IsLoggedIn = Application.isEditor;
            onFinished?.Invoke(IsLoggedIn);
        }

        public void Logout()
        {
            IsLoggedIn = false;
        }

        public void LogEvent(string eventName, float? valueToSum = null)
        {
            Debug.Log("[SDK Hub] Facebook mock event: " + eventName);
        }
    }
}
