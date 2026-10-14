using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class LevelPlayModule : SdkModule
    {
        const string NetworkManagerMenu = "Ads Mediation/Network Manager";
        const string NoProduct = "(none)";

        static readonly string[] RewardedNoAd = { "Hide the button", "Grey out the button", "Give the reward anyway" };
        static readonly string[] InterstitialNoAd = { "Hide the button", "Grey out the button", "Continue without an ad" };

        public override string Id => "levelplay";
        public override string DisplayName => "LevelPlay Ads";
        public override string Description => "Rewarded, interstitial and banner ads, and Remove Ads. Add the ads you need here. They start and load by themselves.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.services.levelplay";
        public override string DefineSymbol => "SDKHUB_LEVELPLAY";
        public override string DocsUrl => "https://developers.is.com/ironsource-mobile/unity/levelplay-starter-kit/";
        public override int Order => 10;
        public override string Category => "Monetization";

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "// Rewarded ad\n" +
            "SdkAds.ShowRewarded(\n" +
            "    () => GiveReward(),        // the player watched it\n" +
            "    () => ShowNoAdMessage(),   // no ad was ready\n" +
            "    () => { });                // the player closed it early\n\n" +
            "// Interstitial ad, for example at the end of a level\n" +
            "SdkAds.ShowInterstitial(\"level_end\", () => LoadNextLevel());\n\n" +
            "// Banner\n" +
            "SdkAds.ShowBanner(BannerPosition.Top);\n" +
            "SdkAds.HideBanner();\n\n" +
            "// Remove ads (for example after a purchase), and turn them back on\n" +
            "SdkAds.RemoveAds();\n" +
            "SdkAds.RemoveAds(AdTypes.Banner | AdTypes.Interstitial);\n" +
            "SdkAds.RestoreAds();";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("LevelPlay Dashboard", "https://platform.ironsrc.com/partners/login"),
            new SdkLink("Unity Dashboard", "https://cloud.unity.com")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            var android = !string.IsNullOrEmpty(Get("androidAppKey"));
            var ios = !string.IsNullOrEmpty(Get("iosAppKey"));

            if (!android && !ios)
            {
                yield return new CheckItem(CheckLevel.Error, "Enter an app key for at least one platform");
                yield break;
            }

            yield return new CheckItem(CheckLevel.Ok, "App key is set");
            if (!android) yield return new CheckItem(CheckLevel.Warning, "Android app key is empty");
            if (!ios) yield return new CheckItem(CheckLevel.Warning, "iOS app key is empty");

            var catalog = AdCatalogStore.Load();
            if (catalog.placements.Count == 0 && catalog.removeAds.Count == 0)
            {
                yield return new CheckItem(CheckLevel.Warning, "Add at least one ad");
                yield break;
            }

            var seen = new HashSet<string>();
            var androidCount = 0;
            var iosCount = 0;
            foreach (var placement in catalog.placements)
            {
                if (string.IsNullOrEmpty(placement.name)) yield return new CheckItem(CheckLevel.Error, "An ad has no name");
                else if (!seen.Add(placement.format + ":" + placement.name)) yield return new CheckItem(CheckLevel.Error, $"Two {placement.format} ads are called '{placement.name}'");

                if (!placement.IsAvailableOn(SdkPlatform.Other)) yield return new CheckItem(CheckLevel.Warning, $"'{placement.name}' has no ad unit ID, so it is off on both platforms");
                if (!string.IsNullOrEmpty(placement.androidId)) androidCount++;
                if (!string.IsNullOrEmpty(placement.iosId)) iosCount++;
            }
            if (androidCount > 0 && !android) yield return new CheckItem(CheckLevel.Warning, "Android ad unit IDs are set but the Android app key is empty");
            if (iosCount > 0 && !ios) yield return new CheckItem(CheckLevel.Warning, "iOS ad unit IDs are set but the iOS app key is empty");

            var iapProducts = IapCatalog.Parse(SdkHubConfigProvider.Get("iap", "catalog")).products;
            foreach (var entry in catalog.removeAds)
            {
                if (string.IsNullOrEmpty(entry.productId))
                {
                    yield return new CheckItem(CheckLevel.Warning, $"'{entry.name}' has no product yet");
                    continue;
                }

                IapProductEntry found = null;
                foreach (var product in iapProducts)
                {
                    if (product.id == entry.productId) found = product;
                }
                if (found == null) yield return new CheckItem(CheckLevel.Warning, $"'{entry.name}' uses '{entry.productId}', which is not on the In-App Purchases page");
                else if (found.kind != IapProductKind.NonConsumable) yield return new CheckItem(CheckLevel.Warning, $"'{entry.productId}' should be a Non-consumable product, or players could buy it again");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            container.Add(Heading("App keys"));
            container.Add(SdkHubUI.ConfigField(this, "androidAppKey", "Android app key", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "iosAppKey", "iOS app key", onChanged));
            container.Add(SdkHubUI.MakeButton("Open Network Manager", () =>
            {
                if (!EditorApplication.ExecuteMenuItem(NetworkManagerMenu))
                {
                    EditorUtility.DisplayDialog("LevelPlay", "Install the SDK first. The Network Manager is under the Ads Mediation menu in the Unity menu bar.", "OK");
                }
            }));

            container.Add(Heading("Your ads"));
            container.Add(Hint("Add the ads your game uses. Give each an Android and an iOS ad unit ID, or leave a platform empty to turn that ad off there. Each ad gets a name you can use in code."));

            // Declared before the buttons, because their click handlers rebuild this list.
            var groups = new VisualElement();

            var add = new VisualElement();
            add.AddToClassList("actions");
            add.Add(SdkHubUI.MakeButton("Add rewarded", () => AddAd(AdFormat.Rewarded)));
            add.Add(SdkHubUI.MakeButton("Add interstitial", () => AddAd(AdFormat.Interstitial)));
            add.Add(SdkHubUI.MakeButton("Add banner", () => AddAd(AdFormat.Banner)));
            add.Add(SdkHubUI.MakeButton("Add remove ads", AddRemoveAds));
            container.Add(add);

            container.Add(groups);
            Rebuild();

            void Rebuild()
            {
                groups.Clear();
                var catalog = AdCatalogStore.Load();

                AddAdGroup(groups, catalog, AdFormat.Rewarded, "REWARDED", Rebuild, onChanged);
                AddAdGroup(groups, catalog, AdFormat.Interstitial, "INTERSTITIAL", Rebuild, onChanged);
                AddAdGroup(groups, catalog, AdFormat.Banner, "BANNER", Rebuild, onChanged);
                AddRemoveAdsGroup(groups, catalog, Rebuild, onChanged);

                if (catalog.placements.Count == 0 && catalog.removeAds.Count == 0)
                {
                    groups.Add(Hint("No ads yet. Use the buttons above."));
                }
            }

            void AddAd(AdFormat format)
            {
                var catalog = AdCatalogStore.Load();
                catalog.placements.Add(new AdPlacementEntry { name = AdCatalogStore.UniqueName(catalog, format), format = format });
                AdCatalogStore.Save(catalog);
                Rebuild();
                onChanged?.Invoke();
            }

            void AddRemoveAds()
            {
                var catalog = AdCatalogStore.Load();
                catalog.removeAds.Add(new RemoveAdsEntry { name = AdCatalogStore.UniqueRemoveAdsName(catalog), types = AdTypes.All });
                AdCatalogStore.Save(catalog);
                Rebuild();
                onChanged?.Invoke();
            }
        }

        // ---- Groups ---------------------------------------------------------------

        void AddAdGroup(VisualElement parent, AdCatalog catalog, AdFormat format, string title, Action rebuild, Action onChanged)
        {
            var indexes = new List<int>();
            for (var i = 0; i < catalog.placements.Count; i++)
            {
                if (catalog.placements[i].format == format) indexes.Add(i);
            }
            if (indexes.Count == 0) return;

            parent.Add(GroupTitle(title));
            foreach (var index in indexes) parent.Add(BuildAdCard(index, catalog.placements[index], rebuild, onChanged));

            if (format == AdFormat.Rewarded)
            {
                var rules = new VisualElement();
                rules.AddToClassList("rules-row");
                rules.Add(SdkHubUI.ConfigChoice(this, "rewardedWhenRemoved", "If rewarded ads are removed",
                    new[] { "give", "fail" }, new[] { "Give the reward without an ad", "Do not give the reward" }, 0,
                    "Only matters when a Remove Ads entry also removes rewarded ads.", onChanged));
                parent.Add(rules);
            }
            else if (format == AdFormat.Interstitial)
            {
                var rules = new VisualElement();
                rules.AddToClassList("rules-row");
                rules.Add(SdkHubUI.ConfigFloat(this, "graceSeconds", "No ads for the first (seconds)", 0f,
                    "No interstitial is shown during the first seconds after the game starts.", onChanged));
                rules.Add(SdkHubUI.ConfigFloat(this, "cooldownSeconds", "Time between ads (seconds)", 30f,
                    "The least time between two interstitials. If a request comes sooner, it is skipped and your game continues.", onChanged));
                parent.Add(rules);
            }
            else
            {
                var safe = SdkHubUI.MakeButton("Add safe area to selected UI", () =>
                    EditorApplication.ExecuteMenuItem("GameObject/SDK Hub/Banner Safe Area (on selected object)"));
                safe.tooltip = "Select your full-screen UI object first. It makes room for the banner so it never covers your buttons.";
                parent.Add(safe);
            }
        }

        void AddRemoveAdsGroup(VisualElement parent, AdCatalog catalog, Action rebuild, Action onChanged)
        {
            if (catalog.removeAds.Count == 0) return;

            parent.Add(GroupTitle("REMOVE ADS"));
            parent.Add(Hint("Pick the In-App Purchase that removes ads, and which ads it removes. After the purchase those ads stay off, also after a restart."));
            for (var i = 0; i < catalog.removeAds.Count; i++) parent.Add(BuildRemoveAdsCard(i, catalog.removeAds[i], rebuild, onChanged));
        }

        // ---- Cards ----------------------------------------------------------------

        static VisualElement BuildAdCard(int index, AdPlacementEntry entry, Action rebuild, Action onChanged)
        {
            var card = new VisualElement();
            card.AddToClassList("placement-card");

            var top = Row();
            var ids = Row();
            card.Add(top);
            card.Add(ids);

            var summary = new Label();
            summary.AddToClassList("chip");
            void UpdateSummary(AdPlacementEntry current)
            {
                if (current == null) return;
                summary.text = current.PlatformSummary;
                summary.EnableInClassList("chip-ok", current.PlatformSummary == "Android + iOS");
                summary.EnableInClassList("chip-info", current.PlatformSummary.EndsWith("only"));
                summary.EnableInClassList("chip-warn", current.PlatformSummary == "No platform");
            }
            UpdateSummary(entry);

            var name = new TextField { value = entry.name };
            name.AddToClassList("field");
            name.AddToClassList("ad-name");
            name.textEdition.placeholder = "Name";
            name.tooltip = "The name you can use in code, for example \"level_end\".";
            name.RegisterValueChangedCallback(evt =>
            {
                UpdateSummary(EditAd(index, e => e.name = evt.newValue.Trim()));
                onChanged?.Invoke();
            });
            top.Add(name);
            top.Add(summary);

            if (entry.format != AdFormat.Banner)
            {
                var addToScene = SdkHubUI.MakeButton("Add to scene", () =>
                {
                    var current = AdCatalogStore.Load().placements;
                    if (index < current.Count) SdkHubSceneMenu.AddAd(current[index].format, current[index].name);
                });
                addToScene.tooltip = "Adds a ready-made button for this ad to the open scene. No setup needed.";
                top.Add(addToScene);
            }

            var remove = SdkHubUI.MakeButton("Remove", () =>
            {
                var catalog = AdCatalogStore.Load();
                if (index < catalog.placements.Count) catalog.placements.RemoveAt(index);
                AdCatalogStore.Save(catalog);
                rebuild();
                onChanged?.Invoke();
            }, "btn-danger");
            top.Add(remove);

            var android = new TextField { value = entry.androidId };
            android.AddToClassList("field");
            android.AddToClassList("ad-id");
            android.textEdition.placeholder = "Android ad unit ID (empty = off on Android)";
            android.RegisterValueChangedCallback(evt =>
            {
                UpdateSummary(EditAd(index, e => e.androidId = evt.newValue.Trim()));
                onChanged?.Invoke();
            });
            ids.Add(android);

            var ios = new TextField { value = entry.iosId };
            ios.AddToClassList("field");
            ios.AddToClassList("ad-id");
            ios.textEdition.placeholder = "iOS ad unit ID (empty = off on iOS)";
            ios.RegisterValueChangedCallback(evt =>
            {
                UpdateSummary(EditAd(index, e => e.iosId = evt.newValue.Trim()));
                onChanged?.Invoke();
            });
            ids.Add(ios);

            if (entry.format == AdFormat.Interstitial)
            {
                var every = Row();
                var field = new IntegerField("Show every") { value = Mathf.Max(1, entry.everyNth) };
                field.AddToClassList("field");
                field.AddToClassList("ad-every");
                field.tooltip = "1 shows the ad on every request. 3 shows it on every 3rd request, for example every 3rd level end.";
                field.RegisterValueChangedCallback(evt =>
                {
                    var value = Mathf.Max(1, evt.newValue);
                    EditAd(index, e => e.everyNth = value);
                    onChanged?.Invoke();
                });
                every.Add(field);
                every.Add(Note("request(s)"));
                card.Add(every);
            }

            if (entry.format == AdFormat.Rewarded || entry.format == AdFormat.Interstitial)
            {
                var labels = entry.format == AdFormat.Rewarded ? RewardedNoAd : InterstitialNoAd;
                var noAd = new DropdownField("If there is no ad here", new List<string>(labels), (int)entry.whenUnavailable);
                noAd.AddToClassList("field");
                noAd.tooltip = "What the button does on a platform where this ad has no ID, or when the player removed this kind of ad.";
                noAd.RegisterValueChangedCallback(_ =>
                {
                    EditAd(index, e => e.whenUnavailable = (AdUnavailableBehavior)noAd.index);
                    onChanged?.Invoke();
                });
                var line = Row();
                line.Add(noAd);
                card.Add(line);
            }

            if (entry.format == AdFormat.Banner)
            {
                var line = Row();
                var position = new DropdownField("Position", new List<string> { "Bottom of the screen", "Top of the screen" }, (int)entry.position);
                position.AddToClassList("field");
                position.RegisterValueChangedCallback(_ =>
                {
                    EditAd(index, e => e.position = (BannerPosition)position.index);
                    onChanged?.Invoke();
                });
                line.Add(position);

                var launch = new Toggle("Show when the game starts") { value = entry.showAtLaunch };
                launch.AddToClassList("field");
                launch.tooltip = "Turn this off to show the banner yourself with SdkAds.ShowBanner().";
                launch.RegisterValueChangedCallback(evt =>
                {
                    EditAd(index, e => e.showAtLaunch = evt.newValue);
                    onChanged?.Invoke();
                });
                line.Add(launch);
                card.Add(line);
            }
            return card;
        }

        VisualElement BuildRemoveAdsCard(int index, RemoveAdsEntry entry, Action rebuild, Action onChanged)
        {
            var card = new VisualElement();
            card.AddToClassList("placement-card");

            var top = Row();
            card.Add(top);

            var name = new TextField { value = entry.name };
            name.AddToClassList("field");
            name.AddToClassList("ad-name");
            name.textEdition.placeholder = "Name";
            name.RegisterValueChangedCallback(evt =>
            {
                EditRemoveAds(index, e => e.name = evt.newValue.Trim());
                onChanged?.Invoke();
            });
            top.Add(name);

            var addToScene = SdkHubUI.MakeButton("Add to scene", () =>
            {
                var current = AdCatalogStore.Load().removeAds;
                if (index < current.Count) SdkHubSceneMenu.AddRemoveAds(current[index].name);
            });
            addToScene.tooltip = "Adds a ready-made Remove Ads button to the open scene. No setup needed.";
            top.Add(addToScene);

            var remove = SdkHubUI.MakeButton("Remove", () =>
            {
                var catalog = AdCatalogStore.Load();
                if (index < catalog.removeAds.Count) catalog.removeAds.RemoveAt(index);
                AdCatalogStore.Save(catalog);
                rebuild();
                onChanged?.Invoke();
            }, "btn-danger");
            top.Add(remove);

            var line = Row();
            card.Add(line);

            var products = new List<string> { NoProduct };
            foreach (var product in IapCatalog.Parse(SdkHubConfigProvider.Get("iap", "catalog")).products)
            {
                if (!string.IsNullOrEmpty(product.id)) products.Add(product.id);
            }
            if (!string.IsNullOrEmpty(entry.productId) && !products.Contains(entry.productId)) products.Add(entry.productId);

            var dropdown = new DropdownField("Product", products, string.IsNullOrEmpty(entry.productId) ? 0 : products.IndexOf(entry.productId));
            dropdown.AddToClassList("field");
            dropdown.tooltip = "The Non-consumable product that removes ads. Add products on the In-App Purchases page.";
            dropdown.RegisterValueChangedCallback(evt =>
            {
                EditRemoveAds(index, e => e.productId = evt.newValue == NoProduct ? "" : evt.newValue);
                onChanged?.Invoke();
            });
            line.Add(dropdown);

            var flags = new EnumFlagsField("Ads to remove", entry.types);
            flags.AddToClassList("field");
            flags.tooltip = "Pick one, several or all. For example Banner only removes just banners.";
            flags.RegisterValueChangedCallback(evt =>
            {
                EditRemoveAds(index, e => e.types = (AdTypes)evt.newValue);
                onChanged?.Invoke();
            });
            line.Add(flags);
            return card;
        }

        // ---- Helpers --------------------------------------------------------------

        static VisualElement Row()
        {
            var row = new VisualElement();
            row.AddToClassList("placement-row");
            return row;
        }

        static Label Heading(string text)
        {
            var heading = new Label(text);
            heading.AddToClassList("section-title");
            heading.AddToClassList("group-title");
            return heading;
        }

        static Label GroupTitle(string text)
        {
            var title = new Label(text);
            title.AddToClassList("ad-group-title");
            return title;
        }

        static Label Hint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("section-hint");
            return label;
        }

        static Label Note(string text)
        {
            var label = new Label(text);
            label.AddToClassList("placement-note");
            return label;
        }

        static AdPlacementEntry EditAd(int index, Action<AdPlacementEntry> change)
        {
            var catalog = AdCatalogStore.Load();
            if (index >= catalog.placements.Count) return null;
            change(catalog.placements[index]);
            AdCatalogStore.Save(catalog);
            return catalog.placements[index];
        }

        static void EditRemoveAds(int index, Action<RemoveAdsEntry> change)
        {
            var catalog = AdCatalogStore.Load();
            if (index >= catalog.removeAds.Count) return;
            change(catalog.removeAds[index]);
            AdCatalogStore.Save(catalog);
        }
    }
}
