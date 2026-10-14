using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    // Setup steps that happen outside Unity (dashboards, stores). The player ticks them in the hub.
    public static class SdkSteps
    {
        public static IEnumerable<CheckItem> Check(SdkModule module, string[][] steps)
        {
            var pending = 0;
            foreach (var step in steps)
            {
                if (SdkHubConfigProvider.Get(module.Id, step[0]) == "1") continue;
                pending++;
                yield return new CheckItem(CheckLevel.Warning, "Not confirmed yet: " + step[1]);
            }
            if (pending == 0 && steps.Length > 0) yield return new CheckItem(CheckLevel.Ok, "All setup steps confirmed");
        }

        public static void Build(VisualElement container, SdkModule module, string heading, string hint, string[][] steps, Action onChanged)
        {
            var title = new Label(heading);
            title.AddToClassList("section-title");
            title.AddToClassList("group-title");
            container.Add(title);

            var text = new Label(hint);
            text.AddToClassList("section-hint");
            container.Add(text);

            foreach (var step in steps) container.Add(SdkHubUI.ConfigToggle(module, step[0], step[1], onChanged));
        }

        public static CheckItem CloudProject()
        {
            return string.IsNullOrEmpty(Application.cloudProjectId)
                ? new CheckItem(CheckLevel.Warning, "Project is not linked to a Unity Cloud project (Project Settings > Services)")
                : new CheckItem(CheckLevel.Ok, "Linked to a Unity Cloud project");
        }
    }
}
