using System.Collections.Generic;

namespace SDKHub
{
    // Decides when an interstitial may show. Plain code with no Unity calls, so it can be tested on its own.
    public sealed class InterstitialPolicy
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        float lastShownAt = float.NegativeInfinity;

        public float GraceSeconds { get; }
        public float MinSecondsBetween { get; }

        public InterstitialPolicy(float graceSeconds, float minSecondsBetween)
        {
            GraceSeconds = graceSeconds;
            MinSecondsBetween = minSecondsBetween;
        }

        // Call once per request. Returns null when the ad may show, otherwise the reason it must wait.
        public string Check(string placement, int everyNth, float now)
        {
            if (everyNth < 1) everyNth = 1;
            var key = placement ?? "";

            int count;
            counts.TryGetValue(key, out count);
            count++;
            counts[key] = count;

            if (count < everyNth) return $"waiting for request {count} of {everyNth}";
            if (now < GraceSeconds) return $"too early, the start delay has {GraceSeconds - now:0} s left";
            var sinceLast = now - lastShownAt;
            if (sinceLast < MinSecondsBetween) return $"cooldown, {MinSecondsBetween - sinceLast:0} s left";
            return null;
        }

        // Call after an ad was really shown.
        public void MarkShown(string placement, float now)
        {
            counts[placement ?? ""] = 0;
            lastShownAt = now;
        }
    }
}
