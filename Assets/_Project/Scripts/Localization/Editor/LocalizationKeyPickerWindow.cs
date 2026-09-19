using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// Key secmek icin acilan arama penceresi. Key adina ya da ceviri metnine gore
    /// filtreler; satira tiklaninca secilen key geri cagriya verilir.
    ///
    /// LocalizedText inspector'i kullaniyor, ama tabloyla calisan baska bir editor
    /// aracina da ayni sekilde baglanabilir.
    /// </summary>
    public class LocalizationKeyPickerWindow : EditorWindow
    {
        private const float WindowWidth = 380f;
        private const float WindowHeight = 340f;
        private const float RowHeight = 36f;

        private readonly List<LocalizationEntry> _results = new List<LocalizationEntry>();

        private LocalizationTableSo _table;
        private Action<string> _onSelected;
        private string _currentKey;
        private string _search = string.Empty;
        private LocalizationSearchMode _searchMode = LocalizationSearchMode.Both;
        private Vector2 _scroll;
        private SearchField _searchField;
        private GUIStyle _rowStyle;
        private bool _focusPending = true;

        /// <summary>
        /// Pencereyi verilen butonun altinda acar.
        /// </summary>
        /// <param name="activatorRect">Butonun GUI koordinatlarindaki alani.</param>
        /// <param name="onSelected">Secilen key ile cagrilir; "Temizle" bos string verir.</param>
        public static void Open(Rect activatorRect, LocalizationTableSo table, string currentKey,
            Action<string> onSelected)
        {
            LocalizationKeyPickerWindow window = CreateInstance<LocalizationKeyPickerWindow>();

            window._table = table;
            window._currentKey = currentKey;
            window._onSelected = onSelected;

            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(activatorRect),
                new Vector2(Mathf.Max(activatorRect.width, WindowWidth), WindowHeight));
        }

        private void OnEnable()
        {
            // Imlec bir satirin uzerine geldiginde vurgunun hemen gorunmesi icin.
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove) Repaint();

            if (_table == null)
            {
                EditorGUILayout.HelpBox("LocalizationTable asset'i bulunamadi.", MessageType.Error);
                return;
            }

            HandleKeyboard();
            DrawSearchBar();
            CollectResults();
            DrawResults();
            DrawFooter();
        }

        private void DrawSearchBar()
        {
            _searchField ??= new SearchField();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _search = _searchField.OnGUI(_search);
                _searchMode = (LocalizationSearchMode)GUILayout.Toolbar(
                    (int)_searchMode, LocalizationEditorUtility.SearchModeLabels);
            }

            // Pencere acilir acilmaz yazmaya baslanabilsin.
            if (!_focusPending) return;

            _focusPending = false;
            _searchField.SetFocus();
        }

        private void CollectResults()
        {
            _results.Clear();

            foreach (LocalizationEntry entry in _table.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                if (LocalizationEditorUtility.Matches(entry, _search, _searchMode)) _results.Add(entry);
            }

            _results.Sort((left, right) => string.Compare(left.key, right.key, StringComparison.OrdinalIgnoreCase));
        }

        private void DrawResults()
        {
            _rowStyle ??= new GUIStyle(EditorStyles.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 2, 2),
                fixedHeight = RowHeight
            };

            using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                if (_results.Count == 0)
                {
                    EditorGUILayout.LabelField("Eslesen key yok.", EditorStyles.centeredGreyMiniLabel);
                }

                foreach (LocalizationEntry entry in _results)
                {
                    DrawRow(entry);
                }
            }
        }

        /// <summary>
        /// Satiri elle ciziyoruz: buton stili iki satirlik icerikte imlecin uzerinde
        /// durdugu satiri belli etmiyordu.
        /// </summary>
        private void DrawRow(LocalizationEntry entry)
        {
            GUIContent content = RowContent(entry);
            Rect rect = GUILayoutUtility.GetRect(content, _rowStyle, GUILayout.Height(RowHeight));
            bool isHovered = rect.Contains(Event.current.mousePosition);

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    if (isHovered)
                    {
                        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                            ? new Color(1f, 1f, 1f, 0.06f)
                            : new Color(0f, 0f, 0f, 0.06f));
                    }

                    _rowStyle.Draw(rect, content, false, false, false, false);
                    break;

                case EventType.MouseDown:
                    if (!isHovered) break;

                    Event.current.Use();
                    Select(entry.key);
                    break;
            }
        }

        private GUIContent RowContent(LocalizationEntry entry)
        {
            bool isCurrent = string.Equals(entry.key, _currentKey, StringComparison.Ordinal);
            string previewColor = EditorGUIUtility.isProSkin ? "#A0A0A0" : "#5A5A5A";
            string preview = LocalizationEditorUtility.Preview(entry, 70);

            return new GUIContent(
                $"{(isCurrent ? "> " : string.Empty)}<b>{entry.key}</b>\n<color={previewColor}>{preview}</color>");
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{_results.Count} / {_table.entries.Count}", EditorStyles.miniLabel);

                if (GUILayout.Button("Key'i temizle", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Select(string.Empty);
                }
            }
        }

        /// <summary>Esc kapatir, Enter ilk sonucu secer.</summary>
        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown) return;

            switch (current.keyCode)
            {
                case KeyCode.Escape:
                    current.Use();
                    Close();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_results.Count == 0) break;

                    current.Use();
                    Select(_results[0].key);
                    break;
            }
        }

        private void Select(string key)
        {
            _onSelected?.Invoke(key);
            Close();
        }
    }
}
