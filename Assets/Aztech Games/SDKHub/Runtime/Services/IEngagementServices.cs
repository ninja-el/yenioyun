using System;
using UnityEngine;

namespace SDKHub
{
    public interface IReviewService
    {
        void RequestReview(Action<bool> onFinished = null);
    }

    public sealed class NullReviewService : IReviewService
    {
        public void RequestReview(Action<bool> onFinished = null)
        {
            Debug.Log("[SDK Hub] Review prompt (mock). It only appears on a real device with the store plugin installed.");
            onFinished?.Invoke(false);
        }
    }

    public enum AppUpdateMode { Flexible, Immediate }

    public enum AppUpdateResult { NoUpdate, Started, Failed, NotSupported }

    public interface IAppUpdateService
    {
        void CheckAndUpdate(AppUpdateMode mode, Action<AppUpdateResult> onResult);
    }

    public sealed class NullAppUpdateService : IAppUpdateService
    {
        public void CheckAndUpdate(AppUpdateMode mode, Action<AppUpdateResult> onResult)
        {
            Debug.Log("[SDK Hub] App update check (mock). It only runs on Android devices from Google Play.");
            onResult?.Invoke(AppUpdateResult.NotSupported);
        }
    }
}
