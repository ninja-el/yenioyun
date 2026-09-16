using System;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// İki durumlu ayar anahtarı. Anahtarın tamamı tek bir butondur; nereye basılırsa basılsın
    /// durum tersine döner. Açıkken yeşil "Open", kapalıyken kırmızı "Close" görseli görünür.
    /// Anahtar değerin nerede saklandığını bilmez, yalnızca event yayınlar.
    /// </summary>
    public class ToggleSwitch : MonoBehaviour
    {
        /// <summary>Değer kullanıcı tarafından değiştirildiğinde yayınlanır.</summary>
        public event Action<bool> OnValueChanged;

        [Tooltip("Anahtarın tamamını kaplayan buton. İki tarafa da basılınca durum değişir.")]
        [SerializeField] private Button _toggleButton;

        [Tooltip("Ayar açıkken görünen görsel (Opn).")]
        [SerializeField] private GameObject _onVisual;

        [Tooltip("Ayar kapalıyken görünen görsel (Cls).")]
        [SerializeField] private GameObject _offVisual;

        public bool IsOn { get; private set; } = true;

        private void Awake()
        {
            _toggleButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy()
        {
            _toggleButton.onClick.RemoveListener(Toggle);
        }

        /// <summary>Durumu tersine çevirir ve event yayınlar.</summary>
        public void Toggle()
        {
            SetValueWithoutNotify(!IsOn);
            OnValueChanged?.Invoke(IsOn);
        }

        /// <summary>Görünümü verilen değere getirir, event yayınlamaz. Kayıttan okurken kullanılır.</summary>
        public void SetValueWithoutNotify(bool isOn)
        {
            IsOn = isOn;
            _onVisual.SetActive(isOn);
            _offVisual.SetActive(!isOn);
        }
    }
}
