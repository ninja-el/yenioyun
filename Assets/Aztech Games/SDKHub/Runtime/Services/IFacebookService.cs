using System;

namespace SDKHub
{
    public interface IFacebookService
    {
        event Action Initialized;

        bool IsInitialized { get; }
        bool IsLoggedIn { get; }
        string UserId { get; }
        string AccessTokenString { get; }

        void Initialize();
        void Login(Action<bool> onFinished, string[] permissions = null);
        void Logout();
        void LogEvent(string eventName, float? valueToSum = null);
    }
}
