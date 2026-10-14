using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class PurchaseValidationModule : SdkModule
    {
        public override string Id => "validation";
        public override string DisplayName => "Purchase Validation";
        public override string NavName => "Purchase validation";
        public override string Description => "Check every purchase with your own server before the item is given. Stops fake purchases from unlocking content.";
        public override InstallType InstallType => InstallType.BuiltIn;
        public override string DefineSymbol => "SDKHUB_VALIDATION";
        public override string DocsUrl => "https://docs.unity.com/iap/receipt-validation";
        public override string Category => "Monetization";
        public override int Order => 25;

        public override IEnumerable<CheckItem> Validate()
        {
            if (Get("enabled") != "1")
            {
                yield return new CheckItem(CheckLevel.Warning, "Validation is off. Purchases are trusted without a server check.");
                yield break;
            }

            var url = Get("url");
            if (string.IsNullOrEmpty(url))
            {
                yield return new CheckItem(CheckLevel.Error, "Enter the address of your validation server");
                yield break;
            }

            yield return new CheckItem(CheckLevel.Ok, "Purchases are checked by your server before they are confirmed");
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CheckItem(CheckLevel.Warning, "Use an https address so receipts are encrypted in transit");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var hint = new Label("Before a purchase is confirmed, the game sends your server a JSON message with productId, platform and receipt. Your server checks it with Google or Apple and answers {\"valid\": true} or {\"valid\": false}. If the answer is anything else, or the server cannot be reached, the purchase stays unconfirmed and is tried again later. Unity does not provide a validation server, so you need your own.");
            hint.AddToClassList("section-hint");
            container.Add(hint);

            container.Add(SdkHubUI.ConfigToggle(this, "enabled", "Check purchases with my server", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "url", "Server address", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "authHeader", "Authorization header", onChanged));

            var limits = new Label("Limits of this first version: it sends the unified receipt only. Apple's newer signed-transaction check is not included yet. Anti-cheat tools for memory editing or save files are not part of this module.");
            limits.AddToClassList("section-hint");
            container.Add(limits);
        }
    }
}
