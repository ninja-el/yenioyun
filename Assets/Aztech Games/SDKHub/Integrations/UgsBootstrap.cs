#if SDKHUB_PUSH || SDKHUB_ANALYTICS || SDKHUB_ACCOUNTS || SDKHUB_LEADERBOARDS
using System;
using System.Threading.Tasks;
using Unity.Services.Core;
#if SDKHUB_ACCOUNTS || SDKHUB_LEADERBOARDS
using Unity.Services.Authentication;
#endif
using UnityEngine;

namespace SDKHub.Integrations
{
    // Shared by every Unity Gaming Services integration so services start only once.
    static class UgsBootstrap
    {
        static Task initialization;

        public static Task InitializeAsync()
        {
            if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
            {
                initialization = UnityServices.InitializeAsync();
            }
            return initialization;
        }

#if SDKHUB_ACCOUNTS || SDKHUB_LEADERBOARDS
        static Task<bool> signIn;

        public static Task<bool> SignInAsync()
        {
            if (signIn == null) signIn = DoSignIn();
            return signIn;
        }

        static async Task<bool> DoSignIn()
        {
            try
            {
                await InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[SDK Hub] Sign-in failed: " + exception.Message);
                signIn = null;
                return false;
            }
        }
#endif
    }
}
#endif
