using System.IO;
using UnityEditor;
using UnityEngine;

namespace SDKHub.Editor
{
    public static class ScopedRegistries
    {
        static string ManifestPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

        public static bool Ensure(ScopedRegistryInfo info)
        {
            var path = ManifestPath;
            if (!File.Exists(path)) return false;

            bool changed;
            string[] manualScopes;
            var updated = ManifestEditor.EnsureRegistry(File.ReadAllText(path), info.Name, info.Url, info.Scopes, out changed, out manualScopes);

            if (manualScopes.Length > 0)
            {
                EditorUtility.DisplayDialog("Package registry",
                    "Add these scopes to " + info.Url + " in Edit > Project Settings > Package Manager, then install again:\n\n" +
                    string.Join("\n", manualScopes), "OK");
                return false;
            }

            if (!changed) return false;
            File.WriteAllText(path, updated);
            UnityEditor.PackageManager.Client.Resolve();
            return true;
        }
    }
}
