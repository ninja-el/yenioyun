using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SDKHub.Editor
{
    public enum InstallType { Registry, GitUrl, External, BuiltIn }

    public enum SdkStatus { NotInstalled, Installing, NeedsSetup, Ready }

    public readonly struct SdkLink
    {
        public readonly string Label;
        public readonly string Url;

        public SdkLink(string label, string url)
        {
            Label = label;
            Url = url;
        }
    }

    public readonly struct SdkSceneAction
    {
        public readonly string Label;
        public readonly string MenuPath;
        public readonly string Tooltip;

        public SdkSceneAction(string label, string menuPath, string tooltip = null)
        {
            Label = label;
            MenuPath = menuPath;
            Tooltip = tooltip;
        }
    }

    public sealed class ScopedRegistryInfo
    {
        public readonly string Name;
        public readonly string Url;
        public readonly string[] Scopes;

        public ScopedRegistryInfo(string name, string url, params string[] scopes)
        {
            Name = name;
            Url = url;
            Scopes = scopes;
        }
    }

    public abstract class SdkModule
    {
        bool? detected;
        int cachedVersion = -1;
        SdkStatus cachedStatus;
        List<CheckItem> cachedChecks = new List<CheckItem>();
        string glyph;

        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract InstallType InstallType { get; }
        public abstract string DefineSymbol { get; }

        public virtual int Order => 100;
        public virtual string Category => "SDKS";
        public virtual ScopedRegistryInfo ScopedRegistry => null;
        public virtual string NavName => DisplayName;
        public virtual bool IsUtility => false;
        public virtual IReadOnlyList<string> Prerequisites => Array.Empty<string>();
        public virtual string PackageName => null;
        public virtual string InstallIdentifier => PackageName;
        public virtual string DetectionType => null;
        public virtual string DownloadUrl => null;
        public virtual string InstallHint => null;
        public virtual string DocsUrl => null;

        // A short, copyable example of how to use this SDK from any script.
        public virtual string CodeUsage => null;
        public virtual IReadOnlyList<SdkLink> Links => Array.Empty<SdkLink>();
        public virtual IReadOnlyList<SdkSceneAction> SceneActions => Array.Empty<SdkSceneAction>();

        public bool IsInstalled
        {
            get
            {
                if (InstallType == InstallType.BuiltIn) return true;
                if (InstallType != InstallType.External && PackageInstaller.IsInstalled(PackageName)) return true;
                if (!detected.HasValue) detected = TypeExists(DetectionType);
                return detected.Value;
            }
        }

        // Two-letter tile text for the sidebar and cards.
        public virtual string Glyph
        {
            get
            {
                if (glyph != null) return glyph;
                var words = DisplayName.Split(new[] { ' ', '-', '&' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2) glyph = (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
                else glyph = DisplayName.Length >= 2 ? DisplayName.Substring(0, 2).ToUpperInvariant() : DisplayName.ToUpperInvariant();
                return glyph;
            }
        }

        // Results are calculated once and reused until something relevant changes.
        public IReadOnlyList<CheckItem> Checks
        {
            get
            {
                Recalculate();
                return cachedChecks;
            }
        }

        public SdkStatus GetStatus()
        {
            Recalculate();
            return cachedStatus;
        }

        void Recalculate()
        {
            if (cachedVersion == SdkHubState.Version) return;
            cachedVersion = SdkHubState.Version;

            var checks = new List<CheckItem>(Validate());
            cachedChecks = checks;

            if (PackageInstaller.IsPending(PackageName))
            {
                cachedStatus = SdkStatus.Installing;
                return;
            }
            if (!IsInstalled)
            {
                cachedStatus = SdkStatus.NotInstalled;
                return;
            }

            cachedStatus = SdkStatus.Ready;
            foreach (var item in checks)
            {
                if (item.Level != CheckLevel.Error) continue;
                cachedStatus = SdkStatus.NeedsSetup;
                break;
            }
        }

        public virtual IEnumerable<CheckItem> Validate()
        {
            yield break;
        }

        public virtual void BuildSettings(VisualElement container, Action onChanged)
        {
        }

        protected string Get(string key) => SdkHubConfigProvider.Get(Id, key);

        static bool TypeExists(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return false;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(fullName, false) != null) return true;
            }
            return false;
        }
    }
}
