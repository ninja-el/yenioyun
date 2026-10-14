using System;
using UnityEditor;

namespace SDKHub.Editor
{
    // Tracks when cached module results (status, checklist) must be recalculated.
    [InitializeOnLoad]
    public static class SdkHubState
    {
        public static int Version { get; private set; }

        // Raised only for outside changes (returning to Unity). Typing in the hub never raises it.
        public static event Action Invalidated;

        static SdkHubState()
        {
            EditorApplication.focusChanged += focused =>
            {
                if (focused) Invalidate();
            };
            EditorApplication.projectChanged += () =>
            {
                Bump();
                SdkHubConfigProvider.ResetSearch();
            };
        }

        public static void Bump()
        {
            Version++;
        }

        public static void Invalidate()
        {
            Version++;
            SdkHubConfigProvider.ResetSearch();
            Invalidated?.Invoke();
        }
    }
}
