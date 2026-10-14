using System;
using System.Collections.Generic;
using SDKHub.Components;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SDKHub.Editor
{
    public sealed class ReleaseCheck
    {
        public string Area;
        public CheckLevel Level;
        public string Title;
        public string Detail;
        public string FixLabel;
        public Action Fix;

        // False for fixes that only open another page or window; re-running would send you straight back.
        public bool RerunAfterFix = true;
    }

    // Looks through the project for things that commonly block a store release.
    public static class ReleaseChecker
    {
        public const string AreaProject = "Project";
        public const string AreaAndroid = "Android";
        public const string AreaIos = "iOS";
        public const string AreaSdks = "SDK setup";
        public const string AreaPrivacy = "Privacy";
        public const string AreaPurchases = "Purchases";
        public const string AreaHub = "Hub";

        public static List<ReleaseCheck> Run(Action<string> openPage)
        {
            var list = new List<ReleaseCheck>();
            CheckProject(list);
            CheckAndroid(list);
            CheckIos(list, openPage);
            CheckSdks(list, openPage);
            CheckPrivacy(list, openPage);
            CheckPurchases(list, openPage);
            CheckHub(list);
            return list;
        }

        static void Add(List<ReleaseCheck> list, string area, CheckLevel level, string title, string detail, string fixLabel = null, Action fix = null, bool rerunAfterFix = true)
        {
            list.Add(new ReleaseCheck { Area = area, Level = level, Title = title, Detail = detail, FixLabel = fixLabel, Fix = fix, RerunAfterFix = rerunAfterFix });
        }

        static void OpenPlayerSettings()
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        static bool Installed(string moduleId)
        {
            var module = SdkRegistry.Find(moduleId);
            return module != null && module.IsInstalled;
        }

        // ---- Project ----------------------------------------------------------------

        static void CheckProject(List<ReleaseCheck> list)
        {
            if (ReleaseRules.IsPlaceholderCompany(PlayerSettings.companyName))
                Add(list, AreaProject, CheckLevel.Warning, "Company name is still the default", "Set your own company name in Player Settings.", "Open Player Settings", OpenPlayerSettings, false);
            else
                Add(list, AreaProject, CheckLevel.Ok, "Company name is set", PlayerSettings.companyName);

            if (string.IsNullOrWhiteSpace(PlayerSettings.productName))
                Add(list, AreaProject, CheckLevel.Error, "Product name is empty", "Players see this name under the app icon.", "Open Player Settings", OpenPlayerSettings, false);
            else
                Add(list, AreaProject, CheckLevel.Ok, "Product name is set", PlayerSettings.productName);

            if (ReleaseRules.IsValidVersion(PlayerSettings.bundleVersion))
                Add(list, AreaProject, CheckLevel.Ok, "Version number is valid", PlayerSettings.bundleVersion);
            else
                Add(list, AreaProject, CheckLevel.Warning, "Version number looks unusual", "Stores expect numbers like 1.0 or 1.2.3. It is now: " + PlayerSettings.bundleVersion, "Open Player Settings", OpenPlayerSettings, false);

            if (Application.unityVersion.StartsWith("6000"))
                Add(list, AreaProject, CheckLevel.Ok, "Unity 6", Application.unityVersion);
            else
                Add(list, AreaProject, CheckLevel.Warning, "SDK Hub is made for Unity 6", "You are using Unity " + Application.unityVersion + ".");

            var hasScene = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) hasScene = true;
            }
            if (hasScene) Add(list, AreaProject, CheckLevel.Ok, "A scene is in the build", "");
            else Add(list, AreaProject, CheckLevel.Error, "No scene is in the build", "Add your scenes in the build window, or the game will be empty.", "Open build window", () => BuildPlayerWindow.ShowBuildPlayerWindow(), false);
        }

        // ---- Android ----------------------------------------------------------------

        static void CheckAndroid(List<ReleaseCheck> list)
        {
            var id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            CheckApplicationId(list, AreaAndroid, id);

            var minApi = (int)PlayerSettings.Android.minSdkVersion;
            var required = Installed("push") ? 26 : 0;
            if (ReleaseRules.IsAndroidApiEnough(minApi, required))
            {
                Add(list, AreaAndroid, CheckLevel.Ok, "Minimum API level", "API " + minApi + (required > 0 ? " (Push needs " + required + " or higher)" : ""));
            }
            else
            {
                Add(list, AreaAndroid, CheckLevel.Error, "Minimum API level is too low for Push Notifications",
                    "It is " + minApi + ". Push needs " + required + " or higher.", "Set to " + required,
                    () => PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)required);
            }

            if (PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto)
                Add(list, AreaAndroid, CheckLevel.Ok, "Target API level", "Highest installed. Google Play asks for a recent level, and this keeps it current.");
            else
                Add(list, AreaAndroid, CheckLevel.Warning, "Target API level is fixed", "Google Play requires a recent target level. 'Highest installed' keeps it current.",
                    "Use highest installed", () => PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto);

            var il2cpp = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP;
            var arm64 = (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
            if (il2cpp && arm64)
            {
                Add(list, AreaAndroid, CheckLevel.Ok, "64-bit build", "IL2CPP with ARM64");
            }
            else
            {
                Add(list, AreaAndroid, CheckLevel.Error, "Google Play needs a 64-bit build",
                    "Use the IL2CPP scripting backend and tick ARM64.", "Fix both",
                    () =>
                    {
                        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                    });
            }

            if (EditorUserBuildSettings.buildAppBundle)
                Add(list, AreaAndroid, CheckLevel.Ok, "App Bundle", "Google Play takes Android App Bundles (.aab).");
            else
                Add(list, AreaAndroid, CheckLevel.Warning, "Build App Bundle is off", "Google Play takes .aab files for new apps. A .apk is only for testing.",
                    "Turn on", () => EditorUserBuildSettings.buildAppBundle = true);

            if (PlayerSettings.Android.useCustomKeystore && !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
                Add(list, AreaAndroid, CheckLevel.Ok, "Signing keystore", "A custom keystore is set.");
            else
                Add(list, AreaAndroid, CheckLevel.Warning, "No custom keystore", "Builds are signed with a debug key, which Google Play does not accept for release.", "Open Player Settings", OpenPlayerSettings, false);
        }

        // ---- iOS --------------------------------------------------------------------

        static void CheckIos(List<ReleaseCheck> list, Action<string> openPage)
        {
            var id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            CheckApplicationId(list, AreaIos, id);

            if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID))
                Add(list, AreaIos, CheckLevel.Warning, "No Apple Developer Team ID", "You need it to sign and run the build on a device.", "Open Player Settings", OpenPlayerSettings, false);
            else
                Add(list, AreaIos, CheckLevel.Ok, "Apple Developer Team ID", "Set.");

            var tracksPlayers = Installed("levelplay") || Installed("facebook");
            if (tracksPlayers && !Installed("att"))
            {
                Add(list, AreaIos, CheckLevel.Warning, "Apple tracking permission is not set up",
                    "Ads and Facebook track players. Apple requires a permission pop-up first.", "Open Privacy page", () => openPage?.Invoke("privacy"), false);
            }
            else if (Installed("att"))
            {
                Add(list, AreaIos, CheckLevel.Ok, "Apple tracking permission", "iOS Tracking Support is installed. The pop-up text is added to your Xcode project when you build.");
            }
        }

        static void CheckApplicationId(List<ReleaseCheck> list, string area, string id)
        {
            if (!ReleaseRules.IsValidApplicationId(id))
                Add(list, area, CheckLevel.Error, "App identifier is not valid", "It is now: " + id + ". Use letters, numbers and dots, like com.yourstudio.yourgame.", "Open Player Settings", OpenPlayerSettings, false);
            else if (ReleaseRules.IsPlaceholderApplicationId(id))
                Add(list, area, CheckLevel.Error, "App identifier is still a placeholder", "It is now: " + id + ". Choose your own, like com.yourstudio.yourgame.", "Open Player Settings", OpenPlayerSettings, false);
            else
                Add(list, area, CheckLevel.Ok, "App identifier", id);
        }

        // ---- SDK setup --------------------------------------------------------------

        static void CheckSdks(List<ReleaseCheck> list, Action<string> openPage)
        {
            var any = false;
            foreach (var module in SdkRegistry.Modules)
            {
                if (module.IsUtility || module.InstallType == InstallType.BuiltIn || !module.IsInstalled) continue;
                any = true;

                var problems = 0;
                var id = module.Id;
                foreach (var item in module.Checks)
                {
                    if (item.Level == CheckLevel.Ok) continue;
                    problems++;
                    Add(list, AreaSdks, item.Level, module.DisplayName, item.Message, "Open " + module.NavName, () => openPage?.Invoke(id), false);
                }
                if (problems == 0) Add(list, AreaSdks, CheckLevel.Ok, module.DisplayName, "All checks passed.");
            }

            if (!any) Add(list, AreaSdks, CheckLevel.Ok, "No SDKs installed yet", "Nothing to set up.");
        }

        // ---- Privacy ----------------------------------------------------------------

        static void CheckPrivacy(List<ReleaseCheck> list, Action<string> openPage)
        {
            var collects = Installed("levelplay") || Installed("analytics") || Installed("push") || Installed("facebook");
            if (!collects)
            {
                Add(list, AreaPrivacy, CheckLevel.Ok, "No data-collecting SDKs installed", "");
                return;
            }

            var requires = SdkHubConfigProvider.Get("privacy", "requireConsent") == "1";
            if (!requires)
            {
                Add(list, AreaPrivacy, CheckLevel.Warning, "Services start without asking the player",
                    "Ads, analytics, push or Facebook are installed, but they do not wait for the player's answer.", "Open Privacy page", () => openPage?.Invoke("privacy"), false);
                return;
            }

            Add(list, AreaPrivacy, CheckLevel.Ok, "Services wait for the player's answer", "");

            var panels = UnityEngine.Object.FindObjectsByType<PrivacyPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (panels.Length > 0)
                Add(list, AreaPrivacy, CheckLevel.Ok, "Privacy panel in the open scene", "");
            else
                Add(list, AreaPrivacy, CheckLevel.Warning, "No privacy panel in the open scene",
                    "Open your first scene and add one, or players are never asked and these services never start. Other scenes are not checked.", "Add privacy panel",
                    () => EditorApplication.ExecuteMenuItem("GameObject/SDK Hub/Privacy Panel"));
        }

        // ---- Purchases --------------------------------------------------------------

        static void CheckPurchases(List<ReleaseCheck> list, Action<string> openPage)
        {
            if (!Installed("iap"))
            {
                Add(list, AreaPurchases, CheckLevel.Ok, "In-App Purchases not installed", "");
                return;
            }

            var oneTime = false;
            foreach (var product in IapCatalog.Parse(SdkHubConfigProvider.Get("iap", "catalog")).products)
            {
                if (product.kind == IapProductKind.NonConsumable) oneTime = true;
            }

            if (oneTime)
            {
                var buttons = UnityEngine.Object.FindObjectsByType<RestorePurchasesButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (buttons.Length > 0)
                    Add(list, AreaPurchases, CheckLevel.Ok, "Restore Purchases button in the open scene", "");
                else
                    Add(list, AreaPurchases, CheckLevel.Warning, "No Restore Purchases button in the open scene",
                        "Apple requires a way to restore one-time purchases. Other scenes are not checked.", "Add button",
                        () => EditorApplication.ExecuteMenuItem("GameObject/SDK Hub/Restore Purchases Button"));
            }

            if (SdkHubConfigProvider.Get("validation", "enabled") == "1")
                Add(list, AreaPurchases, CheckLevel.Ok, "Purchases are checked by your server", "");
            else
                Add(list, AreaPurchases, CheckLevel.Warning, "Purchases are not checked by a server", "Without it, fake purchases could unlock items. Fine for a first test build.", "Open Purchase validation", () => openPage?.Invoke("validation"), false);
        }

        // ---- Hub --------------------------------------------------------------------

        static void CheckHub(List<ReleaseCheck> list)
        {
            if (!PackageInstaller.HasLoaded)
            {
                Add(list, AreaHub, CheckLevel.Warning, "Package list not read yet", "Press Refresh in the hub, then run the check again.", "Refresh", PackageInstaller.Refresh);
                return;
            }

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));

            var missing = 0;
            foreach (var module in SdkRegistry.Modules)
            {
                if (!module.IsInstalled || ReleaseRules.HasDefine(defines, module.DefineSymbol)) continue;
                missing++;
                Add(list, AreaHub, CheckLevel.Warning, module.DisplayName + " is installed but not switched on",
                    "Its code is not compiled for " + group + " yet.", "Switch on", SdkRegistry.SyncDefines);
            }
            if (missing == 0) Add(list, AreaHub, CheckLevel.Ok, "SDK code matches the installed SDKs", "");
        }
    }
}
