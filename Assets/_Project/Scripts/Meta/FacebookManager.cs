using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using MatchPack.Core;
using MatchPack.Data;

public class FacebookManager : MonoBehaviour
{
   public static FacebookManager Instance;

   private GameManager _boundGameManager;

   private void Awake()
   {
      if (Instance != null && Instance != this)
      {
         Destroy(this.gameObject);
         return;
      }

      Instance = this;
      DontDestroyOnLoad(this.gameObject);
   }

   private void OnEnable()
   {
      SceneManager.sceneLoaded += HandleSceneLoaded;
   }

   private void OnDisable()
   {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      UnbindGameManager();
   }

   // FacebookManager BootScene'de, GameManager MainScene'de doğar; bağlantı sahne yüklenince kurulur.
   private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
   {
      if (Instance != this || GameManager.Instance == null || GameManager.Instance == _boundGameManager) return;

      UnbindGameManager();
      _boundGameManager = GameManager.Instance;
      _boundGameManager.OnLevelStarted += HandleLevelStarted;
      _boundGameManager.OnLevelCompleted += HandleLevelCompleted;
      _boundGameManager.OnLevelFailed += HandleLevelFailed;
   }

   private void UnbindGameManager()
   {
      if (_boundGameManager == null) return;

      _boundGameManager.OnLevelStarted -= HandleLevelStarted;
      _boundGameManager.OnLevelCompleted -= HandleLevelCompleted;
      _boundGameManager.OnLevelFailed -= HandleLevelFailed;
      _boundGameManager = null;
   }

   private void HandleLevelStarted(LevelData level)
   {
      if (level != null) LogLevelStarted(level.LevelIndex);
   }

   private void HandleLevelCompleted()
   {
      LevelData level = GameManager.Instance.CurrentLevel;
      if (level != null) LogLevelCompleted(level.LevelIndex);
   }

   private void HandleLevelFailed()
   {
      LevelData level = GameManager.Instance.CurrentLevel;
      if (level != null) LogLevelFailed(level.LevelIndex);
   }

   public async Task InitializeFacebookAsync() 
   {
      var tcs = new TaskCompletionSource<bool>();

      if (!FB.IsInitialized)
      {
         try
         {
            FB.Init(() =>
            {
               if (FB.IsInitialized)
               {
                  FB.ActivateApp();
                  Debug.Log("Facebook başarıyla başlatıldı.");
                  tcs.TrySetResult(true);
               }
               else
               {
                  Debug.LogError("Facebook başlatılamadı.");
                  tcs.TrySetResult(false); 
               }
            });
         }
         catch (System.Exception ex)
         {
            Debug.LogError("FB.Init çağrılırken hata oluştu: " + ex.Message);
            tcs.TrySetResult(false);
         }
      }
      else
      {
         FB.ActivateApp();
         tcs.TrySetResult(true);
         return; 
      }
      
      Task timeoutTask = Task.Delay(4000); 
      Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

      if (completedTask == timeoutTask)
      {
         Debug.LogError("Facebook SDK Timeout! Geri dönüş yapmadı. Süreç atlanıyor...");
         tcs.TrySetResult(false);
      }
   }

   public void LogLevelStarted(int levelnumber)
   {
      if(!FB.IsInitialized) return;
      
      var parameters = new Dictionary<string, object>();
      parameters[AppEventParameterName.Level] = levelnumber.ToString();
      FB.LogAppEvent("Level_Started", null, parameters);
   }

   public void LogLevelCompleted(int levelnumber)
   {
      if(!FB.IsInitialized) return;
      
      var parameters = new Dictionary<string, object>();
      parameters[AppEventParameterName.Level] =  levelnumber.ToString();
      FB.LogAppEvent(AppEventName.AchievedLevel, null, parameters);
   }

   public void LogLevelFailed(int levelnumber)
   {
      if(!FB.IsInitialized) return;
      var parameters = new Dictionary<string, object>();
      parameters[AppEventParameterName.Level] =  levelnumber.ToString();
      FB.LogAppEvent("Level_Failed", null, parameters);
   }

   public void LogIAPurchase(float price, string currency)
   {
      if(!FB.IsInitialized) return;
      if (string.IsNullOrEmpty(currency)) currency = "USD";
      
      var parameters = new Dictionary<string, object>();
      parameters["IAP_Completed"] = "Purchased Price_" + price + " Currency_" + currency;
      
      try
      {
         FB.LogPurchase(price, currency, parameters);
      }
      catch (System.Exception ex)
      {
         Debug.LogWarning($"Facebook LogPurchase failed: {ex.Message}");
      }
   }
}
