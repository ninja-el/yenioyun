using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public sealed class LeaderboardsModule : SdkModule
    {
        public override string Id => "leaderboards";
        public override string DisplayName => "Leaderboards";
        public override string Description => "Global rankings with Unity Leaderboards. Submit scores and show the top players.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.services.leaderboards";
        public override string DefineSymbol => "SDKHUB_LEADERBOARDS";
        public override string DocsUrl => "https://docs.unity.com/ugs/en-us/manual/leaderboards/manual/unity-sdk";
        public override string Category => "Players";
        public override int Order => 35;

        public override IReadOnlyList<string> Prerequisites => new[] { "accounts" };

        public override string CodeUsage =>
            "using SDKHub;\n\n" +
            "SdkManager.Leaderboards.Submit(\"my_leaderboard\", score);\n" +
            "SdkManager.Leaderboards.GetTop(\"my_leaderboard\", 10, (success, rows) =>\n" +
            "{\n" +
            "    foreach (var row in rows) Debug.Log(row.Rank + \" \" + row.PlayerName + \" \" + row.Score);\n" +
            "});";

        public override IReadOnlyList<SdkLink> Links => new[]
        {
            new SdkLink("Unity Dashboard", "https://cloud.unity.com")
        };

        public override IEnumerable<CheckItem> Validate()
        {
            var cloud = SdkSteps.CloudProject();
            if (cloud.Level != CheckLevel.Ok) yield return cloud;

            var ids = LoadIds();
            if (ids.Count == 0)
            {
                yield return new CheckItem(CheckLevel.Error, "Add at least one leaderboard ID");
                yield break;
            }

            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) yield return new CheckItem(CheckLevel.Error, "A leaderboard ID is empty");
                else if (!seen.Add(id)) yield return new CheckItem(CheckLevel.Error, "Two leaderboards use the ID " + id);
            }
        }

        public override void BuildSettings(VisualElement container, Action onChanged)
        {
            var hint = new Label("Use the same IDs you create in the Unity Dashboard.");
            hint.AddToClassList("section-hint");
            container.Add(hint);

            var list = new VisualElement();
            container.Add(list);
            container.Add(SdkHubUI.MakeButton("Add leaderboard", () =>
            {
                var ids = LoadIds();
                ids.Add("");
                SaveIds(ids);
                Rebuild();
                onChanged?.Invoke();
            }));
            Rebuild();

            void Rebuild()
            {
                list.Clear();
                var ids = LoadIds();
                for (var i = 0; i < ids.Count; i++) list.Add(BuildRow(i, ids[i], Rebuild, onChanged));
            }
        }

        VisualElement BuildRow(int index, string id, Action rebuild, Action onChanged)
        {
            var row = new VisualElement();
            row.AddToClassList("product-row");

            var field = new TextField { value = id };
            field.AddToClassList("field");
            field.AddToClassList("product-id");
            field.textEdition.placeholder = "Leaderboard ID";
            field.RegisterValueChangedCallback(evt =>
            {
                var ids = LoadIds();
                if (index >= ids.Count) return;
                ids[index] = evt.newValue.Trim();
                SaveIds(ids);
                onChanged?.Invoke();
            });
            row.Add(field);

            var view = SdkHubUI.MakeButton("Add view to scene", () =>
            {
                var ids = LoadIds();
                if (index < ids.Count && !string.IsNullOrEmpty(ids[index])) SdkHubSceneMenu.AddLeaderboardView(ids[index]);
            });
            view.tooltip = "Adds a text that shows the top 10 players of this leaderboard. No setup needed.";
            row.Add(view);

            row.Add(SdkHubUI.MakeButton("Remove", () =>
            {
                var ids = LoadIds();
                if (index < ids.Count) ids.RemoveAt(index);
                SaveIds(ids);
                rebuild();
                onChanged?.Invoke();
            }, "btn-danger"));
            return row;
        }

        List<string> LoadIds()
        {
            return LeaderboardCatalog.Parse(Get("ids")).ids;
        }

        void SaveIds(List<string> ids)
        {
            SdkHubConfigProvider.Set(Id, "ids", new LeaderboardCatalog { ids = ids }.ToJson());
        }
    }
}
