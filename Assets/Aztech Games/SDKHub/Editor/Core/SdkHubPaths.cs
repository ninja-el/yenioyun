using UnityEditor;

namespace SDKHub.Editor
{
    public static class SdkHubPaths
    {
        const string ScriptSuffix = "/Runtime/SdkHubConfig.cs";

        public static string RootFolder
        {
            get
            {
                foreach (var guid in AssetDatabase.FindAssets("SdkHubConfig t:MonoScript"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(ScriptSuffix)) return path.Substring(0, path.Length - ScriptSuffix.Length);
                }
                return "Assets/SDKHub";
            }
        }
    }
}
