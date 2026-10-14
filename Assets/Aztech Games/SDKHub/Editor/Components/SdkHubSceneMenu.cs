using System;
using SDKHub.Components;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SDKHub.Editor
{
    // Creates the ready-made objects. Everything they do is set in the hub, so there is nothing to fill in afterwards.
    static class SdkHubSceneMenu
    {
        const string Root = "GameObject/SDK Hub/";

        // ---- Menu items -----------------------------------------------------------

        [MenuItem(Root + "Privacy Panel", false, 5)]
        static void CreatePrivacyPanel(MenuCommand command)
        {
            if (!EnsureTextMeshPro()) return;
            var parent = ResolveParent(command);
            EnsureEventSystem();
            var root = BuildPrivacyPanel(parent);
            Undo.RegisterCreatedObjectUndo(root, "Create Privacy Panel");
            Selection.activeGameObject = root;
        }

        [MenuItem(Root + "IAP Buy Button", false, 30)]
        static void CreateBuyButtonItem(MenuCommand command)
        {
            CreateBuyButton(FirstProductId(), command);
        }

        [MenuItem(Root + "Rewarded Ad Button", false, 31)]
        static void CreateRewardedItem(MenuCommand command)
        {
            CreateAdButton(AdFormat.Rewarded, FirstPlacement(AdFormat.Rewarded), command);
        }

        [MenuItem(Root + "Interstitial Ad Button", false, 32)]
        static void CreateInterstitialItem(MenuCommand command)
        {
            CreateAdButton(AdFormat.Interstitial, FirstPlacement(AdFormat.Interstitial), command);
        }

        [MenuItem(Root + "Facebook Login Button", false, 33)]
        static void CreateFacebookItem(MenuCommand command)
        {
            var button = CreateButton("Facebook Login Button", "Log in with Facebook", command, false);
            if (button != null) button.AddComponent<FacebookLoginButton>();
        }

        [MenuItem(Root + "Review Button", false, 36)]
        static void CreateReviewItem(MenuCommand command)
        {
            var button = CreateButton("Review Button", "Rate this game", command, false);
            if (button != null) button.AddComponent<ReviewButton>();
        }

        [MenuItem(Root + "Leaderboard View", false, 38)]
        static void CreateLeaderboardItem(MenuCommand command)
        {
            CreateLeaderboardView(FirstLeaderboard(), command);
        }

        [MenuItem(Root + "Remove Ads Button", false, 39)]
        static void CreateRemoveAdsItem(MenuCommand command)
        {
            CreateRemoveAdsButton(FirstRemoveAds(), command);
        }

        [MenuItem(Root + "Restore Purchases Button", false, 40)]
        static void CreateRestoreItem(MenuCommand command)
        {
            var button = CreateButton("Restore Purchases Button", "Restore purchases", command, false);
            if (button != null) button.AddComponent<RestorePurchasesButton>();
        }

        [MenuItem(Root + "Banner Safe Area (on selected object)", false, 41)]
        static void AddBannerSafeArea()
        {
            var target = Selection.activeGameObject;
            if (target == null || target.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog("Banner Safe Area", "Select your full-screen UI object in the Hierarchy first. The banner space is taken from that object.", "OK");
                return;
            }
            Undo.AddComponent<BannerSafeArea>(target);
        }

        // ---- Called from the hub pages --------------------------------------------

        internal static void AddAd(AdFormat format, string placement)
        {
            CreateAdButton(format, placement, null);
        }

        internal static void AddRemoveAds(string name)
        {
            CreateRemoveAdsButton(name, null);
        }

        internal static void AddBuyButton(string productId)
        {
            CreateBuyButton(productId, null);
        }

        internal static void AddLeaderboardView(string id)
        {
            CreateLeaderboardView(id, null);
        }

        // ---- Creation -------------------------------------------------------------

        static void CreateAdButton(AdFormat format, string placement, MenuCommand command)
        {
            var rewarded = format == AdFormat.Rewarded;
            var suffix = string.IsNullOrEmpty(placement) ? "" : " (" + placement + ")";
            var button = CreateButton((rewarded ? "Rewarded Ad Button" : "Interstitial Ad Button") + suffix,
                rewarded ? "Watch ad" : "Show ad", command, false);
            if (button == null) return;

            Component component = rewarded
                ? (Component)button.AddComponent<RewardedAdButton>()
                : button.AddComponent<InterstitialAdButton>();
            SetString(component, "placement", placement);
        }

        static void CreateRemoveAdsButton(string name, MenuCommand command)
        {
            var suffix = string.IsNullOrEmpty(name) ? "" : " (" + name + ")";
            var button = CreateButton("Remove Ads Button" + suffix, "Remove ads", command, true);
            if (button == null) return;

            var component = button.AddComponent<RemoveAdsButton>();
            SetString(component, "placement", name);
            SetPriceLabel(component, button);
        }

        static void CreateBuyButton(string productId, MenuCommand command)
        {
            var suffix = string.IsNullOrEmpty(productId) ? "" : " (" + productId + ")";
            var button = CreateButton("IAP Buy Button" + suffix, "Buy", command, true);
            if (button == null) return;

            var component = button.AddComponent<IapBuyButton>();
            SetString(component, "productId", productId);
            SetPriceLabel(component, button);
        }

        static void CreateLeaderboardView(string id, MenuCommand command)
        {
            if (!EnsureTextMeshPro()) return;
            var parent = ResolveParent(command);
            EnsureEventSystem();

            var label = CreateLabel(parent, string.IsNullOrEmpty(id) ? "Leaderboard View" : "Leaderboard View (" + id + ")", "Leaderboard", 36f, TextAlignmentOptions.TopLeft);
            Undo.RegisterCreatedObjectUndo(label.gameObject, "Create Leaderboard View");
            ((RectTransform)label.transform).sizeDelta = new Vector2(800f, 600f);

            var view = label.gameObject.AddComponent<LeaderboardView>();
            SetString(view, "leaderboardId", id);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("output").objectReferenceValue = label;
            serialized.ApplyModifiedProperties();
            Selection.activeGameObject = label.gameObject;
        }

        static void SetString(Component component, string field, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedProperties();
        }

        static void SetPriceLabel(Component component, GameObject button)
        {
            var price = button.transform.Find("Price");
            if (price == null) return;
            var serialized = new SerializedObject(component);
            serialized.FindProperty("priceLabel").objectReferenceValue = price.GetComponent<TMP_Text>();
            serialized.ApplyModifiedProperties();
        }

        static string FirstProductId()
        {
            foreach (var product in IapCatalog.Parse(SdkHubConfigProvider.Get("iap", "catalog")).products)
            {
                if (!string.IsNullOrEmpty(product.id)) return product.id;
            }
            return "";
        }

        static string FirstPlacement(AdFormat format)
        {
            foreach (var placement in AdCatalogStore.Load().placements)
            {
                if (placement.format == format && !string.IsNullOrEmpty(placement.name)) return placement.name;
            }
            return "";
        }

        static string FirstRemoveAds()
        {
            var entries = AdCatalogStore.Load().removeAds;
            return entries.Count > 0 ? entries[0].name : "";
        }

        static string FirstLeaderboard()
        {
            foreach (var id in LeaderboardCatalog.Parse(SdkHubConfigProvider.Get("leaderboards", "ids")).ids)
            {
                if (!string.IsNullOrEmpty(id)) return id;
            }
            return "";
        }

        // ---- Building blocks (also used by the demo scene) ------------------------

        static GameObject CreateButton(string name, string text, MenuCommand command, bool withPrice)
        {
            if (!EnsureTextMeshPro()) return null;

            var parent = ResolveParent(command);
            EnsureEventSystem();

            var button = BuildButton(parent, name, text, withPrice);
            Undo.RegisterCreatedObjectUndo(button, "Create " + name);
            Selection.activeGameObject = button;
            return button;
        }

        internal static GameObject BuildButton(GameObject parent, string name, string text, bool withPrice)
        {
            var button = TMP_DefaultControls.CreateButton(DefaultResources());
            button.name = name;
            GameObjectUtility.SetParentAndAlign(button, parent);

            ((RectTransform)button.transform).sizeDelta = new Vector2(420f, withPrice ? 150f : 110f);

            var label = button.GetComponentInChildren<TMP_Text>();
            label.text = text;
            label.fontSize = 42f;

            if (withPrice)
            {
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = new Vector2(0f, 0.5f);
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var price = TMP_DefaultControls.CreateText(DefaultResources());
                price.name = "Price";
                GameObjectUtility.SetParentAndAlign(price, button);
                var priceRect = (RectTransform)price.transform;
                priceRect.anchorMin = Vector2.zero;
                priceRect.anchorMax = new Vector2(1f, 0.5f);
                priceRect.offsetMin = Vector2.zero;
                priceRect.offsetMax = Vector2.zero;
                var priceText = price.GetComponent<TMP_Text>();
                priceText.text = "$0.99";
                priceText.fontSize = 34f;
                priceText.alignment = TextAlignmentOptions.Center;
            }
            return button;
        }

        internal static GameObject BuildPrivacyPanel(GameObject parent)
        {
            if (string.IsNullOrEmpty(SdkHubConfigProvider.Get("privacy", "requireConsent")))
            {
                SdkHubConfigProvider.Set("privacy", "requireConsent", "1");
            }

            var root = new GameObject("Privacy Panel", typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            GameObjectUtility.SetParentAndAlign(root, parent);
            SetRect((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.layer = LayerMask.NameToLayer("UI");
            panel.transform.SetParent(root.transform, false);
            SetRect((RectTransform)panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.layer = LayerMask.NameToLayer("UI");
            card.transform.SetParent(panel.transform, false);
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(860f, 780f);
            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.14f, 0.15f, 0.19f, 1f);

            var title = CreateLabel(card, "Title", PrivacyModule.Title, 52f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            SetRect((RectTransform)title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -130f), new Vector2(-40f, -30f));

            var body = CreateLabel(card, "Info Text", PrivacyModule.Body, 34f, TextAlignmentOptions.Center);
            SetRect((RectTransform)body.transform, Vector2.zero, Vector2.one, new Vector2(50f, 190f), new Vector2(-50f, -150f));

            var agree = BuildButton(card, "Agree Button", PrivacyModule.Agree, false);
            SetRect((RectTransform)agree.transform, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(40f, 40f), new Vector2(-15f, 150f));
            var disagree = BuildButton(card, "Disagree Button", PrivacyModule.Disagree, false);
            SetRect((RectTransform)disagree.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(15f, 40f), new Vector2(-40f, 150f));

            var component = root.AddComponent<PrivacyPanel>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("panel").objectReferenceValue = panel;
            serialized.FindProperty("agreeButton").objectReferenceValue = agree.GetComponent<Button>();
            serialized.FindProperty("disagreeButton").objectReferenceValue = disagree.GetComponent<Button>();
            serialized.ApplyModifiedProperties();
            return root;
        }

        internal static TMP_Text CreateLabel(GameObject parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            var go = TMP_DefaultControls.CreateText(DefaultResources());
            go.name = name;
            GameObjectUtility.SetParentAndAlign(go, parent);
            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static GameObject ResolveParent(MenuCommand command)
        {
            var context = command != null ? command.context as GameObject : null;
            if (context == null) context = Selection.activeGameObject;
            if (context != null && context.GetComponentInParent<Canvas>() != null) return context;

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.gameObject : CreateCanvas();
        }

        internal static GameObject CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            go.layer = LayerMask.NameToLayer("UI");
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        internal static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
#if ENABLE_INPUT_SYSTEM
            var module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            go.AddComponent(module ?? typeof(StandaloneInputModule));
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        internal static bool EnsureTextMeshPro()
        {
            if (TMP_Settings.instance != null) return true;
            if (EditorUtility.DisplayDialog("TextMesh Pro", "TextMesh Pro essential resources are needed to create buttons. Import them now? Then try again.", "Import", "Cancel"))
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            }
            return false;
        }

        internal static TMP_DefaultControls.Resources DefaultResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }
    }
}
