#if SDKHUB_REVIEW
using System;
using UnityEngine;
#if UNITY_ANDROID
using System.Collections;
using Google.Play.Review;
#endif

namespace SDKHub.Integrations
{
    public sealed class ReviewService : IReviewService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
#if !UNITY_EDITOR
            SdkManager.RegisterReview(new ReviewService());
#endif
        }

        public void RequestReview(Action<bool> onFinished = null)
        {
#if UNITY_ANDROID
            SdkHubRunner.Run(AndroidFlow(onFinished));
#elif UNITY_IOS
            UnityEngine.iOS.Device.RequestStoreReview();
            onFinished?.Invoke(true);
#else
            Debug.Log("[SDK Hub] Review prompts only work on Android and iOS devices.");
            onFinished?.Invoke(false);
#endif
        }

#if UNITY_ANDROID
        static IEnumerator AndroidFlow(Action<bool> onFinished)
        {
            var manager = new ReviewManager();
            var request = manager.RequestReviewFlow();
            yield return request;
            if (request.Error != ReviewErrorCode.NoError)
            {
                Debug.LogWarning("[SDK Hub] Review request failed: " + request.Error);
                onFinished?.Invoke(false);
                yield break;
            }

            var launch = manager.LaunchReviewFlow(request.GetResult());
            yield return launch;
            onFinished?.Invoke(launch.Error == ReviewErrorCode.NoError);
        }
#endif
    }
}
#endif
