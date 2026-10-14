using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class PushModule : SdkModule
    {
        public override string Id => "push";
        public override string DisplayName => "Push Notifications";
        public override string Description => "Remote push notifications through Unity Gaming Services. Send campaigns from the Unity Dashboard.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.services.push-notifications";
        public override string DefineSymbol => "SDKHUB_PUSH";
        public override string DocsUrl => "https://docs.unity.com/en-us/push-notifications/get-started";
        public override int Order => 45;
        public override string Category => "Engagement";

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "SdkManager.Push.TokenReceived += token => SendToYourServer(token);\n" +
            "SdkManager.Push.MessageReceived += data => { };\n\n" +
            "// Start it when you want the permission pop-up, for example after the player agreed.\n" +
            "// The true says the player agreed to analytics.\n" +
            "SdkManager.Push.Initialize(true);";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Unity Dashboard", "https://cloud.unity.com"),
            new SdkLink("Firebase Console", "https://console.firebase.google.com")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
            {
                yield return new CheckItem(CheckLevel.Warning, "Push needs a minimum Android API level of 26 or higher");
            }
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                yield return new CheckItem(CheckLevel.Warning, "Project is not linked to a Unity Cloud project (Project Settings > Services)");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var steps = new Label(
                "Setup outside Unity, in this order:\n" +
                "1. Enter the Firebase settings in Project Settings > Services > Push Notifications.\n" +
                "2. Upload the Firebase service account key in the Unity Dashboard (Android).\n" +
                "3. Upload the Apple APNs key in the Unity Dashboard (iOS).\n" +
                "4. On iOS, turn on Request Authorization, Enable Push Notifications and Register on App Launch in Project Settings > Mobile Notifications, and add the Remote notifications capability in Xcode.");
            steps.AddToClassList("section-hint");
            container.Add(steps);

            container.Add(SdkHubUI.MakeButton("Open Push settings", () =>
                SettingsService.OpenProjectSettings("Project/Services/Push Notifications")));
        }
    }
}
