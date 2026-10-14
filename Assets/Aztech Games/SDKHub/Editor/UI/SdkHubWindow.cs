using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public class SdkHubWindow : EditorWindow
    {
        const string DashboardId = "dashboard";
        const string SettingsId = "settings";
        const string ReleaseId = "release";

        string currentPage = DashboardId;
        VisualElement navList;
        ScrollView content;
        Label statusBar;
        VisualElement settingsButton;

        [MenuItem("Tools/Aztech Games/SDK Hub")]
        public static void Open()
        {
            var window = GetWindow<SdkHubWindow>();
            window.titleContent = new GUIContent("Aztech Games SDK Hub");
            window.minSize = new Vector2(860, 540);
            window.Show();
        }

        void CreateGUI()
        {
            var tree = SdkHubAssets.Load<VisualTreeAsset>("SdkHubWindow");
            if (tree == null)
            {
                rootVisualElement.Add(new Label("SdkHubWindow.uxml was not found."));
                return;
            }
            tree.CloneTree(rootVisualElement);

            navList = rootVisualElement.Q("navList");
            content = rootVisualElement.Q<ScrollView>("content");
            statusBar = rootVisualElement.Q<Label>("statusBar");
            settingsButton = rootVisualElement.Q("settingsButton");
            settingsButton.tooltip = "Settings";
            var gear = new GearIcon();
            gear.AddToClassList("gear-icon");
            settingsButton.Add(gear);
            settingsButton.RegisterCallback<ClickEvent>(_ => ShowPage(SettingsId));
            rootVisualElement.Q<Button>("refreshButton").clicked += PackageInstaller.Refresh;

            statusBar.RegisterCallback<ClickEvent>(_ =>
            {
                PackageInstaller.ClearError();
                UpdateStatusBar();
            });

            PackageInstaller.Changed -= OnPackagesChanged;
            PackageInstaller.Changed += OnPackagesChanged;
            SdkHubState.Invalidated -= OnInvalidated;
            SdkHubState.Invalidated += OnInvalidated;

            UpdateStatusBar();
            ShowPage(currentPage);
        }

        void OnDisable()
        {
            PackageInstaller.Changed -= OnPackagesChanged;
            SdkHubState.Invalidated -= OnInvalidated;
        }

        void OnInvalidated()
        {
            if (content != null) ShowPage(currentPage);
        }

        void OnPackagesChanged()
        {
            if (content == null) return;
            UpdateStatusBar();
            ShowPage(currentPage);
        }

        void UpdateStatusBar()
        {
            var working = PackageInstaller.IsWorking;
            var error = PackageInstaller.LastError;
            var showError = !working && !string.IsNullOrEmpty(error);

            statusBar.EnableInClassList("status-bar--hidden", !working && !showError);
            statusBar.EnableInClassList("status-bar--error", showError);
            if (working)
            {
                var queued = PackageInstaller.QueueCount;
                statusBar.text = PackageInstaller.BusyLabel + (queued > 0 ? $"  ({queued} more queued)" : "");
            }
            else if (showError)
            {
                statusBar.text = error + "   (click to dismiss)";
            }
        }

        void ShowPage(string id)
        {
            currentPage = id;
            content.contentContainer.Clear();

            VisualElement page;
            var module = SdkRegistry.Find(id);
            if (id == SettingsId) page = SettingsPage.Build();
            else if (id == ReleaseId) page = ReleaseCheckPage.Build(ShowPage, () => ShowPage(ReleaseId));
            else if (module != null) page = ModulePage.Build(module, BuildNav);
            else
            {
                currentPage = DashboardId;
                page = DashboardPage.Build(ShowPage);
            }

            content.Add(page);

            // Module pages rebuild the sidebar themselves when their status is calculated.
            if (module == null || id == SettingsId) BuildNav();
        }

        void BuildNav()
        {
            navList.contentContainer.Clear();
            AddNavItem(DashboardId, "Dashboard", null, true);
            AddNavItem(ReleaseId, "Release check", null, true);

            var categories = new List<string>();
            foreach (var module in SdkRegistry.Modules)
            {
                if (!module.IsUtility && !categories.Contains(module.Category)) categories.Add(module.Category);
            }

            foreach (var category in categories)
            {
                AddSection(category.ToUpperInvariant());
                foreach (var module in SdkRegistry.Modules)
                {
                    if (module.IsUtility || module.Category != category) continue;
                    AddNavItem(module.Id, module.NavName, SdkHubUI.DotClass(module.GetStatus()), false);
                }
            }

            var hasTools = false;
            foreach (var module in SdkRegistry.Modules)
            {
                if (!module.IsUtility) continue;
                if (!hasTools)
                {
                    AddSection("TOOLS");
                    hasTools = true;
                }
                AddNavItem(module.Id, module.NavName, SdkHubUI.DotClass(module.GetStatus()), false);
            }

            settingsButton?.EnableInClassList("icon-button--selected", currentPage == SettingsId);
        }

        void AddSection(string title)
        {
            var heading = new Label(title);
            heading.AddToClassList("nav-section");
            navList.Add(heading);
        }

        void AddNavItem(string id, string label, string dotClass, bool primary)
        {
            var item = new VisualElement();
            item.AddToClassList("nav-item");
            if (primary) item.AddToClassList("nav-item--primary");
            if (id == currentPage) item.AddToClassList("nav-item--selected");

            if (!primary)
            {
                var dot = new VisualElement();
                dot.AddToClassList("nav-dot");
                if (dotClass != null) dot.AddToClassList(dotClass);
                item.Add(dot);
            }

            var text = new Label(label);
            text.AddToClassList("nav-label");
            item.tooltip = label;
            item.Add(text);
            item.RegisterCallback<ClickEvent>(_ => ShowPage(id));
            navList.Add(item);
        }
    }
}
