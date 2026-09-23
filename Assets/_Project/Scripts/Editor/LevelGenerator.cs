using System.Collections.Generic;
using MatchPack.Data;
using UnityEditor;
using UnityEngine;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// <see cref="LevelBalanceConfig"/> eğrilerinden bölüm asset'lerini üretir ve <see cref="LevelCatalog"/>'u
    /// günceller. Var olan Level_XXX asset'lerinin üzerine yazar; GUID'leri korunduğu için kayıtlar bozulmaz.
    /// </summary>
    public static class LevelGenerator
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";

        [MenuItem("MatchPack/Generate Levels")]
        public static void Generate()
        {
            LevelBalanceConfig balance = LoadSingle<LevelBalanceConfig>();
            GameConfig gameConfig = LoadSingle<GameConfig>();
            LevelCatalog catalog = LoadSingle<LevelCatalog>();
            if (balance == null || gameConfig == null || catalog == null)
            {
                Debug.LogError("LevelGenerator: LevelBalanceConfig, GameConfig or LevelCatalog asset is missing.");
                return;
            }

            if (balance.ItemPool.Length == 0 || System.Array.IndexOf(balance.ItemPool, null) >= 0)
            {
                Debug.LogError("LevelGenerator: LevelBalanceConfig item pool is empty or has empty rows.", balance);
                return;
            }

            var levels = new LevelData[balance.LevelCount];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = WriteLevel(i + 1, balance, gameConfig.BoxCapacity);
            }

            var catalogObject = new SerializedObject(catalog);
            SerializedProperty levelsProperty = catalogObject.FindProperty("_levels");
            levelsProperty.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                levelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"LevelGenerator: generated {levels.Length} levels.");
        }

        private static LevelData WriteLevel(int levelNumber, LevelBalanceConfig balance, int boxCapacity)
        {
            float duration = balance.GetDuration(levelNumber);
            int typeCount = balance.GetTypeCount(levelNumber);
            float boxesInTime = balance.EstimateBoxesInTime(duration, typeCount, boxCapacity);
            int estimatedBoxCount = Mathf.FloorToInt(boxesInTime * balance.GetTargetTimeUsage(levelNumber));
            int boxCount = Mathf.Max(1, estimatedBoxCount, balance.GetBoxCountForDuration(duration, boxCapacity));
            boxCount = Mathf.Min(boxCount, balance.GetMaxBoxCount(levelNumber, boxCapacity));
            typeCount = Mathf.Min(typeCount, boxCount);

            var random = new System.Random(balance.Seed + levelNumber);
            List<ItemType> boxTypes = BuildBoxOrder(PickTypes(levelNumber, typeCount, balance, random), boxCount, random);

            string path = $"{LevelFolder}/Level_{levelNumber:000}.asset";
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level, path);
            }

            var levelObject = new SerializedObject(level);
            levelObject.FindProperty("_levelIndex").intValue = levelNumber;
            levelObject.FindProperty("_duration").floatValue = duration;
            levelObject.FindProperty("_targetBoxCount").intValue = boxCount;
            levelObject.FindProperty("_conveyorCapacity").intValue = balance.ConveyorCapacity;
            WriteItems(levelObject.FindProperty("_items"), boxTypes, boxCapacity);
            levelObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
            return level;
        }

        private static List<ItemType> PickTypes(int levelNumber, int typeCount, LevelBalanceConfig balance, System.Random random)
        {
            int unlocked = balance.GetUnlockedTypeCount(levelNumber);
            var candidates = new List<ItemType>(balance.ItemPool).GetRange(0, unlocked);
            var picked = new List<ItemType>(typeCount);

            // Bölümde yeni açılan tip her zaman yer alır ki oyuncu yeni objeyi hemen görsün.
            bool hasNewType = levelNumber > 1 && unlocked > balance.GetUnlockedTypeCount(levelNumber - 1);
            if (hasNewType)
            {
                picked.Add(candidates[unlocked - 1]);
                candidates.RemoveAt(unlocked - 1);
            }

            while (picked.Count < typeCount)
            {
                int index = random.Next(candidates.Count);
                picked.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return picked;
        }

        private static List<ItemType> BuildBoxOrder(List<ItemType> types, int boxCount, System.Random random)
        {
            var boxes = new List<ItemType>(boxCount);
            for (int i = 0; i < boxCount; i++)
            {
                boxes.Add(types[i % types.Count]);
            }

            // Bant kutuları Items sırasıyla gönderir; karıştırılmazsa aynı tipin kutuları art arda gelir.
            for (int i = boxes.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (boxes[i], boxes[j]) = (boxes[j], boxes[i]);
            }

            return boxes;
        }

        private static void WriteItems(SerializedProperty items, List<ItemType> boxTypes, int boxCapacity)
        {
            items.arraySize = 0;
            for (int i = 0; i < boxTypes.Count; i++)
            {
                int last = items.arraySize - 1;
                if (last >= 0 && items.GetArrayElementAtIndex(last).FindPropertyRelative("_type").objectReferenceValue == boxTypes[i])
                {
                    items.GetArrayElementAtIndex(last).FindPropertyRelative("_count").intValue += boxCapacity;
                    continue;
                }

                items.arraySize++;
                SerializedProperty entry = items.GetArrayElementAtIndex(items.arraySize - 1);
                entry.FindPropertyRelative("_type").objectReferenceValue = boxTypes[i];
                entry.FindPropertyRelative("_count").intValue = boxCapacity;
            }
        }

        private static T LoadSingle<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
