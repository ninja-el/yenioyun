using NUnit.Framework;
using SDKHub.Editor;

namespace SDKHub.Tests
{
    public class ReleaseRulesTests
    {
        [Test]
        public void ApplicationIdsNeedDotsAndLetters()
        {
            Assert.IsTrue(ReleaseRules.IsValidApplicationId("com.studio.game"));
            Assert.IsTrue(ReleaseRules.IsValidApplicationId("com.studio"));
            Assert.IsTrue(ReleaseRules.IsValidApplicationId("com.my_studio.game2"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId("game"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId("com..game"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId("1com.studio.game"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId("com.studio.2game"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId("com.my-studio.game"));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId(""));
            Assert.IsFalse(ReleaseRules.IsValidApplicationId(null));
        }

        [Test]
        public void PlaceholderIdsAreRecognised()
        {
            Assert.IsTrue(ReleaseRules.IsPlaceholderApplicationId("com.DefaultCompany.MyGame"));
            Assert.IsTrue(ReleaseRules.IsPlaceholderApplicationId("com.defaultcompany.mygame"));
            Assert.IsTrue(ReleaseRules.IsPlaceholderApplicationId("com.Company.ProductName"));
            Assert.IsTrue(ReleaseRules.IsPlaceholderApplicationId(null));
            Assert.IsFalse(ReleaseRules.IsPlaceholderApplicationId("com.aztech.mygame"));
        }

        [Test]
        public void DefaultCompanyNamesAreRecognised()
        {
            Assert.IsTrue(ReleaseRules.IsPlaceholderCompany("DefaultCompany"));
            Assert.IsTrue(ReleaseRules.IsPlaceholderCompany("  "));
            Assert.IsTrue(ReleaseRules.IsPlaceholderCompany(null));
            Assert.IsFalse(ReleaseRules.IsPlaceholderCompany("Aztech Games"));
        }

        [Test]
        public void VersionsHaveUpToThreeNumbers()
        {
            Assert.IsTrue(ReleaseRules.IsValidVersion("1"));
            Assert.IsTrue(ReleaseRules.IsValidVersion("1.0"));
            Assert.IsTrue(ReleaseRules.IsValidVersion("1.2.3"));
            Assert.IsFalse(ReleaseRules.IsValidVersion("1.2.3.4"));
            Assert.IsFalse(ReleaseRules.IsValidVersion("v1.0"));
            Assert.IsFalse(ReleaseRules.IsValidVersion("1.0-beta"));
            Assert.IsFalse(ReleaseRules.IsValidVersion(""));
            Assert.IsFalse(ReleaseRules.IsValidVersion(null));
        }

        [Test]
        public void OsVersionsParseWithAnyLanguageSetting()
        {
            float value;
            Assert.IsTrue(ReleaseRules.TryParseOsVersion("15.0", out value));
            Assert.AreEqual(15f, value);
            Assert.IsFalse(ReleaseRules.TryParseOsVersion("fifteen", out value));
        }

        [Test]
        public void AndroidApiLevelsAreCompared()
        {
            Assert.IsTrue(ReleaseRules.IsAndroidApiEnough(26, 26));
            Assert.IsTrue(ReleaseRules.IsAndroidApiEnough(30, 26));
            Assert.IsFalse(ReleaseRules.IsAndroidApiEnough(23, 26));
            Assert.IsTrue(ReleaseRules.IsAndroidApiEnough(21, 0));
        }

        [Test]
        public void DefineSymbolsAreFoundInTheList()
        {
            Assert.IsTrue(ReleaseRules.HasDefine("A;SDKHUB_IAP;B", "SDKHUB_IAP"));
            Assert.IsTrue(ReleaseRules.HasDefine("A; SDKHUB_IAP ;B", "SDKHUB_IAP"));
            Assert.IsFalse(ReleaseRules.HasDefine("A;SDKHUB_IAP_EXTRA", "SDKHUB_IAP"));
            Assert.IsFalse(ReleaseRules.HasDefine("", "SDKHUB_IAP"));
            Assert.IsFalse(ReleaseRules.HasDefine(null, "SDKHUB_IAP"));
        }

        [Test]
        public void ResultsAreCounted()
        {
            int errors, warnings, passed;
            ReleaseRules.Count(new[] { CheckLevel.Ok, CheckLevel.Error, CheckLevel.Warning, CheckLevel.Ok, CheckLevel.Ok }, out errors, out warnings, out passed);

            Assert.AreEqual(1, errors);
            Assert.AreEqual(1, warnings);
            Assert.AreEqual(3, passed);
        }

        [Test]
        public void TheVerdictSaysWhatToDoNext()
        {
            Assert.AreEqual("Fix 1 error first", ReleaseRules.Verdict(1, 5));
            Assert.AreEqual("Fix 3 errors first", ReleaseRules.Verdict(3, 0));
            Assert.AreEqual("Almost ready: 1 warning", ReleaseRules.Verdict(0, 1));
            Assert.AreEqual("Almost ready: 2 warnings", ReleaseRules.Verdict(0, 2));
            Assert.AreEqual("Ready to build", ReleaseRules.Verdict(0, 0));
        }
    }
}
