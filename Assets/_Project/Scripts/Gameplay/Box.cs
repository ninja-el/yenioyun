using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Banttaki tek kutu. Obje dokunulduğu anda yuvasını ayırtır, uçuşu bitince yuvasına oturur;
    /// kutu ancak ayrılan yuvaların hepsi dolduğunda dolmuş sayılır.
    /// </summary>
    public class Box : MonoBehaviour, IPoolable
    {
        /// <summary>Ayrılan yuvaların tümü dolduğunda yayınlanır. Kutuyu havuza iade etmek Conveyor'ın işidir.</summary>
        public event Action<Box> OnBoxFilled;

        [Tooltip("Objelerin kutu içinde oturacağı noktalar. Adedi GameConfig'teki kutu kapasitesiyle aynı olmalı.")]
        [SerializeField] private Transform[] _itemSlots;

        private readonly List<StackItem> _items = new List<StackItem>();
        private int _arrivedCount;

        public ItemType Type { get; private set; }

        /// <summary>Tüm yuvalar ayrıldıysa true. Uçuşu süren objeler de yuvayı işgal eder.</summary>
        public bool IsFilled => _items.Count >= _itemSlots.Length;

        /// <summary>Kutuyu bir obje tipine hazırlar. Havuzdan alındıktan sonra çağrılır.</summary>
        public void Setup(ItemType type)
        {
            Type = type;
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
            if (_arrivedCount >= _itemSlots.Length) { OnBoxFilled?.Invoke(this); }
        }

        public void OnSpawned()
        {
            _items.Clear();
            _arrivedCount = 0;
        }

        public void OnDespawned()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i].gameObject);
            }

            _items.Clear();
            _arrivedCount = 0;
            Type = null;
        }
    }
}
