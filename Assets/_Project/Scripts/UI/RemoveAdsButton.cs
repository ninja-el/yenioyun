using MatchPack.Core;
using MatchPack.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Ana menüdeki Remove Ads butonu. Basılınca marketi açar; reklamlar kaldırılmışsa kendini gizler.
    /// Reklamı kaldıran paketler marketteki kutulardır, bu buton ayrı bir ürün satmaz.
    /// </summary>
    public class RemoveAdsButton : MonoBehaviour
    {
        [Tooltip("Marketi açan buton.")]
        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null) { _button = GetComponent<Button>(); }

            _button.onClick.AddListener(OpenMarket);
        }

        private void Start()
        {
            ShopManager.Instance.OnOwnershipChanged += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OpenMarket);

            if (ShopManager.Instance != null) { ShopManager.Instance.OnOwnershipChanged -= RefreshVisibility; }
        }

        private void OpenMarket()
        {
            UIManager.Instance.ShowMarket();
        }

        private void RefreshVisibility()
        {
            gameObject.SetActive(!SaveManager.Instance.Data.HasRemovedAds);
        }
    }
}
