using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class AccountsModule : SdkModule
    {
        public override string Id => "accounts";
        public override string DisplayName => "Accounts and Cloud Save";
        public override string NavName => "Accounts & Cloud Save";
        public override string Description => "Anonymous player accounts and cloud saves, so progress follows the player.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.services.cloudsave";
        public override string DefineSymbol => "SDKHUB_ACCOUNTS";
        public override string DocsUrl => "https://docs.unity.com/ugs/en-us/manual/cloud-save/manual/tutorials/unity-sdk";
        public override string Category => "Players";
        public override int Order => 30;

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "// Players are signed in anonymously by themselves the first time you save or load.\n" +
            "SdkManager.Accounts.SaveText(\"progress\", json);\n" +
            "SdkManager.Accounts.LoadText(\"progress\", (found, value) => { });\n\n" +
            "string playerId = SdkManager.Accounts.PlayerId;";

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
            var hint = new Label("Nothing to enter here. Link your project to a Unity Cloud project (Project Settings > Services). Without the package installed, saves stay on the device only, so your game works before this is set up.");
            hint.AddToClassList("section-hint");
            container.Add(hint);
        }
    }
}
