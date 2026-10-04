using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MatchPack.Data;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// PlayerData'nın tek sahibi. Veri, persistentDataPath altındaki tek bir JSON dosyasına yazılır
    /// ve Cloud Save ile eşlenir: açılışta bölümü ileride olan kayıt kazanır, satın alınan ürünler birleştirilir.
    /// Cihaza özgü ayarlar (SettingsData) ayrı bir JSON dosyasında tutulur ve cloud'a gönderilmez.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveFileName = "playerdata.json";
        private const string SettingsFileName = "settings.json";
        private const string TempSuffix = ".tmp";
        private const string LegacyPrefsKey = "MatchPack.PlayerData";
        private const string LegacyCloudKey = "playerdata";
        private const char FieldPrefix = '_';
        private const int UnknownCloudLevel = -1;

        // Can zamanlayıcıları ve etkinlik sayaçları yalnızca lokal kayıtta tutulur; cloud'a gönderilmez, cloud'dan okunmaz.
        private static readonly HashSet<string> LocalOnlyCloudKeys = new HashSet<string>
        {
            "lastLifeRegenTime", "infiniteLivesUntilTime", "eventItemCounts"
        };

        // Eski sürümlerin cloud'a yazdığı ve artık kullanılmayan anahtarlar; ilk başarılı yüklemede silinir.
        private static readonly HashSet<string> RetiredCloudKeys = new HashSet<string>
        {
            LegacyCloudKey, "isSoundEnabled", "isMusicEnabled", "isHapticsEnabled"
        };

        // Boot'ta okunan cloud kaydı, MainScene'deki SaveManager ayağa kalkana kadar burada bekler.
        private static string _pendingCloudJson;
        private static int _knownCloudLevel = UnknownCloudLevel;
        private static readonly List<string> _staleCloudKeys = new List<string>();

        [Tooltip("Aktif oyuncu verisi. Play Mode'da buradan değiştirilen değerler kayda yansır.")]
        [SerializeField] private PlayerData _data = new PlayerData();

        [Tooltip("Aktif ayarlar (ses, müzik, titreşim). Play Mode'da buradan değiştirilen değerler kayda yansır.")]
        [SerializeField] private SettingsData _settings = new SettingsData();

        private string _savePath;
        private string _tempPath;
        private string _settingsPath;
        private bool _isUploading;
        private bool _hasQueuedUpload;

        public PlayerData Data => _data;
        public SettingsData Settings => _settings;

        /// <summary>Cihazda daha önce yazılmış bir kayıt var mı? İlk açılış varsayılanlarını kurmak için kullanılır.</summary>
        public bool HasSave => File.Exists(_savePath) || File.Exists(_tempPath);

        private static bool IsSignedIn =>
            UnityServices.State == ServicesInitializationState.Initialized
            && AuthenticationService.Instance.IsSignedIn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            _tempPath = _savePath + TempSuffix;
            _settingsPath = Path.Combine(Application.persistentDataPath, SettingsFileName);

            MigrateFromPlayerPrefs();
            LoadSettings();
            Load();
            ApplyPendingCloudSave();
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Save();
                SaveSettings();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
            SaveSettings();
        }

        /// <summary>
        /// Oyuncunun cloud kaydını okur. Boot'ta giriş tamamlandıktan sonra, MainScene yüklenmeden çağrılır.
        /// SaveManager zaten ayaktaysa veri bu oturumda uygulanmaz, yalnızca yükleme kontrolü için not edilir.
        /// </summary>
        public static async Task FetchCloudSaveAsync()
        {
            if (!IsSignedIn) { return; }

            try
            {
                Dictionary<string, Item> items = await CloudSaveService.Instance.Data.Player.LoadAllAsync();
                _staleCloudKeys.Clear();
                foreach (string key in items.Keys)
                {
                    if (LocalOnlyCloudKeys.Contains(key) || RetiredCloudKeys.Contains(key)) { _staleCloudKeys.Add(key); }
                }

                string json = ToPlayerDataJson(items);
                if (json == null)
                {
                    _knownCloudLevel = 0;
                    return;
                }

                PlayerData cloudData = new PlayerData();
                JsonUtility.FromJsonOverwrite(json, cloudData);

                _knownCloudLevel = cloudData.CurrentLevel;
                if (Instance == null) { _pendingCloudJson = json; }
            }
            // Paketin çözümleme hatası internal sınıf olduğu için ayrı yakalanamaz; her hata "cloud bilinmiyor" sayılır.
            catch (Exception exception)
            {
                Debug.LogWarning($"Cloud save could not be loaded, continuing with local save: {exception.Message}");
            }
        }

        /// <summary>Kaydı okur. Kayıt yoksa veya bozuksa veri varsayılan değerlerinde kalır.</summary>
        public void Load()
        {
            ReadInto(_savePath, _data);
        }

        /// <summary>Aktif veriyi diske yazar.</summary>
        public void Save()
        {
            WriteAtomically(_savePath, JsonUtility.ToJson(_data));
        }

        /// <summary>Aktif ayarları diske yazar. Cloud'a gönderilmez.</summary>
        public void SaveSettings()
        {
            WriteAtomically(_settingsPath, JsonUtility.ToJson(_settings));
        }

        /// <summary>
        /// Aktif veriyi Cloud Save'e gönderir. Bölüm geçilince ve satın alma sonrası çağrılır.
        /// Cloud kaydı okunamadıysa veya cloud'daki bölüm daha ilerideyse gönderilmez.
        /// </summary>
        public void UploadToCloud()
        {
            if (_isUploading)
            {
                _hasQueuedUpload = true;
                return;
            }

            _ = UploadToCloudAsync();
        }

        /// <summary>Kaydı siler ve veriyi varsayılanlara döndürür.</summary>
        public void ResetProgress()
        {
            if (File.Exists(_savePath)) { File.Delete(_savePath); }
            if (File.Exists(_tempPath)) { File.Delete(_tempPath); }

            // Data referansını tutan sistemler bozulmasın diye yeni nesne atanmaz, alanların üzerine yazılır.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new PlayerData()), _data);
        }

        // Ayar dosyası yoksa ayarlar eski playerdata.json'dan okunur; alan adları aynı olduğu için doğrudan eşleşir.
        private void LoadSettings()
        {
            if (ReadInto(_settingsPath, _settings)) { return; }
            if (ReadInto(_savePath, _settings)) { SaveSettings(); }
        }

        // Yazma, silme ile taşıma arasında kesilirse elde yalnızca .tmp kalır; o da geçerli tam kayıttır.
        private bool ReadInto(string path, object target)
        {
            string tempPath = path + TempSuffix;
            string readPath = File.Exists(path) ? path : tempPath;
            if (!File.Exists(readPath)) { return false; }

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(readPath), target);
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                Debug.LogError($"{Path.GetFileName(path)} could not be read, falling back to defaults: {exception.Message}", this);
                return false;
            }
        }

        // Önce geçici dosyaya yazılır ki yazma yarıda kesilirse eski kayıt bozulmasın.
        private void WriteAtomically(string path, string json)
        {
            string tempPath = path + TempSuffix;
            try
            {
                File.WriteAllText(tempPath, json);
                if (File.Exists(path)) { File.Delete(path); }
                File.Move(tempPath, path);
            }
            catch (IOException exception)
            {
                Debug.LogError($"{Path.GetFileName(path)} could not be written: {exception.Message}", this);
            }
        }

        private async Task UploadToCloudAsync()
        {
            _isUploading = true;

            do
            {
                _hasQueuedUpload = false;

                // Cloud durumu bilinmeden yazmak, başka cihazdaki ilerlemeyi geride kalan kayıtla ezebilir.
                if (!IsSignedIn || _knownCloudLevel == UnknownCloudLevel || _data.CurrentLevel < _knownCloudLevel) { break; }

                int uploadedLevel = _data.CurrentLevel;
                try
                {
                    await CloudSaveService.Instance.Data.Player.SaveAsync(ToCloudFields(_data));
                    _knownCloudLevel = uploadedLevel;

                    while (_staleCloudKeys.Count > 0)
                    {
                        await CloudSaveService.Instance.Data.Player.DeleteAsync(
                            _staleCloudKeys[0], new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
                        _staleCloudKeys.RemoveAt(0);
                    }
                }
                catch (RequestFailedException exception)
                {
                    Debug.LogWarning($"Cloud save could not be uploaded: {exception.Message}");
                }
            }
            while (_hasQueuedUpload);

            _isUploading = false;
        }

        // Her PlayerData alanı Dashboard'da ayrı satır olsun diye ayrı anahtara yazılır; anahtar, alan adının baştaki "_" olmadan hâlidir.
        private static Dictionary<string, object> ToCloudFields(PlayerData data)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();
            foreach (JProperty field in JObject.Parse(JsonUtility.ToJson(data)).Properties())
            {
                string key = field.Name.TrimStart(FieldPrefix);
                if (LocalOnlyCloudKeys.Contains(key)) { continue; }

                fields[key] = field.Value;
            }

            return fields;
        }

        // Alan anahtarları yoksa eski tek anahtarlı kayıt okunur.
        private static string ToPlayerDataJson(Dictionary<string, Item> items)
        {
            JObject fields = new JObject();
            foreach (Item item in items.Values)
            {
                if (LocalOnlyCloudKeys.Contains(item.Key) || RetiredCloudKeys.Contains(item.Key)) { continue; }

                fields[FieldPrefix + item.Key] = item.Value.GetAs<JToken>();
            }

            if (fields.Count > 0) { return fields.ToString(); }
            if (!items.TryGetValue(LegacyCloudKey, out Item legacy)) { return null; }

            JObject legacyFields = JObject.Parse(legacy.Value.GetAsString());
            foreach (string key in LocalOnlyCloudKeys) { legacyFields.Remove(FieldPrefix + key); }

            return legacyFields.ToString();
        }

        private void ApplyPendingCloudSave()
        {
            if (_pendingCloudJson == null) { return; }

            PlayerData cloudData = new PlayerData();
            JsonUtility.FromJsonOverwrite(_pendingCloudJson, cloudData);

            string[] ownedProductIds = MergeProductIds(_data.OwnedProductIds, cloudData.OwnedProductIds);
            bool hasRemovedAds = _data.HasRemovedAds || cloudData.HasRemovedAds;

            if (!HasSave || cloudData.CurrentLevel > _data.CurrentLevel)
            {
                JsonUtility.FromJsonOverwrite(_pendingCloudJson, _data);
            }

            _data.OwnedProductIds = ownedProductIds;
            _data.HasRemovedAds = hasRemovedAds;
            _pendingCloudJson = null;
            Save();
        }

        private static string[] MergeProductIds(string[] local, string[] cloud)
        {
            List<string> merged = new List<string>(local);
            foreach (string productId in cloud)
            {
                if (!merged.Contains(productId)) { merged.Add(productId); }
            }

            return merged.ToArray();
        }

        // PlayerPrefs dönemindeki kayıt bir kez dosyaya taşınır ki mevcut test cihazlarındaki ilerleme kaybolmasın.
        private void MigrateFromPlayerPrefs()
        {
            if (HasSave || !PlayerPrefs.HasKey(LegacyPrefsKey)) { return; }

            try
            {
                File.WriteAllText(_savePath, PlayerPrefs.GetString(LegacyPrefsKey));
            }
            catch (IOException exception)
            {
                Debug.LogError($"Legacy save could not be migrated: {exception.Message}", this);
                return;
            }

            PlayerPrefs.DeleteKey(LegacyPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
