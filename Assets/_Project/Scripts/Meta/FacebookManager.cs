using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Facebook.Unity;

public class FacebookManager : MonoBehaviour
{
   public static FacebookManager Instance;
   
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
      parameters["level_name"] = "Level_" + levelnumber;
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
