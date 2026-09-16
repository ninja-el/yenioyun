using System;
using MatchPack.Core;
using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Meta
{
    /// <summary>
    /// Titreşimin tek sahibi. Hatalı hamlede cihazı titretir, tercihi PlayerData üzerinden
    /// kaydeder. Android'de süre ve şiddet ayarlanır, iOS'ta sistemin kısa titreşimi kullanılır.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        /// <summary>Titreşim tercihi değiştiğinde yayınlanır.</summary>
        public event Action<bool> OnHapticsEnabledChanged;

        public static HapticManager Instance { get; private set; }

        [Tooltip("Hatalı hamle event'inin dinlendiği çözümleyici.")]
        [SerializeField] private MatchResolver _matchResolver;

        [Tooltip("Hatalı hamlede titreşim süresi (milisaniye).")]
        [SerializeField, Range(10, 400)] private int _missDurationMilliseconds = 60;

        [Tooltip("Hatalı hamlede titreşim şiddeti. Yalnızca Android 8+ destekler.")]
        [SerializeField, Range(1, 255)] private int _missAmplitude = 160;

        [Tooltip("Aynı titreşimin üst üste tetiklenmemesi için beklenecek süre (saniye).")]
        [SerializeField, Min(0f)] private float _cooldownSeconds = 0.08f;

        private float _nextAllowedTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _vibrationEffectClass;
        private bool _hasAmplitudeControl;
#endif

        public bool IsHapticsEnabled => SaveManager.Instance.Data.IsHapticsEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDevice();
        }

        private void Start()
        {
            if (_matchResolver != null)
            {
                _matchResolver.OnItemMissed += HandleItemMissed;
            }
        }

        private void OnDestroy()
        {
            if (_matchResolver != null)
            {
                _matchResolver.OnItemMissed -= HandleItemMissed;
            }

            if (Instance == this) { Instance = null; }
        }

        /// <summary>Titreşim tercihini değiştirir ve kaydeder. Açıldığında tek bir örnek titreşim verir.</summary>
        public void SetHapticsEnabled(bool isEnabled)
        {
            SaveManager.Instance.Data.IsHapticsEnabled = isEnabled;
            SaveManager.Instance.Save();
            OnHapticsEnabledChanged?.Invoke(isEnabled);

            if (isEnabled) { Vibrate(_missDurationMilliseconds, _missAmplitude); }
        }

        /// <summary>Hatalı hamle titreşimi. Ayar kapalıysa hiçbir şey yapmaz.</summary>
        public void PlayMissFeedback()
        {
            Vibrate(_missDurationMilliseconds, _missAmplitude);
        }

        /// <summary>
        /// Cihazı verilen süre ve şiddetle titretir. Şiddet yalnızca Android 8 ve üzerinde
        /// uygulanır; diğer platformlarda sistemin varsayılan titreşimi kullanılır.
        /// </summary>
        public void Vibrate(int durationMilliseconds, int amplitude)
        {
            if (!IsHapticsEnabled || Time.unscaledTime < _nextAllowedTime) { return; }

            _nextAllowedTime = Time.unscaledTime + _cooldownSeconds;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibrator == null) { return; }

            try
            {
                if (_hasAmplitudeControl && _vibrationEffectClass != null)
                {
                    using (AndroidJavaObject effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", (long)durationMilliseconds, Mathf.Clamp(amplitude, 1, 255)))
                    {
                        _vibrator.Call("vibrate", effect);
                    }

                    return;
                }

                _vibrator.Call("vibrate", (long)durationMilliseconds);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Haptic feedback failed: {exception.Message}", this);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void InitializeDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _hasAmplitudeControl = version.GetStatic<int>("SDK_INT") >= 26;
                }

                if (_hasAmplitudeControl)
                {
                    _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Vibrator service is unavailable: {exception.Message}", this);
                _vibrator = null;
            }
#endif
        }

        private void HandleItemMissed(StackItem item)
        {
            PlayMissFeedback();
        }
    }
}
