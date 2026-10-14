namespace SDKHub.Editor
{
    public sealed class AttSupportModule : SdkModule
    {
        public override string Id => "att";
        public override string DisplayName => "iOS Tracking Support";
        public override string NavName => "iOS Tracking";
        public override string Description => "Apple's App Tracking Transparency pop-up for iOS. The privacy panel uses it after the player agrees.";
        public override InstallType InstallType => InstallType.Registry;
        public override string PackageName => "com.unity.ads.ios-support";
        public override string DefineSymbol => "SDKHUB_ATT";
        public override string DocsUrl => "https://docs.unity.com/ads/en-us/manual/ATTCompliance";
        public override bool IsUtility => true;
        public override int Order => 3;
    }
}
