using System;
using UnityEngine;

namespace SDKHub
{
    public interface IAccountService
    {
        event Action SignedIn;

        bool IsSignedIn { get; }
        string PlayerId { get; }

        void SignIn(Action<bool> onFinished = null);
        void SaveText(string key, string value, Action<bool> onFinished = null);
        void LoadText(string key, Action<bool, string> onFinished);
    }

    // Mock service: keeps data on this device so your game works before Unity Gaming Services is set up.
    public sealed class NullAccountService : IAccountService
    {
        const string Prefix = "SDKHub.LocalSave.";

        public event Action SignedIn;

        public bool IsSignedIn { get; private set; }
        public string PlayerId => IsSignedIn ? "local-player" : "";

        public void SignIn(Action<bool> onFinished = null)
        {
            if (!IsSignedIn)
            {
                IsSignedIn = true;
                Debug.Log("[SDK Hub] Accounts are running in mock mode. Data is saved on this device only.");
                SignedIn?.Invoke();
            }
            onFinished?.Invoke(true);
        }

        public void SaveText(string key, string value, Action<bool> onFinished = null)
        {
            PlayerPrefs.SetString(Prefix + key, value ?? "");
            PlayerPrefs.Save();
            onFinished?.Invoke(true);
        }

        public void LoadText(string key, Action<bool, string> onFinished)
        {
            var exists = PlayerPrefs.HasKey(Prefix + key);
            onFinished?.Invoke(exists, exists ? PlayerPrefs.GetString(Prefix + key) : "");
        }
    }
}
