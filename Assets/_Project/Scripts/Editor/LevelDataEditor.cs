using MatchPack.Data;
using UnityEditor;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// LevelData inspector'ının altına item/kutu özetini ekler: toplam item, bu item'ların
    /// dolduracağı kutu sayısı ve kutu hedefi için gereken item sayısı. Alanlar varsayılan
    /// çizimle aynen kalır.
    /// </summary>
    [CustomEditor(typeof(LevelData))]
    [CanEditMultipleObjects]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private GameConfig _config;

        private void OnEnable()
        {
            _config = FindGameConfig();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1) { return; }

            EditorGUILayout.Space();
            DrawSummary((LevelData)target);
        }

        private void DrawSummary(LevelData level)
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("GameConfig asset'i bulunamadı; kutu kapasitesi okunamıyor.", MessageType.Error);
                return;
            }

            int capacity = _config.BoxCapacity;
            int totalItems = level.TotalItemCount;
            int boxesFromItems = totalItems / capacity;
            int leftoverItems = totalItems % capacity;
            int requiredItems = level.TargetBoxCount * capacity;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Item / Kutu Özeti", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Toplam item", totalItems.ToString());
                EditorGUILayout.LabelField(
                    $"Olması gereken kutu (item / {capacity})",
                    leftoverItems == 0 ? boxesFromItems.ToString() : $"{boxesFromItems} (+{leftoverItems} artık item)");
                EditorGUILayout.LabelField(
                    $"Hedef için gereken item (kutu hedefi x {capacity})",
                    requiredItems.ToString());
            }

            if (totalItems < requiredItems)
            {
                EditorGUILayout.HelpBox(
                    $"{requiredItems - totalItems} item eksik: bu item'larla en fazla {boxesFromItems} kutu dolar, " +
                    $"hedef {level.TargetBoxCount}. Bölüm süre bitmeden kazanılamaz.",
                    MessageType.Error);
            }
            else if (totalItems > requiredItems)
            {
                EditorGUILayout.HelpBox(
                    $"{totalItems - requiredItems} item fazla: hedef dolduğunda yığında item kalır.",
                    MessageType.Warning);
            }

            if (leftoverItems != 0)
            {
                EditorGUILayout.HelpBox(
                    $"Toplam item {capacity}'ün katı değil; {leftoverItems} item hiçbir kutuya giremez.",
                    MessageType.Warning);
            }

            DrawRowWarnings(level, capacity);
        }

        // Bant kutuları satır satır kurar (adet / kapasite, aşağı yuvarlanır); toplam tutsa bile
        // katı olmayan satırın artığı hiçbir kutuya giremez.
        private static void DrawRowWarnings(LevelData level, int capacity)
        {
            for (int i = 0; i < level.Items.Count; i++)
            {
                LevelData.ItemEntry entry = level.Items[i];
                if (entry.Type == null || entry.Count % capacity == 0) { continue; }

                EditorGUILayout.HelpBox(
                    $"{i}. satır ({entry.Type.name}): adet {entry.Count}, {capacity}'ün katı değil; " +
                    $"{entry.Count % capacity} item hiçbir kutuya giremez.",
                    MessageType.Warning);
            }
        }

        private static GameConfig FindGameConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length == 0) { return null; }

            return AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
