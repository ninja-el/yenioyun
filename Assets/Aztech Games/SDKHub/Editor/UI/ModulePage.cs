using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    static class ModulePage
    {
        public static VisualElement Build(SdkModule module, Action onStatusChanged)
        {
            var page = new VisualElement();
            page.AddToClassList("page");

            var eyebrow = new Label((module.IsUtility ? "TOOLS" : module.Category).ToUpperInvariant());
            eyebrow.AddToClassList("page-eyebrow");
            page.Add(eyebrow);

            var header = new VisualElement();
            header.AddToClassList("page-header");
            header.Add(SdkHubUI.Tile(module));
            var title = new Label(module.DisplayName);
            title.AddToClassList("page-title");
            header.Add(title);
            var chipHolder = new VisualElement();
            header.Add(chipHolder);
            page.Add(header);

            var subtitle = new Label(module.Description);
            subtitle.AddToClassList("page-subtitle");
            page.Add(subtitle);

            page.Add(BuildActions(module));
            if (module.InstallType == InstallType.External) page.Add(BuildExternalSection(module));

            // Only shown when something needs attention.
            var attention = new VisualElement();
            attention.AddToClassList("attention");
            page.Add(attention);

            // Typing in a field refreshes the status after a short pause instead of on every key.
            var pending = page.schedule.Execute(Refresh);
            pending.Pause();

            var setup = SdkHubUI.Section("Setup");
            module.BuildSettings(setup, () => pending.ExecuteLater(150));
            if (setup.childCount > 1) page.Add(setup);

            if (module.SceneActions.Count > 0) page.Add(BuildSceneSection(module));

            var usage = BuildCodeUsage(module);
            if (usage != null) page.Add(usage);

            Refresh();
            return page;

            void Refresh()
            {
                chipHolder.Clear();
                chipHolder.Add(SdkHubUI.Chip(module.GetStatus()));
                FillAttention(attention, module);
                onStatusChanged?.Invoke();
            }
        }

        static VisualElement BuildCodeUsage(SdkModule module)
        {
            if (string.IsNullOrEmpty(module.CodeUsage)) return null;

            var section = SdkHubUI.Section("Code usage");
            var hint = new Label("Call these from any script. Nothing needs to be started or set up first.");
            hint.AddToClassList("section-hint");
            section.Add(hint);

            var code = new Label(module.CodeUsage);
            code.AddToClassList("usage-code");
            code.selection.isSelectable = true;
            section.Add(code);
            return section;
        }

        static VisualElement BuildActions(SdkModule module)
        {
            var actions = new VisualElement();
            actions.AddToClassList("actions");
            var busy = PackageInstaller.IsWorking;

            if (module.InstallType != InstallType.External && module.InstallType != InstallType.BuiltIn)
            {
                if (module.IsInstalled)
                {
                    var remove = SdkHubUI.MakeButton("Remove", () =>
                    {
                        if (EditorUtility.DisplayDialog("Remove " + module.DisplayName,
                                "Remove package " + module.PackageName + " from this project?", "Remove", "Cancel"))
                        {
                            PackageInstaller.Remove(module.PackageName);
                        }
                    }, "btn-danger");
                    remove.SetEnabled(!busy);
                    actions.Add(remove);
                }
                else
                {
                    var install = SdkHubUI.MakeButton(busy ? "Working..." : "Install",
                        () => SdkInstaller.Install(module), "btn-primary");
                    install.SetEnabled(!busy);
                    actions.Add(install);
                }
            }

            if (!string.IsNullOrEmpty(module.DocsUrl))
            {
                actions.Add(SdkHubUI.MakeButton("Docs", () => Application.OpenURL(module.DocsUrl)));
            }
            foreach (var link in module.Links)
            {
                var url = link.Url;
                actions.Add(SdkHubUI.MakeButton(link.Label, () => Application.OpenURL(url)));
            }
            return actions;
        }

        static VisualElement BuildSceneSection(SdkModule module)
        {
            var section = SdkHubUI.Section("Add to scene");
            var hint = new Label("Adds a ready-made object to the open scene. Everything it does is set in this hub.");
            hint.AddToClassList("section-hint");
            section.Add(hint);

            var row = new VisualElement();
            row.AddToClassList("actions");
            foreach (var action in module.SceneActions)
            {
                var path = action.MenuPath;
                var button = SdkHubUI.MakeButton(action.Label, () => EditorApplication.ExecuteMenuItem(path));
                if (!string.IsNullOrEmpty(action.Tooltip)) button.tooltip = action.Tooltip;
                row.Add(button);
            }
            section.Add(row);
            return section;
        }

        static VisualElement BuildExternalSection(SdkModule module)
        {
            var section = SdkHubUI.Section("Install");
            var hint = new Label(module.InstallHint ?? "This SDK is not on the Unity registry. Download the package, then drop the .unitypackage file below or browse for it.");
            hint.AddToClassList("section-hint");
            section.Add(hint);

            var row = new VisualElement();
            row.AddToClassList("actions");
            if (!string.IsNullOrEmpty(module.DownloadUrl))
            {
                row.Add(SdkHubUI.MakeButton("Open download page", () => Application.OpenURL(module.DownloadUrl), "btn-primary"));
            }
            row.Add(SdkHubUI.MakeButton("Browse...", () =>
            {
                var path = EditorUtility.OpenFilePanel("Select .unitypackage", "", "unitypackage");
                PackageInstaller.ImportPackageFile(path);
            }));
            section.Add(row);
            section.Add(BuildDropArea());
            return section;
        }

        static VisualElement BuildDropArea()
        {
            var area = new VisualElement();
            area.AddToClassList("drop-area");
            area.Add(new Label("Drop .unitypackage here"));

            area.RegisterCallback<DragEnterEvent>(_ => area.AddToClassList("drop-area--active"));
            area.RegisterCallback<DragLeaveEvent>(_ => area.RemoveFromClassList("drop-area--active"));
            area.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = FindPackage() != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            });
            area.RegisterCallback<DragPerformEvent>(evt =>
            {
                area.RemoveFromClassList("drop-area--active");
                var path = FindPackage();
                if (path == null) return;
                DragAndDrop.AcceptDrag();
                PackageInstaller.ImportPackageFile(path);
                evt.StopPropagation();
            });
            return area;
        }

        static string FindPackage()
        {
            foreach (var path in DragAndDrop.paths)
            {
                if (path.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)) return path;
            }
            return null;
        }

        static void FillAttention(VisualElement box, SdkModule module)
        {
            box.Clear();

            var rows = new List<CheckItem>();
            foreach (var id in module.Prerequisites)
            {
                var prerequisite = SdkRegistry.Find(id);
                if (prerequisite == null || prerequisite.IsInstalled) continue;
                if (prerequisite.InstallType == InstallType.External) rows.Add(new CheckItem(CheckLevel.Error, "Requires " + prerequisite.DisplayName + " (install it manually first)"));
                else rows.Add(new CheckItem(CheckLevel.Warning, prerequisite.DisplayName + " will be installed together with this SDK"));
            }
            foreach (var item in module.Checks)
            {
                if (item.Level != CheckLevel.Ok) rows.Add(item);
            }

            box.EnableInClassList("hidden", rows.Count == 0);
            if (rows.Count == 0) return;

            var title = new Label("Needs attention");
            title.AddToClassList("attention-title");
            box.Add(title);
            foreach (var row in rows) AddRow(box, row.Level, row.Message);
        }

        static void AddRow(VisualElement section, CheckLevel level, string message)
        {
            var row = new VisualElement();
            row.AddToClassList("check-row");
            var dot = new VisualElement();
            dot.AddToClassList("check-dot");
            dot.AddToClassList(SdkHubUI.DotClass(level));
            row.Add(dot);
            var text = new Label(message);
            text.AddToClassList("check-text");
            row.Add(text);
            section.Add(row);
        }
    }
}
