using System;
using NUnit.Framework;

namespace SDKHub.Tests
{
    public class AdsFacadeTests
    {
        sealed class FakeAds : IAdsService
        {
            public event Action Initialized;

            public bool IsInitialized { get; private set; }
            public bool Ready = true;
            public int InterstitialsShown;
            public int RewardedShown;
            public int BannersShown;
            public int BannersDestroyed;

            public void Initialize()
            {
                if (IsInitialized) return;
                IsInitialized = true;
                Initialized?.Invoke();
            }

            public bool IsAvailable(AdFormat format, string placement) { return true; }
            public bool IsReady(AdFormat format, string placement) { return Ready; }

            public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
            {
                RewardedShown++;
                onFinished?.Invoke(RewardedResult.Success);
            }

            public void ShowInterstitial(string placement, Action onClosed = null)
            {
                if (Ready) InterstitialsShown++;
                onClosed?.Invoke();
            }

            public void ShowBanner(string placement, BannerPosition position = BannerPosition.Bottom) { BannersShown++; }
            public void HideBanner(string placement) { }
            public void DestroyBanner(string placement) { BannersDestroyed++; }
        }

        FakeAds fake;
        AdsFacade facade;

        [SetUp]
        public void SetUp()
        {
            AdRemoval.Restore(AdTypes.All);
            fake = new FakeAds();
            facade = new AdsFacade(fake);
        }

        [TearDown]
        public void TearDown()
        {
            facade.Dispose();
            AdRemoval.Restore(AdTypes.All);
        }

        [Test]
        public void ShowingAnAdStartsTheServiceByItself()
        {
            facade.ShowInterstitial(null);
            Assert.IsTrue(fake.IsInitialized);
        }

        [Test]
        public void RemovedInterstitialsAreSkippedButTheGameContinues()
        {
            AdRemoval.Remove(AdTypes.Interstitial);
            var closed = false;

            facade.ShowInterstitial(null, () => closed = true);

            Assert.IsTrue(closed);
            Assert.AreEqual(0, fake.InterstitialsShown);
            Assert.IsFalse(facade.IsAvailable(AdFormat.Interstitial, null));
        }

        [Test]
        public void RemovingBannersOnlyKeepsInterstitialsWorking()
        {
            AdRemoval.Remove(AdTypes.Banner);

            facade.ShowBanner(null, BannerPosition.Top);
            facade.ShowInterstitial(null);

            Assert.AreEqual(0, fake.BannersShown);
            Assert.AreEqual(1, fake.InterstitialsShown);
        }

        [Test]
        public void RemovingAdsHidesAVisibleBannerAndRestoringBringsItBack()
        {
            facade.ShowBanner(null, BannerPosition.Top);
            Assert.IsTrue(facade.IsBannerVisible);
            Assert.AreEqual(1, fake.BannersShown);

            AdRemoval.Remove(AdTypes.Banner);
            Assert.IsFalse(facade.IsBannerVisible);
            Assert.AreEqual(1, fake.BannersDestroyed);

            AdRemoval.Restore(AdTypes.Banner);
            Assert.IsTrue(facade.IsBannerVisible);
            Assert.AreEqual(2, fake.BannersShown);
        }

        [Test]
        public void ASecondInterstitialInsideTheCooldownIsSkipped()
        {
            facade.ShowInterstitial(null);
            facade.ShowInterstitial(null);

            Assert.AreEqual(1, fake.InterstitialsShown);
        }

        [Test]
        public void AnInterstitialThatIsNotLoadedYetDoesNotUseUpTheCooldown()
        {
            fake.Ready = false;
            facade.ShowInterstitial(null);
            Assert.AreEqual(0, fake.InterstitialsShown);

            fake.Ready = true;
            facade.ShowInterstitial(null);
            Assert.AreEqual(1, fake.InterstitialsShown);
        }

        [Test]
        public void RemovedRewardedAdsGiveTheRewardByDefault()
        {
            AdRemoval.Remove(AdTypes.Rewarded);
            RewardedResult? result = null;

            facade.ShowRewarded(null, r => result = r);

            Assert.AreEqual(RewardedResult.Success, result);
            Assert.AreEqual(0, fake.RewardedShown);
        }
    }
}
