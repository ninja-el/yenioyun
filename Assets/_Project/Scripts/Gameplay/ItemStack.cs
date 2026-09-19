using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Level'in objelerini karıştırıp <see cref="StackArea"/> içinde birbirine değmeyecek noktalara
    /// doğurur ve fiziğe bırakır. Alana sığmayan objeler bekler; yığından obje eksildikçe açılan
    /// boşluklara doğarlar. Objeler havuzdan geldiği için sahnedeki alana parent edilmez.
    /// </summary>
    public class ItemStack : MonoBehaviour
    {
        /// <summary>İlk dolumda doğan objelerin tamamı durulduğunda bir kez yayınlanır.</summary>
        public event Action OnStackSettled;

        [Tooltip("Objeler bu süre içinde durulmazsa yığın oturmuş sayılır.")]
        [SerializeField, Min(0f)] private float _settleTimeout = 5f;

        [Tooltip("Alana sığmayan objeler için boş yer taramasının tekrar aralığı (saniye).")]
        [SerializeField, Min(0.05f)] private float _refillInterval = 0.5f;

        private readonly List<StackItem> _items = new List<StackItem>();
        private readonly List<StackItem> _spawnBuffer = new List<StackItem>();
        private readonly List<ItemType> _typeBuffer = new List<ItemType>();
        private readonly Queue<ItemType> _pendingTypes = new Queue<ItemType>();

        private StackArea _area;
        private float _settleTimer;
        private float _refillTimer;

        /// <summary>Alanda duran objeler.</summary>
        public IReadOnlyList<StackItem> Items => _items;

        /// <summary>Alana sığmadığı için doğmayı bekleyen obje sayısı.</summary>
        public int PendingItemCount => _pendingTypes.Count;

        /// <summary>İlk dolum durulduysa true. Süre sayacı bundan sonra başlar.</summary>
        public bool IsSettled { get; private set; }

        /// <summary>LevelData'daki objeleri karıştırıp alanın içine doğurur.</summary>
        public void Build(LevelData level, StackArea area)
        {
            Clear();

            if (area == null)
            {
                Debug.LogError("ItemStack.Build received no StackArea; no item will spawn.", this);
                return;
            }

            _area = area;

            CollectTypes(level);
            Shuffle(_typeBuffer);

            for (int i = 0; i < _typeBuffer.Count; i++)
            {
                _pendingTypes.Enqueue(_typeBuffer[i]);
            }

            Fill();

            if (_items.Count == 0)
            {
                Debug.LogError(
                    "StackArea has no room for a single item; widen the area or shrink the item scale.",
                    this);
            }
        }

        /// <summary>
        /// Yığındaki objeleri alanın içinde yeniden dağıtır: hepsi fizik dışına alınır, yeni
        /// çakışmasız noktalara ışınlanır ve tekrar fiziğe bırakılır. Kutuya uçmakta olan objeler
        /// yığından çıkmış olduğu için etkilenmez. Shuffle booster'ı bunu çağırır.
        /// </summary>
        public bool Reshuffle()
        {
            if (_area == null || _items.Count == 0) { return false; }

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSimulated(false);
            }

            // Collider'lar kapandı; yeni noktalar aranmadan önce fizik motorunun bunu görmesi gerekir,
            // yoksa objeler kendi eski yerlerini dolu sayar.
            Physics.SyncTransforms();
            _area.BeginPlacement();

            for (int i = 0; i < _items.Count; i++)
            {
                StackItem item = _items[i];
                if (!_area.TryReserveSpot(item.BoundingRadius, out Vector3 position)) { continue; }

                item.Teleport(position, UnityEngine.Random.rotation);
            }

            Physics.SyncTransforms();

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSimulated(true);
            }

            return true;
        }

        /// <summary>Yığında verilen tipten bir obje varsa onu döner; yoksa null.</summary>
        public StackItem FindItem(ItemType type)
        {
            if (type == null) { return null; }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Type == type) { return _items[i]; }
            }

            return null;
        }

        /// <summary>Objeyi yığından çıkarır. Kutuya uçan obje artık yığının parçası değildir.</summary>
        public void Remove(StackItem item)
        {
            _items.Remove(item);
        }

        /// <summary>Alandaki objeleri havuza iade eder ve bekleyenleri düşürür.</summary>
        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i].gameObject);
            }

            _items.Clear();
            _spawnBuffer.Clear();
            _typeBuffer.Clear();
            _pendingTypes.Clear();

            _area = null;
            _settleTimer = 0f;
            _refillTimer = 0f;
            IsSettled = false;
        }

        private void Update()
        {
            if (_pendingTypes.Count > 0)
            {
                _refillTimer -= Time.deltaTime;
                if (_refillTimer <= 0f) { Fill(); }
            }

            if (IsSettled || _items.Count == 0) { return; }

            _settleTimer += Time.deltaTime;
            if (_settleTimer < _settleTimeout && !AreAllItemsResting()) { return; }

            IsSettled = true;
            OnStackSettled?.Invoke();
        }

        private bool AreAllItemsResting()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].IsResting) { return false; }
            }

            return true;
        }

        private void CollectTypes(LevelData level)
        {
            _typeBuffer.Clear();
            IReadOnlyList<LevelData.ItemEntry> entries = level.Items;

            for (int i = 0; i < entries.Count; i++)
            {
                LevelData.ItemEntry entry = entries[i];
                if (entry.Type == null) { continue; }

                for (int j = 0; j < entry.Count; j++)
                {
                    _typeBuffer.Add(entry.Type);
                }
            }
        }

        private void Fill()
        {
            _refillTimer = _refillInterval;

            if (_area == null || _pendingTypes.Count == 0) { return; }

            _spawnBuffer.Clear();
            _area.BeginPlacement();

            while (_pendingTypes.Count > 0)
            {
                ItemType type = _pendingTypes.Peek();
                GameObject instance = PoolManager.Instance.Get(type.Prefab);

                if (instance == null)
                {
                    _pendingTypes.Dequeue();
                    continue;
                }

                StackItem item = instance.GetComponent<StackItem>();

                if (!_area.TryReserveSpot(item.BoundingRadius, out Vector3 position))
                {
                    // Alanda yer kalmadı; obje havuza geri döner ve boşluk açılınca yeniden denenir.
                    PoolManager.Instance.Release(instance);
                    break;
                }

                _pendingTypes.Dequeue();
                item.SetSimulated(false);
                item.Setup(type);
                item.Teleport(position, UnityEngine.Random.rotation);

                _items.Add(item);
                _spawnBuffer.Add(item);
            }

            if (_spawnBuffer.Count == 0) { return; }

            // Physics.autoSyncTransforms kapalı; yeni pozlar fizik motoruna ancak bu çağrıyla geçer.
            Physics.SyncTransforms();

            // Fizik ancak tüm objeler yerleştikten sonra açılır; aksi halde solver onları üst üste bulur.
            for (int i = 0; i < _spawnBuffer.Count; i++)
            {
                _spawnBuffer[i].SetSimulated(true);
            }

            _spawnBuffer.Clear();
        }

        private static void Shuffle(List<ItemType> types)
        {
            for (int i = types.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (types[i], types[swapIndex]) = (types[swapIndex], types[i]);
            }
        }
    }
}
