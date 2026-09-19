using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using MatchPack.Localization;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Stoğu biten bir booster'a basıldığında açılan satın alma paneli. Panel hangi booster için
    /// açıldıysa onun ikonunu, metinlerini, paket adedini ve fiyatını gösterir; satın alma biter
    /// bitmez kapanır ve booster kullanılır. Panel açıkken oyun yerinde durur.
    /// </summary>
    public class BoosterPurchasePanel : MonoBehaviour
    {
        [Tooltip("Booster adının yazıldığı localization bileşeni.")]
        [SerializeField] private LocalizedText _titleText;

        [Tooltip("Booster açıklamasının yazıldığı localization bileşeni.")]
        [SerializeField] private LocalizedText _infoText;

        [Tooltip("Booster ikonunun gösterildiği Image.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Paket adedinin yazıldığı alan.")]
        [SerializeField] private TMP_Text _amountText;

        [Tooltip("Gold fiyatının yazıldığı alan.")]
        [SerializeField] private TMP_Text _priceText;

        [Tooltip("Satın almayı başlatan buton.")]
        [SerializeField] private Button _buyButton;

        [Tooltip("Paket adedinin yazım biçimi. {0} adet ile değişir.")]
        [SerializeField] private string _amountFormat = "x{0}";

        private BoosterType _type;
        private bool _hasTarget;

        private void Awake()
        {
            _buyButton.onClick.AddListener(Buy);
        }

        private void OnEnable()
        {
            if (BoosterManager.Instance != null) { BoosterManager.Instance.SetGameplayPaused(true); }
        }

        private void OnDisable()
        {
            if (BoosterManager.Instance != null) { BoosterManager.Instance.SetGameplayPaused(false); }
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(Buy);
        }

        /// <summary>Paneli verilen booster'ın bilgileriyle doldurup açar.</summary>
        public void Open(BoosterType type)
        {
            _type = type;
            _hasTarget = true;

            Refresh();
            UIManager.Instance.ShowBoosterPanel();
        }

        /// <summary>
        /// Paneldeki booster'ı gold ile satın alır ve hemen kullanır. Gold yetmiyorsa panel açık
        /// kalır ve gold satın alma popup'ı açılır.
        /// </summary>
        public void Buy()
        {
            if (!_hasTarget || BoosterManager.Instance == null) { return; }

            BoosterData data = BoosterManager.Instance.GetData(_type);
            if (data == null) { return; }

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayButtonClick(); }

            if (!EconomyManager.Instance.TrySpendGold(data.GoldPrice))
            {
                UIManager.Instance.ShowGoldPopup();
                return;
            }

            EconomyManager.Instance.AddBooster((int)_type, data.PackAmount);

            // Panel kapanınca oyun devam eder; booster ancak ondan sonra çalıştırılır.
            UIManager.Instance.HideBoosterPanel();
            BoosterManager.Instance.TryUse(_type);
        }

        private void Refresh()
        {
            BoosterData data = BoosterManager.Instance.GetData(_type);
            if (data == null) { return; }

            if (_titleText != null) { _titleText.SetKey(data.TitleKey); }
            if (_infoText != null) { _infoText.SetKey(data.InfoKey); }
            if (_iconImage != null && data.Icon != null) { _iconImage.sprite = data.Icon; }
            if (_amountText != null) { _amountText.text = string.Format(_amountFormat, data.PackAmount); }
            if (_priceText != null) { _priceText.text = data.GoldPrice.ToString(); }
        }
    }
}
