using System.Text.RegularExpressions;
using NUnit.Framework;
using SDKHub.Editor;

namespace SDKHub.Tests
{
    public class ManifestEditorTests
    {
        const string Url = "https://package.openupm.com";
        static readonly string[] Scopes = { "com.google.play.core", "com.google.play.review" };

        const string Plain = "{\n  \"dependencies\": {\n    \"com.unity.ugui\": \"2.0.0\"\n  }\n}";

        static bool Balanced(string text)
        {
            return Count(text, '{') == Count(text, '}') && Count(text, '[') == Count(text, ']');
        }

        static int Count(string text, char c)
        {
            var total = 0;
            foreach (var character in text)
            {
                if (character == c) total++;
            }
            return total;
        }

        static string Ensure(string text, out bool changed, out string[] manual)
        {
            return ManifestEditor.EnsureRegistry(text, "package.openupm.com", Url, Scopes, out changed, out manual);
        }

        [Test]
        public void AddsTheRegistryWhenThereIsNone()
        {
            bool changed;
            string[] manual;
            var result = Ensure(Plain, out changed, out manual);

            Assert.IsTrue(changed);
            Assert.IsTrue(result.Contains("\"scopedRegistries\""));
            Assert.IsTrue(result.Contains(Url));
            Assert.IsTrue(result.Contains("com.google.play.review"));
            Assert.IsTrue(result.Contains("com.unity.ugui"));
            Assert.IsTrue(Balanced(result));
        }

        [Test]
        public void FillsAnEmptyRegistryList()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [ ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsTrue(changed);
            Assert.IsTrue(result.Contains(Url));
            Assert.IsFalse(Regex.IsMatch(result, @",\s*\]"));
            Assert.IsTrue(Balanced(result));
        }

        [Test]
        public void KeepsAnotherRegistryAndAddsTheNewOne()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [\n    { \"name\": \"other\", \"url\": \"https://example.com\", \"scopes\": [ \"com.other\" ] }\n  ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsTrue(changed);
            Assert.IsTrue(result.Contains("https://example.com"));
            Assert.IsTrue(result.Contains(Url));
            Assert.IsTrue(Balanced(result));
        }

        [Test]
        public void LeavesTheFileAloneWhenTheRegistryAndScopesExist()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [\n    { \"name\": \"openupm\", \"url\": \"" + Url + "\", \"scopes\": [ \"com.google.play.core\", \"com.google.play.review\" ] }\n  ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsFalse(changed);
            Assert.AreEqual(text, result);
            Assert.AreEqual(0, manual.Length);
        }

        [Test]
        public void AddsOnlyTheMissingScopes()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [\n    { \"name\": \"openupm\", \"url\": \"" + Url + "\", \"scopes\": [ \"com.google.play.core\" ] }\n  ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsTrue(changed);
            Assert.IsTrue(result.Contains("com.google.play.review"));
            Assert.IsTrue(Balanced(result));
        }

        [Test]
        public void AddsMissingScopesWhenScopesComeBeforeTheUrl()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [\n    { \"scopes\": [ \"com.google.play.core\" ], \"name\": \"openupm\", \"url\": \"" + Url + "\" }\n  ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsTrue(changed);
            Assert.IsTrue(result.Contains("com.google.play.review"));
            Assert.IsTrue(Balanced(result));
        }

        [Test]
        public void AsksForManualHelpWhenTheRegistryHasNoScopesList()
        {
            bool changed;
            string[] manual;
            var text = "{\n  \"scopedRegistries\": [\n    { \"name\": \"openupm\", \"url\": \"" + Url + "\" }\n  ],\n  \"dependencies\": { }\n}";
            var result = Ensure(text, out changed, out manual);

            Assert.IsFalse(changed);
            Assert.AreEqual(text, result);
            Assert.IsTrue(manual.Length > 0);
        }
    }
}
