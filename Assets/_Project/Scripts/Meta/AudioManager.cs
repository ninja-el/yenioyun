using System;
using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>
    /// Sesin tek sahibi. Gameplay ve level event'lerine abone olur, SFX ve müziği çalar,
    /// açık/kapalı tercihini PlayerData üzerinden kaydeder. Ses çalma kararı başka sınıflara dağılmaz.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        /// <summary>Ses efekti tercihi değiştiğinde yayınlanır.</summary>
        public event Action<bool> OnSoundEnabledChanged;

        /// <summary>Müzik tercihi değiştiğinde yayınlanır.</summary>
        public event Action<bool> OnMusicEnabledChanged;

        [Tooltip("Klip referanslarının okunduğu kütüphane.")]
        [SerializeField] private AudioLibrary _library;

        [Tooltip("Tek seferlik efektlerin çalındığı kaynak. PlayOneShot ile sesler üst üste biner.")]
        [SerializeField] private AudioSource _sfxSource;

        [Tooltip("Döngüye alınmış müziğin çalındığı kaynak.")]
        [SerializeField] private AudioSource _musicSource;

        [Tooltip("Hatalı hamle event'inin dinlendiği çözümleyici.")]
        [SerializeField] private MatchResolver _matchResolver;

        [Tooltip("Kutu event'lerinin dinlendiği bant.")]
        [SerializeField] private Conveyor _conveyor;

        [Tooltip("Efektlerin açıkken çalacağı ses düzeyi.")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        [Tooltip("Müziğin açıkken çalacağı ses düzeyi.")]
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.4f;

        public bool IsSoundEnabled => SaveManager.Instance.Data.IsSoundEnabled;
        public bool IsMusicEnabled => SaveManager.Instance.Data.IsMusicEnabled;

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

        private void Start()
        {
            ApplyVolumes();

            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnLevelCompleted += HandleLevelCompleted;
            GameManager.Instance.OnLevelFailed += HandleLevelFailed;

            if (_matchResolver != null)
            {
                _matchResolver.OnItemMatched += HandleItemMatched;
                _matchResolver.OnItemMissed += HandleItemMissed;
            }

            if (_conveyor != null)
            {
                _conveyor.OnBoxFilled += HandleBoxFilled;
            }

            PlayMusic(_library != null ? _library.MenuMusic : null);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
                GameManager.Instance.OnLevelFailed -= HandleLevelFailed;
            }

            if (_matchResolver != null)
            {
                _matchResolver.OnItemMatched -= HandleItemMatched;
                _matchResolver.OnItemMissed -= HandleItemMissed;
            }

            if (_conveyor != null)
            {
                _conveyor.OnBoxFilled -= HandleBoxFilled;
            }

            if (Instance == this) { Instance = null; }
        }

        /// <summary>Ses efekti tercihini değiştirir ve kaydeder.</summary>
        public void SetSoundEnabled(bool isEnabled)
        {
            SaveManager.Instance.Data.IsSoundEnabled = isEnabled;
            SaveManager.Instance.Save();
            ApplyVolumes();
            OnSoundEnabledChanged?.Invoke(isEnabled);
        }

        /// <summary>Müzik tercihini değiştirir ve kaydeder.</summary>
        public void SetMusicEnabled(bool isEnabled)
        {
            SaveManager.Instance.Data.IsMusicEnabled = isEnabled;
            SaveManager.Instance.Save();
            ApplyVolumes();
            OnMusicEnabledChanged?.Invoke(isEnabled);
        }

        /// <summary>Verilen klibi tek seferlik çalar. Klip yoksa sessizce geçer.</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || _sfxSource == null || !IsSoundEnabled) { return; }

            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        /// <summary>Buton tıklama sesi. Tüm UI butonları bunu çağırır.</summary>
        public void PlayButtonClick()
        {
            if (_library == null) { return; }

            PlaySfx(_library.ButtonClick);
        }

        /// <summary>Satın alma tamamlandığında çalar.</summary>
        public void PlayPurchaseSucceeded()
        {
            if (_library == null) { return; }

            PlaySfx(_library.PurchaseSucceeded);
        }

        /// <summary>Gold veya can ödülü verildiğinde çalar.</summary>
        public void PlayRewardGranted()
        {
            if (_library == null) { return; }

            PlaySfx(_library.RewardGranted);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (_musicSource == null || clip == null || _musicSource.clip == clip) { return; }

            _musicSource.clip = clip;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        private void ApplyVolumes()
        {
            if (_sfxSource != null) { _sfxSource.volume = IsSoundEnabled ? _sfxVolume : 0f; }
            if (_musicSource != null) { _musicSource.volume = IsMusicEnabled ? _musicVolume : 0f; }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (_library == null) { return; }

            if (state == GameState.Playing) { PlayMusic(_library.GameMusic); }
            if (state == GameState.Menu) { PlayMusic(_library.MenuMusic); }
        }

        private void HandleItemMatched(StackItem item)
        {
            PlaySfx(_library != null ? _library.ItemMatched : null);
        }

        private void HandleItemMissed(StackItem item)
        {
            PlaySfx(_library != null ? _library.ItemMissed : null);
        }

        private void HandleBoxFilled(Box box)
        {
            PlaySfx(_library != null ? _library.BoxFilled : null);
        }

        private void HandleLevelCompleted()
        {
            PlaySfx(_library != null ? _library.LevelCompleted : null);
        }

        private void HandleLevelFailed()
        {
            PlaySfx(_library != null ? _library.LevelFailed : null);
        }
    }
}
