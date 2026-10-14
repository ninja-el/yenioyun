using UnityEngine;

namespace SDKHub
{
    public static class SdkManager
    {
        static IAdsService adsCore;
        static AdsFacade adsFacade;
        static IIapService iap;
        static INotificationService notifications;
        static IPushService push;
        static IFacebookService facebook;
        static IConsentService consent;
        static IAnalyticsService analytics;
        static IAccountService accounts;
        static ILeaderboardService leaderboards;
        static IReviewService review;
        static IAppUpdateService updates;

        // Ads go through a layer that starts them automatically and applies "remove ads" and the interstitial rules.
        public static IAdsService Ads
        {
            get
            {
                if (adsFacade == null) adsFacade = new AdsFacade(adsCore ?? (adsCore = new NullAdsService()));
                return adsFacade;
            }
        }
        public static IIapService Iap => iap ?? (iap = new NullIapService());
        public static INotificationService Notifications => notifications ?? (notifications = new NullNotificationService());
        public static IPushService Push => push ?? (push = new NullPushService());
        public static IFacebookService Facebook => facebook ?? (facebook = new NullFacebookService());
        public static IConsentService Consent => consent ?? (consent = new ConsentService());
        public static IAnalyticsService Analytics => analytics ?? (analytics = new NullAnalyticsService());
        public static IAccountService Accounts => accounts ?? (accounts = new NullAccountService());
        public static ILeaderboardService Leaderboards => leaderboards ?? (leaderboards = new NullLeaderboardService());
        public static IReviewService Review => review ?? (review = new NullReviewService());
        public static IAppUpdateService Updates => updates ?? (updates = new NullAppUpdateService());

        public static void RegisterAds(IAdsService service)
        {
            adsFacade?.Dispose();
            adsFacade = null;
            adsCore = service;
        }
        public static void RegisterIap(IIapService service) { iap = service; }
        public static void RegisterNotifications(INotificationService service) { notifications = service; }
        public static void RegisterPush(IPushService service) { push = service; }
        public static void RegisterFacebook(IFacebookService service) { facebook = service; }
        public static void RegisterAnalytics(IAnalyticsService service) { analytics = service; }
        public static void RegisterAccounts(IAccountService service) { accounts = service; }
        public static void RegisterLeaderboards(ILeaderboardService service) { leaderboards = service; }
        public static void RegisterReview(IReviewService service) { review = service; }
        public static void RegisterUpdates(IAppUpdateService service) { updates = service; }

        // Keeps results correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            adsFacade?.Dispose();
            adsFacade = null;
            adsCore = null;
            iap = null;
            notifications = null;
            push = null;
            facebook = null;
            consent = null;
            analytics = null;
            accounts = null;
            leaderboards = null;
            review = null;
            updates = null;
            ConsentService.TrackingRequester = null;
        }
    }
}
