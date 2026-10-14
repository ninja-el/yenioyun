using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class UpdatesModule : SdkModule
    {
        public override string Id => "updates";
        public override string DisplayName => "In-App Updates";
        public override string NavName => "App updates";
        public override string Description => "Let Android players update your game from inside it, without leaving for the Play Store.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.google.play.appupdate";
        public override string DefineSymbol => "SDKHUB_UPDATES";
        public override string DocsUrl => "https://developer.android.com/guide/playcore/in-app-updates/unity";
        public override string Category => "Engagement";
        public override int Order => 65;

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "SdkManager.Updates.CheckAndUpdate(AppUpdateMode.Flexible, result => { });";

        public override ScopedRegistryInfo ScopedRegistry => new ScopedRegistryInfo(
            "package.openupm.com", "https://package.openupm.com",
            "com.google.external-dependency-manager", "com.google.play.common", "com.google.play.core", "com.google.play.appupdate");

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Google Play Console", "https://play.google.com/console")
        };

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            container.Add(SdkHubUI.ConfigChoice(this, "startupCheck", "Check for an update when the game starts",
                new[] { "off", "flexible", "immediate" },
                new[] { "Never", "Yes, the player keeps playing (flexible)", "Yes, a full-screen update (immediate)" }, 0,
                "Flexible downloads in the background. Immediate takes over the screen until the update is installed.", onChanged));

            var hint = new Label("Android only, and only for apps installed from Google Play, so test with an internal test track. Install adds the OpenUPM package registry to your project.");
            hint.AddToClassList("section-hint");
            container.Add(hint);
        }
    }
}
