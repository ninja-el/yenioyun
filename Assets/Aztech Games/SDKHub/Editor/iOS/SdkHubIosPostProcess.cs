#if SDKHUB_ATT
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SDKHub.Editor.IOS
{
    static class SdkHubIosPostProcess
    {
        const string DefaultText = "This lets us show you ads that are more relevant to you.";

        [PostProcessBuild(900)]
        static void AddTrackingDescription(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;

            var description = SdkHubConfigProvider.Get("privacy", "attDescription");
            if (string.IsNullOrEmpty(description)) description = DefaultText;

            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", description);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
