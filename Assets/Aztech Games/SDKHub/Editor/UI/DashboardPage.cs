using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    static class DashboardPage
    {
        sealed class Group
        {
            public string Name;
            public readonly List<SdkModule> Modules = new List<SdkModule>();
            public int Ready;
        }

        public static VisualElement Build(Action<string> navigate)
        {
            var page = new VisualElement();
            page.AddToClassList("page");

            var groups = new List<Group>();
            var tools = new List<SdkModule>();
            var sdkCount = 0;
            var ready = 0;
            var missing = new List<SdkModule>();

            foreach (var module in SdkRegistry.Modules)
            {
                if (module.IsUtility)
                {
                    tools.Add(module);
                    continue;
                }

                sdkCount++;
                var isReady = module.GetStatus() == SdkStatus.Ready;
                if (isReady) ready++;
                if (!module.IsInstalled && module.InstallType != InstallType.External && module.InstallType != InstallType.BuiltIn) missing.Add(module);

                Group group = null;
                foreach (var candidate in groups)
                {
                    if (candidate.Name == module.Category) group = candidate;
                }
                if (group == null)
                {
                    group = new Group { Name = module.Category };
                    groups.Add(group);
                }
                group.Modules.Add(module);
                if (isReady) group.Ready++;
            }

            var title = new Label("Dashboard");
            title.AddToClassList("page-title");
            page.Add(title);

            var subtitle = new Label($"{ready} of {sdkCount} SDKs ready");
            subtitle.AddToClassList("page-subtitle");
            page.Add(subtitle);

            var bar = new VisualElement();
            bar.AddToClassList("progress");
            var fill = new VisualElement();
            fill.AddToClassList("progress-fill");
            fill.style.width = Length.Percent(sdkCount == 0 ? 0f : 100f * ready / sdkCount);
            bar.Add(fill);
            page.Add(bar);

            var actions = new VisualElement();
            actions.AddToClassList("actions");
            if (missing.Count > 0)
            {
                var installAll = SdkHubUI.MakeButton($"Install all missing ({missing.Count})",
                    () => SdkInstaller.InstallMany(missing), "btn-primary");
                installAll.SetEnabled(!PackageInstaller.IsWorking);
                actions.Add(installAll);
            }
            actions.Add(SdkHubUI.MakeButton("Add privacy panel to scene",
                () => UnityEditor.EditorApplication.ExecuteMenuItem("GameObject/SDK Hub/Privacy Panel")));
            actions.Add(SdkHubUI.MakeButton("Run release check", () => navigate("release")));
            actions.Add(SdkHubUI.MakeButton("Create demo scene",
                () => UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Aztech Games/Create Demo Scene")));
            page.Add(actions);

            foreach (var group in groups)
            {
                page.Add(Heading(group.Name.ToUpperInvariant(), $"{group.Ready}/{group.Modules.Count} ready"));
                page.Add(BuildGrid(group.Modules, navigate));
            }

            if (tools.Count > 0)
            {
                page.Add(Heading("TOOLS", null));
                page.Add(BuildGrid(tools, navigate));
            }
            return page;
        }

        static VisualElement Heading(string text, string detail)
        {
            var row = new VisualElement();
            row.AddToClassList("grid-heading-row");
            var heading = new Label(text);
            heading.AddToClassList("grid-heading");
            row.Add(heading);
            if (detail != null)
            {
                var count = new Label(detail);
                count.AddToClassList("grid-count");
                row.Add(count);
            }
            return row;
        }

        static VisualElement BuildGrid(List<SdkModule> modules, Action<string> navigate)
        {
            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            foreach (var module in modules) grid.Add(BuildCard(module, navigate));
            return grid;
        }

        static VisualElement BuildCard(SdkModule module, Action<string> navigate)
        {
            var status = module.GetStatus();
            var card = new VisualElement();
            card.AddToClassList("card");
            if (status == SdkStatus.Ready) card.AddToClassList("card--ready");
            else if (status == SdkStatus.NeedsSetup || status == SdkStatus.Installing) card.AddToClassList("card--setup");
            card.RegisterCallback<ClickEvent>(_ => navigate(module.Id));

            var header = new VisualElement();
            header.AddToClassList("card-header");
            header.Add(SdkHubUI.Tile(module));
            var title = new Label(module.DisplayName);
            title.AddToClassList("card-title");
            header.Add(title);
            card.Add(header);

            var desc = new Label(module.Description);
            desc.AddToClassList("card-desc");
            card.Add(desc);

            var footer = new VisualElement();
            footer.AddToClassList("card-footer");
            footer.Add(SdkHubUI.Chip(status));

            var canQuickInstall = status == SdkStatus.NotInstalled && module.InstallType != InstallType.External && module.InstallType != InstallType.BuiltIn;
            var button = SdkHubUI.MakeButton(canQuickInstall ? "Install" : "Open", () => { }, canQuickInstall ? "btn-primary" : null);
            button.SetEnabled(!canQuickInstall || !PackageInstaller.IsWorking);
            button.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                if (canQuickInstall) SdkInstaller.Install(module);
                else navigate(module.Id);
            });
            footer.Add(button);
            card.Add(footer);
            return card;
        }
    }
}
