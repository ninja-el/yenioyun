using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// LocalizedText icin key secici inspector.
    ///
    /// Key'i elle yazmak yerine "Sec" butonuyla tablodaki key'ler arasinda
    /// arama yapilabilir; arama key adina ya da ceviri metnine gore calisir.
    /// Secili key'in butun dillerdeki karsiligi altta onizlenir.
    /// </summary>
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    public class LocalizedTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _keyProp;

        // Secim, pencerenin geri cagrisinda degil bir sonraki cizimde uygulanir:
        // pencere kapanirken serializedObject'e dokunmak guvenli degil.
        private string _pendingKey;

        private void OnEnable()
        {
            _keyProp = serializedObject.FindProperty("key");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ApplyPendingKey();
            DrawProperties();
            DrawKeyStatus();

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPendingKey()
        {
            if (_pendingKey == null) return;

            _keyProp.stringValue = _pendingKey;
            _pendingKey = null;
        }

        /// <summary>
        /// Butun alanlari sirasiyla cizer, sadece key satirina secici butonu ekler.
        /// Bilesene yeni bir alan eklenirse burada kendiliginden gorunur.
        /// </summary>
        private void DrawProperties()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                if (iterator.propertyPath == _keyProp.propertyPath)
                {
                    DrawKeyField();
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private void DrawKeyField()
        {
            LocalizationTableSo table = LocalizationEditorUtility.FindTable();

            using (EditorGUILayout.HorizontalScope row = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_keyProp);

                using (new EditorGUI.DisabledScope(table == null))
                {
                    if (GUILayout.Button("Sec", GUILayout.Width(46)))
                    {
                        // Pencere key satirinin tam altinda acilsin diye satirin alani veriliyor.
                        LocalizationKeyPickerWindow.Open(row.rect, table, _keyProp.stringValue,
                            selected =>
                            {
                                _pendingKey = selected;
                                Repaint();
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Key'in tabloda karsiligi var mi ve varsa hangi metinlere denk geliyor.
        /// Yanlis yazilmis bir key oyunda sessizce key olarak gorunurdu; burada gorulur.
        /// </summary>
        private void DrawKeyStatus()
        {
            LocalizationTableSo table = LocalizationEditorUtility.FindTable();

            if (table == null)
            {
                EditorGUILayout.HelpBox(
                    "LocalizationTable asset'i bulunamadi; key secici kullanilamiyor.", MessageType.Error);
                return;
            }

            if (_keyProp.hasMultipleDifferentValues) return;

            string key = _keyProp.stringValue;

            if (string.IsNullOrWhiteSpace(key))
            {
                EditorGUILayout.HelpBox("Key bos. \"Sec\" ile tablodan bir key sec.", MessageType.Warning);
                return;
            }

            LocalizationEntry entry = LocalizationEditorUtility.FindEntry(table, key);

            if (entry == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{key}' tabloda yok; oyunda key'in kendisi ekrana yazilir.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Onizleme", EditorStyles.miniBoldLabel);

                foreach (KeyValuePair<string, string> value in LocalizationEditorUtility.Values(entry))
                {
                    EditorGUILayout.LabelField(
                        value.Key,
                        string.IsNullOrWhiteSpace(value.Value) ? "(bos)" : value.Value,
                        EditorStyles.miniLabel);
                }
            }
        }
    }
}
