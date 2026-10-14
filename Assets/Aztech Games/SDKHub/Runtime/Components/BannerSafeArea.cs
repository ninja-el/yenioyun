using UnityEngine;

namespace SDKHub.Components
{
    [SdkHubHelp("Keeps your game UI clear of the banner. Put this on a full-screen UI object. While a banner shows, the object gets smaller by the banner's height, at the top or bottom to match. It needs no settings.")]
    [AddComponentMenu("SDK Hub/Banner Safe Area")]
    [RequireComponent(typeof(RectTransform))]
    public class BannerSafeArea : SdkHubBehaviour
    {
        // A standard phone banner is 50 dp tall.
        const float BannerHeightDp = 50f;

        RectTransform rect;
        Vector2 baseMin;
        Vector2 baseMax;

        void OnEnable()
        {
            rect = (RectTransform)transform;
            baseMin = rect.offsetMin;
            baseMax = rect.offsetMax;
            SdkAds.BannerStateChanged += Apply;
            Apply();
        }

        void OnDisable()
        {
            SdkAds.BannerStateChanged -= Apply;
            if (rect == null) return;
            rect.offsetMin = baseMin;
            rect.offsetMax = baseMax;
        }

        void Apply()
        {
            var min = baseMin;
            var max = baseMax;

            if (SdkAds.IsBannerVisible)
            {
                var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
                var pixels = BannerHeightDp * dpi / 160f;
                var canvas = GetComponentInParent<Canvas>();
                var units = canvas != null && canvas.scaleFactor > 0f ? pixels / canvas.scaleFactor : pixels;

                if (SdkAds.CurrentBannerPosition == BannerPosition.Top) max.y -= units;
                else min.y += units;
            }

            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
