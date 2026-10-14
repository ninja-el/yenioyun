using System;
using System.Linq;

namespace SDKHub.Editor
{
    // Adds a scoped registry to the text of Packages/manifest.json. Plain code, so it can be tested on its own.
    public static class ManifestEditor
    {
        public static string EnsureRegistry(string text, string name, string url, string[] scopes, out bool changed, out string[] manualScopes)
        {
            changed = false;
            manualScopes = new string[0];

            if (text.Contains(url)) return AddMissingScopes(text, url, scopes, out changed, out manualScopes);

            var entry = BuildEntry(name, url, scopes);
            changed = true;

            var index = text.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
            if (index >= 0)
            {
                var open = text.IndexOf('[', index);
                var close = text.IndexOf(']', open);
                var brace = text.IndexOf('{', open);
                var hasItems = brace >= 0 && brace < close;
                return text.Insert(open + 1, "\n    " + entry + (hasItems ? "," : ""));
            }

            var first = text.IndexOf('{');
            return text.Insert(first + 1, "\n  \"scopedRegistries\": [\n    " + entry + "\n  ],");
        }

        static string AddMissingScopes(string text, string url, string[] scopes, out bool changed, out string[] manualScopes)
        {
            changed = false;
            manualScopes = new string[0];

            var missing = scopes.Where(scope => !text.Contains("\"" + scope + "\"")).ToArray();
            if (missing.Length == 0) return text;

            var urlIndex = text.IndexOf(url, StringComparison.Ordinal);
            var objectStart = text.LastIndexOf('{', urlIndex);
            var objectEnd = text.IndexOf('}', urlIndex);
            var scopesKey = objectStart >= 0 ? text.IndexOf("\"scopes\"", objectStart, StringComparison.Ordinal) : -1;
            if (scopesKey < 0 || scopesKey > objectEnd)
            {
                manualScopes = missing;
                return text;
            }

            var open = text.IndexOf('[', scopesKey);
            var close = text.IndexOf(']', open);
            var quote = text.IndexOf('"', open);
            var hasItems = quote >= 0 && quote < close;

            changed = true;
            var insert = " " + string.Join(", ", missing.Select(scope => "\"" + scope + "\"")) + (hasItems ? "," : " ");
            return text.Insert(open + 1, insert);
        }

        static string BuildEntry(string name, string url, string[] scopes)
        {
            return "{ \"name\": \"" + name + "\", \"url\": \"" + url + "\", \"scopes\": [" +
                   string.Join(", ", scopes.Select(scope => "\"" + scope + "\"")) + "] }";
        }
    }
}
