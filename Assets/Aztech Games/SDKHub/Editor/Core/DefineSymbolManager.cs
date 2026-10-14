using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace SDKHub.Editor
{
    public static class DefineSymbolManager
    {
        static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone
        };

        // Applies all changes in one write per platform, so Unity recompiles once.
        public static void Sync(IDictionary<string, bool> desired)
        {
            foreach (var target in Targets)
            {
                var symbols = Read(target);
                var changed = false;
                foreach (var pair in desired)
                {
                    if (pair.Value)
                    {
                        if (symbols.Contains(pair.Key)) continue;
                        symbols.Add(pair.Key);
                        changed = true;
                    }
                    else if (symbols.Remove(pair.Key))
                    {
                        changed = true;
                    }
                }
                if (changed) PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            }
        }

        static List<string> Read(NamedBuildTarget target)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(target);
            return new List<string>(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
