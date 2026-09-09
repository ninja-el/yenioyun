using System;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// PlayerData'nın tek sahibi. Veri, tek bir JSON string olarak PlayerPrefs'e yazılır.
    /// Cloud Save'e geçildiğinde yalnızca bu sınıfın içi değişir.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveKey = "MatchPack.PlayerData";

        [Tooltip("Aktif oyuncu verisi. Play Mode'da buradan değiştirilen değerler kayda yansır.")]
        [SerializeField] private PlayerData _data = new PlayerData();

        public PlayerData Data => _data;

        /// <summary>Cihazda daha önce yazılmış bir kayıt var mı? İlk açılış varsayılanlarını kurmak için kullanılır.</summary>
        public bool HasSave => PlayerPrefs.HasKey(SaveKey);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
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

        /// <summary>Kaydı okur. Kayıt yoksa veya bozuksa veri varsayılan değerlerinde kalır.</summary>
        public void Load()
        {
            if (!HasSave) { return; }

            try
            {
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SaveKey), _data);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Save data is corrupt, falling back to defaults: {exception.Message}", this);
            }
        }

        /// <summary>Aktif veriyi diske yazar.</summary>
        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        /// <summary>Kaydı siler ve veriyi varsayılanlara döndürür.</summary>
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            // Data referansını tutan sistemler bozulmasın diye yeni nesne atanmaz, alanların üzerine yazılır.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new PlayerData()), _data);
        }
    }
}
