using System;
using System.Collections.Generic;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// Localization'a tek giris noktasi. Kullanimi: Loc.Get("ui.settings.title")
    ///
    /// Sinif adi bilerek kisa: namespace zaten "Localization" oldugu icin ayni adi
    /// tasiyan bir sinif C# tarafinda cakisirdi.
    ///
    /// Dil secimi su sirayla belirlenir:
    ///   1) Oyuncunun ayarlardan sectigi dil (PlayerPrefs)
    ///   2) Cihaz dili — Turkce/Ispanyolca/Portekizce ise ona gecer
    ///   3) English
    ///
    /// Bir metnin secili dildeki karsiligi bossa English'e duser; o da bossa key'in
    /// kendisi ekrana yazilir (eksik ceviri gozle gorulur olsun diye).
    ///
    /// Sahneye hicbir sey eklemek gerekmez; ilk erisimde kendini kurar.
    /// </summary>
    public static class Loc
    {
        /// <summary>Ceviri bulunamadiginda donulen dil. Tablodaki English sutunu her zaman dolu olmali.</summary>
        public const LanguageCode FallbackLanguage = LanguageCode.English;

        private const string TableResourcePath = "LocalizationTable";

        // Dil, diger ayarlarin aksine PlayerPrefs'te tutulur: Loc static ve ilk Get cagrisinda
        // kendini kurar, SaveManager o an henuz ayakta olmayabilir.
        private const string LanguagePrefKey = "matchpack.language";

        /// <summary>
        /// Dil degistiginde tetiklenir. LocalizedText bunu dinleyip kendini yeniler;
        /// metni elle kuran yerler (format'li HUD metinleri gibi) de buna abone olmali.
        /// </summary>
        public static event Action OnLanguageChanged;

        private static LanguageCode _currentLanguage = FallbackLanguage;
        private static LocalizationTableSo _table;
        private static Dictionary<string, LocalizationEntry> _lookup;
        private static bool _isInitialized;

        // Ayni eksik key her frame loglanmasin diye.
        private static readonly HashSet<string> WarnedKeys = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Su an aktif olan dil.</summary>
        public static LanguageCode CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return _currentLanguage;
            }
        }

        /// <summary>Ayarlar ekranindaki dil secicisini doldurmak icin.</summary>
        public static LanguageCode[] AvailableLanguages =>
            (LanguageCode[])Enum.GetValues(typeof(LanguageCode));

        /// <summary>
        /// Dili degistirir, secimi kaydeder ve OnLanguageChanged'i tetikler.
        /// </summary>
        /// <param name="save">
        /// false verilirse secim PlayerPrefs'e yazilmaz. Sadece gecici onizleme
        /// senaryolari icin; normal kullanimda true birak.
        /// </param>
        public static void SetLanguage(LanguageCode language, bool save = true)
        {
            EnsureInitialized();

            if (save) SaveLanguage(language);

            // Ayni dile tekrar gecisde event atmiyoruz; abonelerin bosuna
            // kendini yenilemesine gerek yok.
            if (_currentLanguage == language) return;

            _currentLanguage = language;
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Key'in aktif dildeki karsiligini dondurur.
        /// Key tabloda yoksa veya butun karsiliklari bossa key'in kendisi doner.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            EnsureInitialized();

            if (_lookup == null || !_lookup.TryGetValue(key, out LocalizationEntry entry))
            {
                WarnOnce(key, $"[Localization] '{key}' key'i tabloda yok. " +
                              "Resources/LocalizationTable.asset'e ekle.");
                return key;
            }

            string value = entry.Get(_currentLanguage);
            if (!string.IsNullOrEmpty(value)) return value;

            if (_currentLanguage != FallbackLanguage)
            {
                value = entry.Get(FallbackLanguage);
                if (!string.IsNullOrEmpty(value))
                {
                    WarnOnce(key, $"[Localization] '{key}' icin {_currentLanguage} cevirisi bos; " +
                                  $"{FallbackLanguage} kullanildi.");
                    return value;
                }
            }

            WarnOnce(key, $"[Localization] '{key}' hicbir dilde dolu degil.");
            return key;
        }

        /// <summary>
        /// Icinde {0}, {1} gibi yer tutucular olan metinler icin.
        /// Ornek: Loc.Format("ui.hud.level", 7) -> "Bolum 7"
        ///
        /// Cevirmen yer tutucuyu bozarsa (mesela {0} yerine {O} yazarsa) exception
        /// firlatilmaz; ham metin donulur ve hata loglanir.
        /// </summary>
        public static string Format(string key, params object[] args)
        {
            string value = Get(key);
            if (args == null || args.Length == 0) return value;

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[Localization] '{key}' metnindeki yer tutucular hatali: {e.Message}");
                return value;
            }
        }

        /// <summary>Key tabloda tanimli mi. Opsiyonel metinler icin.</summary>
        public static bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            EnsureInitialized();
            return _lookup != null && _lookup.ContainsKey(key);
        }

        /// <summary>
        /// Cihaz dilinin destekledigimiz karsiligi. Desteklenmeyen her dil English'e duser.
        /// </summary>
        public static LanguageCode DetectDeviceLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Turkish: return LanguageCode.Turkish;
                case SystemLanguage.Spanish: return LanguageCode.Spanish;
                case SystemLanguage.Portuguese: return LanguageCode.Portuguese;
                default: return LanguageCode.English;
            }
        }

        private static void EnsureInitialized()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            LoadTable();
            _currentLanguage = ResolveStartupLanguage();
        }

        private static void LoadTable()
        {
            _table = Resources.Load<LocalizationTableSo>(TableResourcePath);

            if (_table == null)
            {
                Debug.LogError($"[Localization] Resources/{TableResourcePath}.asset bulunamadi; " +
                               "butun metinler key olarak gorunecek.");
                return;
            }

            _lookup = _table.BuildLookup();
        }

        /// <summary>
        /// Oyuncu daha once bir dil sectiyse onu, secmediyse cihaz dilini kullanir.
        /// Kayitli deger tanimsizsa (eski surumden kalan bir dil gibi) cihaz diline doner.
        /// </summary>
        private static LanguageCode ResolveStartupLanguage()
        {
            if (PlayerPrefs.HasKey(LanguagePrefKey))
            {
                string saved = PlayerPrefs.GetString(LanguagePrefKey);
                if (Enum.TryParse(saved, out LanguageCode savedLanguage) &&
                    Enum.IsDefined(typeof(LanguageCode), savedLanguage))
                {
                    return savedLanguage;
                }

                Debug.LogWarning($"[Localization] Kayitli dil '{saved}' taninmiyor; cihaz diline donuluyor.");
            }

            return DetectDeviceLanguage();
        }

        private static void SaveLanguage(LanguageCode language)
        {
            // Int yerine isim yaziyoruz: enum'a ileride ortadan bir dil eklenirse
            // oyuncularin kayitli secimi baska bir dile kaymasin.
            PlayerPrefs.SetString(LanguagePrefKey, language.ToString());
            PlayerPrefs.Save();
        }

        // Iki Conditional birlikte "veya" anlamina gelir. Release build'de cagri tamamen
        // silinir; interpolasyonlu mesaj string'i de hic olusturulmaz.
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void WarnOnce(string key, string message)
        {
            if (WarnedKeys.Add(key)) Debug.LogWarning(message);
        }

        // Domain reload kapaliyken static alanlar Play oturumlari arasinda tasinir;
        // her Play'de sifirlanmalari gerekiyor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _isInitialized = false;
            _currentLanguage = FallbackLanguage;
            _table = null;
            _lookup = null;
            OnLanguageChanged = null;
            WarnedKeys.Clear();
        }
    }
}
