using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// LocalizationTable asset'i icin arama + siralama destekli inspector.
    ///
    /// Arama key'e, ceviri metinlerine ya da ikisine birden bakabilir. Siralama
    /// varsayilan olarak sadece gorunumu degistirir; asset'teki gercek sirayi
    /// degistirmek icin alttaki "Listeyi kalici sirala" butonu kullanilir.
    /// </summary>
    [CustomEditor(typeof(LocalizationTableSo))]
    public class LocalizationTableSoEditor : UnityEditor.Editor
    {
        // Cok uzun listelerde inspector'in kilitlenmemesi icin bir seferde cizilen
        // satir sayisi sinirli; gerisine aramayla inilir.
        private const int MaxVisibleEntries = 100;

        private readonly HashSet<int> _expanded = new HashSet<int>();
        private readonly List<int> _visible = new List<int>();

        private SerializedProperty _entriesProp;
        private SearchField _searchField;
        private string _search = string.Empty;
        private LocalizationSearchMode _searchMode = LocalizationSearchMode.Both;
        private LocalizationSortMode _sortMode = LocalizationSortMode.Unsorted;

        private void OnEnable()
        {
            _entriesProp = serializedObject.FindProperty(nameof(LocalizationTableSo.entries));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSearchBar();
            CollectVisibleEntries();
            DrawEntries();
            DrawFooter();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSearchBar()
        {
            _searchField ??= new SearchField();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Ara", GUILayout.Width(EditorGUIUtility.labelWidth));
                    _search = _searchField.OnGUI(_search);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Arama alani", GUILayout.Width(EditorGUIUtility.labelWidth));
                    _searchMode = (LocalizationSearchMode)GUILayout.Toolbar(
                        (int)_searchMode, LocalizationEditorUtility.SearchModeLabels);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Siralama", GUILayout.Width(EditorGUIUtility.labelWidth));
                    _sortMode = (LocalizationSortMode)GUILayout.Toolbar(
                        (int)_sortMode, LocalizationEditorUtility.SortModeLabels);
                }

                // Arama degistiyse acik satirlari kapatiyoruz; yoksa onceki aramadan
                // kalan satirlar yeni sonuclarin arasinda asili kalirdi.
                if (changeCheck.changed) _expanded.Clear();
            }
        }

        /// <summary>
        /// Aramaya uyan satirlarin asset'teki indexlerini secili siraya gore toplar.
        /// Acik olan satirlar aramaya uymasa da listede kalir: aksi halde key alanini
        /// duzenlerken satir yazarken gozden kaybolurdu.
        /// </summary>
        private void CollectVisibleEntries()
        {
            LocalizationTableSo table = (LocalizationTableSo)target;

            _visible.Clear();

            for (int i = 0; i < table.entries.Count; i++)
            {
                LocalizationEntry entry = table.entries[i];
                if (entry == null) continue;

                if (_expanded.Contains(i) ||
                    LocalizationEditorUtility.Matches(entry, _search, _searchMode))
                {
                    _visible.Add(i);
                }
            }

            if (_sortMode == LocalizationSortMode.Unsorted) return;

            int direction = _sortMode == LocalizationSortMode.KeyAscending ? 1 : -1;
            _visible.Sort((left, right) => direction * string.Compare(
                table.entries[left].key, table.entries[right].key, StringComparison.OrdinalIgnoreCase));
        }

        private void DrawEntries()
        {
            LocalizationTableSo table = (LocalizationTableSo)target;
            int removeIndex = -1;
            int drawCount = Mathf.Min(_visible.Count, MaxVisibleEntries);

            for (int i = 0; i < drawCount; i++)
            {
                int index = _visible[i];
                if (index >= _entriesProp.arraySize) continue;

                if (DrawEntry(index, table.entries[index])) removeIndex = index;
            }

            if (_visible.Count > drawCount)
            {
                EditorGUILayout.HelpBox(
                    $"{_visible.Count} satirdan ilk {drawCount} tanesi gosteriliyor. " +
                    "Aramayi daraltarak digerlerine ulasabilirsin.", MessageType.Info);
            }

            if (removeIndex < 0) return;

            _entriesProp.DeleteArrayElementAtIndex(removeIndex);
            serializedObject.ApplyModifiedProperties();

            // Silme sonrasi index'ler kaydigi icin acik satir bilgisi gecersizlesir.
            // ExitGUI, kalan cizimin gecersiz index'lerle devam etmesini engeller.
            _expanded.Clear();
            GUIUtility.ExitGUI();
        }

        /// <summary>Tek bir satiri cizer; silme istendiyse true doner.</summary>
        private bool DrawEntry(int index, LocalizationEntry entry)
        {
            SerializedProperty element = _entriesProp.GetArrayElementAtIndex(index);
            bool wasExpanded = _expanded.Contains(index);
            bool remove = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string label = string.IsNullOrWhiteSpace(entry.key) ? "(key bos)" : entry.key;
                    bool isExpanded = EditorGUILayout.Foldout(wasExpanded, label, true);

                    if (!isExpanded)
                    {
                        GUILayout.Label(LocalizationEditorUtility.Preview(entry), EditorStyles.miniLabel);
                    }

                    if (GUILayout.Button("Sil", EditorStyles.miniButton, GUILayout.Width(38))) remove = true;

                    if (isExpanded != wasExpanded)
                    {
                        if (isExpanded) _expanded.Add(index);
                        else _expanded.Remove(index);
                    }

                    if (!isExpanded) return remove;
                }

                DrawEntryFields(element);
            }

            return remove;
        }

        /// <summary>
        /// Entry'nin butun alanlarini tek tek adlandirmadan cizer; LocalizationEntry'ye
        /// yeni bir dil alani eklendiginde burasi kendiliginden onu da gosterir.
        /// </summary>
        private static void DrawEntryFields(SerializedProperty element)
        {
            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private void DrawFooter()
        {
            LocalizationTableSo table = (LocalizationTableSo)target;

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(_search)
                        ? $"Toplam {table.entries.Count} satir"
                        : $"{table.entries.Count} satirdan {_visible.Count} tanesi eslesti",
                    EditorStyles.miniLabel);

                if (GUILayout.Button("Yeni satir", GUILayout.Width(90))) AddEntry();
            }

            using (new EditorGUI.DisabledScope(_sortMode == LocalizationSortMode.Unsorted))
            {
                if (GUILayout.Button("Listeyi kalici sirala"))
                {
                    SortAsset();
                }
            }

            if (_sortMode == LocalizationSortMode.Unsorted)
            {
                EditorGUILayout.HelpBox(
                    "Siralama secersen listeyi asset icinde de kalici olarak siralayabilirsin.",
                    MessageType.None);
            }
        }

        private void AddEntry()
        {
            int index = _entriesProp.arraySize;
            _entriesProp.arraySize++;

            // Unity yeni elemani bir oncekinin kopyasi olarak olusturuyor; temizliyoruz.
            SerializedProperty element = _entriesProp.GetArrayElementAtIndex(index);
            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.String) iterator.stringValue = string.Empty;
            }

            _expanded.Add(index);
            _search = string.Empty;
        }

        /// <summary>
        /// Asset'teki gercek sirayi secili siralamaya gore degistirir. Undo ile geri alinabilir.
        /// </summary>
        private void SortAsset()
        {
            LocalizationTableSo table = (LocalizationTableSo)target;

            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(table, "Sort Localization Table");

            int direction = _sortMode == LocalizationSortMode.KeyAscending ? 1 : -1;
            table.entries.Sort((left, right) => direction * string.Compare(
                left?.key, right?.key, StringComparison.OrdinalIgnoreCase));

            EditorUtility.SetDirty(table);
            serializedObject.Update();

            _expanded.Clear();
            GUIUtility.ExitGUI();
        }
    }
}
