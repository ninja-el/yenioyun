using System.Collections;
using UnityEngine;

namespace SDKHub
{
    public sealed class SdkHubRunner : MonoBehaviour
    {
        static SdkHubRunner instance;

        // Raised when the player leaves the app (true) or comes back (false).
        public static event System.Action<bool> AppPaused;

        static SdkHubRunner Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("SDK Hub Runner") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SdkHubRunner>();
                return instance;
            }
        }

        public static void EnsureCreated()
        {
            if (Instance == null) return;
        }

        void OnApplicationPause(bool paused)
        {
            AppPaused?.Invoke(paused);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            AppPaused = null;
            instance = null;
        }

        public static Coroutine Run(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }
    }
}
