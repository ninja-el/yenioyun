using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class FacebookModule : SdkModule
    {
        public override string Id => "facebook";
        public override string DisplayName => "Facebook";
        public override string Description => "Facebook Login, app events and sharing through Meta's official SDK.";
        public override InstallType InstallType => InstallType.External;
        public override string DetectionType => "Facebook.Unity.FB";
        public override string DefineSymbol => "SDKHUB_FACEBOOK";
        public override string DownloadUrl => "https://developers.facebook.com/docs/unity/downloads";
        public override string DocsUrl => "https://developers.facebook.com/docs/unity/gettingstarted";
        public override int Order => 50;
        public override string Category => "Engagement";

        public override IReadOnlyList<SdkSceneAction> SceneActions => new[]
        {
            new SdkSceneAction("Add Login Button", "GameObject/SDK Hub/Facebook Login Button")
        };

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "// Log in (the SDK starts by itself)\n" +
            "SdkManager.Facebook.Login(success => { });\n" +
            "SdkManager.Facebook.Logout();\n\n" +
            "// Who is logged in\n" +
            "bool loggedIn = SdkManager.Facebook.IsLoggedIn;\n" +
            "string id = SdkManager.Facebook.UserId;\n\n" +
            "// App events\n" +
            "SdkManager.Facebook.LogEvent(\"level_complete\");";

        public override string InstallHint =>
            "Download FacebookSDK.unitypackage from Meta, then drop it below and click Import. If an older Facebook SDK is already in your project, remove it first.";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Meta for Developers", "https://developers.facebook.com/apps")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            if (!IsInstalled || FacebookSettingsAccess.SettingsType == null) yield break;

            if (string.IsNullOrEmpty(FacebookSettingsAccess.ReadFirst("AppIds")))
            {
                yield return new CheckItem(CheckLevel.Error, "Enter your Facebook App ID");
            }
            else
            {
                yield return new CheckItem(CheckLevel.Ok, "App ID is set");
            }

            if (string.IsNullOrEmpty(FacebookSettingsAccess.ReadFirst("ClientTokens")))
            {
                yield return new CheckItem(CheckLevel.Warning, "Client token is empty (Settings > Advanced in your Facebook app)");
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var heading = new Label("Facebook app");
            heading.AddToClassList("section-title");
            heading.AddToClassList("group-title");
            container.Add(heading);

            if (!IsInstalled || FacebookSettingsAccess.SettingsType == null)
            {
                var notice = new Label("Import the SDK first. Then you can enter your App ID and Client token here.");
                notice.AddToClassList("section-hint");
                container.Add(notice);
            }
            else
            {
                container.Add(BuildSettingField("AppIds", "App ID", onChanged));
                container.Add(BuildSettingField("ClientTokens", "Client token", onChanged));
                container.Add(SdkHubUI.MakeButton("Open Facebook settings", () =>
                {
                    if (!EditorApplication.ExecuteMenuItem("Facebook/Edit Settings"))
                    {
                        EditorUtility.DisplayDialog("Facebook", "Open Facebook > Edit Settings from the Unity menu bar.", "OK");
                    }
                }));
            }

            var permissions = SdkHubUI.ConfigField(this, "permissions", "Login permissions", onChanged);
            permissions.tooltip = "Separated by commas. Empty uses public_profile and email.";
            container.Add(permissions);

            var platforms = new Label("Platform IDs");
            platforms.AddToClassList("section-title");
            platforms.AddToClassList("group-title");
            container.Add(platforms);

            var hint = new Label("In your Facebook app settings, add each platform and enter its Bundle ID. For Android you also need the key hash and class name (see the Facebook menu in Unity).");
            hint.AddToClassList("section-hint");
            container.Add(hint);

            container.Add(ReadOnlyField("Android Bundle ID", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)));
            container.Add(ReadOnlyField("iOS Bundle ID", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS)));

        }

        static TextField BuildSettingField(string property, string label, Action onChanged)
        {
            var field = new TextField(label) { value = FacebookSettingsAccess.ReadFirst(property) };
            field.AddToClassList("field");
            field.RegisterValueChangedCallback(evt =>
            {
                if (!FacebookSettingsAccess.WriteFirst(property, evt.newValue.Trim()))
                {
                    UnityEngine.Debug.LogWarning("[SDK Hub] Could not write the Facebook setting. Use Facebook > Edit Settings instead.");
                }
                onChanged?.Invoke();
            });
            field.RegisterCallback<FocusOutEvent>(_ => AssetDatabase.SaveAssets());
            return field;
        }

        static TextField ReadOnlyField(string label, string value)
        {
            var field = new TextField(label) { value = value, isReadOnly = true };
            field.AddToClassList("field");
            return field;
        }
    }

    static class FacebookSettingsAccess
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Static;
        static Type settingsType;

        public static Type SettingsType
        {
            get
            {
                if (settingsType != null) return settingsType;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    settingsType = assembly.GetType("Facebook.Unity.Settings.FacebookSettings", false);
                    if (settingsType != null) break;
                }
                return settingsType;
            }
        }

        public static string ReadFirst(string property)
        {
            var type = SettingsType;
            if (type == null) return "";
            var list = type.GetProperty(property, Flags)?.GetValue(null) as List<string>;
            return list != null && list.Count > 0 ? list[0] : "";
        }

        public static bool WriteFirst(string property, string value)
        {
            var type = SettingsType;
            if (type == null) return false;

            var written = Update(type, property, list =>
            {
                if (list.Count == 0) list.Add(value);
                else list[0] = value;
            });

            foreach (var other in new[] { "AppIds", "ClientTokens", "AppLabels" })
            {
                if (other == property) continue;
                Update(type, other, list =>
                {
                    if (list.Count == 0) list.Add(other == "AppLabels" ? "App Name" : "");
                });
            }
            return written;
        }

        static bool Update(Type type, string property, Action<List<string>> change)
        {
            var info = type.GetProperty(property, Flags);
            if (info == null || !info.CanRead || !info.CanWrite) return false;
            var current = info.GetValue(null) as List<string>;
            var copy = current != null ? new List<string>(current) : new List<string>();
            change(copy);
            info.SetValue(null, copy);
            return true;
        }
    }
}
