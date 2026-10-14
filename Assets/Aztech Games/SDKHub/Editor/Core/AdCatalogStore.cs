using UnityEngine;

namespace SDKHub.Editor
{
    public static class AdCatalogStore
    {
        const string Module = "levelplay";
        const string Key = "adPlacements";

        public static AdCatalog Load()
        {
            var json = SdkHubConfigProvider.Get(Module, Key);
            AdCatalog catalog;
            var changed = false;

            if (!string.IsNullOrEmpty(json))
            {
                catalog = AdCatalog.Parse(json);
            }
            else
            {
                catalog = new AdCatalog();
                AddLegacy(catalog, "Rewarded", AdFormat.Rewarded, "RewardedId");
                AddLegacy(catalog, "Interstitial", AdFormat.Interstitial, "InterstitialId");
                changed = catalog.placements.Count > 0;
            }

            // Older versions kept one Remove Ads product in two settings. Move it into the list once.
            var product = SdkHubConfigProvider.Get(Module, "removeAdsProduct");
            if (catalog.removeAds.Count == 0 && !string.IsNullOrEmpty(product))
            {
                int stored;
                var types = int.TryParse(SdkHubConfigProvider.Get(Module, "removeAdsTypes"), out stored) ? (AdTypes)(stored & (int)AdTypes.All) : AdTypes.All;
                catalog.removeAds.Add(new RemoveAdsEntry { name = "Remove Ads", productId = product, types = types });
                SdkHubConfigProvider.Set(Module, "removeAdsProduct", "");
                changed = true;
            }

            if (changed) Save(catalog);
            return catalog;
        }

        public static void Save(AdCatalog catalog)
        {
            SdkHubConfigProvider.Set(Module, Key, catalog.ToJson());
        }

        public static string UniqueName(AdCatalog catalog, AdFormat format)
        {
            var baseName = format.ToString();
            var name = baseName;
            var number = 1;
            while (Exists(catalog, format, name)) name = baseName + " " + (++number);
            return name;
        }

        public static string UniqueRemoveAdsName(AdCatalog catalog)
        {
            const string baseName = "Remove Ads";
            var name = baseName;
            var number = 1;
            while (catalog.removeAds.Exists(entry => entry.name == name)) name = baseName + " " + (++number);
            return name;
        }

        static bool Exists(AdCatalog catalog, AdFormat format, string name)
        {
            foreach (var placement in catalog.placements)
            {
                if (placement.format == format && placement.name == name) return true;
            }
            return false;
        }

        static void AddLegacy(AdCatalog catalog, string name, AdFormat format, string suffix)
        {
            var android = SdkHubConfigProvider.Get(Module, "android" + suffix);
            var ios = SdkHubConfigProvider.Get(Module, "ios" + suffix);
            if (string.IsNullOrEmpty(android) && string.IsNullOrEmpty(ios)) return;
            catalog.placements.Add(new AdPlacementEntry { name = name, format = format, androidId = android, iosId = ios });
        }
    }
}
