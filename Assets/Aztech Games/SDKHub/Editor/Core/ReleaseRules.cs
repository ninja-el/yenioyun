using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SDKHub.Editor
{
    // The yes/no rules behind the release check. Plain code with no Unity calls, so it can be tested on its own.
    public static class ReleaseRules
    {
        static readonly Regex ApplicationId = new Regex(@"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$");
        static readonly Regex Version = new Regex(@"^\d+(\.\d+){0,2}$");

        public static bool IsValidApplicationId(string id)
        {
            return !string.IsNullOrEmpty(id) && ApplicationId.IsMatch(id);
        }

        // Unity's default id contains DefaultCompany, and old templates use Company.ProductName.
        public static bool IsPlaceholderApplicationId(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return id.IndexOf("DefaultCompany", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("Company.ProductName", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsPlaceholderCompany(string name)
        {
            return string.IsNullOrWhiteSpace(name) || string.Equals(name.Trim(), "DefaultCompany", StringComparison.OrdinalIgnoreCase);
        }

        // Stores expect up to three numbers, for example 1.0 or 1.2.3.
        public static bool IsValidVersion(string version)
        {
            return !string.IsNullOrEmpty(version) && Version.IsMatch(version);
        }

        public static bool TryParseOsVersion(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool IsAndroidApiEnough(int minApi, int requiredApi)
        {
            return requiredApi <= 0 || minApi >= requiredApi;
        }

        public static bool HasDefine(string defines, string symbol)
        {
            if (string.IsNullOrEmpty(defines) || string.IsNullOrEmpty(symbol)) return false;
            foreach (var entry in defines.Split(';'))
            {
                if (entry.Trim() == symbol) return true;
            }
            return false;
        }

        public static void Count(IEnumerable<CheckLevel> levels, out int errors, out int warnings, out int passed)
        {
            errors = 0;
            warnings = 0;
            passed = 0;
            foreach (var level in levels)
            {
                if (level == CheckLevel.Error) errors++;
                else if (level == CheckLevel.Warning) warnings++;
                else passed++;
            }
        }

        public static string Verdict(int errors, int warnings)
        {
            if (errors > 0) return errors == 1 ? "Fix 1 error first" : "Fix " + errors + " errors first";
            if (warnings > 0) return warnings == 1 ? "Almost ready: 1 warning" : "Almost ready: " + warnings + " warnings";
            return "Ready to build";
        }
    }
}
