using UnityEditor;

namespace SDKHub.Editor
{
    [InitializeOnLoad]
    static class SdkHubBootstrap
    {
        static SdkHubBootstrap()
        {
            PackageInstaller.Changed += OnPackagesChanged;
            EditorApplication.delayCall += () => PackageInstaller.Refresh(PackageInstaller.ResumeQueue);
        }

        static void OnPackagesChanged()
        {
            if (!PackageInstaller.IsWorking) SdkRegistry.SyncDefines();
        }
    }
}
