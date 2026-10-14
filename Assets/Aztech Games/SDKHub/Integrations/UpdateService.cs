#if SDKHUB_UPDATES
using System;
using UnityEngine;
#if UNITY_ANDROID
using System.Collections;
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

namespace SDKHub.Integrations
{
    public sealed class UpdateService : IAppUpdateService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
#if !UNITY_EDITOR
            SdkManager.RegisterUpdates(new UpdateService());
#endif
        }

        public void CheckAndUpdate(AppUpdateMode mode, Action<AppUpdateResult> onResult)
        {
#if UNITY_ANDROID
            SdkHubRunner.Run(Flow(mode, onResult));
#else
            onResult?.Invoke(AppUpdateResult.NotSupported);
#endif
        }

#if UNITY_ANDROID
        static IEnumerator Flow(AppUpdateMode mode, Action<AppUpdateResult> onResult)
        {
            var manager = new AppUpdateManager();
            var infoOperation = manager.GetAppUpdateInfo();
            yield return infoOperation;
            if (infoOperation.Error != AppUpdateErrorCode.NoError)
            {
                Debug.LogWarning("[SDK Hub] Update check failed: " + infoOperation.Error);
                onResult?.Invoke(AppUpdateResult.Failed);
                yield break;
            }

            var info = infoOperation.GetResult();
            if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable)
            {
                onResult?.Invoke(AppUpdateResult.NoUpdate);
                yield break;
            }

            var options = mode == AppUpdateMode.Immediate
                ? AppUpdateOptions.ImmediateAppUpdateOptions()
                : AppUpdateOptions.FlexibleAppUpdateOptions();
            if (!info.IsUpdateTypeAllowed(options))
            {
                onResult?.Invoke(AppUpdateResult.NoUpdate);
                yield break;
            }

            var start = manager.StartUpdate(info, options);
            yield return start;
            if (start.Error != AppUpdateErrorCode.NoError)
            {
                onResult?.Invoke(AppUpdateResult.Failed);
                yield break;
            }

            onResult?.Invoke(AppUpdateResult.Started);
            if (mode == AppUpdateMode.Flexible)
            {
                var complete = manager.CompleteUpdate();
                yield return complete;
            }
        }
#endif
    }
}
#endif
