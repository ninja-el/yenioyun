using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Banttaki tek kutu. Yalnızca kendi tipindeki objeleri kabul eder; dolduğunda içindeki
    /// objelerle birlikte havuza döner.
    /// </summary>
    public class Box : MonoBehaviour, IPoolable
    {
        /// <summary>Kutu dolduğunda, havuza iade edilmeden hemen önce yayınlanır.</summary>
        public event Action<Box> OnBoxFilled;

        [Tooltip("Objelerin kutu içinde oturacağı noktalar. Adedi GameConfig'teki kutu kapasitesiyle aynı olmalı.")]
        [SerializeField] private Transform[] _itemSlots;

        private readonly List<GameObject> _items = new List<GameObject>();

        public ItemType Type { get; private set; }
        public bool IsFilled => _items.Count >= _itemSlots.Length;

        /// <summary>Kutuyu bir obje tipine hazırlar. Havuzdan alındıktan sonra çağrılır.</summary>
        public void Setup(ItemType type)
        {
            Type = type;
        }

        /// <summary>Obje kutuya yerleşebiliyorsa true döner ve objeyi boştaki yuvaya oturtur.</summary>
        public bool TryAddItem(GameObject item, ItemType type)
        {
            if (item == null || IsFilled || type != Type) { return false; }

            Transform slot = _itemSlots[_items.Count];
            item.transform.SetParent(slot, false);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _items.Add(item);

            if (IsFilled) { Fill(); }

            return true;
        }

        public void OnSpawned()
        {
            _items.Clear();
        }

        public void OnDespawned()
        {
            Type = null;
            _items.Clear();
        }

        private void Fill()
        {
            OnBoxFilled?.Invoke(this);

            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i]);
            }

            PoolManager.Instance.Release(gameObject);
        }
    }
}
