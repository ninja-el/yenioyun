using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchPack.Core
{
    /// <summary>
    /// Sahne yükleme ve boşaltmanın tek sahibi. MainScene hiç kapatılmaz; level sahnesi
    /// additive yüklenir. Level geçişinde sıra her zaman "önce yeni sahneyi yükle, sonra eskisini
    /// boşalt"tır, bu yüzden geçiş anında iki level sahnesi birden yüklü olabilir.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        /// <summary>Geçiş başladığında true, bittiğinde false yayınlar. Input bu event ile kapatılır.</summary>
        public event Action<bool> OnSceneTransitionChanged;

        /// <summary>Bir level sahnesi boşaltılmadan hemen önce; havuz iadesi ve tween temizliği burada yapılır.</summary>
        public event Action OnBeforeLevelUnload;

        public event Action<LevelContext> OnLevelSceneReady;
        public event Action OnLevelSceneUnloaded;
        public event Action<float> OnLoadProgressChanged;

        public bool IsBusy { get; private set; }
        public LevelContext ActiveLevel { get; private set; }
        public bool HasActiveLevel => _activeLevelScene.IsValid() && _activeLevelScene.isLoaded;

        private Scene _activeLevelScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        /// <summary>Game sahnesini additive yükler; yüklü bir level varsa yeni sahne hazır olduktan sonra onu boşaltır.</summary>
        public void LoadLevel(Action<LevelContext> onReady = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy, level load request ignored.");
                return;
            }

            StartCoroutine(LoadLevelRoutine(onReady));
        }

        /// <summary>Aktif game sahnesini boşaltıp aktif sahneyi MainScene'e döndürür.</summary>
        public void UnloadLevel(Action onComplete = null)
        {
            if (IsBusy)
            {
                Debug.LogWarning("SceneLoader is busy, level unload request ignored.");
                return;
            }

            if (!HasActiveLevel)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(UnloadLevelRoutine(onComplete));
        }

        private IEnumerator LoadLevelRoutine(Action<LevelContext> onReady)
        {
            SetTransitionState(true);

            Scene previousScene = _activeLevelScene;
            LevelContext previousContext = ActiveLevel;
            bool hasPrevious = HasActiveLevel;

            Scene loadedScene = default;
            void CaptureLoadedScene(Scene scene, LoadSceneMode mode) => loadedScene = scene;

            SceneManager.sceneLoaded += CaptureLoadedScene;
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneIndices.Game, LoadSceneMode.Additive);

            while (!load.isDone)
            {
                OnLoadProgressChanged?.Invoke(load.progress);
                yield return null;
            }

            SceneManager.sceneLoaded -= CaptureLoadedScene;
            OnLoadProgressChanged?.Invoke(1f);

            if (hasPrevious)
            {
                yield return UnloadSceneRoutine(previousScene, previousContext);
            }

            _activeLevelScene = loadedScene;
            ActiveLevel = FindLevelContext(loadedScene);

            if (ActiveLevel == null)
            {
                Debug.LogError($"LevelContext not found in scene '{loadedScene.name}'.");
            }
            else
            {
                ActiveLevel.Activate();
            }

            SceneManager.SetActiveScene(loadedScene);

            SetTransitionState(false);
            OnLevelSceneReady?.Invoke(ActiveLevel);
            onReady?.Invoke(ActiveLevel);
        }

        private IEnumerator UnloadLevelRoutine(Action onComplete)
        {
            SetTransitionState(true);

            yield return UnloadSceneRoutine(_activeLevelScene, ActiveLevel);

            _activeLevelScene = default;
            ActiveLevel = null;

            Scene menuScene = SceneManager.GetSceneByBuildIndex(SceneIndices.Main);
            if (menuScene.IsValid() && menuScene.isLoaded)
            {
                SceneManager.SetActiveScene(menuScene);
            }

            SetTransitionState(false);
            OnLevelSceneUnloaded?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator UnloadSceneRoutine(Scene scene, LevelContext context)
        {
            OnBeforeLevelUnload?.Invoke();

            if (context != null) { context.Teardown(); }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }

        private static LevelContext FindLevelContext(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                LevelContext context = root.GetComponentInChildren<LevelContext>(true);
                if (context != null) { return context; }
            }

            return null;
        }

        private void SetTransitionState(bool isBusy)
        {
            IsBusy = isBusy;
            OnSceneTransitionChanged?.Invoke(isBusy);
        }
    }
}
