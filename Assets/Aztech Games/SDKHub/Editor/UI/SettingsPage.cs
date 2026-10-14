using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    static class SettingsPage
    {
        public const string Version = "0.9.6";

        public static VisualElement Build()
        {
            var page = new VisualElement();
            page.AddToClassList("page");

            var title = new Label("Settings");
            title.AddToClassList("page-title");
            page.Add(title);

            var subtitle = new Label($"Aztech Games SDK Hub {Version}. Requires Unity 6 (6000.0) or newer. Running Unity {Application.unityVersion}.");
            subtitle.AddToClassList("page-subtitle");
            page.Add(subtitle);

            var section = SdkHubUI.Section("Configuration asset");
            var hint = new Label("All keys and IDs you enter in the Hub are stored in one asset that your game reads at runtime.");
            hint.AddToClassList("section-hint");
            section.Add(hint);
            section.Add(SdkHubUI.MakeButton("Select config asset", () =>
            {
                var config = SdkHubConfigProvider.GetOrCreate();
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }));
            page.Add(section);

            var testing = SdkHubUI.Section("Testing in the Editor");
            var testingHint = new Label("Ads that exist on one platform only can be tested here. In the Editor, the hub pretends to be the platform you pick. Builds are not affected.");
            testingHint.AddToClassList("section-hint");
            testing.Add(testingHint);
            testing.Add(SdkHubUI.ConfigChoice("general", "editorPlatform", "Pretend to be",
                new[] { "", "Android", "iOS" }, new[] { "My build target", "Android", "iOS" }, 0,
                "Used only in the Editor.", null));
            page.Add(testing);
            return page;
        }
    }
}
