using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class ReviewModule : SdkModule
    {
        public override string Id => "review";
        public override string DisplayName => "In-App Review";
        public override string NavName => "Review prompt";
        public override string Description => "Ask players to rate your game without leaving it. Uses Google Play on Android and Apple's prompt on iOS.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.google.play.review";
        public override string DefineSymbol => "SDKHUB_REVIEW";
        public override string DocsUrl => "https://developer.android.com/guide/playcore/in-app-review/unity";
        public override string Category => "Engagement";
        public override int Order => 60;

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "SdkManager.Review.RequestReview();   // Google Play or Apple shows its own rating prompt";

        public override ScopedRegistryInfo ScopedRegistry => new ScopedRegistryInfo(
            "package.openupm.com", "https://package.openupm.com",
            "com.google.external-dependency-manager", "com.google.play.common", "com.google.play.core", "com.google.play.review");

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Google Play Console", "https://play.google.com/console")
        };

        public override IReadOnlyList<SdkSceneAction> SceneActions => new[]
        {
            new SdkSceneAction("Add Review Button", "GameObject/SDK Hub/Review Button")
        };

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var hint = new Label("Google's prompt only shows for apps installed from Google Play, so test with an internal test track. The package is needed for Android builds. On iOS the prompt is built in, and Apple limits how often it appears. Install adds the OpenUPM package registry to your project.");
            hint.AddToClassList("section-hint");
            container.Add(hint);
        }
    }
}
