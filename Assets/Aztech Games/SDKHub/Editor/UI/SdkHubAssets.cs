using UnityEditor;
using UnityEngine;

namespace SDKHub.Editor
{
    static class SdkHubAssets
    {
        public static T Load<T>(string name) where T : Object
        {
            var guids = AssetDatabase.FindAssets($"{name} t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Editor/UI/")) return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }
    }
}
