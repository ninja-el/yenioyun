using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// UIIntegrationTool'un sahne üzerinde çalışırken kullandığı yardımcılar. Tek seferlik bir
    /// araca ait olduğu için kalıcı kod standardı yerine "hata verirse anlaşılsın" önceliklidir.
    /// </summary>
    public static class UIIntegrationUtility
    {
        private static readonly List<string> Report = new List<string>();
        private static readonly List<string> Problems = new List<string>();

        public static void ResetReport()
        {
            Report.Clear();
            Problems.Clear();
        }

        public static void Log(string line)
        {
            Report.Add(line);
        }

        public static void Problem(string line)
        {
            Problems.Add(line);
        }

        public static bool HasProblems => Problems.Count > 0;

        public static string BuildReport()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"UI entegrasyonu tamamlandı. {Report.Count} adım uygulandı.");

            for (int i = 0; i < Report.Count; i++) { builder.Append("  + ").AppendLine(Report[i]); }

            if (Problems.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Elle bakılması gereken {Problems.Count} nokta:");
                for (int i = 0; i < Problems.Count; i++) { builder.Append("  ! ").AppendLine(Problems[i]); }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Verilen kökten "/" ile ayrılmış yolu izleyerek objeyi bulur. İsimlerdeki baş/son
        /// boşluklar yok sayılır; sahnedeki bazı objelerin adında sondan boşluk var.
        /// </summary>
        public static GameObject Find(GameObject root, string path)
        {
            if (root == null) { return null; }
            if (string.IsNullOrEmpty(path)) { return root; }

            Transform current = root.transform;
            string[] segments = path.Split('/');

            for (int i = 0; i < segments.Length; i++)
            {
                Transform next = FindChild(current, segments[i]);
                if (next == null) { return null; }

                current = next;
            }

            return current.gameObject;
        }

        /// <summary>Objeyi bulur; bulamazsa rapora sorun olarak yazar ve null döner.</summary>
        public static GameObject Require(GameObject root, string path)
        {
            GameObject found = Find(root, path);
            if (found == null) { Problem($"Sahnede bulunamadı: {root.name}/{path}"); }

            return found;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            string target = name.Trim();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name.Trim(), target, System.StringComparison.Ordinal)) { return child; }
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name.Trim(), target, System.StringComparison.OrdinalIgnoreCase)) { return child; }
            }

            return null;
        }

        /// <summary>Bileşen yoksa ekler, varsa mevcut olanı döner.</summary>
        public static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (target == null) { return null; }

            T existing = target.GetComponent<T>();
            if (existing != null) { return existing; }

            T added = Undo.AddComponent<T>(target);
            Log($"{target.name} objesine {typeof(T).Name} eklendi.");
            return added;
        }

        /// <summary>Verilen yoldaki objenin bileşenini döner; obje veya bileşen yoksa rapora yazar.</summary>
        public static T RequireComponent<T>(GameObject root, string path) where T : Component
        {
            GameObject target = Require(root, path);
            if (target == null) { return null; }

            T component = target.GetComponent<T>();
            if (component == null) { Problem($"{path} üzerinde {typeof(T).Name} yok."); }

            return component;
        }

        /// <summary>[SerializeField] private bir alana referans yazar. Alan yoksa rapora yazar.</summary>
        public static void SetField(Component target, string fieldName, Object value)
        {
            if (target == null) { return; }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Problem($"{target.GetType().Name} içinde {fieldName} alanı yok.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>[SerializeField] private bir string alana değer yazar.</summary>
        public static void SetString(Component target, string fieldName, string value)
        {
            if (target == null) { return; }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Problem($"{target.GetType().Name} içinde {fieldName} alanı yok.");
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>[SerializeField] private bir float alana değer yazar.</summary>
        public static void SetFloat(Component target, string fieldName, float value)
        {
            if (target == null) { return; }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                Problem($"{target.GetType().Name} içinde {fieldName} alanı yok.");
                return;
            }

            property.floatValue = value;
            serialized.ApplyModifiedProperties();
            Log($"{target.GetType().Name}.{fieldName} = {value}");
        }

        /// <summary>
        /// Bir UI grafiğinin raycast hedefliğini kapatır veya açar. Butonun üstünde duran yazılar
        /// açık kaldığında tıklamayı yutuyor; anahtarların çalışmaması bundan kaynaklanıyordu.
        /// </summary>
        public static void SetRaycastTarget(GameObject root, string path, bool isRaycastTarget)
        {
            GameObject target = Find(root, path);
            if (target == null) { return; }

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null || graphic.raycastTarget == isRaycastTarget) { return; }

            Undo.RecordObject(graphic, "UI Integration Raycast Target");
            graphic.raycastTarget = isRaycastTarget;
            EditorUtility.SetDirty(graphic);
            Log($"{target.name}.raycastTarget = {isRaycastTarget}");
        }

        /// <summary>
        /// Objeyi yeni bir ebeveynin altına, verilen sıraya taşır. RectTransform değerleri
        /// taşımadan önce okunup sonra geri yazılır; iki ebeveyn de tam ekran olduğu için yerleşim değişmez.
        /// </summary>
        public static void Reparent(GameObject target, Transform newParent, int siblingIndex)
        {
            if (target == null || newParent == null) { return; }

            RectTransform rect = target.transform as RectTransform;
            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.one;
            Vector2 anchoredPosition = Vector2.zero;
            Vector2 sizeDelta = Vector2.zero;
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            if (rect != null)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                anchoredPosition = rect.anchoredPosition;
                sizeDelta = rect.sizeDelta;
                pivot = rect.pivot;
            }

            Vector3 localScale = target.transform.localScale;

            if (target.transform.parent != newParent)
            {
                Undo.SetTransformParent(target.transform, newParent, "UI Integration Reparent");
                Log($"{target.name} -> {newParent.name} altına taşındı.");
            }

            if (rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.sizeDelta = sizeDelta;
                rect.anchoredPosition = anchoredPosition;
            }

            target.transform.localScale = localScale;
            target.transform.SetSiblingIndex(siblingIndex);
            EditorUtility.SetDirty(target);
        }

        /// <summary>Objenin aktifliğini geri alınabilir şekilde ayarlar.</summary>
        public static void SetActive(GameObject target, bool isActive)
        {
            if (target == null || target.activeSelf == isActive) { return; }

            Undo.RegisterCompleteObjectUndo(target, "UI Integration SetActive");
            target.SetActive(isActive);
            Log($"{target.name} -> SetActive({isActive})");
        }
    }
}
