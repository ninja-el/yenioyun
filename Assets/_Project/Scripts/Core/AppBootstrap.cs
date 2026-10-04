using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchPack.Core
{
    /// <summary>
    /// Boot sahnesinin tek görevi: açılış ayarlarını uygulamak, gizlilik onayını beklemek, giriş yapıp cloud kaydını okumak ve
    /// ardından Facebook SDK'sını başlatıp MainScene'i yüklemek. Onay en fazla `_consentTimeout` kadar beklenir; süre
    /// dolarsa oyun onay beklemeden açılır.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Tooltip("Hedef kare hızı. Mobilde 60 FPS hedefleniyor.")]
        [SerializeField] private int _targetFrameRate = 60;

        [Tooltip("Gizlilik onayını (GDPR + iOS ATT) yöneten bileşen. Onay süreci bitmeden oyun açılmaz.")]
        [SerializeField] private PrivacyManager _privacyManager;

        [Tooltip("Gizlilik onayı (GDPR paneli + iOS ATT) için açılışta beklenecek en uzun süre (saniye). Dolarsa oyun onay beklemeden açılır.")]
        [SerializeField] private float _consentTimeout = 15f;

        [Tooltip("Unity Services girişini yapan bileşen. Atanmazsa cloud kaydı okunmaz, oyun yerel kayıtla açılır.")]
        [SerializeField] private AutoLoginManager _autoLoginManager;

        [Tooltip("Giriş ve cloud kaydı için açılışta beklenecek en uzun süre (saniye). Dolarsa oyun yerel kayıtla açılır.")]
        [SerializeField] private float _cloudLoadTimeout = 3f;

        private IEnumerator Start()
        {
            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (_privacyManager == null)
            {
                Debug.LogError("AppBootstrap has no PrivacyManager assigned; the game will not start.", this);
                yield break;
            }

            var consentCancellation = new CancellationTokenSource();
            Task consentTask = _privacyManager.RequestConsentAsync(consentCancellation.Token);
            float consentDeadline = Time.realtimeSinceStartup + _consentTimeout;
            yield return new WaitUntil(() => consentTask.IsCompleted || Time.realtimeSinceStartup >= consentDeadline);

            if (!consentTask.IsCompleted)
            {
                Debug.LogWarning($"AppBootstrap: consent was not answered within {_consentTimeout} s; starting the game without it.", this);
                consentCancellation.Cancel();
            }
            else if (consentTask.IsFaulted) { Debug.LogException(consentTask.Exception, this); }

            // Beklenmez: Facebook init 4 sn'ye kadar sürebilir, oyunun açılışını geciktirmemeli.
            if (FacebookManager.Instance != null) { _ = FacebookManager.Instance.InitializeFacebookAsync(); }

            if (_autoLoginManager != null)
            {
                Task cloudTask = SignInAndFetchCloudSaveAsync();
                float deadline = Time.realtimeSinceStartup + _cloudLoadTimeout;
                yield return new WaitUntil(() => cloudTask.IsCompleted || Time.realtimeSinceStartup >= deadline);
            }
            else
            {
                Debug.LogWarning("AppBootstrap has no AutoLoginManager assigned; cloud save is skipped.", this);
            }

            yield return SceneManager.LoadSceneAsync(SceneIndices.Main, LoadSceneMode.Single);
        }

        private async Task SignInAndFetchCloudSaveAsync()
        {
            await _autoLoginManager.InitializeAndSignInAsync();
            await SaveManager.FetchCloudSaveAsync();
        }
    }
}
