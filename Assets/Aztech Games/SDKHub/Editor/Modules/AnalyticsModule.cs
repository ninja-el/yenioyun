using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class AnalyticsModule : SdkModule
    {
        public override string Id => "analytics";
        public override string DisplayName => "Unity Analytics";
        public override string NavName => "Analytics";
        public override string Description => "See how players use your game. Data is collected only after the player agrees.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.services.analytics";
        public override string DefineSymbol => "SDKHUB_ANALYTICS";
        public override string DocsUrl => "https://docs.unity.com/ugs/en-us/manual/analytics/manual/sdk-guide";
        public override string Category => "Privacy & Analytics";
        public override int Order => 6;

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "// Starts by itself. Sends only after the player agreed in the privacy panel.\n" +
            "SdkManager.Analytics.LogEvent(\"level_complete\");";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Unity Dashboard", "https://cloud.unity.com")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            var cloud = SdkSteps.CloudProject();
            if (cloud.Level != CheckLevel.Ok) yield return cloud;
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var hint = new Label("Nothing to enter here. Create each event (name and parameters) in the Event Manager of the Unity Dashboard first, because Unity rejects events it does not know. Add the Privacy panel so players can agree, or nothing is sent.");
            hint.AddToClassList("section-hint");
            container.Add(hint);
        }
    }
}
