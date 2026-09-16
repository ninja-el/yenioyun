using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using MatchPack.Meta;
using MatchPack.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// MainScene'deki hazır UI görsellerini çalışan sisteme bağlayan tek seferlik araç.
    /// Manager'ları kurar, panel script'lerini ekler, Inspector referanslarını yazar, panellerin
    /// açık/kapalı durumunu normalize eder ve eksik asset'leri üretir.
    /// İş bittikten sonra Editor/UIIntegration klasörü tamamen silinebilir.
    /// </summary>
    public static class UIIntegrationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainScene.unity";

        private const string MarketContentPath = "MainMenu/MarketPanel/Scroll View/Viewport/Content";

        // Yığının oturması için beklenecek en uzun süre; bu süre boyunca dokunuş hamle üretmiyor.
        private const float StackSettleTimeout = 1f;

        // Market panelindeki gold kartlarının sırası ile ürün kimlikleri.
        private static readonly string[] MarketCoinNodes =
        {
            "Gold_1", "Gold_1 (1)", "Gold_1 (2)", "Gold_1 (3)", "Gold_1 (4)", "Gold_1 (5)"
        };

        private static readonly string[] MarketCoinProducts =
        {
            "coins_1000", "coins_5000", "coins_10000", "coins_25000", "coins_50000", "coins_100000"
        };

        // Gold popup'ındaki kartların sırası markettekinden farklı; eşleme görseldeki tutara göre.
        private static readonly string[] PopupCoinNodes =
        {
            "Gold_1", "Gold_1 (1)", "Gold_1 (2)", "Gold_1 (3)", "Gold_1 (4)", "Gold_1 (5)"
        };

        private static readonly string[] PopupCoinProducts =
        {
            "coins_1000", "coins_25000", "coins_5000", "coins_50000", "coins_10000", "coins_100000"
        };

        private static readonly string[] BoxNodes = { "StarterBox", "BeginnerBox", "MegaBox", "GoldenBox" };

        private static readonly string[] BoxProducts = { "box_starter", "box_beginner", "box_mega", "box_golden" };

        [MenuItem("MatchPack/UI Entegrasyonunu Kur", false, 0)]
        public static void Run()
        {
            if (!EnsureSceneIsOpen()) { return; }

            UIIntegrationUtility.ResetReport();

            GameObject canvas = GameObject.Find("UI_Canvas");
            GameObject core = GameObject.Find("Core");
            GameObject gameplay = GameObject.Find("GamePlay");

            if (canvas == null || core == null || gameplay == null)
            {
                EditorUtility.DisplayDialog(
                    "UI Entegrasyonu",
                    "MainScene içinde UI_Canvas, Core veya GamePlay objesi bulunamadı. Doğru sahne açık mı?",
                    "Tamam");
                return;
            }

            Undo.SetCurrentGroupName("UI Integration");
            int undoGroup = Undo.GetCurrentGroup();

            GameConfig config = UIIntegrationAssets.LoadGameConfig();
            AudioLibrary audioLibrary = UIIntegrationAssets.EnsureAudioLibrary();
            ShopCatalog shopCatalog = UIIntegrationAssets.EnsureShopCatalog();
            Material flashMaterial = UIIntegrationAssets.EnsureFlashMaterial();

            CleanMissingScripts(canvas);
            MoveSettingsToCanvasRoot(canvas);

            MatchResolver matchResolver = gameplay.GetComponent<MatchResolver>();
            Conveyor conveyor = gameplay.GetComponent<Conveyor>();

            // Yığın oturana kadar dokunuşlar yok sayılıyor; 5 sn beklemek oyuncuya kilitlenme gibi geliyordu.
            UIIntegrationUtility.SetFloat(gameplay.GetComponent<ItemStack>(), "_settleTimeout", StackSettleTimeout);

            SetUpManagers(core, canvas, config, audioLibrary, shopCatalog, matchResolver, conveyor);
            SetUpMainMenu(canvas, config);
            SetUpSettings(canvas);
            SetUpLevelResult(canvas, config);
            SetUpShop(canvas);
            SetUpHeartPopup(canvas, config);
            SetUpScreenFlash(canvas, matchResolver, flashMaterial);
            NormalizePanelStates(canvas);

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = UIIntegrationUtility.BuildReport();
            Debug.Log(report);
            EditorUtility.DisplayDialog(
                "UI Entegrasyonu",
                UIIntegrationUtility.HasProblems
                    ? "Entegrasyon tamamlandı ama elle bakılması gereken noktalar var. Ayrıntı Console'da."
                    : "Entegrasyon tamamlandı. Ayrıntı Console'da.",
                "Tamam");
        }

        [MenuItem("MatchPack/UI Entegrasyonunu Kur", true)]
        private static bool ValidateRun()
        {
            return !Application.isPlaying;
        }

        private static bool EnsureSceneIsOpen()
        {
            if (SceneManager.GetActiveScene().path == ScenePath) { return true; }

            bool shouldOpen = EditorUtility.DisplayDialog(
                "UI Entegrasyonu",
                "Bu araç MainScene üzerinde çalışır. MainScene açılsın mı? Açık sahnedeki kaydedilmemiş değişiklikler sorulacak.",
                "Aç ve devam et",
                "Vazgeç");

            if (!shouldOpen) { return false; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return false; }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static void CleanMissingScripts(GameObject root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            int removed = 0;

            for (int i = 0; i < all.Length; i++)
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(all[i].gameObject);
            }

            if (removed > 0)
            {
                UIIntegrationUtility.Log($"UI_Canvas altında {removed} adet eksik (missing) script bileşeni temizlendi.");
            }
        }

        /// <summary>
        /// Ayarlar panelini ve butonunu MainMenu'nün altından UI_Canvas köküne taşır; böylece
        /// oyun içinde de (MainMenu kapalıyken) açılabilir, can ve gold popup'ları gibi davranır.
        /// </summary>
        private static void MoveSettingsToCanvasRoot(GameObject canvas)
        {
            GameObject mainMenu = UIIntegrationUtility.Require(canvas, "MainMenu");
            GameObject inGame = UIIntegrationUtility.Require(canvas, "InGame");
            if (mainMenu == null || inGame == null) { return; }

            GameObject button = UIIntegrationUtility.Find(canvas, "SettingBttn");
            if (button == null) { button = UIIntegrationUtility.Find(canvas, "MainMenu/SettingBttn"); }

            GameObject panel = UIIntegrationUtility.Find(canvas, "SettingPanel");
            if (panel == null) { panel = UIIntegrationUtility.Find(canvas, "MainMenu/SettingPanel"); }

            if (button == null || panel == null)
            {
                UIIntegrationUtility.Problem("SettingBttn veya SettingPanel bulunamadı; ayarlar oyun içine taşınamadı.");
                return;
            }

            UIIntegrationUtility.Reparent(button, canvas.transform, mainMenu.transform.GetSiblingIndex() + 1);
            UIIntegrationUtility.Reparent(panel, canvas.transform, inGame.transform.GetSiblingIndex() + 1);
        }

        private static void SetUpManagers(
            GameObject core,
            GameObject canvas,
            GameConfig config,
            AudioLibrary audioLibrary,
            ShopCatalog shopCatalog,
            MatchResolver matchResolver,
            Conveyor conveyor)
        {
            UIManager uiManager = UIIntegrationUtility.GetOrAdd<UIManager>(core);
            UIIntegrationUtility.SetField(uiManager, "_mainMenuRoot", UIIntegrationUtility.Require(canvas, "MainMenu"));
            UIIntegrationUtility.SetField(uiManager, "_settingsPanel", UIIntegrationUtility.Require(canvas, "SettingPanel"));
            UIIntegrationUtility.SetField(uiManager, "_marketPanel", UIIntegrationUtility.Require(canvas, "MainMenu/MarketPanel"));
            UIIntegrationUtility.SetField(uiManager, "_goldPopup", UIIntegrationUtility.Require(canvas, "GoldPopUp"));
            UIIntegrationUtility.SetField(uiManager, "_heartPopup", UIIntegrationUtility.Require(canvas, "HeartPopUp"));
            UIIntegrationUtility.SetField(uiManager, "_inGameRoot", UIIntegrationUtility.Require(canvas, "InGame"));
            UIIntegrationUtility.SetField(uiManager, "_winPanel", UIIntegrationUtility.Require(canvas, "InGame/WinPanel"));
            UIIntegrationUtility.SetField(uiManager, "_losePanel", UIIntegrationUtility.Require(canvas, "InGame/LosePanel"));
            UIIntegrationUtility.SetField(uiManager, "_boosterPanel", UIIntegrationUtility.Require(canvas, "InGame/BoosterPanel"));

            EconomyManager economy = UIIntegrationUtility.GetOrAdd<EconomyManager>(core);
            UIIntegrationUtility.SetField(economy, "_config", config);

            HapticManager haptics = UIIntegrationUtility.GetOrAdd<HapticManager>(core);
            UIIntegrationUtility.SetField(haptics, "_matchResolver", matchResolver);

            ShopManager shop = UIIntegrationUtility.GetOrAdd<ShopManager>(core);
            UIIntegrationUtility.SetField(shop, "_catalog", shopCatalog);

            AudioManager audio = UIIntegrationUtility.GetOrAdd<AudioManager>(core);
            UIIntegrationUtility.SetField(audio, "_library", audioLibrary);
            UIIntegrationUtility.SetField(audio, "_matchResolver", matchResolver);
            UIIntegrationUtility.SetField(audio, "_conveyor", conveyor);
            UIIntegrationUtility.SetField(audio, "_sfxSource", EnsureAudioSource(core, "SfxSource", false));
            UIIntegrationUtility.SetField(audio, "_musicSource", EnsureAudioSource(core, "MusicSource", true));

            LevelManager levelManager = core.GetComponent<LevelManager>();
            UIIntegrationUtility.SetField(levelManager, "_config", config);
        }

        private static AudioSource EnsureAudioSource(GameObject core, string childName, bool isLooping)
        {
            Transform existing = core.transform.Find(childName);
            GameObject target;

            if (existing != null)
            {
                target = existing.gameObject;
            }
            else
            {
                target = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(target, "UI Integration Audio Source");
                target.transform.SetParent(core.transform, false);
                UIIntegrationUtility.Log($"Core altına {childName} oluşturuldu.");
            }

            AudioSource source = UIIntegrationUtility.GetOrAdd<AudioSource>(target);
            source.playOnAwake = false;
            source.loop = isLooping;
            source.spatialBlend = 0f;
            EditorUtility.SetDirty(source);
            return source;
        }

        private static void SetUpMainMenu(GameObject canvas, GameConfig config)
        {
            GameObject mainMenu = UIIntegrationUtility.Require(canvas, "MainMenu");
            MainMenuScreen menu = UIIntegrationUtility.GetOrAdd<MainMenuScreen>(mainMenu);
            UIIntegrationUtility.SetField(menu, "_startButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "MainMenu/StartButton"));

            CurrencyView currency = UIIntegrationUtility.GetOrAdd<CurrencyView>(canvas);
            UIIntegrationUtility.SetField(currency, "_goldText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "MainMenu/Gold/Text (TMP)"));
            UIIntegrationUtility.SetField(currency, "_livesText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "MainMenu/Heart/HearthIcon/Text (TMP)"));
            UIIntegrationUtility.SetField(currency, "_lifeTimerText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "MainMenu/Heart/Timer"));
            UIIntegrationUtility.SetField(currency, "_goldAddButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "MainMenu/Gold/AddBttn"));
            UIIntegrationUtility.SetField(currency, "_heartAddButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "MainMenu/Heart/AddBttn"));

            if (config == null) { UIIntegrationUtility.Problem("GameConfig bulunamadığı için ekonomi değerleri bağlanamadı."); }
        }

        private static void SetUpSettings(GameObject canvas)
        {
            SettingsPanel settings = UIIntegrationUtility.GetOrAdd<SettingsPanel>(canvas);

            UIIntegrationUtility.SetField(settings, "_openButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "SettingBttn"));
            UIIntegrationUtility.SetField(settings, "_closeButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "SettingPanel/BG/Close_Btn"));
            UIIntegrationUtility.SetField(settings, "_continueButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "SettingPanel/BG/Continue_Btn"));
            UIIntegrationUtility.SetField(settings, "_exitButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "SettingPanel/BG/Exit_Btn"));

            UIIntegrationUtility.SetField(settings, "_soundSwitch", BuildSwitch(canvas, "SettingPanel/BG/SoundContainer/BtnHolder"));
            UIIntegrationUtility.SetField(settings, "_musicSwitch", BuildSwitch(canvas, "SettingPanel/BG/MusicContainer/BtnHolder"));
            UIIntegrationUtility.SetField(settings, "_hapticSwitch", BuildSwitch(canvas, "SettingPanel/BG/TapticContainer/BtnHolder"));
        }

        private static ToggleSwitch BuildSwitch(GameObject canvas, string holderPath)
        {
            GameObject holder = UIIntegrationUtility.Require(canvas, holderPath);
            if (holder == null) { return null; }

            // Anahtarın tamamı tek buton: tıklama BtnHolder'a düşsün diye üstteki her şeyin
            // raycast'i kapatılır. Yazılar butonları örttüğü için eskiden tıklama hiç ulaşmıyordu.
            UIIntegrationUtility.SetRaycastTarget(holder, "Opn", false);
            UIIntegrationUtility.SetRaycastTarget(holder, "Cls", false);
            UIIntegrationUtility.SetRaycastTarget(holder, "Opn_Txt", false);
            UIIntegrationUtility.SetRaycastTarget(holder, "Cls_Txt", false);
            UIIntegrationUtility.SetRaycastTarget(holder, string.Empty, true);

            Button holderButton = UIIntegrationUtility.GetOrAdd<Button>(holder);
            Undo.RecordObject(holderButton, "UI Integration Toggle Button");
            holderButton.targetGraphic = holder.GetComponent<Image>();

            // Basınca tüm rayın rengi solmasın diye geçiş kapalı; geri bildirimi görsel takas veriyor.
            holderButton.transition = Selectable.Transition.None;
            EditorUtility.SetDirty(holderButton);

            ToggleSwitch toggle = UIIntegrationUtility.GetOrAdd<ToggleSwitch>(holder);
            UIIntegrationUtility.SetField(toggle, "_toggleButton", holderButton);
            UIIntegrationUtility.SetField(toggle, "_onVisual", UIIntegrationUtility.Require(holder, "Opn"));
            UIIntegrationUtility.SetField(toggle, "_offVisual", UIIntegrationUtility.Require(holder, "Cls"));

            // Varsayılan olarak açık görünür; çalışma anında kayıttaki değerle eşitlenir.
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(holder, "Opn"), true);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(holder, "Cls"), false);
            return toggle;
        }

        private static void SetUpLevelResult(GameObject canvas, GameConfig config)
        {
            LevelResultScreen result = UIIntegrationUtility.GetOrAdd<LevelResultScreen>(canvas);
            UIIntegrationUtility.SetField(result, "_config", config);

            UIIntegrationUtility.SetField(result, "_winClaimButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/WinPanel/Bg/Gold_Btn"));
            UIIntegrationUtility.SetField(result, "_winDoubleRewardButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/WinPanel/Bg/Ads_Btn"));
            UIIntegrationUtility.SetField(result, "_winCloseButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/WinPanel/Bg/Close_Btn"));

            UIIntegrationUtility.SetField(result, "_loseContinueWithGoldButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/LosePanel/BG/Gold_Btn"));
            UIIntegrationUtility.SetField(result, "_loseContinueWithAdButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/LosePanel/BG/Ads_Btn"));
            UIIntegrationUtility.SetField(result, "_loseCloseButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/LosePanel/BG/Close_Btn"));
            UIIntegrationUtility.SetField(result, "_loseContinueCostText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "InGame/LosePanel/BG/Gold_Btn/Text (TMP)"));

            UIIntegrationUtility.Problem(
                "Kazanma panelindeki Gold_Btn yazısı sabit 'Get Gold'. Ödül miktarını göstermek istersen " +
                "LevelResultScreen._winRewardText alanına bir TMP alanı bağla.");
        }

        private static void SetUpShop(GameObject canvas)
        {
            ShopPanel shop = UIIntegrationUtility.GetOrAdd<ShopPanel>(canvas);
            UIIntegrationUtility.SetField(shop, "_goldPopupCloseButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "GoldPopUp/Close_Btn"));
            UIIntegrationUtility.SetField(shop, "_boosterPanelCloseButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "InGame/BoosterPanel/Bg/Close_Btn"));

            for (int i = 0; i < BoxNodes.Length; i++)
            {
                BindProductButton(canvas, $"{MarketContentPath}/{BoxNodes[i]}/Button", "Text (TMP)", BoxProducts[i]);
            }

            for (int i = 0; i < MarketCoinNodes.Length; i++)
            {
                BindProductButton(canvas, $"{MarketContentPath}/{MarketCoinNodes[i]}/Bottom/Image", "Text (TMP)", MarketCoinProducts[i]);
            }

            for (int i = 0; i < PopupCoinNodes.Length; i++)
            {
                BindProductButton(canvas, $"GoldPopUp/{PopupCoinNodes[i]}/Bottom/Image", "Text (TMP)", PopupCoinProducts[i]);
            }

            UIIntegrationUtility.Problem(
                "MarketPanel'i açan ve kapatan buton sahnede yok. Buton eklendiğinde UI_Canvas > ShopPanel " +
                "üzerindeki _marketOpenButton / _marketCloseButton alanlarına bağla.");
        }

        private static void BindProductButton(GameObject canvas, string buttonPath, string priceChildName, string productId)
        {
            GameObject target = UIIntegrationUtility.Require(canvas, buttonPath);
            if (target == null) { return; }

            ShopProductButton productButton = UIIntegrationUtility.GetOrAdd<ShopProductButton>(target);
            UIIntegrationUtility.SetField(productButton, "_button", target.GetComponent<Button>());
            UIIntegrationUtility.SetString(productButton, "_productId", productId);

            GameObject priceObject = UIIntegrationUtility.Find(target, priceChildName);
            UIIntegrationUtility.SetField(productButton, "_priceText", priceObject != null ? priceObject.GetComponent<TMP_Text>() : null);
        }

        private static void SetUpHeartPopup(GameObject canvas, GameConfig config)
        {
            HeartPopup popup = UIIntegrationUtility.GetOrAdd<HeartPopup>(canvas);
            UIIntegrationUtility.SetField(popup, "_config", config);
            UIIntegrationUtility.SetField(popup, "_closeButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "HeartPopUp/Bg/Close_Btn"));
            UIIntegrationUtility.SetField(popup, "_adButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "HeartPopUp/Bg/Ads_Btn"));
            UIIntegrationUtility.SetField(popup, "_goldButton", UIIntegrationUtility.RequireComponent<Button>(canvas, "HeartPopUp/Bg/Gold_Btn"));
            UIIntegrationUtility.SetField(popup, "_livesText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "HeartPopUp/Bg/Icon/Text (TMP)"));
            UIIntegrationUtility.SetField(popup, "_timerText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "HeartPopUp/Bg/TimeHolder/Text (TMP)"));
            UIIntegrationUtility.SetField(popup, "_goldCostText", UIIntegrationUtility.RequireComponent<TMP_Text>(canvas, "HeartPopUp/Bg/Gold_Btn/Text (TMP)"));
        }

        private static void SetUpScreenFlash(GameObject canvas, MatchResolver matchResolver, Material flashMaterial)
        {
            Transform existing = canvas.transform.Find("ScreenEdgeFlash");
            GameObject target;

            if (existing != null)
            {
                target = existing.gameObject;
            }
            else
            {
                target = new GameObject("ScreenEdgeFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(target, "UI Integration Screen Flash");
                target.transform.SetParent(canvas.transform, false);
                UIIntegrationUtility.Log("UI_Canvas altına ScreenEdgeFlash oluşturuldu.");
            }

            target.transform.SetAsLastSibling();

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = target.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            if (flashMaterial != null) { image.material = flashMaterial; }

            ScreenEdgeFlash flash = UIIntegrationUtility.GetOrAdd<ScreenEdgeFlash>(target);
            UIIntegrationUtility.SetField(flash, "_image", image);
            UIIntegrationUtility.SetField(flash, "_matchResolver", matchResolver);

            if (matchResolver == null)
            {
                UIIntegrationUtility.Problem("GamePlay objesinde MatchResolver bulunamadı; ekran efekti ve titreşim bağlanamadı.");
            }

            EditorUtility.SetDirty(target);
        }

        private static void NormalizePanelStates(GameObject canvas)
        {
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "MainMenu"), true);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "InGame"), true);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "MainMenu/MarketPanel"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "SettingPanel"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "InGame/WinPanel"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "InGame/LosePanel"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "InGame/BoosterPanel"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "HeartPopUp"), false);
            UIIntegrationUtility.SetActive(UIIntegrationUtility.Find(canvas, "GoldPopUp"), false);
        }
    }
}
