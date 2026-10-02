using System;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market paketinin satın alma butonu. Basılınca IAPManager'a paket anahtarını iletir, fiyat
/// yazısını günceller ve tek alımlık paket alınmışsa butonu deaktif eder ya da kartı gizler.
/// Ödülü ve sahiplik kuralını bilmez; sahipliği ShopManager'dan okur.
/// </summary>
[RequireComponent(typeof(Button))]
public class IAPBuyButton : MonoBehaviour
{
   [Tooltip("Satın alınacak paket.")]
   [SerializeField] private IAPProductKey productKey;

   [Tooltip("Fiyatın yazıldığı alan. Boş bırakılırsa fiyat güncellenmez.")]
   [SerializeField] private TMP_Text _priceText;

   [Tooltip("Tek alımlık paket alındığında gizlenecek kart. Boş bırakılırsa kart kalır, yalnızca buton deaktif olur.")]
   [SerializeField] private GameObject _hideWhenOwned;

   private Button _button;
   private bool _isSubscribed;

   private void Awake()
   {
      _button = GetComponent<Button>();
      _button.onClick.AddListener(Buy);
   }

   private void OnEnable()
   {
      Subscribe();
      Refresh();
   }

   private void Start()
   {
      // Butonu taşıyan panel ShopManager'dan önce açılmışsa abonelik burada tamamlanır.
      Subscribe();
      Refresh();
   }

   private void OnDestroy()
   {
      _button.onClick.RemoveListener(Buy);

      if (!_isSubscribed) { return; }

      if (IAPManager.Instance != null) { IAPManager.Instance.OnPricesUpdated -= RefreshPrice; }
      if (ShopManager.Instance != null) { ShopManager.Instance.OnOwnershipChanged -= RefreshOwnership; }
   }

   private void Buy()
   {
      if (IAPManager.Instance == null)
      {
         Debug.LogWarning("IAPBuyButton: IAPManager is not available, purchase skipped.");
         return;
      }

      IAPManager.Instance.BuyProduct(productKey);
   }

   private void Subscribe()
   {
      if (_isSubscribed || IAPManager.Instance == null || ShopManager.Instance == null) { return; }

      IAPManager.Instance.OnPricesUpdated += RefreshPrice;
      ShopManager.Instance.OnOwnershipChanged += RefreshOwnership;
      _isSubscribed = true;
   }

   private void Refresh()
   {
      RefreshPrice();
      RefreshOwnership();
   }

   private void RefreshPrice()
   {
      if (_priceText == null || IAPManager.Instance == null) { return; }

      string productId = IAPManager.Instance.GetProductId(productKey);
      string price = IAPManager.Instance.GetLocalizedPrice(productId);

      if (string.IsNullOrEmpty(price) && ShopManager.Instance != null && ShopManager.Instance.Catalog != null)
      {
         ShopProduct product = ShopManager.Instance.Catalog.Find(productId);
         if (product != null) { price = product.FallbackPriceText; }
      }

      if (!string.IsNullOrEmpty(price)) { _priceText.text = price; }
   }

   private void RefreshOwnership()
   {
      if (IAPManager.Instance == null || ShopManager.Instance == null) { return; }

      if (!ShopManager.Instance.IsOwned(IAPManager.Instance.GetProductId(productKey))) { return; }

      if (_hideWhenOwned != null) { _hideWhenOwned.SetActive(false); }
      else { _button.interactable = false; }
   }
}
