using System;
using System.Collections.Generic;

namespace SDKHub
{
    public interface IPushService
    {
        event Action Initialized;
        event Action<string> TokenReceived;
        event Action<IDictionary<string, object>> MessageReceived;

        bool IsInitialized { get; }
        string Token { get; }

        void Initialize(bool analyticsConsentGiven);
    }
}
