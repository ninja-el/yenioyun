using NUnit.Framework;

namespace SDKHub.Tests
{
    public class AdCatalogTests
    {
        static AdPlacementEntry Entry(string name, AdFormat format, string android, string ios)
        {
            return new AdPlacementEntry { name = name, format = format, androidId = android, iosId = ios };
        }

        [Test]
        public void ResolvePrefersAPlacementThatExistsOnThisPlatform()
        {
            var catalog = new AdCatalog();
            catalog.placements.Add(Entry("ios_only", AdFormat.Rewarded, "", "ios-id"));
            catalog.placements.Add(Entry("both", AdFormat.Rewarded, "a", "i"));

            Assert.AreEqual("both", catalog.Resolve(AdFormat.Rewarded, "", SdkPlatform.Android).name);
        }

        [Test]
        public void ResolveFindsAPlacementByName()
        {
            var catalog = new AdCatalog();
            catalog.placements.Add(Entry("one", AdFormat.Interstitial, "a", "i"));
            catalog.placements.Add(Entry("two", AdFormat.Interstitial, "a", "i"));

            Assert.AreEqual("two", catalog.Resolve(AdFormat.Interstitial, "two", SdkPlatform.iOS).name);
        }

        [Test]
        public void ResolveIgnoresOtherFormats()
        {
            var catalog = new AdCatalog();
            catalog.placements.Add(Entry("banner", AdFormat.Banner, "a", "i"));
            Assert.IsNull(catalog.Resolve(AdFormat.Rewarded, "", SdkPlatform.Android));
        }

        [Test]
        public void APlacementWithoutAnIdIsOffOnThatPlatform()
        {
            var entry = Entry("x", AdFormat.Banner, "android-id", "");
            Assert.IsTrue(entry.IsAvailableOn(SdkPlatform.Android));
            Assert.IsFalse(entry.IsAvailableOn(SdkPlatform.iOS));
            Assert.IsTrue(entry.IsAvailableOn(SdkPlatform.Other));
        }

        [Test]
        public void PlatformSummaryNamesWhereTheAdRuns()
        {
            Assert.AreEqual("Android + iOS", Entry("x", AdFormat.Banner, "a", "i").PlatformSummary);
            Assert.AreEqual("Android only", Entry("x", AdFormat.Banner, "a", "").PlatformSummary);
            Assert.AreEqual("iOS only", Entry("x", AdFormat.Banner, "", "i").PlatformSummary);
            Assert.AreEqual("No platform", Entry("x", AdFormat.Banner, "", "").PlatformSummary);
        }

        [Test]
        public void JsonKeepsTheEveryNthSetting()
        {
            var catalog = new AdCatalog();
            var entry = Entry("level_end", AdFormat.Interstitial, "a", "i");
            entry.everyNth = 3;
            catalog.placements.Add(entry);

            var copy = AdCatalog.Parse(catalog.ToJson());
            Assert.AreEqual(1, copy.placements.Count);
            Assert.AreEqual(3, copy.placements[0].everyNth);
        }

        [Test]
        public void OldJsonWithoutEveryNthStillWorks()
        {
            var copy = AdCatalog.Parse("{\"placements\":[{\"name\":\"a\",\"format\":0,\"androidId\":\"x\",\"iosId\":\"\"}]}");
            Assert.AreEqual(1, copy.placements.Count);
            Assert.GreaterOrEqual(copy.placements[0].everyNth, 1);
        }

        [Test]
        public void BrokenJsonGivesAnEmptyCatalog()
        {
            Assert.AreEqual(0, AdCatalog.Parse("{not json").placements.Count);
            Assert.AreEqual(0, AdCatalog.Parse("").placements.Count);
        }

        [Test]
        public void IapCatalogRoundTrips()
        {
            var catalog = new IapCatalog();
            catalog.products.Add(new IapProductEntry { id = "remove_ads", kind = IapProductKind.NonConsumable });

            var copy = IapCatalog.Parse(catalog.ToJson());
            Assert.AreEqual("remove_ads", copy.products[0].id);
            Assert.AreEqual(IapProductKind.NonConsumable, copy.products[0].kind);
        }

        [Test]
        public void NewPlacementsHideWhenUnavailableAndSitAtTheBottom()
        {
            var entry = new AdPlacementEntry();
            Assert.AreEqual(AdUnavailableBehavior.Hide, entry.whenUnavailable);
            Assert.AreEqual(BannerPosition.Bottom, entry.position);
            Assert.IsTrue(entry.showAtLaunch);
            Assert.AreEqual(1, entry.everyNth);
        }

        [Test]
        public void PlacementSettingsSurviveJson()
        {
            var catalog = new AdCatalog();
            var banner = Entry("top_banner", AdFormat.Banner, "a", "i");
            banner.position = BannerPosition.Top;
            banner.showAtLaunch = false;
            var rewarded = Entry("extra_life", AdFormat.Rewarded, "a", "");
            rewarded.whenUnavailable = AdUnavailableBehavior.Skip;
            catalog.placements.Add(banner);
            catalog.placements.Add(rewarded);

            var copy = AdCatalog.Parse(catalog.ToJson());
            Assert.AreEqual(BannerPosition.Top, copy.placements[0].position);
            Assert.IsFalse(copy.placements[0].showAtLaunch);
            Assert.AreEqual(AdUnavailableBehavior.Skip, copy.placements[1].whenUnavailable);
        }

        [Test]
        public void RemoveAdsEntriesKeepTheirProductAndKinds()
        {
            var catalog = new AdCatalog();
            catalog.removeAds.Add(new RemoveAdsEntry { name = "Remove Ads", productId = "remove_ads", types = AdTypes.Banner | AdTypes.Interstitial });

            var copy = AdCatalog.Parse(catalog.ToJson());
            Assert.AreEqual(1, copy.removeAds.Count);
            Assert.AreEqual("remove_ads", copy.removeAds[0].productId);
            Assert.AreEqual(AdTypes.Banner | AdTypes.Interstitial, copy.removeAds[0].types);
        }

        [Test]
        public void FindRemoveAdsByNameOrTakesTheFirst()
        {
            var catalog = new AdCatalog();
            catalog.removeAds.Add(new RemoveAdsEntry { name = "Remove banners", productId = "a" });
            catalog.removeAds.Add(new RemoveAdsEntry { name = "Remove all", productId = "b" });

            Assert.AreEqual("b", catalog.FindRemoveAds("Remove all").productId);
            Assert.AreEqual("a", catalog.FindRemoveAds("").productId);
            Assert.IsNull(catalog.FindRemoveAds("missing"));
            Assert.IsNull(new AdCatalog().FindRemoveAds(""));
        }
    }
}
