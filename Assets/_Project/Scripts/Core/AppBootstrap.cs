using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchPack.Core
{
    /// <summary>Boot sahnesinin tek görevi: açılış ayarlarını uygulayıp MainScene'i yüklemek.</summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Tooltip("Hedef kare hızı. Mobilde 60 FPS hedefleniyor.")]
        [SerializeField] private int _targetFrameRate = 60;

        private IEnumerator Start()
        {
            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            yield return SceneManager.LoadSceneAsync(SceneIndices.Main, LoadSceneMode.Single);
        }
    }
}
