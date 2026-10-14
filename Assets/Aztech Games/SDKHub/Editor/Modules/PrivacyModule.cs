using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class PrivacyModule : SdkModule
    {
        public const string DefaultAttText = "This lets us show you ads that are more relevant to you.";

        public override string Id => "privacy";
        public override string DisplayName => "Privacy and Consent";
        public override string NavName => "Privacy";
        public override string Description => "Ask players for permission before data is collected. Adds a ready-made panel with an info text and Agree and Disagree buttons.";
        public override InstallType InstallType => InstallType.BuiltIn;
        public override string DefineSymbol => "SDKHUB_PRIVACY";
        public override string Category => "Privacy & Analytics";
        public override int Order => 5;

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Apple tracking guide", "https://developer.apple.com/documentation/apptrackingtransparency")
        };

        public override IReadOnlyList<SdkSceneAction> SceneActions => new[]
        {
            new SdkSceneAction("Add Privacy Panel", "GameObject/SDK Hub/Privacy Panel")
        };

        public override string CodeUsage =>
            "using SDKHub;\nusing SDKHub.Components;\n\n" +
            "SdkManager.Consent.Agree();      // or Disagree()\n" +
            "bool answered = SdkManager.Consent.HasAnswered;\n" +
            "SdkManager.Consent.Changed += status => { };\n\n" +
            "// Let the player change their answer, for example from a settings screen\n" +
            "PrivacyPanel.ShowPrivacyPanel();";

        public static string Title => Read("title", "Your privacy");
        public static string Body => Read("body", "We use analytics and advertising partners to improve the game and to show ads. Tap Agree to allow this, or Disagree to continue with limited data use. You can change your choice later in the settings.");
        public static string Agree => Read("agree", "Agree");
        public static string Disagree => Read("disagree", "Disagree");

        public override IEnumerable<CheckItem> Validate()
        {
            if (Get("requireConsent") == "1")
            {
                yield return new CheckItem(CheckLevel.Ok, "Ads, analytics, push and Facebook wait for the player's answer");
            }
            else
            {
                yield return new CheckItem(CheckLevel.Warning, "Services do not wait for the player's answer. Add the privacy panel to turn this on.");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var behavior = new Label("Behavior");
            behavior.AddToClassList("section-title");
            behavior.AddToClassList("group-title");
            container.Add(behavior);

            var hint = new Label("When this is on, Facebook, push and analytics start only after the player agrees. Ads start after any answer and receive the choice, so they are limited when the player disagrees.");
            hint.AddToClassList("section-hint");
            container.Add(hint);
            container.Add(SdkHubUI.ConfigToggle(this, "requireConsent", "Wait for the player's answer before starting these services", onChanged));

            var panel = new Label("Panel text");
            panel.AddToClassList("section-title");
            panel.AddToClassList("group-title");
            container.Add(panel);

            var textHint = new Label("These texts are used when you add a privacy panel to a scene. You can still edit the panel in the scene afterwards.");
            textHint.AddToClassList("section-hint");
            container.Add(textHint);

            container.Add(SdkHubUI.ConfigField(this, "title", "Title", onChanged));
            var body = SdkHubUI.ConfigField(this, "body", "Info text", onChanged);
            body.multiline = true;
            body.AddToClassList("field-multiline");
            if (string.IsNullOrEmpty(body.value)) body.SetValueWithoutNotify(Body);
            container.Add(body);
            container.Add(SdkHubUI.ConfigField(this, "agree", "Agree button", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "disagree", "Disagree button", onChanged));

            var ios = new Label("iOS tracking permission");
            ios.AddToClassList("section-title");
            ios.AddToClassList("group-title");
            container.Add(ios);

            var iosHint = new Label("Apple requires its own permission pop-up before apps track players. The panel can show it after the player agrees. The text below is what Apple shows in that pop-up.");
            iosHint.AddToClassList("section-hint");
            container.Add(iosHint);

            var description = SdkHubUI.ConfigField(this, "attDescription", "Pop-up text", onChanged);
            if (string.IsNullOrEmpty(description.value)) description.SetValueWithoutNotify(DefaultAttText);
            container.Add(description);

            var att = SdkRegistry.Find("att");
            if (att != null && !att.IsInstalled)
            {
                container.Add(SdkHubUI.MakeButton("Install iOS Tracking Support", () => SdkInstaller.Install(att), "btn-primary"));
            }
        }

        static string Read(string key, string fallback)
        {
            var value = SdkHubConfigProvider.Get("privacy", key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
