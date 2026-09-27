using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchPack.Core
{
    /// <summary>
    /// Boot sahnesinin tek görevi: açılış ayarlarını uygulamak, gizlilik onayını beklemek ve
    /// ardından MainScene'i yüklemek. Onay süreci bitmeden MainScene yüklenmez.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Tooltip("Hedef kare hızı. Mobilde 60 FPS hedefleniyor.")]
        [SerializeField] private int _targetFrameRate = 60;

        [Tooltip("Gizlilik onayını (GDPR + iOS ATT) yöneten bileşen. Onay süreci bitmeden oyun açılmaz.")]
        [SerializeField] private PrivacyManager _privacyManager;

        private IEnumerator Start()
        {
            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (_privacyManager == null)
            {
                Debug.LogError("AppBootstrap has no PrivacyManager assigned; the game will not start.", this);
                yield break;
            }

            Task consentTask = _privacyManager.RequestConsentAsync();
            yield return new WaitUntil(() => consentTask.IsCompleted);

            if (consentTask.IsFaulted) { Debug.LogException(consentTask.Exception, this); }

            yield return SceneManager.LoadSceneAsync(SceneIndices.Main, LoadSceneMode.Single);
        }
    }
}
