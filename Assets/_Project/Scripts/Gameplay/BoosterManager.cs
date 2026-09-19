using System;
using System.Collections;
using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Meta;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Booster kullanımının tek kapısı. Kilit ve stok kontrolünü yapar, etkiyi ilgili
    /// <see cref="BoosterBehaviour"/>'a uygulatır ve ancak etki uygulandıysa envanterden düşer.
    /// Etkinin kendisini bu sınıf bilmez.
    /// </summary>
    public class BoosterManager : MonoBehaviour
    {
        public static BoosterManager Instance { get; private set; }

        /// <summary>Bir booster kullanıldığında yayınlanır.</summary>
        public event Action<BoosterType> OnBoosterUsed;

        [Tooltip("Booster tanımlarının okunduğu katalog.")]
        [SerializeField] private BoosterCatalog _catalog;

        [Tooltip("Etkiyi uygulayan bileşenler. Her booster tipi için bir tane bağlanır.")]
        [SerializeField] private BoosterBehaviour[] _boosters;

        [Tooltip("Booster efektlerinin oynatılacağı nokta. Boş bırakılırsa efekt oynatılmaz.")]
        [SerializeField] private Transform _effectAnchor;

        [Tooltip("Satın alma paneli açıkken durdurulacak sayaç.")]
        [SerializeField] private LevelTimer _timer;

        [Tooltip("Satın alma paneli açıkken durdurulacak bant.")]
        [SerializeField] private Conveyor _conveyor;

        /// <summary>Booster satın alma paneli gibi bir sebeple oyun yerinde durduruldu mu?</summary>
        public bool IsGameplayPaused { get; private set; }

        /// <summary>Level sayacı işliyor mu? Süreye dokunan boosterlar bunu sorar.</summary>
        public bool IsTimerRunning => _timer != null && _timer.IsRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_catalog == null)
            {
                Debug.LogError("BoosterManager has no BoosterCatalog assigned; no booster can be used.", this);
            }
        }

        private void Start()
        {
            SceneLoader.Instance.OnBeforeLevelTeardown += HandleBeforeLevelTeardown;
        }

        private void OnDestroy()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnBeforeLevelTeardown -= HandleBeforeLevelTeardown;
            }

            if (Instance == this) { Instance = null; }
        }

        /// <summary>Envanterdeki booster adedi.</summary>
        public int GetCount(BoosterType type)
        {
            return EconomyManager.Instance != null ? EconomyManager.Instance.GetBoosterCount((int)type) : 0;
        }

        /// <summary>Booster'ın tanımı; katalogda yoksa null.</summary>
        public BoosterData GetData(BoosterType type)
        {
            return _catalog != null ? _catalog.Find(type) : null;
        }

        /// <summary>Oyuncunun bölümü booster'ın açılış bölümüne ulaşmış mı?</summary>
        public bool IsUnlocked(BoosterType type)
        {
            BoosterData data = GetData(type);
            if (data == null) { return false; }

            return SaveManager.Instance.Data.CurrentLevel >= data.UnlockLevel;
        }

        /// <summary>Booster şu an kullanılabilir mi? Oyun oynanmıyorsa, kilitliyse veya stok yoksa false.</summary>
        public bool CanUse(BoosterType type)
        {
            if (GameManager.Instance.State != GameState.Playing) { return false; }
            if (!IsUnlocked(type) || GetCount(type) <= 0) { return false; }

            return FindBooster(type) != null;
        }

        /// <summary>
        /// Booster'ı kullanır. Etki uygulanamazsa envanterden düşülmez ve false döner.
        /// </summary>
        public bool TryUse(BoosterType type)
        {
            if (!CanUse(type)) { return false; }
            if (!FindBooster(type).TryActivate(GetData(type))) { return false; }
            if (!EconomyManager.Instance.TryUseBooster((int)type)) { return false; }

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayButtonClick(); }

            OnBoosterUsed?.Invoke(type);
            return true;
        }

        /// <summary>
        /// Oyunu yerinde durdurur veya devam ettirir: sayaç, bant ve dokunuş kapanır. Booster
        /// satın alma paneli açıkken oyunun arkada işlememesi için kullanılır.
        /// </summary>
        public void SetGameplayPaused(bool isPaused)
        {
            IsGameplayPaused = isPaused;

            ApplyHolds();
            if (InputManager.Instance != null) { InputManager.Instance.SetInputEnabled(!isPaused); }
        }

        /// <summary>
        /// Sayacın ve bandın durup durmayacağını yeniden hesaplar. Kısıt iki kaynaktan gelebilir:
        /// satın alma paneli oyunu durdurmuş olabilir veya Time Freeze sürüyor olabilir. Biri
        /// kalktığında diğerinin kısıtı bozulmasın diye ikisi de tek noktadan uygulanır.
        /// </summary>
        public void ApplyHolds()
        {
            FreezeBooster freeze = FindBooster(BoosterType.Freeze) as FreezeBooster;

            bool holdTimer = IsGameplayPaused || (freeze != null && freeze.IsFrozen);
            bool holdBelt = IsGameplayPaused || (freeze != null && freeze.IsBeltFrozen);

            if (_timer != null) { _timer.SetPaused(holdTimer); }
            if (_conveyor != null) { _conveyor.SetSpeedScale(holdBelt ? 0f : 1f); }
        }

        /// <summary>Time Freeze booster'ını kullanır. Butona bu method bağlanır.</summary>
        public void UseFreeze() { TryUse(BoosterType.Freeze); }

        /// <summary>Shuffle booster'ını kullanır. Butona bu method bağlanır.</summary>
        public void UseShuffle() { TryUse(BoosterType.Shuffle); }

        /// <summary>Auto-Match booster'ını kullanır. Butona bu method bağlanır.</summary>
        public void UseAutoMatch() { TryUse(BoosterType.AutoMatch); }

        /// <summary>Joker Box booster'ını kullanır. Butona bu method bağlanır.</summary>
        public void UseJokerBox() { TryUse(BoosterType.JokerBox); }

        /// <summary>
        /// Efekti ankraj noktasında oynatır ve süresi dolunca havuza iade eder. Efekt veya ankraj
        /// bağlı değilse hiçbir şey yapmaz.
        /// </summary>
        public void PlayEffect(GameObject prefab, float duration)
        {
            if (prefab == null || _effectAnchor == null || duration <= 0f) { return; }

            GameObject instance = PoolManager.Instance.Get(prefab);
            if (instance == null) { return; }

            instance.transform.SetPositionAndRotation(_effectAnchor.position, _effectAnchor.rotation);
            StartCoroutine(ReleaseEffectRoutine(instance, duration));
        }

        private IEnumerator ReleaseEffectRoutine(GameObject instance, float duration)
        {
            yield return new WaitForSeconds(duration);

            PoolManager.Instance.Release(instance);
        }

        private void HandleBeforeLevelTeardown()
        {
            StopAllCoroutines();
            IsGameplayPaused = false;

            for (int i = 0; i < _boosters.Length; i++)
            {
                if (_boosters[i] != null) { _boosters[i].Cancel(); }
            }
        }

        private BoosterBehaviour FindBooster(BoosterType type)
        {
            if (_boosters == null) { return null; }

            for (int i = 0; i < _boosters.Length; i++)
            {
                if (_boosters[i] != null && _boosters[i].Type == type) { return _boosters[i]; }
            }

            return null;
        }
    }
}
