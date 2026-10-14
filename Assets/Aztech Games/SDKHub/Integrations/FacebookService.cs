#if SDKHUB_FACEBOOK
using System;
using System.Collections.Generic;
using Facebook.Unity;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class FacebookService : IFacebookService
    {
        static readonly string[] DefaultPermissions = { "public_profile", "email" };

        bool initializing;

        public event Action Initialized;

        public bool IsInitialized => FB.IsInitialized;
        public bool IsLoggedIn => FB.IsInitialized && FB.IsLoggedIn;
        public string UserId => IsLoggedIn ? Facebook.Unity.AccessToken.CurrentAccessToken.UserId : "";
        public string AccessTokenString => IsLoggedIn ? Facebook.Unity.AccessToken.CurrentAccessToken.TokenString : "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterFacebook(new FacebookService());
        }

        public void Initialize()
        {
            if (initializing) return;
            initializing = true;
            ConsentGate.WhenReady(() =>
            {
                if (ConsentGate.Required && SdkManager.Consent.Status != ConsentStatus.Agreed)
                {
                    initializing = false;
                    Debug.Log("[SDK Hub] Facebook is off because the player did not agree to privacy terms.");
                    return;
                }
                if (FB.IsInitialized)
                {
                    OnInitComplete();
                    return;
                }
                FB.Init(OnInitComplete);
            });
        }

        public void Login(Action<bool> onFinished, string[] permissions = null)
        {
            if (!FB.IsInitialized)
            {
                onFinished?.Invoke(false);
                return;
            }
            var list = new List<string>(permissions ?? DefaultPermissions);
            FB.LogInWithReadPermissions(list, result => onFinished?.Invoke(FB.IsLoggedIn));
        }

        public void Logout()
        {
            if (FB.IsInitialized) FB.LogOut();
        }

        public void LogEvent(string eventName, float? valueToSum = null)
        {
            if (FB.IsInitialized) FB.LogAppEvent(eventName, valueToSum);
        }

        void OnInitComplete()
        {
            initializing = false;
            if (!FB.IsInitialized)
            {
                Debug.LogError("[SDK Hub] Failed to initialize the Facebook SDK. Check your App ID in the SDK Hub.");
                return;
            }
            FB.ActivateApp();
            Initialized?.Invoke();
        }
    }
}
#endif
