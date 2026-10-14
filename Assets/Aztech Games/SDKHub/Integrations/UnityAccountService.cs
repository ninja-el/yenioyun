#if SDKHUB_ACCOUNTS
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace SDKHub.Integrations
{
    public sealed class UnityAccountService : IAccountService
    {
        bool signedIn;

        public event Action SignedIn;

        public bool IsSignedIn => signedIn;
        public string PlayerId { get; private set; } = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SdkManager.RegisterAccounts(new UnityAccountService());
        }

        public async void SignIn(Action<bool> onFinished = null)
        {
            var success = await UgsBootstrap.SignInAsync();
            if (success && !signedIn)
            {
                signedIn = true;
                PlayerId = AuthenticationService.Instance.PlayerId;
                Debug.Log("[SDK Hub] Signed in. Player ID: " + PlayerId);
                SignedIn?.Invoke();
            }
            onFinished?.Invoke(success);
        }

        public async void SaveText(string key, string value, Action<bool> onFinished = null)
        {
            if (!await UgsBootstrap.SignInAsync())
            {
                onFinished?.Invoke(false);
                return;
            }

            try
            {
                var data = new Dictionary<string, object> { { key, value ?? "" } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                onFinished?.Invoke(true);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SDK Hub] Cloud save failed: " + exception.Message);
                onFinished?.Invoke(false);
            }
        }

        public async void LoadText(string key, Action<bool, string> onFinished)
        {
            if (!await UgsBootstrap.SignInAsync())
            {
                onFinished?.Invoke(false, "");
                return;
            }

            try
            {
                var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                Item item;
                if (data.TryGetValue(key, out item)) onFinished?.Invoke(true, item.Value.GetAsString());
                else onFinished?.Invoke(false, "");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SDK Hub] Cloud load failed: " + exception.Message);
                onFinished?.Invoke(false, "");
            }
        }
    }
}
#endif
