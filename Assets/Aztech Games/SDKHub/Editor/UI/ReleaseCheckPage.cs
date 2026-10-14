using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    static class ReleaseCheckPage
    {
        public static VisualElement Build(Action<string> navigate, Action rerun)
        {
            var page = new VisualElement();
            page.AddToClassList("page");

            var checks = ReleaseChecker.Run(navigate);
            var levels = new List<CheckLevel>();
            foreach (var check in checks) levels.Add(check.Level);

            int errors, warnings, passed;
            ReleaseRules.Count(levels, out errors, out warnings, out passed);

            var eyebrow = new Label("TOOLS");
            eyebrow.AddToClassList("page-eyebrow");
            page.Add(eyebrow);

            var header = new VisualElement();
            header.AddToClassList("page-header");
            var title = new Label("Release check");
            title.AddToClassList("page-title");
            header.Add(title);

            var verdict = new Label(ReleaseRules.Verdict(errors, warnings));
            verdict.AddToClassList("chip");
            verdict.AddToClassList(errors > 0 ? "chip-err" : warnings > 0 ? "chip-warn" : "chip-ok");
            header.Add(verdict);
            page.Add(header);

            var subtitle = new Label("Looks through your project for things that commonly block a store release. Fix buttons change project settings, so read what each one says first.");
            subtitle.AddToClassList("page-subtitle");
            page.Add(subtitle);

            var summary = new Label($"{errors} error(s), {warnings} warning(s), {passed} passed");
            summary.AddToClassList("check-summary");
            page.Add(summary);

            var actions = new VisualElement();
            actions.AddToClassList("actions");
            actions.Add(SdkHubUI.MakeButton("Run again", rerun, "btn-primary"));
            page.Add(actions);

            var areas = new List<string>();
            foreach (var check in checks)
            {
                if (!areas.Contains(check.Area)) areas.Add(check.Area);
            }

            foreach (var area in areas)
            {
                var section = SdkHubUI.Section(area);
                var okRows = new List<ReleaseCheck>();

                foreach (var check in checks)
                {
                    if (check.Area != area) continue;
                    if (check.Level == CheckLevel.Ok) okRows.Add(check);
                    else section.Add(BuildRow(check, rerun));
                }

                if (okRows.Count > 0)
                {
                    var fold = SdkHubUI.Collapsible(okRows.Count + " passed", false);
                    foreach (var check in okRows) fold.Add(BuildRow(check, rerun));
                    section.Add(fold);
                }
                page.Add(section);
            }
            return page;
        }

        static VisualElement BuildRow(ReleaseCheck check, Action rerun)
        {
            var row = new VisualElement();
            row.AddToClassList("release-row");

            var dot = new VisualElement();
            dot.AddToClassList("check-dot");
            dot.AddToClassList(SdkHubUI.DotClass(check.Level));
            row.Add(dot);

            var text = new VisualElement();
            text.AddToClassList("release-text");
            var title = new Label(check.Title);
            title.AddToClassList("release-title");
            text.Add(title);
            if (!string.IsNullOrEmpty(check.Detail))
            {
                var detail = new Label(check.Detail);
                detail.AddToClassList("release-detail");
                text.Add(detail);
            }
            row.Add(text);

            if (check.Fix != null)
            {
                var fix = check.Fix;
                var rerunAfter = check.RerunAfterFix;
                var button = SdkHubUI.MakeButton(check.FixLabel ?? "Fix", () =>
                {
                    fix();
                    if (rerunAfter) rerun?.Invoke();
                });
                row.Add(button);
            }
            return row;
        }
    }
}
