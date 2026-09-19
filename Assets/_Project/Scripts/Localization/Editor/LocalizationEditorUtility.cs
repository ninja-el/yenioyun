using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace MatchPack.Localization
{
    /// <summary>Aramanin hangi sutunlara bakacagi.</summary>
    public enum LocalizationSearchMode
    {
        Key = 0,
        Content = 1,
        Both = 2
    }

    /// <summary>Listenin ekranda hangi sirayla gosterilecegi.</summary>
    public enum LocalizationSortMode
    {
        Unsorted = 0,
        KeyAscending = 1,
        KeyDescending = 2
    }

    /// <summary>
    /// Tablo inspector'i ile key secici penceresinin ortak arama/onizleme mantigi.
    /// Editor klasorunde durdugu icin build'e girmez.
    /// </summary>
    internal static class LocalizationEditorUtility
    {
        public static readonly string[] SearchModeLabels = { "Key", "Icerik", "Ikisi" };
        public static readonly string[] SortModeLabels = { "Sirasiz", "Key A-Z", "Key Z-A" };

        /// <summary>
        /// LocalizationEntry'deki key disindaki butun string alanlari, yani dil sutunlari.
        /// Reflection ile buluyoruz: yeni bir dil eklendiginde (LocalizationEntry'ye yeni
        /// bir alan acildiginda) arama ve onizleme bu dosyaya dokunmadan onu da kapsar.
        /// </summary>
        private static readonly FieldInfo[] ValueFields = typeof(LocalizationEntry)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(string) && field.Name != nameof(LocalizationEntry.key))
            .ToArray();

        private static LocalizationTableSo _cachedTable;

        /// <summary>
        /// Entry aramaya uyuyor mu. Arama metni bossa her satir uyar.
        /// Karsilastirma buyuk/kucuk harf duyarsizdir.
        /// </summary>
        public static bool Matches(LocalizationEntry entry, string search, LocalizationSearchMode mode)
        {
            if (entry == null) return false;
            if (string.IsNullOrWhiteSpace(search)) return true;

            search = search.Trim();

            if (mode != LocalizationSearchMode.Content && Contains(entry.key, search)) return true;

            if (mode != LocalizationSearchMode.Key)
            {
                foreach (FieldInfo field in ValueFields)
                {
                    if (Contains(field.GetValue(entry) as string, search)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Satirin yaninda gosterilecek tek satirlik ozet. English dolu degilse
        /// dolu olan ilk dile duser; hicbiri yoksa bunu gorunur kilar.
        /// </summary>
        public static string Preview(LocalizationEntry entry, int maxLength = 60)
        {
            if (entry == null) return string.Empty;

            string value = entry.english;

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (FieldInfo field in ValueFields)
                {
                    string candidate = field.GetValue(entry) as string;
                    if (string.IsNullOrWhiteSpace(candidate)) continue;

                    value = candidate;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(value)) return "(ceviri yok)";

            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "...";
        }

        /// <summary>Dil adi -> o dildeki metin. Inspector'daki onizleme kutusu icin.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Values(LocalizationEntry entry)
        {
            if (entry == null) yield break;

            foreach (FieldInfo field in ValueFields)
            {
                yield return new KeyValuePair<string, string>(
                    ObjectNames.NicifyVariableName(field.Name),
                    field.GetValue(entry) as string);
            }
        }

        public static LocalizationEntry FindEntry(LocalizationTableSo table, string key)
        {
            if (table == null || string.IsNullOrWhiteSpace(key)) return null;

            foreach (LocalizationEntry entry in table.entries)
            {
                if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal)) return entry;
            }

            return null;
        }

        /// <summary>
        /// Projedeki tabloyu bulur. Loc yalnizca Resources altindakini yukledigi icin
        /// birden fazla asset varsa oyunda gecerli olan tercih edilir.
        /// </summary>
        public static LocalizationTableSo FindTable()
        {
            if (_cachedTable != null) return _cachedTable;

            LocalizationTableSo fallback = null;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(LocalizationTableSo)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LocalizationTableSo table = AssetDatabase.LoadAssetAtPath<LocalizationTableSo>(path);
                if (table == null) continue;

                if (path.Contains("/Resources/")) return _cachedTable = table;

                fallback ??= table;
            }

            return _cachedTable = fallback;
        }

        private static bool Contains(string source, string search)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
