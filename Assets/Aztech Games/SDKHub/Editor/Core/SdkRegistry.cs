using System;
using System.Collections.Generic;
using UnityEditor;

namespace SDKHub.Editor
{
    public static class SdkRegistry
    {
        static List<SdkModule> modules;

        public static IReadOnlyList<SdkModule> Modules
        {
            get
            {
                if (modules == null) Load();
                return modules;
            }
        }

        public static SdkModule Find(string id)
        {
            foreach (var module in Modules)
            {
                if (module.Id == id) return module;
            }
            return null;
        }

        public static void SyncDefines()
        {
            // Never change defines before the package list has been read, or installed SDKs could lose theirs.
            if (!PackageInstaller.HasLoaded) return;

            var desired = new Dictionary<string, bool>();
            foreach (var module in Modules) desired[module.DefineSymbol] = module.IsInstalled;
            DefineSymbolManager.Sync(desired);
        }

        static void Load()
        {
            modules = new List<SdkModule>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<SdkModule>())
            {
                if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null) continue;
                modules.Add((SdkModule)Activator.CreateInstance(type));
            }
            modules.Sort((a, b) =>
            {
                var byOrder = a.Order.CompareTo(b.Order);
                return byOrder != 0 ? byOrder : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });
        }
    }
}
