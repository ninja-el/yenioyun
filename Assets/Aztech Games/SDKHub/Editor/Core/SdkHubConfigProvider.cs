using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDKHub.Editor
{
    public static class SdkHubConfigProvider
    {
        static SdkHubConfig cached;
        static bool searched;

        public static void ResetSearch()
        {
            searched = false;
        }

        // Finds the config asset without creating it, and remembers the answer.
        static SdkHubConfig Peek()
        {
            if (cached != null) return cached;
            if (searched) return null;
            searched = true;

            foreach (var guid in AssetDatabase.FindAssets("t:SdkHubConfig"))
            {
                cached = AssetDatabase.LoadAssetAtPath<SdkHubConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (cached != null) return cached;
            }
            return null;
        }

        public static SdkHubConfig GetOrCreate()
        {
            var existing = Peek();
            if (existing != null) return existing;

            var folder = SdkHubPaths.RootFolder + "/Resources";
            Directory.CreateDirectory(folder);
            cached = ScriptableObject.CreateInstance<SdkHubConfig>();
            AssetDatabase.CreateAsset(cached, folder + "/SdkHubConfig.asset");
            AssetDatabase.SaveAssets();
            return cached;
        }

        // Reading never creates the asset, so simply looking at a page leaves your project untouched.
        public static string Get(string module, string key)
        {
            var config = Peek();
            return config != null ? config.Get(module, key) : "";
        }

        public static void Set(string module, string key, string value)
        {
            value = value ?? "";
            if (Get(module, key) == value) return;

            var config = GetOrCreate();
            Undo.RecordObject(config, "SDK Hub setting");
            config.Set(module, key, value);
            EditorUtility.SetDirty(config);
            SdkHubState.Bump();
        }
    }
}
