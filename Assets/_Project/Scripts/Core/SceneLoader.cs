using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatchPack.Core
{
    /// <summary>
    /// GameScene'in tek sahibi. Sahne açılışta bir kez additive yüklenir ve bir daha boşaltılmaz;
    /// oyunun tek kamerası orada durduğu için menüde de yüklü kalması gerekir. Level değişimi
    /// sahne değişimi değildir: aktif level söküldükten sonra aynı sahne yeni LevelData ile kurulur.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        /// <summary>Geçiş başladığında true, bittiğinde false yayınlar. Input bu event ile kapatılır.</summary>
        public event Action<bool> OnSceneTransitionChanged;

        /// <summary>Aktif level sökülmeden hemen önce; havuz iadesi ve tween temizliği burada yapılır.</summary>
        public event Action OnBeforeLevelTeardown;

        /// <summary>Sahne hazır ve level kurulabilir durumda. LevelData'yı uygulama işi dinleyicilerin.</summary>
        public event Action<LevelContext> OnLevelSceneReady;

        /// <summary>Aktif level söküldükten sonra yayınlanır.</summary>
        public event Action OnLevelTornDown;

        public bool IsBusy { get; private set; }

        /// <summary>GameScene'deki referans noktası. Sahne yüklenene kadar null'dır.</summary>
        public LevelContext ActiveLevel { get; private set; }

        /// <summary>Şu an kurulmuş bir level var mı?</summary>
        public bool HasActiveLevel { get; private set; }

        private Scene _gameScene;

        private bool IsGameSceneLoaded => _gameScene.IsValid() && _gameScene.isLoaded;

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

        private IEnumerator Start()
        {
            // Kamera GameScene'de durduğu için sahne menüden önce yüklenmeli; aksi halde menü
            // hiçbir kamera tarafından render edilmez.
            SetTransitionState(true);

            yield return EnsureGameSceneRoutine();

            if (ActiveLevel != null) { ActiveLevel.SetContentActive(false); }

            SetTransitionState(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }

        /// <summary>
        /// Aktif leveli söküp sahneyi yeni level için hazırlar ve <see cref="OnLevelSceneReady"/>
        /// yayınlar. Sahne yüklü değilse önce yüklenir.
        /// </summary>
        public IEnumerator BuildLevelRoutine()
        {
            SetTransitionState(true);

            TeardownLevelContent();

            yield return EnsureGameSceneRoutine();

            if (ActiveLevel == null)
            {
                Debug.LogError("GameScene has no LevelContext; the level cannot be built.", this);
                SetTransitionState(false);
                yield break;
            }

            ActiveLevel.SetContentActive(true);
            HasActiveLevel = true;

            SetTransitionState(false);
            OnLevelSceneReady?.Invoke(ActiveLevel);
        }

        /// <summary>Aktif leveli söker ve 3B içeriği gizler. Sahne yüklü kalır.</summary>
        public void TeardownLevel()
        {
            if (!HasActiveLevel) { return; }

            TeardownLevelContent();
            OnLevelTornDown?.Invoke();
        }

        private void TeardownLevelContent()
        {
            if (!HasActiveLevel) { return; }

            OnBeforeLevelTeardown?.Invoke();

            if (ActiveLevel != null) { ActiveLevel.SetContentActive(false); }

            HasActiveLevel = false;
        }

        private IEnumerator EnsureGameSceneRoutine()
        {
            if (IsGameSceneLoaded) { yield break; }

            Scene loadedScene = default;
            void CaptureLoadedScene(Scene scene, LoadSceneMode mode) => loadedScene = scene;

            SceneManager.sceneLoaded += CaptureLoadedScene;
            yield return SceneManager.LoadSceneAsync(SceneIndices.Game, LoadSceneMode.Additive);
            SceneManager.sceneLoaded -= CaptureLoadedScene;

            _gameScene = loadedScene;
            ActiveLevel = FindLevelContext(loadedScene);

            if (ActiveLevel == null)
            {
                Debug.LogError($"LevelContext not found in scene '{loadedScene.name}'.", this);
            }

            SceneManager.SetActiveScene(loadedScene);
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
