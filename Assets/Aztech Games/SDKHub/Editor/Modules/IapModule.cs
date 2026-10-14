using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class IapModule : SdkModule
    {
        public override string Id => "iap";
        public override string DisplayName => "In-App Purchases";
        public override string Description => "Sell consumables, non-consumables and subscriptions through Google Play and the App Store.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.purchasing";
        public override string InstallIdentifier => "com.unity.purchasing@5.1.2";
        public override string DefineSymbol => "SDKHUB_IAP";
        public override string DocsUrl => "https://docs.unity.com/iap";
        public override int Order => 20;
        public override string Category => "Monetization";

        public override IReadOnlyList<SdkSceneAction> SceneActions => new[]
        {
            new SdkSceneAction("Add Restore Purchases Button", "GameObject/SDK Hub/Restore Purchases Button",
                "Lets players get their one-time purchases back after reinstalling. Apple requires it.")
        };

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "// Buy something\n" +
            "SdkManager.Iap.Purchase(\"coins_100\");\n\n" +
            "// React to purchases\n" +
            "SdkManager.Iap.PurchaseSucceeded += productId => GiveItem(productId);\n" +
            "SdkManager.Iap.PurchaseFailed += (productId, reason) => ShowError(reason);\n\n" +
            "// Price text and ownership of one-time items\n" +
            "string price = SdkManager.Iap.GetPrice(\"coins_100\");\n" +
            "bool owned = SdkManager.Iap.IsOwned(\"remove_ads\");\n\n" +
            "// Restore purchases (Apple requires a restore button)\n" +
            "SdkManager.Iap.RestorePurchases(success => { });";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Google Play Console", "https://play.google.com/console"),
            new SdkLink("App Store Connect", "https://appstoreconnect.apple.com")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
            if (info != null && !info.version.StartsWith("5."))
            {
                yield return new CheckItem(CheckLevel.Error, $"In-App Purchasing 5.x is required (found {info.version})");
            }

            var products = LoadCatalog().products;
            if (products.Count == 0)
            {
                yield return new CheckItem(CheckLevel.Error, "Add at least one product");
                yield break;
            }

            var seen = new HashSet<string>();
            var problem = false;
            foreach (var product in products)
            {
                if (string.IsNullOrEmpty(product.id))
                {
                    yield return new CheckItem(CheckLevel.Error, "A product has an empty ID");
                    problem = true;
                }
                else if (!seen.Add(product.id))
                {
                    yield return new CheckItem(CheckLevel.Error, "Duplicate product ID: " + product.id);
                    problem = true;
                }
            }
            if (!problem) yield return new CheckItem(CheckLevel.Ok, $"{products.Count} product(s) defined");
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var heading = new Label("Products");
            heading.AddToClassList("section-title");
            heading.AddToClassList("group-title");
            container.Add(heading);

            var hint = new Label("Use the same product IDs you create in Google Play Console and App Store Connect. Use Non-consumable for items bought once, like Remove Ads.");
            hint.AddToClassList("section-hint");
            container.Add(hint);

            var list = new VisualElement();
            container.Add(list);

            container.Add(SdkHubUI.MakeButton("Add product", () =>
            {
                var catalog = LoadCatalog();
                catalog.products.Add(new IapProductEntry { id = "", kind = IapProductKind.Consumable });
                SaveCatalog(catalog);
                Rebuild();
                onChanged?.Invoke();
            }));


            Rebuild();

            void Rebuild()
            {
                list.Clear();
                var products = LoadCatalog().products;
                for (var i = 0; i < products.Count; i++) list.Add(BuildRow(i, products[i], Rebuild, onChanged));
            }
        }

        VisualElement BuildRow(int index, IapProductEntry product, Action rebuild, Action onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("product-row");

            var idField = new TextField { value = product.id };
            idField.AddToClassList("field");
            idField.AddToClassList("product-id");
            idField.RegisterValueChangedCallback(evt =>
            {
                var catalog = LoadCatalog();
                if (index >= catalog.products.Count) return;
                catalog.products[index].id = evt.newValue.Trim();
                SaveCatalog(catalog);
                onChanged?.Invoke();
            });
            row.Add(idField);

            var kind = new DropdownField(new List<string>(Enum.GetNames(typeof(IapProductKind))), (int)product.kind);
            kind.AddToClassList("product-kind");
            kind.RegisterValueChangedCallback(_ =>
            {
                var catalog = LoadCatalog();
                if (index >= catalog.products.Count) return;
                catalog.products[index].kind = (IapProductKind)kind.index;
                SaveCatalog(catalog);
                onChanged?.Invoke();
            });
            row.Add(kind);

            var addToScene = SdkHubUI.MakeButton("Add to scene", () =>
            {
                var current = LoadCatalog().products;
                if (index < current.Count && !string.IsNullOrEmpty(current[index].id)) SdkHubSceneMenu.AddBuyButton(current[index].id);
            });
            addToScene.tooltip = "Adds a ready-made buy button for this product to the open scene. No setup needed.";
            row.Add(addToScene);

            row.Add(SdkHubUI.MakeButton("Remove", () =>
            {
                var catalog = LoadCatalog();
                if (index < catalog.products.Count) catalog.products.RemoveAt(index);
                SaveCatalog(catalog);
                rebuild();
                onChanged?.Invoke();
            }, "btn-danger"));
            return row;
        }

        IapCatalog LoadCatalog()
        {
            return IapCatalog.Parse(Get("catalog"));
        }

        void SaveCatalog(IapCatalog catalog)
        {
            SdkHubConfigProvider.Set(Id, "catalog", catalog.ToJson());
        }
    }
}
