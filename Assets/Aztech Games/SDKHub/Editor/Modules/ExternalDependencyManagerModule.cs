namespace SDKHub.Editor
{
    public sealed class ExternalDependencyManagerModule : SdkModule
    {
        public override string Id => "edm";
        public override string DisplayName => "External Dependency Manager";
        public override string Description => "Google's resolver for Android libraries and iOS pods. Needed by Firebase and Facebook. Installing from Git requires Git on this computer.";
        public override InstallType InstallType => InstallType.GitUrl;
        public override string PackageName => "com.google.external-dependency-manager";
        public override string InstallIdentifier => "https://github.com/googlesamples/unity-jar-resolver.git?path=upm";
        public override string DetectionType => "Google.VersionHandler";
        public override string DefineSymbol => "SDKHUB_EDM";
        public override string DocsUrl => "https://github.com/googlesamples/unity-jar-resolver";
        public override string NavName => "Dependency Manager";
        public override bool IsUtility => true;
        public override int Order => 1;
    }
}
