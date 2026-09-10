using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Bandın kutu kuyruğunu yönetir. Slotları LevelData.conveyorCapacity kadar açar, dolan kutunun
    /// yerine kuyruktaki bir sonrakini getirir, kuyruk ve slotlar boşalınca leveli tamamlanmış sayar.
    /// </summary>
    public class Conveyor : MonoBehaviour
    {
        public event Action<Box> OnBoxSpawned;
        public event Action<Box> OnBoxFilled;
        public event Action OnAllBoxesCompleted;

        [Tooltip("Kutu kapasitesinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [Tooltip("Slot prefab'ı. Havuzdan alınır.")]
        [SerializeField] private GameObject _slotPrefab;

        [Tooltip("Kutu prefab'ı. Havuzdan alınır.")]
        [SerializeField] private GameObject _boxPrefab;

        [Tooltip("Bant üzerinde iki slot arasındaki mesafe.")]
        [SerializeField] private float _slotSpacing = 1.5f;

        private readonly List<ConveyorSlot> _slots = new List<ConveyorSlot>();
        private readonly Queue<ItemType> _boxQueue = new Queue<ItemType>();

        /// <summary>Henüz banta gelmemiş kutu sayısı.</summary>
        public int QueuedBoxCount => _boxQueue.Count;

        /// <summary>Level'in kutularını kurar ve bandı doldurur.</summary>
        public void Build(LevelData level, Transform conveyorRoot)
        {
            Clear();
            BuildQueue(level);
            BuildSlots(level.ConveyorCapacity, conveyorRoot);
            FillEmptySlots();
        }

        /// <summary>Bandı boşaltır; slotları ve kutuları havuza iade eder.</summary>
        public void Clear()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                ConveyorSlot slot = _slots[i];

                if (!slot.IsEmpty)
                {
                    ReleaseBox(slot.CurrentBox);
                    slot.Clear();
                }

                PoolManager.Instance.Release(slot.gameObject);
            }

            _slots.Clear();
            _boxQueue.Clear();
        }

        private void BuildQueue(LevelData level)
        {
            IReadOnlyList<LevelData.ItemEntry> entries = level.Items;

            for (int i = 0; i < entries.Count; i++)
            {
                LevelData.ItemEntry entry = entries[i];
                if (entry.Type == null) { continue; }

                int boxCount = entry.Count / _config.BoxCapacity;
                for (int j = 0; j < boxCount; j++)
                {
                    _boxQueue.Enqueue(entry.Type);
                }
            }
        }

        private void BuildSlots(int capacity, Transform conveyorRoot)
        {
            for (int i = 0; i < capacity; i++)
            {
                GameObject instance = PoolManager.Instance.Get(_slotPrefab);
                if (instance == null) { return; }

                float offset = (i - (capacity - 1) * 0.5f) * _slotSpacing;
                instance.transform.SetPositionAndRotation(
                    conveyorRoot.position + conveyorRoot.right * offset,
                    conveyorRoot.rotation);

                _slots.Add(instance.GetComponent<ConveyorSlot>());
            }
        }

        private void FillEmptySlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                ConveyorSlot slot = _slots[i];
                if (!slot.IsEmpty || _boxQueue.Count == 0) { continue; }

                GameObject instance = PoolManager.Instance.Get(_boxPrefab);
                if (instance == null) { return; }

                Box box = instance.GetComponent<Box>();
                box.Setup(_boxQueue.Dequeue());
                box.OnBoxFilled += HandleBoxFilled;

                slot.PlaceBox(box);
                OnBoxSpawned?.Invoke(box);
            }
        }

        private void HandleBoxFilled(Box box)
        {
            ConveyorSlot slot = FindSlot(box);
            if (slot != null) { slot.Clear(); }

            OnBoxFilled?.Invoke(box);
            ReleaseBox(box);
            FillEmptySlots();

            if (IsCompleted) { OnAllBoxesCompleted?.Invoke(); }
        }

        private bool IsCompleted
        {
            get
            {
                if (_boxQueue.Count > 0) { return false; }

                for (int i = 0; i < _slots.Count; i++)
                {
                    if (!_slots[i].IsEmpty) { return false; }
                }

                return true;
            }
        }

        private ConveyorSlot FindSlot(Box box)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].CurrentBox == box) { return _slots[i]; }
            }

            return null;
        }

        private void ReleaseBox(Box box)
        {
            box.OnBoxFilled -= HandleBoxFilled;
            PoolManager.Instance.Release(box.gameObject);
        }
    }
}
