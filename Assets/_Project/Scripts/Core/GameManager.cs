using System;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Oyun durumunun tek sahibi. Level başlatma isteğini alır, sahne hazır olunca oyunu başlatır,
    /// kazanma ve kaybetme kararlarını yayınlar. Sahne yükleme işini SceneLoader'a devreder.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<LevelData> OnLevelStarted;
        public event Action OnLevelCompleted;
        public event Action OnLevelFailed;

        public GameState State { get; private set; } = GameState.Menu;
        public LevelData CurrentLevel { get; private set; }

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

        /// <summary>Level sahnesini yükletir; sahne hazır olduğunda durum Playing'e geçer.</summary>
        public void StartLevel(LevelData level)
        {
            if (level == null)
            {
                Debug.LogError("GameManager.StartLevel was called with a null level.", this);
                return;
            }

            if (State == GameState.Loading || SceneLoader.Instance.IsBusy) { return; }

            CurrentLevel = level;
            SetState(GameState.Loading);
            SceneLoader.Instance.LoadLevel(HandleLevelSceneReady);
        }

        /// <summary>Levelin tüm kutuları dolduğunda çağrılır.</summary>
        public void CompleteLevel()
        {
            if (State != GameState.Playing) { return; }

            SetState(GameState.Win);
            OnLevelCompleted?.Invoke();
        }

        /// <summary>Süre dolduğunda çağrılır.</summary>
        public void FailLevel()
        {
            if (State != GameState.Playing) { return; }

            SetState(GameState.Lose);
            OnLevelFailed?.Invoke();
        }

        private void HandleLevelSceneReady(LevelContext context)
        {
            SetState(GameState.Playing);
            OnLevelStarted?.Invoke(CurrentLevel);
        }

        private void SetState(GameState state)
        {
            if (State == state) { return; }

            State = state;
            OnGameStateChanged?.Invoke(state);
        }
    }
}
