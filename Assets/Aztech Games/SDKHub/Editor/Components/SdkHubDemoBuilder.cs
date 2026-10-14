using System.IO;
using System.Reflection;
using SDKHub.Components;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SDKHub.Editor
{
    static class SdkHubDemoBuilder
    {
        [MenuItem("Tools/Aztech Games/Create Demo Scene")]
        public static void Create()
        {
            if (!SdkHubSceneMenu.EnsureTextMeshPro()) return;

            var folder = SdkHubPaths.RootFolder + "/Demo";
            var path = folder + "/SDKHubDemo.unity";
            if (File.Exists(path) && !EditorUtility.DisplayDialog("Demo scene", "The demo scene already exists. Replace it?", "Replace", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.1f, 0.11f, 0.14f);
            }

            var canvas = SdkHubSceneMenu.CreateCanvas();
            SdkHubSceneMenu.EnsureEventSystem();
            BuildTitle(canvas);
            var log = BuildLog(canvas);
            BuildButtons(canvas, log);
            BuildPrivacy(canvas, log);

            Directory.CreateDirectory(folder);
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));
            EditorUtility.DisplayDialog("Demo scene", "The demo scene was created. Press Play to try every SDK. Without the SDKs installed it runs in mock mode.", "OK");
        }

        static void BuildTitle(GameObject canvas)
        {
            var title = SdkHubSceneMenu.CreateLabel(canvas, "Title", "Aztech Games SDK Hub Demo", 56f, TextAlignmentOptions.Center);
            var rect = (RectTransform)title.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -70f);
            rect.sizeDelta = new Vector2(-80f, 120f);
        }

        static SdkHubDemoLog BuildLog(GameObject canvas)
        {
            var root = new GameObject("Demo Log", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0.27f);
            rect.offsetMin = new Vector2(40f, 40f);
            rect.offsetMax = new Vector2(-40f, -20f);

            var text = SdkHubSceneMenu.CreateLabel(root, "Output", "Events appear here.", 30f, TextAlignmentOptions.TopLeft);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var log = root.AddComponent<SdkHubDemoLog>();
            var serialized = new SerializedObject(log);
            serialized.FindProperty("output").objectReferenceValue = text;
            serialized.ApplyModifiedProperties();
            return log;
        }

        static void BuildPrivacy(GameObject canvas, SdkHubDemoLog log)
        {
            var root = SdkHubSceneMenu.BuildPrivacyPanel(canvas);
            var panel = root.GetComponent<PrivacyPanel>();
            WireVoid(panel, "onAgreed", log, "Privacy: player agreed");
            WireVoid(panel, "onDisagreed", log, "Privacy: player disagreed");
        }

        static void BuildButtons(GameObject canvas, SdkHubDemoLog log)
        {
            var panel = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            panel.layer = LayerMask.NameToLayer("UI");
            panel.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -220f);
            rect.sizeDelta = new Vector2(800f, 800f);

            var grid = panel.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(392f, 100f);
            grid.spacing = new Vector2(16f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Each button below calls one method of SdkHubDemo, which is a short example of using the SDK Hub from code.
            var demo = new GameObject("SDK Hub Demo").AddComponent<SdkHubDemo>();
            var demoSerialized = new SerializedObject(demo);
            demoSerialized.FindProperty("log").objectReferenceValue = log;
            demoSerialized.ApplyModifiedProperties();

            var buy = AddButton(panel, "IAP Buy Button", "Buy", true);
            var buyComponent = buy.AddComponent<IapBuyButton>();
            SetString(buyComponent, "productId", FirstProductId());
            var buySerialized = new SerializedObject(buyComponent);
            buySerialized.FindProperty("priceLabel").objectReferenceValue = buy.transform.Find("Price").GetComponent<TMP_Text>();
            buySerialized.ApplyModifiedProperties();
            WireString(buyComponent, "onPurchaseSucceeded", log.LogPurchased);
            WireString(buyComponent, "onPurchaseFailed", log.LogPurchaseFailed);

            var rewarded = AddButton(panel, "Rewarded Ad Button", "Rewarded ad", false);
            var rewardedComponent = rewarded.AddComponent<RewardedAdButton>();
            SetString(rewardedComponent, "placement", FirstPlacement(AdFormat.Rewarded));
            WireVoid(rewardedComponent, "onSuccess", log, "Rewarded ad finished: reward granted");
            WireVoid(rewardedComponent, "onCancelled", log, "Rewarded ad cancelled: no reward");
            WireVoid(rewardedComponent, "onFailed", log, "Rewarded ad failed: no ad available");

            var interstitial = AddButton(panel, "Interstitial Ad Button", "Interstitial ad", false);
            var interstitialComponent = interstitial.AddComponent<InterstitialAdButton>();
            SetString(interstitialComponent, "placement", FirstPlacement(AdFormat.Interstitial));
            WireVoid(interstitialComponent, "onClosed", log, "Interstitial ad closed (or skipped by a rule)");

            Call(AddButton(panel, "Show Banner Button", "Show banner", false), demo.ShowBanner);
            Call(AddButton(panel, "Hide Banner Button", "Hide banner", false), demo.HideBanner);

            var facebook = AddButton(panel, "Facebook Login Button", "Facebook login", false);
            var facebookComponent = facebook.AddComponent<FacebookLoginButton>();
            WireVoid(facebookComponent, "onLoggedIn", log, "Facebook: logged in");
            WireVoid(facebookComponent, "onLoginFailed", log, "Facebook: login failed or cancelled");
            WireVoid(facebookComponent, "onLoggedOut", log, "Facebook: logged out");

            Call(AddButton(panel, "Notification Button", "Notify in 10 s", false), demo.NotifyIn10Seconds);
            Call(AddButton(panel, "Analytics Button", "Analytics event", false), demo.SendAnalyticsEvent);
            Call(AddButton(panel, "Submit Score Button", "Submit score 100", false), demo.SubmitScore);

            var review = AddButton(panel, "Review Button", "Rate the game", false);
            review.AddComponent<ReviewButton>();

            Call(AddButton(panel, "Update Button", "Check for update", false), demo.CheckForUpdate);
            Call(AddButton(panel, "Remove Ads Button", "Remove all ads", false), demo.RemoveAllAds);
            Call(AddButton(panel, "Restore Ads Button", "Restore ads", false), demo.RestoreAds);
        }

        static void Call(GameObject button, UnityAction action)
        {
            UnityEventTools.AddVoidPersistentListener(button.GetComponent<Button>().onClick, action);
        }

        static GameObject AddButton(GameObject panel, string name, string text, bool withPrice)
        {
            var button = SdkHubSceneMenu.BuildButton(panel, name, text, withPrice);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>())
            {
                if (label.name != "Price") label.fontSize = 32f;
            }
            return button;
        }

        static string FirstPlacement(AdFormat format)
        {
            foreach (var placement in AdCatalogStore.Load().placements)
            {
                if (placement.format == format && !string.IsNullOrEmpty(placement.name)) return placement.name;
            }
            return "";
        }

        static string FirstLeaderboard()
        {
            foreach (var id in LeaderboardCatalog.Parse(SdkHubConfigProvider.Get("leaderboards", "ids")).ids)
            {
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return "";
        }

        static string FirstProductId()
        {
            foreach (var product in IapCatalog.Parse(SdkHubConfigProvider.Get("iap", "catalog")).products)
            {
                if (!string.IsNullOrEmpty(product.id)) return product.id;
            }
            return "";
        }

        static void SetString(Component component, string field, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedProperties();
        }

        static UnityEventBase GetEvent(Component target, string field)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            return info != null ? info.GetValue(target) as UnityEventBase : null;
        }

        static void WireVoid(Component target, string field, SdkHubDemoLog log, string message)
        {
            if (GetEvent(target, field) is UnityEvent unityEvent)
            {
                UnityEventTools.AddStringPersistentListener(unityEvent, log.LogMessage, message);
                EditorUtility.SetDirty(target);
            }
        }

        static void WireString(Component target, string field, UnityAction<string> action)
        {
            if (GetEvent(target, field) is UnityEvent<string> unityEvent)
            {
                UnityEventTools.AddPersistentListener(unityEvent, action);
                EditorUtility.SetDirty(target);
            }
        }
    }
}
