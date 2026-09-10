using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Banttaki tek kutu. Yalnızca kendi tipindeki objeleri kabul eder; havuza iade edilirken
    /// içindeki objeleri de iade eder.
    /// </summary>
    public class Box : MonoBehaviour, IPoolable
    {
        /// <summary>Kutu dolduğunda yayınlanır. Kutuyu havuza iade etmek Conveyor'ın işidir.</summary>
        public event Action<Box> OnBoxFilled;

        [Tooltip("Objelerin kutu içinde oturacağı noktalar. Adedi GameConfig'teki kutu kapasitesiyle aynı olmalı.")]
        [SerializeField] private Transform[] _itemSlots;

        private readonly List<StackItem> _items = new List<StackItem>();

        public ItemType Type { get; private set; }
        public bool IsFilled => _items.Count >= _itemSlots.Length;

        /// <summary>Kutuyu bir obje tipine hazırlar. Havuzdan alındıktan sonra çağrılır.</summary>
        public void Setup(ItemType type)
        {
            Type = type;
        }

        /// <summary>Obje kutuya yerleşebiliyorsa true döner ve objeyi boştaki yuvaya oturtur.</summary>
        public bool TryAddItem(StackItem item)
        {
            if (item == null || IsFilled || item.Type != Type) { return false; }

            Transform slot = _itemSlots[_items.Count];
            item.transform.SetParent(slot, false);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _items.Add(item);

            if (IsFilled) { OnBoxFilled?.Invoke(this); }

            return true;
        }

        public void OnSpawned()
        {
            _items.Clear();
        }

        public void OnDespawned()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i].gameObject);
            }

            _items.Clear();
            Type = null;
        }
    }
}
