using System;
using System.Collections.Generic;
using DG.Tweening;
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
        private readonly Dictionary<ItemType, float> _scaleMultipliers = new Dictionary<ItemType, float>();
        private readonly Queue<ItemType> _pendingTypes = new Queue<ItemType>();
        private readonly List<Vector3> _vacatedSpots = new List<Vector3>();
        private readonly List<StackItem> _unplacedItems = new List<StackItem>();

        private StackArea _area;
        private float _settleTimer;
        private float _refillTimer;
        private int _shufflingItemCount;
        private Action _handleShuffleArrived;

        /// <summary>Alanda duran objeler.</summary>
        public IReadOnlyList<StackItem> Items => _items;

        /// <summary>Alana sığmadığı için doğmayı bekleyen obje sayısı.</summary>
        public int PendingItemCount => _pendingTypes.Count;

        /// <summary>İlk dolum durulduysa true. Süre sayacı bundan sonra başlar.</summary>
        public bool IsSettled { get; private set; }

        /// <summary>Shuffle sonrası objeler yeni yerlerine kayarken true.</summary>
        public bool IsShuffling => _shufflingItemCount > 0;

        private void Awake()
        {
            _handleShuffleArrived = HandleShuffleArrived;
        }

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
        /// çakışmasız noktalarına verilen sürede kayar ve hepsi varınca tekrar fiziğe bırakılır.
        /// Rastgele boş nokta bulamayan obje, başka bir objenin boşalttığı yere gider; böylece hiçbir
        /// obje yerinde kalmaz. Kayma sırasında collider'lar kapalı olduğu için objeler birbirine
        /// çarpmaz. Kutuya uçmakta olan objeler yığından çıkmış olduğu için etkilenmez.
        /// </summary>
        public bool Reshuffle(float duration, Ease ease)
        {
            if (_area == null || _items.Count == 0 || IsShuffling) { return false; }

            _vacatedSpots.Clear();
            _unplacedItems.Clear();

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSimulated(false);
                _vacatedSpots.Add(_items[i].BoundsCenter);
            }

            // Collider'lar kapandı; yeni noktalar aranmadan önce fizik motorunun bunu görmesi gerekir,
            // yoksa objeler kendi eski yerlerini dolu sayar.
            Physics.SyncTransforms();
            _area.BeginPlacement();

            for (int i = 0; i < _items.Count; i++)
            {
                StackItem item = _items[i];

                if (_area.TryReserveSpot(item.BoundingRadius, out Vector3 position))
                {
                    StartShuffleMove(item, position, duration, ease);
                }
                else
                {
                    _unplacedItems.Add(item);
                }
            }

            MoveUnplacedItemsToVacatedSpots(duration, ease);

            if (_shufflingItemCount == 0) { FinishShuffle(); }

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
            if (!_items.Remove(item) || !item.IsMoving) { return; }

            // Auto-Match kayan bir objeyi alabilir; kayma kesilip varmış sayılmazsa fizik hiç açılmaz.
            item.StopMove();
            HandleShuffleArrived();
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
            _shufflingItemCount = 0;
            IsSettled = false;
        }

        private void Update()
        {
            // Kayan objelerin collider'ı kapalı; bu sırada boş yer aranırsa onların yerine obje doğar.
            if (_pendingTypes.Count > 0 && !IsShuffling)
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

        private void MoveUnplacedItemsToVacatedSpots(float duration, Ease ease)
        {
            Shuffle(_vacatedSpots);

            for (int i = 0; i < _unplacedItems.Count; i++)
            {
                StackItem item = _unplacedItems[i];
                int spotIndex = FindVacatedSpot(item, true);

                // Ayrılabilen boş yer kalmadıysa yine de başka bir objenin eski yerine gider; olası
                // küçük çakışmayı fizik açıldığında çözer. Obje yerinde kalmamalı.
                if (spotIndex < 0) { spotIndex = FindVacatedSpot(item, false); }
                if (spotIndex < 0) { continue; }

                Vector3 spot = _vacatedSpots[spotIndex];
                _vacatedSpots.RemoveAt(spotIndex);
                StartShuffleMove(item, spot, duration, ease);
            }

            _unplacedItems.Clear();
            _vacatedSpots.Clear();
        }

        private int FindVacatedSpot(StackItem item, bool mustReserve)
        {
            Vector3 currentPosition = item.BoundsCenter;

            for (int i = 0; i < _vacatedSpots.Count; i++)
            {
                if (_vacatedSpots[i] == currentPosition) { continue; }
                if (mustReserve && !_area.TryReserveAt(_vacatedSpots[i], item.BoundingRadius)) { continue; }

                return i;
            }

            return -1;
        }

        private void StartShuffleMove(StackItem item, Vector3 position, float duration, Ease ease)
        {
            _shufflingItemCount++;
            // Yer collider merkezine göre ayrıldı; pivot, seçilen rotasyonda merkezi oraya getirecek yere gider.
            Quaternion rotation = UnityEngine.Random.rotation;
            item.MoveTo(item.GetPivotForCenter(position, rotation), rotation, duration, ease, _handleShuffleArrived);
        }

        private void HandleShuffleArrived()
        {
            _shufflingItemCount--;
            if (_shufflingItemCount > 0) { return; }

            FinishShuffle();
        }

        private void FinishShuffle()
        {
            _shufflingItemCount = 0;

            // Fizik ancak tüm objeler yerine vardıktan sonra açılır; aksi halde kayanlar duranlara çarpar.
            Physics.SyncTransforms();

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSimulated(true);
            }
        }

        private bool AreAllItemsResting()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].IsResting) { return false; }
            }

            return true;
        }

        private float GetScaleMultiplier(ItemType type)
        {
            return _scaleMultipliers.TryGetValue(type, out float multiplier) ? multiplier : 1f;
        }

        private void CollectTypes(LevelData level)
        {
            _typeBuffer.Clear();
            _scaleMultipliers.Clear();
            IReadOnlyList<LevelData.ItemEntry> entries = level.Items;

            for (int i = 0; i < entries.Count; i++)
            {
                LevelData.ItemEntry entry = entries[i];
                if (entry.Type == null) { continue; }

                // Aynı tip birden fazla satırda geçerse ilk satırın çarpanı geçerlidir; yığında aynı
                // tipin objeleri farklı boyutta durmaz.
                _scaleMultipliers.TryAdd(entry.Type, entry.ScaleMultiplier);

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
                item.Setup(type, GetScaleMultiplier(type));

                if (!_area.TryReserveSpot(item.BoundingRadius, out Vector3 position))
                {
                    // Alanda yer kalmadı; obje havuza geri döner ve boşluk açılınca yeniden denenir.
                    PoolManager.Instance.Release(instance);
                    break;
                }

                _pendingTypes.Dequeue();
                item.SetSimulated(false);
                Quaternion rotation = UnityEngine.Random.rotation;
                item.Teleport(item.GetPivotForCenter(position, rotation), rotation);

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

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
