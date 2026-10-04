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
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveFileName = "playerdata.json";
        private const string LegacyPrefsKey = "MatchPack.PlayerData";
        private const string LegacyCloudKey = "playerdata";
        private const char FieldPrefix = '_';
        private const int UnknownCloudLevel = -1;

        // Boot'ta okunan cloud kaydı, MainScene'deki SaveManager ayağa kalkana kadar burada bekler.
        private static string _pendingCloudJson;
        private static int _knownCloudLevel = UnknownCloudLevel;
        private static bool _hasLegacyCloudKey;

        [Tooltip("Aktif oyuncu verisi. Play Mode'da buradan değiştirilen değerler kayda yansır.")]
        [SerializeField] private PlayerData _data = new PlayerData();

        private string _savePath;
        private string _tempPath;
        private bool _isUploading;
        private bool _hasQueuedUpload;

        public PlayerData Data => _data;

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
            _tempPath = _savePath + ".tmp";

            MigrateFromPlayerPrefs();
            Load();
            ApplyPendingCloudSave();
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) { Save(); }
        }

        private void OnApplicationQuit()
        {
            Save();
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
                _hasLegacyCloudKey = items.ContainsKey(LegacyCloudKey);

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
            // Yazma, silme ile taşıma arasında kesilirse elde yalnızca .tmp kalır; o da geçerli tam kayıttır.
            string path = File.Exists(_savePath) ? _savePath : _tempPath;
            if (!File.Exists(path)) { return; }

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), _data);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                Debug.LogError($"Save data could not be read, falling back to defaults: {exception.Message}", this);
            }
        }

        /// <summary>Aktif veriyi diske yazar.</summary>
        public void Save()
        {
            try
            {
                // Önce geçici dosyaya yazılır ki yazma yarıda kesilirse eski kayıt bozulmasın.
                File.WriteAllText(_tempPath, JsonUtility.ToJson(_data));
                if (File.Exists(_savePath)) { File.Delete(_savePath); }
                File.Move(_tempPath, _savePath);
            }
            catch (IOException exception)
            {
                Debug.LogError($"Save data could not be written: {exception.Message}", this);
            }
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

                    if (_hasLegacyCloudKey)
                    {
                        await CloudSaveService.Instance.Data.Player.DeleteAsync(
                            LegacyCloudKey, new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
                        _hasLegacyCloudKey = false;
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
                fields[field.Name.TrimStart(FieldPrefix)] = field.Value;
            }

            return fields;
        }

        // Alan anahtarları yoksa eski tek anahtarlı kayıt okunur.
        private static string ToPlayerDataJson(Dictionary<string, Item> items)
        {
            JObject fields = new JObject();
            foreach (Item item in items.Values)
            {
                if (item.Key == LegacyCloudKey) { continue; }

                fields[FieldPrefix + item.Key] = item.Value.GetAs<JToken>();
            }

            if (fields.Count > 0) { return fields.ToString(); }

            return items.TryGetValue(LegacyCloudKey, out Item legacy) ? legacy.Value.GetAsString() : null;
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
