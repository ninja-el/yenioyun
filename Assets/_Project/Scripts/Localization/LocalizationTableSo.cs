using System;
using System.Collections.Generic;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// Tek bir metnin butun dillerdeki karsiliklari.
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        [Tooltip("Metnin koddaki kimligi. Ornek: ui.settings.title")]
        public string key;

        [Tooltip("Fallback dili. Bos birakilirsa o metin hicbir dilde gorunmez, key'in kendisi ekrana yazilir.")]
        [TextArea(1, 4)] public string english;

        [TextArea(1, 4)] public string turkish;
        [TextArea(1, 4)] public string spanish;

        [Tooltip("Brezilya Portekizcesi (pt-BR).")]
        [TextArea(1, 4)] public string portuguese;

        /// <summary>
        /// Istenen dildeki metni dondurur. Bos olabilir; fallback mantigi Loc icinde.
        /// </summary>
        public string Get(LanguageCode language)
        {
            switch (language)
            {
                case LanguageCode.Turkish: return turkish;
                case LanguageCode.Spanish: return spanish;
                case LanguageCode.Portuguese: return portuguese;
                case LanguageCode.English: return english;

                default:
                    Debug.LogError($"[Localization] {language} icin LocalizationEntry alani tanimlanmamis. " +
                                   "LocalizationEntry'ye alanini ve Get switch'ine satirini ekle.");
                    return english;
            }
        }
    }

    /// <summary>
    /// Butun oyun metinlerinin tek kaynagi.
    ///
    /// Asset Resources/LocalizationTable.asset yolunda durmali; Loc onu buradan
    /// yukluyor. Adini veya yerini degistirirsen Loc.TableResourcePath'i de guncelle.
    ///
    /// CSV disa/ice aktarma ve otomatik ceviri sonradan eklenecek. Entry listesi
    /// duz bir yapida tutuldugu icin CSV importer'i bu dosyaya dokunmadan yazilabilir.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationTable", menuName = "Game/Localization Table")]
    public class LocalizationTableSo : ScriptableObject
    {
        [Tooltip("Metin listesi. Key'ler benzersiz olmali.")]
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        /// <summary>
        /// Key -> entry sozlugu uretir. Loc bunu bir kez kurup onbellekte tutar.
        /// Kopyalanmis key'lerde ilk satir kazanir; ikincisi uyari verir.
        /// </summary>
        public Dictionary<string, LocalizationEntry> BuildLookup()
        {
            Dictionary<string, LocalizationEntry> lookup =
                new Dictionary<string, LocalizationEntry>(entries.Count, StringComparer.Ordinal);

            foreach (LocalizationEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;

                if (!lookup.TryAdd(entry.key, entry))
                {
                    Debug.LogError($"[Localization] '{entry.key}' key'i tabloda birden fazla kez var; " +
                                   "ilk satir kullanilacak.", this);
                }
            }

            return lookup;
        }

        private void OnValidate() => ValidateEntries();

        /// <summary>
        /// Bos key, bos English karsiligi ve kopyalanmis key'leri yakalar. Ucu de
        /// sessiz bozulmadir: oyunda key'in kendisi ekrana yazilir, hata gec fark edilir.
        /// </summary>
        private void ValidateEntries()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationEntry entry = entries[i];
                if (entry == null) continue;

                if (string.IsNullOrWhiteSpace(entry.key))
                {
                    Debug.LogError($"[Localization] {i}. satirin key'i bos.", this);
                    continue;
                }

                if (!seen.Add(entry.key))
                {
                    Debug.LogError($"[Localization] '{entry.key}' key'i birden fazla satirda kullanilmis.", this);
                }

                if (string.IsNullOrWhiteSpace(entry.english))
                {
                    Debug.LogWarning($"[Localization] '{entry.key}' icin English karsiligi bos; " +
                                     "bu metin hicbir dilde fallback bulamaz.", this);
                }
            }
        }
    }
}
