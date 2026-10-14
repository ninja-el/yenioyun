using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class NotificationsModule : SdkModule
    {
        public override string Id => "notifications";
        public override string DisplayName => "Local Notifications";
        public override string Description => "Reminders and reward notifications on Android and iOS.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.mobile.notifications";
        public override string DefineSymbol => "SDKHUB_NOTIFICATIONS";
        public override string DocsUrl => "https://docs.unity3d.com/Packages/com.unity.mobile.notifications@2.4/manual/Unified.html";
        public override int Order => 40;
        public override string Category => "Engagement";

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "SdkManager.Notifications.RequestPermission();\n\n" +
            "// Show a notification after 3 hours\n" +
            "int id = SdkManager.Notifications.Schedule(\"Your reward is ready\", \"Come and collect it\", TimeSpan.FromHours(3));\n" +
            "SdkManager.Notifications.Cancel(id);\n" +
            "SdkManager.Notifications.CancelAll();";

        public override IEnumerable<CheckItem> Validate()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
            if (info == null) yield break;

            var text = info.version.Split('-')[0];
            if (!Version.TryParse(text, out var version) || version < new Version(2, 3))
            {
                yield return new CheckItem(CheckLevel.Error, $"Mobile Notifications 2.3 or newer is required (found {info.version})");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            Heading(container, "Come-back reminder");
            Hint(container, "Reminds players to return. It is set when they leave the game and cancelled when they come back. Nothing to start in code.");
            container.Add(SdkHubUI.ConfigToggle(this, "reminderEnabled", "Remind players who leave the game", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "reminderTitle", "Title", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "reminderText", "Text", onChanged));
            container.Add(SdkHubUI.ConfigFloat(this, "reminderHours", "Remind after (hours)", 24f, "How long after the player leaves the notification appears.", onChanged));

            Heading(container, "Android channel (optional)");
            Hint(container, "Leave empty to use the defaults.");
            container.Add(SdkHubUI.ConfigField(this, "channelId", "Channel ID", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "channelName", "Channel name", onChanged));
            container.Add(SdkHubUI.ConfigField(this, "channelDescription", "Channel description", onChanged));

            Hint(container, "Notification icons are set in Project Settings (look for Mobile Notifications).");
            container.Add(SdkHubUI.MakeButton("Open Project Settings", () => SettingsService.OpenProjectSettings("Project/Mobile Notifications")));
        }

        static void Heading(VisualElement container, string text)
        {
            var heading = new Label(text);
            heading.AddToClassList("section-title");
            heading.AddToClassList("group-title");
            container.Add(heading);
        }

        static void Hint(VisualElement container, string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("section-hint");
            container.Add(hint);
        }
    }
}
