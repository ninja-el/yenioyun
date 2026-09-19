using MatchPack.Core;
using MatchPack.Data;
using MatchPack.Gameplay;
using MatchPack.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.UI
{
    /// <summary>
    /// Tek bir booster'ın oyun içi butonu. Basılınca BoosterManager'a tipini bildirir, stok bittiyse
    /// booster satın alma panelini açar. Kilit ve stok kuralını bu sınıf işletmez, yalnızca gösterir.
    /// </summary>
    public class BoosterButton : MonoBehaviour
    {
        [Tooltip("Bu butonun kullandığı booster.")]
        [SerializeField] private BoosterType _type;

        [Tooltip("Booster'ı kullanan buton.")]
        [SerializeField] private Button _button;

        [Tooltip("Eldeki adedin yazıldığı alan. Boş bırakılırsa adet gösterilmez.")]
        [SerializeField] private TMP_Text _countText;

        [Tooltip("Adet rozetinin kökü. Stok bittiğinde kapatılır.")]
        [SerializeField] private GameObject _countRoot;

        [Tooltip("Stok bittiğinde açılacak satın alma paneli.")]
        [SerializeField] private BoosterPurchasePanel _purchasePanel;

        [Tooltip("Booster ikonunun gösterildiği Image. Boş bırakılırsa ikon güncellenmez.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Booster kilitliyken açılacak görsel (asma kilit vb.).")]
        [SerializeField] private GameObject _lockedRoot;

        [Tooltip("Stok bittiğinde açılacak görsel (artı işareti vb.).")]
        [SerializeField] private GameObject _emptyRoot;

        private void Awake()
        {
            if (_button == null) { _button = GetComponent<Button>(); }

            _button.onClick.AddListener(Use);
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBoosterCountChanged += HandleBoosterCountChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelStarted += HandleLevelStarted;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBoosterCountChanged -= HandleBoosterCountChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelStarted -= HandleLevelStarted;
            }
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Use);
        }

        /// <summary>Booster'ı kullanır; stok bittiyse satın alma panelini o booster için açar.</summary>
        public void Use()
        {
            if (BoosterManager.Instance == null || !BoosterManager.Instance.IsUnlocked(_type)) { return; }

            if (BoosterManager.Instance.GetCount(_type) <= 0)
            {
                if (_purchasePanel == null) { return; }

                if (AudioManager.Instance != null) { AudioManager.Instance.PlayButtonClick(); }

                _purchasePanel.Open(_type);
                return;
            }

            BoosterManager.Instance.TryUse(_type);
        }

        /// <summary>İkonu, adedi ve kilit durumunu tazeler.</summary>
        public void Refresh()
        {
            if (BoosterManager.Instance == null) { return; }

            BoosterData data = BoosterManager.Instance.GetData(_type);
            bool isUnlocked = BoosterManager.Instance.IsUnlocked(_type);
            int count = BoosterManager.Instance.GetCount(_type);

            if (_iconImage != null && data != null && data.Icon != null) { _iconImage.sprite = data.Icon; }
            if (_countText != null) { _countText.text = count.ToString(); }
            if (_countRoot != null) { _countRoot.SetActive(isUnlocked && count > 0); }
            if (_lockedRoot != null) { _lockedRoot.SetActive(!isUnlocked); }
            if (_emptyRoot != null) { _emptyRoot.SetActive(isUnlocked && count <= 0); }

            _button.interactable = isUnlocked;
        }

        private void HandleBoosterCountChanged(int index, int count)
        {
            if (index != (int)_type) { return; }

            Refresh();
        }

        private void HandleLevelStarted(LevelData level)
        {
            Refresh();
        }
    }
}
