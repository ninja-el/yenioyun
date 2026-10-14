using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public static class SdkHubUI
    {
        public static Label Chip(SdkStatus status)
        {
            string text;
            string style;
            switch (status)
            {
                case SdkStatus.Ready:
                    text = "Ready";
                    style = "chip-ok";
                    break;
                case SdkStatus.NeedsSetup:
                    text = "Needs setup";
                    style = "chip-warn";
                    break;
                case SdkStatus.Installing:
                    text = "Working...";
                    style = "chip-info";
                    break;
                default:
                    text = "Not installed";
                    style = "chip-off";
                    break;
            }
            var chip = new Label(text);
            chip.AddToClassList("chip");
            chip.AddToClassList(style);
            return chip;
        }

        public static string DotClass(SdkStatus status)
        {
            switch (status)
            {
                case SdkStatus.Ready: return "dot-ok";
                case SdkStatus.NeedsSetup: return "dot-warn";
                case SdkStatus.Installing: return "dot-warn";
                default: return null;
            }
        }

        public static string DotClass(CheckLevel level)
        {
            switch (level)
            {
                case CheckLevel.Ok: return "dot-ok";
                case CheckLevel.Warning: return "dot-warn";
                default: return "dot-err";
            }
        }

        public static VisualElement Section(string title)
        {
            var section = new VisualElement();
            section.AddToClassList("section");
            var label = new Label(title);
            label.AddToClassList("section-title");
            section.Add(label);
            return section;
        }

        public static Button MakeButton(string text, Action onClick, string extraClass = null)
        {
            var button = new Button { text = text };
            button.AddToClassList("btn");
            if (extraClass != null) button.AddToClassList(extraClass);
            button.clicked += () => onClick();
            return button;
        }

        public static TextField ConfigField(SdkModule module, string key, string label, Action onChanged)
        {
            var field = new TextField(label) { value = SdkHubConfigProvider.Get(module.Id, key) };
            field.AddToClassList("field");
            field.RegisterValueChangedCallback(evt =>
            {
                SdkHubConfigProvider.Set(module.Id, key, evt.newValue.Trim());
                onChanged?.Invoke();
            });
            return field;
        }

        public static VisualElement Tile(SdkModule module)
        {
            var tile = new VisualElement();
            tile.AddToClassList("tile");
            tile.AddToClassList(TileClass(module));
            var glyph = new Label(module.Glyph);
            glyph.AddToClassList("tile-glyph");
            tile.Add(glyph);
            return tile;
        }

        static string TileClass(SdkModule module)
        {
            if (module.IsUtility) return "tile-tools";
            switch (module.Category)
            {
                case "Monetization": return "tile-monetization";
                case "Privacy & Analytics": return "tile-privacy";
                case "Players": return "tile-players";
                case "Engagement": return "tile-engagement";
                default: return "tile-default";
            }
        }

        public static FloatField ConfigFloat(SdkModule module, string key, string label, float defaultValue, string tooltip, Action onChanged)
        {
            float parsed;
            var stored = SdkHubConfigProvider.Get(module.Id, key);
            var value = float.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : defaultValue;

            var field = new FloatField(label) { value = value, tooltip = tooltip };
            field.AddToClassList("field");
            field.RegisterValueChangedCallback(evt =>
            {
                var clean = Mathf.Max(0f, evt.newValue);
                SdkHubConfigProvider.Set(module.Id, key, clean.ToString(CultureInfo.InvariantCulture));
                onChanged?.Invoke();
            });
            return field;
        }

        public static DropdownField ConfigChoice(SdkModule module, string key, string label, string[] values, string[] labels, int defaultIndex, string tooltip, Action onChanged)
        {
            return ConfigChoice(module.Id, key, label, values, labels, defaultIndex, tooltip, onChanged);
        }

        public static DropdownField ConfigChoice(string moduleId, string key, string label, string[] values, string[] labels, int defaultIndex, string tooltip, Action onChanged)
        {
            var index = Array.IndexOf(values, SdkHubConfigProvider.Get(moduleId, key));
            if (index < 0) index = defaultIndex;

            var field = new DropdownField(label, new List<string>(labels), index) { tooltip = tooltip };
            field.AddToClassList("field");
            field.RegisterValueChangedCallback(_ =>
            {
                SdkHubConfigProvider.Set(moduleId, key, values[field.index]);
                onChanged?.Invoke();
            });
            return field;
        }

        public static Foldout Collapsible(string title, bool expanded)
        {
            var foldout = new Foldout { text = title, value = expanded };
            foldout.AddToClassList("fold");
            return foldout;
        }

        public static Toggle ConfigToggle(SdkModule module, string key, string label, Action onChanged)
        {
            var toggle = new Toggle { text = label, value = SdkHubConfigProvider.Get(module.Id, key) == "1" };
            toggle.AddToClassList("check-field");
            toggle.RegisterValueChangedCallback(evt =>
            {
                SdkHubConfigProvider.Set(module.Id, key, evt.newValue ? "1" : "0");
                onChanged?.Invoke();
            });
            return toggle;
        }

        public static VisualElement DropArea(string label, Func<string, bool> accepts, Action<string> onDrop)
        {
            var area = new VisualElement();
            area.AddToClassList("drop-area");
            area.Add(new Label(label));

            string Find()
            {
                foreach (var path in DragAndDrop.paths)
                {
                    if (accepts(path)) return path;
                }
                return null;
            }

            area.RegisterCallback<DragEnterEvent>(_ => area.AddToClassList("drop-area--active"));
            area.RegisterCallback<DragLeaveEvent>(_ => area.RemoveFromClassList("drop-area--active"));
            area.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = Find() != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            });
            area.RegisterCallback<DragPerformEvent>(evt =>
            {
                area.RemoveFromClassList("drop-area--active");
                var path = Find();
                if (path == null) return;
                DragAndDrop.AcceptDrag();
                onDrop(path);
                evt.StopPropagation();
            });
            return area;
        }
    }
}
