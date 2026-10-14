using UnityEngine;

namespace SDKHub
{
    // Remembers which one-time purchases the player owns, so "Remove Ads" and similar items stay unlocked.
    public static class IapOwnership
    {
        const string Prefix = "SDKHub.Owned.";

        public static bool IsOwned(string productId)
        {
            return !string.IsNullOrEmpty(productId) && PlayerPrefs.GetInt(Prefix + productId, 0) == 1;
        }

        public static void MarkIfNonConsumable(string productId)
        {
            var config = SdkHubConfig.Instance;
            if (config == null || string.IsNullOrEmpty(productId)) return;

            foreach (var product in IapCatalog.Parse(config.Get("iap", "catalog")).products)
            {
                if (product.id != productId || product.kind != IapProductKind.NonConsumable) continue;
                PlayerPrefs.SetInt(Prefix + productId, 1);
                PlayerPrefs.Save();
                return;
            }
        }

        public static void Clear(string productId)
        {
            PlayerPrefs.DeleteKey(Prefix + productId);
        }
    }

    // Applies each "Remove Ads" entry you set up in the hub as soon as its product is bought or restored.
    static class AdRemovalBinding
    {
        static bool bound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()
        {
            bound = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bind()
        {
            var entries = AdCatalog.Current.removeAds;
            if (bound || entries.Count == 0) return;
            bound = true;

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.productId) && IapOwnership.IsOwned(entry.productId)) AdRemoval.Remove(entry.types);
            }

            SdkManager.Iap.PurchaseSucceeded += id =>
            {
                foreach (var entry in AdCatalog.Current.removeAds)
                {
                    if (entry.productId == id) AdRemoval.Remove(entry.types);
                }
            };
        }
    }
}
