using System.Collections.Generic;

namespace SDKHub.Editor
{
    public static class SdkInstaller
    {
        public static void Install(SdkModule module)
        {
            InstallMany(new[] { module });
        }

        public static void InstallMany(IEnumerable<SdkModule> modules)
        {
            var items = new List<KeyValuePair<string, string>>();
            var seen = new HashSet<string>();
            foreach (var module in modules) Collect(module, items, seen);
            PackageInstaller.InstallBatch(items);
        }

        static void Collect(SdkModule module, List<KeyValuePair<string, string>> items, HashSet<string> seen)
        {
            if (!seen.Add(module.Id)) return;

            foreach (var id in module.Prerequisites)
            {
                var prerequisite = SdkRegistry.Find(id);
                if (prerequisite != null) Collect(prerequisite, items, seen);
            }

            if (module.InstallType == InstallType.External || module.InstallType == InstallType.BuiltIn || module.IsInstalled) return;
            if (module.ScopedRegistry != null) ScopedRegistries.Ensure(module.ScopedRegistry);
            items.Add(new KeyValuePair<string, string>(module.PackageName, module.InstallIdentifier));
        }
    }
}
