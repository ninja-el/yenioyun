using TMPro;
using UnityEngine;

namespace MatchPack.Localization
{
    /// <summary>
    /// Bir TMP metnini localization tablosuna baglar.
    ///
    /// Kullanim: metnin uzerindeki GameObject'e ekle, key alanina tablodaki key'i yaz.
    /// Dil degistiginde kendini otomatik yeniler; kod yazmaya gerek yok.
    ///
    /// Icinde {0} gibi yer tutucu olan metinler icin SetFormatArgs kullan:
    ///     levelLabel.SetFormatArgs(data.levelIndex);
    /// Verilen argumanlar saklanir, dil degisince ayni argumanlarla yeniden kurulur.
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Resources/LocalizationTable.asset icindeki key. Ornek: ui.settings.title")]
        [SerializeField] private string key;

        [Tooltip("Bos birakilirsa ayni GameObject uzerindeki TMP bileseni kullanilir.")]
        [SerializeField] private TMP_Text target;

        private object[] _formatArgs;

        /// <summary>Bu bilesenin bagli oldugu key.</summary>
        public string Key => key;

        private void Reset() => target = GetComponent<TMP_Text>();

        private void Awake()
        {
            if (target == null) target = GetComponent<TMP_Text>();

            if (target == null)
            {
                Debug.LogError($"[Localization] '{name}' uzerinde TMP bileseni yok; " +
                               "LocalizedText'in target alanini elle doldur.", this);
            }
        }

        private void OnEnable()
        {
            Loc.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.OnLanguageChanged -= Refresh;
        }

        /// <summary>
        /// Key'i calisma aninda degistirir. Ayni bilesenin duruma gore farkli metin
        /// gosterdigi yerlerde (ornegin ON/OFF) kullanilir.
        /// </summary>
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        /// <summary>
        /// Metindeki {0}, {1} yer tutucularini dolduracak degerleri verir ve yeniler.
        /// Dil degistiginde bu degerler korunur.
        /// </summary>
        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            Refresh();
        }

        /// <summary>Metni aktif dile gore yeniden kurar.</summary>
        public void Refresh()
        {
            if (target == null) return;

            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning($"[Localization] '{name}' uzerindeki LocalizedText'in key'i bos.", this);
                return;
            }

            target.SetText(_formatArgs == null || _formatArgs.Length == 0
                ? Loc.Get(key)
                : Loc.Format(key, _formatArgs));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (target == null) target = GetComponent<TMP_Text>();
        }
#endif
    }
}
