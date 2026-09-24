using System;
using System.Collections.Generic;
using DG.Tweening;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Banttaki tek kutu. Obje dokunulduğu anda yuvasını ayırtır, uçuşu bitince yuvasına oturur;
    /// kutu ancak ayrılan yuvaların hepsi dolduğunda dolmuş sayılır.
    /// </summary>
    public class Box : MonoBehaviour, IPoolable
    {
        [Serializable]
        private class Lid
        {
            [Tooltip("Kapak objesi. Pivotu menteşe kenarında olmalı.")]
            [SerializeField] private Transform _transform;

            [Tooltip("Kapağın açık hâlinden kapalı hâline dönüşü, parent uzayında derece. Tek eksen kullanılır; " +
                "180'i aşan değer kapağın aşağıdan değil yukarıdan dolanarak kapanmasını sağlar.")]
            [SerializeField] private Vector3 _closingRotation;

            [Tooltip("Kapanma başlamadan önceki bekleme. Kısa kapaklar önce kapanırsa uzunlar üstlerine oturur.")]
            [SerializeField, Min(0f)] private float _delay;

            public Transform Transform => _transform;
            public Vector3 ClosingRotation => _closingRotation;
            public float Delay => _delay;
        }

        /// <summary>Ayrılan yuvaların tümü dolduğunda yayınlanır. Kutuyu havuza iade etmek Conveyor'ın işidir.</summary>
        public event Action<Box> OnBoxFilled;

        [Tooltip("Objelerin kutu içinde oturacağı noktalar. Adedi GameConfig'teki kutu kapasitesiyle aynı olmalı.")]
        [SerializeField] private Transform[] _itemSlots;

        [Tooltip("Kutu havuzdan çıkarken joker olsun mu? Bant, joker booster'ı için bunu çalışma anında açar.")]
        [SerializeField] private bool _isJoker;

        [Tooltip("Kutunun üzerindeki obje ikonu. Boş bırakılırsa ikon güncellenmez.")]
        [SerializeField] private SpriteRenderer _iconRenderer;

        [Tooltip("Tipi belli olmayan joker kutunun ikonu. Kutu ilk objesini alınca yerini o objenin ikonu alır.")]
        [SerializeField] private Sprite _jokerIcon;

        [Tooltip("Dolan yuva sayısını gösteren görseller. Sırayla açılır; adedi yuva sayısıyla aynı olmalı.")]
        [SerializeField] private Image[] _fillIndicators;

        [Tooltip("Boş yuvaları gösteren görseller. Her obje geldiğinde sırayla kapanır; adedi yuva sayısıyla aynı olmalı.")]
        [SerializeField] private Image[] _emptyIndicators;

        [Tooltip("Kutu dolunca kapanan kapaklar.")]
        [SerializeField] private Lid[] _lids;

        [Tooltip("Tek bir kapağın kapanma süresi.")]
        [SerializeField, Min(0f)] private float _lidCloseDuration = 0.25f;

        [Tooltip("Kapaklar kapandıktan sonra kutunun yukarı yükseleceği mesafe (dünya birimi). Kutu bu mesafenin sonunda 0 ölçeğe ulaşır.")]
        [SerializeField, Min(0f)] private float _riseDistance = 2.5f;

        [Tooltip("Yükselip küçülerek kaybolmanın süresi.")]
        [SerializeField, Min(0f)] private float _riseDuration = 0.6f;

        private readonly List<StackItem> _items = new List<StackItem>();
        private int _arrivedCount;
        private bool _isJokerByDefault;
        private Quaternion _baseRotation;
        private float _baseHeight;
        private Vector3 _baseScale;
        private Quaternion[] _lidOpenRotations;
        private Sequence _departureSequence;

        public ItemType Type { get; private set; }

        /// <summary>
        /// Prefab'ta verilmiş duruş. Kutu bant turunda dönmediği için bu değer kutunun kalıcı
        /// rotasyonudur; bant yalnızca konumunu değiştirir.
        /// </summary>
        public Quaternion BaseRotation => _baseRotation;

        /// <summary>Prefab'ta verilmiş yükseklik. Bant kutuyu tur zemininin bu kadar üstüne oturtur.</summary>
        public float BaseHeight => _baseHeight;

        private void Awake()
        {
            // Havuz instance'ı prefab'ın yerel değerleriyle üretilir; sonrasında transform'u bant yazar.
            _baseRotation = transform.localRotation;
            _baseHeight = transform.localPosition.y;
            _baseScale = transform.localScale;
            _isJokerByDefault = _isJoker;

            _lidOpenRotations = new Quaternion[_lids.Length];
            for (int i = 0; i < _lids.Length; i++)
            {
                if (_lids[i].Transform != null) { _lidOpenRotations[i] = _lids[i].Transform.localRotation; }
            }
        }

        /// <summary>Tüm yuvalar ayrıldıysa true. Uçuşu süren objeler de yuvayı işgal eder.</summary>
        public bool IsFilled => _items.Count >= _itemSlots.Length;

        /// <summary>Hiçbir yuvası ayrılmamış kutu. Joker kutu karşılığında iptal edilecek kutu bununla aranır.</summary>
        public bool IsEmpty => _items.Count == 0;

        /// <summary>Yuvası ayrılmış obje sayısı. Aynı tipten kutular arasında en dolusu bununla seçilir.</summary>
        public int ItemCount => _items.Count;

        /// <summary>Henüz ayrılmamış yuva sayısı.</summary>
        public int FreeSlotCount => _itemSlots.Length - _items.Count;

        /// <summary>Tipsiz gelip ilk objeden tipini alan kutu mu?</summary>
        public bool IsJoker => _isJoker;

        /// <summary>Kutunun tipi belli mi? Joker kutu ilk objesini alana kadar false döner.</summary>
        public bool IsTypeLocked => Type != null;

        /// <summary>
        /// Kutuyu bir obje tipine hazırlar. Havuzdan alındıktan sonra çağrılır; joker kutu için
        /// Conveyor tipi belirlendiğinde ikinci kez çağırır. null verilince kutu tipsiz kalır.
        /// </summary>
        public void Setup(ItemType type)
        {
            Type = type;
            RefreshIcon();
        }

        /// <summary>
        /// Kutuyu joker yapar veya joker'likten çıkarır. Bant, normal kutu prefab'ını joker olarak
        /// göndermek için havuzdan aldıktan hemen sonra çağırır.
        /// </summary>
        public void SetJoker(bool isJoker)
        {
            _isJoker = isJoker;
            RefreshIcon();
        }

        /// <summary>Obje kabul edilebiliyorsa yuvayı ona ayırır. Obje henüz yerleşmez, uçuşa başlar.</summary>
        public bool TryAddItem(StackItem item, out Transform slot)
        {
            slot = null;
            if (item == null || IsFilled || item.Type != Type) { return false; }

            slot = _itemSlots[_items.Count];
            _items.Add(item);
            return true;
        }

        /// <summary>Uçuşu biten objeyi ayrılmış yuvasına oturtur.</summary>
        public void ConfirmItem(StackItem item)
        {
            int index = _items.IndexOf(item);
            if (index < 0) { return; }

            item.transform.SetParent(_itemSlots[index], false);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _arrivedCount++;
            ShowFillIndicators(_arrivedCount);

            if (_arrivedCount >= _itemSlots.Length) { OnBoxFilled?.Invoke(this); }
        }

        /// <summary>
        /// Dolan kutunun ayrılış animasyonu: kapaklar kapanır, ardından kutu yukarı yükselirken
        /// küçülüp kaybolur. Bitince <paramref name="onComplete"/> çağrılır; havuza iade çağıranın işidir.
        /// </summary>
        public void PlayDeparture(Action onComplete)
        {
            StopDeparture();

            _departureSequence = DOTween.Sequence();
            float lidsEnd = 0f;

            for (int i = 0; i < _lids.Length; i++)
            {
                Transform lid = _lids[i].Transform;
                if (lid == null) { continue; }

                Quaternion openRotation = _lidOpenRotations[i];
                Vector3 closingRotation = _lids[i].ClosingRotation;

                // Quaternion ara değeri en kısa yolu seçip kapağı kutunun içinden geçirir; açı
                // tek eksende ilerletilince kapak menteşesi etrafında yukarıdan dolanır.
                _departureSequence.Insert(_lids[i].Delay, DOVirtual
                    .Float(0f, 1f, _lidCloseDuration, t => lid.localRotation = Quaternion.Euler(closingRotation * t) * openRotation)
                    .SetEase(Ease.InOutQuad));

                lidsEnd = Mathf.Max(lidsEnd, _lids[i].Delay + _lidCloseDuration);
            }

            // Ölçek doğrudan katedilen yükseklikten hesaplanır: kutu yukarı çıktıkça küçülür ve tam
            // yükselme mesafesinin sonunda 0 ölçeğe varır.
            float startY = transform.position.y;
            _departureSequence
                .Insert(lidsEnd, DOVirtual
                    .Float(0f, 1f, _riseDuration, progress =>
                    {
                        Vector3 position = transform.position;
                        position.y = startY + _riseDistance * progress;
                        transform.position = position;
                        transform.localScale = _baseScale * (1f - progress);
                    })
                    .SetEase(Ease.InOutSine))
                .OnComplete(() =>
                {
                    _departureSequence = null;
                    onComplete?.Invoke();
                });
        }

        public void OnSpawned()
        {
            _items.Clear();
            _arrivedCount = 0;
            _isJoker = _isJokerByDefault;
            ShowFillIndicators(0);
            ResetDepartureVisuals();
        }

        public void OnDespawned()
        {
            StopDeparture();
            ResetDepartureVisuals();

            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i].gameObject);
            }

            _items.Clear();
            _arrivedCount = 0;
            _isJoker = _isJokerByDefault;
            ShowFillIndicators(0);
            Setup(null);
        }

        private void StopDeparture()
        {
            _departureSequence?.Kill();
            _departureSequence = null;
        }

        // Ayrılışı yarıda kesilen ya da tamamlayan kutu havuza küçülmüş ve kapağı kapalı döner;
        // bir sonraki kullanımda banta açık ve tam boyutta girmesi için geri alınır.
        private void ResetDepartureVisuals()
        {
            transform.localScale = _baseScale;

            for (int i = 0; i < _lids.Length; i++)
            {
                if (_lids[i].Transform != null) { _lids[i].Transform.localRotation = _lidOpenRotations[i]; }
            }
        }

        private void RefreshIcon()
        {
            if (_iconRenderer == null) { return; }

            if (Type != null)
            {
                _iconRenderer.sprite = Type.Icon;
                return;
            }

            _iconRenderer.sprite = _isJoker ? _jokerIcon : null;
        }

        private void ShowFillIndicators(int filledCount)
        {
            SetIndicators(_fillIndicators, filledCount, true);
            SetIndicators(_emptyIndicators, filledCount, false);
        }

        // isFilledState true ise görsel dolu yuvalarda, false ise boş yuvalarda açık kalır.
        private static void SetIndicators(Image[] indicators, int filledCount, bool isFilledState)
        {
            if (indicators == null) { return; }

            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] == null) { continue; }

                indicators[i].gameObject.SetActive(i < filledCount == isFilledState);
            }
        }
    }
}
