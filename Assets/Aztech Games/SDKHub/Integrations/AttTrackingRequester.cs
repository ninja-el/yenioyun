#if SDKHUB_ATT && UNITY_IOS
using System;
using System.Threading.Tasks;
using Unity.Advertisement.IosSupport;
using UnityEngine;

namespace SDKHub.Integrations
{
    static class AttTrackingRequester
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            ConsentService.TrackingRequester = Request;
        }

        static async void Request(Action<bool> onFinished)
        {
            var notDetermined = ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == notDetermined)
            {
                ATTrackingStatusBinding.RequestAuthorizationTracking();
                while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == notDetermined)
                {
                    await Task.Delay(250);
                }
            }

            var authorized = ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
            onFinished?.Invoke(authorized);
        }
    }
}
#endif
