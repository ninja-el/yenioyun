using System;

namespace SDKHub
{
    // Lets services wait for the player's privacy answer before they start.
    public static class ConsentGate
    {
        public static bool Required
        {
            get
            {
                var config = SdkHubConfig.Instance;
                return config != null && config.Get("privacy", "requireConsent") == "1";
            }
        }

        // Runs start now when no answer is needed, otherwise once the player has answered.
        public static void WhenReady(Action start)
        {
            var consent = SdkManager.Consent;
            if (!Required || consent.HasAnswered)
            {
                start();
                return;
            }

            Action<ConsentStatus> handler = null;
            handler = status =>
            {
                if (status == ConsentStatus.Unknown) return;
                consent.Changed -= handler;
                start();
            };
            consent.Changed += handler;
        }
    }
}
