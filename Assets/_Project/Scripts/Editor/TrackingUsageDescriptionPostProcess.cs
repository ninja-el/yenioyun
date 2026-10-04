using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace MatchPack.EditorTools
{
    /// <summary>
    /// iOS build'inin Info.plist dosyasına ATT izin penceresinin açıklama metnini yazar. Bu anahtar yoksa iOS izin
    /// penceresini hiç göstermez ve ATT durumu hep NOT_DETERMINED kalır.
    /// </summary>
    public static class TrackingUsageDescriptionPostProcess
    {
        private const string TrackingUsageDescriptionKey = "NSUserTrackingUsageDescription";
        private const string TrackingUsageDescription = "Verileriniz size daha ilgili reklamlar göstermek için kullanılır.";

        [PostProcessBuild(110)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (buildTarget != BuildTarget.iOS) { return; }

            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath))
            {
                Debug.LogError($"Info.plist not found at {plistPath}; {TrackingUsageDescriptionKey} was not written and the ATT prompt will not show.");
                return;
            }

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString(TrackingUsageDescriptionKey, TrackingUsageDescription);
            plist.WriteToFile(plistPath);
#endif
        }
    }
}
