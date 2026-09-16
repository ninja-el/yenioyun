using MatchPack.Core;
using MatchPack.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Ayarlar paneli. Üç anahtarı (ses, müzik, titreşim) ilgili manager'lara bağlar ve panelin
    /// kapatma butonlarını işletir. Ayarın nerede saklandığını manager'lar bilir.
    /// Panel kapalıyken de butonların bağlı kalması için bileşen UI_Canvas üzerinde durur.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Tooltip("Paneli açan ayar butonu.")]
        [SerializeField] private Button _openButton;

        [Tooltip("Sağ üstteki kapatma butonu.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Devam (Continue) butonu; paneli kapatır.")]
        [SerializeField] private Button _continueButton;

        [Tooltip("Çıkış (Log Out) butonu; level açıksa menüye döner.")]
        [SerializeField] private Button _exitButton;

        [Tooltip("Ses efekti anahtarı.")]
        [SerializeField] private ToggleSwitch _soundSwitch;

        [Tooltip("Müzik anahtarı.")]
        [SerializeField] private ToggleSwitch _musicSwitch;

        [Tooltip("Titreşim (taptic) anahtarı.")]
        [SerializeField] private ToggleSwitch _hapticSwitch;

        private void Awake()
        {
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
            _continueButton.onClick.AddListener(Close);
            _exitButton.onClick.AddListener(ExitToMenu);

            _soundSwitch.OnValueChanged += HandleSoundChanged;
            _musicSwitch.OnValueChanged += HandleMusicChanged;
            _hapticSwitch.OnValueChanged += HandleHapticChanged;
        }

        private void Start()
        {
            RefreshSwitches();
        }

        private void OnDestroy()
        {
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
            _continueButton.onClick.RemoveListener(Close);
            _exitButton.onClick.RemoveListener(ExitToMenu);

            _soundSwitch.OnValueChanged -= HandleSoundChanged;
            _musicSwitch.OnValueChanged -= HandleMusicChanged;
            _hapticSwitch.OnValueChanged -= HandleHapticChanged;
        }

        /// <summary>Ayarlar panelini açar ve anahtarları kayıttaki değerlerle eşitler.</summary>
        public void Open()
        {
            RefreshSwitches();
            UIManager.Instance.ShowSettings();
        }

        /// <summary>Ayarlar panelini kapatır.</summary>
        public void Close()
        {
            UIManager.Instance.HideSettings();
        }

        /// <summary>Paneli kapatır; bir level açıksa menüye döner.</summary>
        public void ExitToMenu()
        {
            UIManager.Instance.HideSettings();

            if (GameManager.Instance.State == GameState.Menu) { return; }

            GameManager.Instance.ReturnToMenu();
        }

        /// <summary>Anahtarların görünümünü kayıttaki değerlerle eşitler; event yayınlamaz.</summary>
        public void RefreshSwitches()
        {
            if (AudioManager.Instance != null)
            {
                _soundSwitch.SetValueWithoutNotify(AudioManager.Instance.IsSoundEnabled);
                _musicSwitch.SetValueWithoutNotify(AudioManager.Instance.IsMusicEnabled);
            }

            if (HapticManager.Instance != null)
            {
                _hapticSwitch.SetValueWithoutNotify(HapticManager.Instance.IsHapticsEnabled);
            }
        }

        private void HandleSoundChanged(bool isOn)
        {
            AudioManager.Instance.SetSoundEnabled(isOn);
        }

        private void HandleMusicChanged(bool isOn)
        {
            AudioManager.Instance.SetMusicEnabled(isOn);
        }

        private void HandleHapticChanged(bool isOn)
        {
            HapticManager.Instance.SetHapticsEnabled(isOn);
        }
    }
}
