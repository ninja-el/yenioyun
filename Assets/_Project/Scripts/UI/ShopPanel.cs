using MatchPack.Core;
using MatchPack.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Market panelinin ve gold popup'ının açma/kapama butonlarını işletir. Ürün butonlarının
    /// kendi ShopProductButton bileşeni vardır; bu sınıf satın almaya karışmaz.
    /// Paneller kapalıyken de bağlı kalması için bileşen UI_Canvas üzerinde durur.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Tooltip("Market panelini açan buton. Sahnede yoksa boş bırakılır.")]
        [SerializeField] private Button _marketOpenButton;

        [Tooltip("Market panelini kapatan buton. Sahnede yoksa boş bırakılır.")]
        [SerializeField] private Button _marketCloseButton;

        [Tooltip("Gold popup'ını kapatan buton.")]
        [SerializeField] private Button _goldPopupCloseButton;

        [Tooltip("Booster panelini kapatan buton.")]
        [SerializeField] private Button _boosterPanelCloseButton;

        private void Awake()
        {
            if (_marketOpenButton != null) { _marketOpenButton.onClick.AddListener(OpenMarket); }
            if (_marketCloseButton != null) { _marketCloseButton.onClick.AddListener(CloseMarket); }
            if (_goldPopupCloseButton != null) { _goldPopupCloseButton.onClick.AddListener(CloseGoldPopup); }
            if (_boosterPanelCloseButton != null) { _boosterPanelCloseButton.onClick.AddListener(CloseBoosterPanel); }
        }

        private void Start()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
            }
        }

        private void OnDestroy()
        {
            if (_marketOpenButton != null) { _marketOpenButton.onClick.RemoveListener(OpenMarket); }
            if (_marketCloseButton != null) { _marketCloseButton.onClick.RemoveListener(CloseMarket); }
            if (_goldPopupCloseButton != null) { _goldPopupCloseButton.onClick.RemoveListener(CloseGoldPopup); }
            if (_boosterPanelCloseButton != null) { _boosterPanelCloseButton.onClick.RemoveListener(CloseBoosterPanel); }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
            }
        }

        /// <summary>Market panelini açar.</summary>
        public void OpenMarket()
        {
            UIManager.Instance.ShowMarket();
        }

        /// <summary>Market panelini kapatır.</summary>
        public void CloseMarket()
        {
            UIManager.Instance.HideMarket();
        }

        /// <summary>Gold popup'ını kapatır.</summary>
        public void CloseGoldPopup()
        {
            UIManager.Instance.HideGoldPopup();
        }

        /// <summary>Booster panelini kapatır.</summary>
        public void CloseBoosterPanel()
        {
            UIManager.Instance.HideBoosterPanel();
        }

        private void HandlePurchaseFailed(string productId, PurchaseFailure reason)
        {
            if (reason == PurchaseFailure.UserCancelled) { return; }

            Debug.LogWarning($"Purchase failed for {productId}: {reason}", this);
        }
    }
}
