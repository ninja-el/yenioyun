using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Mağazadaki tek bir ürünün butonu. Basılınca ShopManager'a ürün kimliğini bildirir ve
    /// store hazır olduğunda fiyat etiketini yerelleştirilmiş fiyatla değiştirir.
    /// Satın alma kuralını ve ödülü bu sınıf bilmez.
    /// </summary>
    public class ShopProductButton : MonoBehaviour
    {
        [Tooltip("Store'daki ürün kimliği. ShopCatalog içindeki ShopProduct ile aynı olmalı.")]
        [SerializeField] private string _productId;

        [Tooltip("Satın almayı başlatan buton.")]
        [SerializeField] private Button _button;

        [Tooltip("Fiyatın yazıldığı alan. Boş bırakılırsa fiyat güncellenmez.")]
        [SerializeField] private TMP_Text _priceText;

        public string ProductId => _productId;

        private void Awake()
        {
            if (_button == null) { _button = GetComponent<Button>(); }

            _button.onClick.AddListener(Purchase);
        }

        private void OnEnable()
        {
            if (ShopManager.Instance == null) { return; }

            ShopManager.Instance.OnStoreReady += RefreshPrice;
            RefreshPrice();
        }

        private void OnDisable()
        {
            if (ShopManager.Instance == null) { return; }

            ShopManager.Instance.OnStoreReady -= RefreshPrice;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Purchase);
        }

        /// <summary>Satın alma akışını başlatır.</summary>
        public void Purchase()
        {
            if (ShopManager.Instance == null) { return; }

            if (AudioManager.Instance != null) { AudioManager.Instance.PlayButtonClick(); }

            ShopManager.Instance.Purchase(_productId);
        }

        /// <summary>Fiyat etiketini store'dan gelen yerelleştirilmiş fiyatla günceller.</summary>
        public void RefreshPrice()
        {
            if (_priceText == null || ShopManager.Instance == null) { return; }

            string price = ShopManager.Instance.GetLocalizedPrice(_productId);
            if (string.IsNullOrEmpty(price)) { return; }

            _priceText.text = price;
        }
    }
}
