using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Level'in objelerini karıştırıp yığın alanının üstünden döker ve fiziğe bırakır. Objeler
    /// havuzdan geldiği için sahnedeki yığın köküne parent edilmez; kök yalnızca dökülme noktasıdır.
    /// </summary>
    public class ItemStack : MonoBehaviour
    {
        /// <summary>Dökülen objelerin tamamı durulduğunda bir kez yayınlanır.</summary>
        public event Action OnStackSettled;

        [Tooltip("Objelerin doğduğu ızgaranın sütun sayısı (yığın kökünün sağ ekseni).")]
        [SerializeField, Min(1)] private int _spawnColumns = 3;

        [Tooltip("Objelerin doğduğu ızgaranın sıra sayısı (yığın kökünün ileri ekseni).")]
        [SerializeField, Min(1)] private int _spawnRows = 3;

        [Tooltip("Doğma anında objeler arası en az mesafe. Obje çapı bundan büyükse obje çapı kullanılır.")]
        [SerializeField, Min(0f)] private float _spawnSpacing = 0.9f;

        [Tooltip("İlk katın yığın kökünden yüksekliği.")]
        [SerializeField, Min(0f)] private float _spawnHeight = 1.2f;

        [Tooltip("Doğma noktasına eklenen rastgele sapma. Yığının fazla düzenli oturmasını engeller.")]
        [SerializeField, Min(0f)] private float _spawnJitter = 0.05f;

        [Tooltip("Objeler bu süre içinde durulmazsa yığın oturmuş sayılır.")]
        [SerializeField, Min(0f)] private float _settleTimeout = 5f;

        private readonly List<StackItem> _items = new List<StackItem>();
        private readonly List<ItemType> _typeBuffer = new List<ItemType>();

        private float _settleTimer;

        /// <summary>Yığında duran objeler.</summary>
        public IReadOnlyList<StackItem> Items => _items;

        /// <summary>Dökülen objeler durulduysa true. Süre sayacı bundan sonra başlar.</summary>
        public bool IsSettled { get; private set; }

        /// <summary>LevelData'daki objeleri karıştırıp yığın kökünün üstünden döker.</summary>
        public void Build(LevelData level, Transform stackRoot)
        {
            Clear();
            CollectTypes(level);
            Shuffle(_typeBuffer);
            Pour(stackRoot);
        }

        /// <summary>Objeyi yığından çıkarır. Kutuya uçan obje artık yığının parçası değildir.</summary>
        public void Remove(StackItem item)
        {
            _items.Remove(item);
        }

        /// <summary>Yığındaki objeleri havuza iade eder.</summary>
        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                PoolManager.Instance.Release(_items[i].gameObject);
            }

            _items.Clear();
            _settleTimer = 0f;
            IsSettled = false;
        }

        private void Update()
        {
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

        private void Pour(Transform stackRoot)
        {
            float spacing = _spawnSpacing;

            for (int i = 0; i < _typeBuffer.Count; i++)
            {
                ItemType type = _typeBuffer[i];
                GameObject instance = PoolManager.Instance.Get(type.Prefab);
                if (instance == null) { continue; }

                StackItem item = instance.GetComponent<StackItem>();
                item.SetSimulated(false);
                item.Setup(type);
                _items.Add(item);

                spacing = Mathf.Max(spacing, item.BoundingRadius * 2f);
            }

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].transform.SetPositionAndRotation(
                    GetSpawnPosition(stackRoot, i, spacing),
                    UnityEngine.Random.rotation);
            }

            // Fizik ancak tüm objeler yerleştikten sonra açılır; aksi halde solver onları üst üste bulur.
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSimulated(true);
            }
        }

        private Vector3 GetSpawnPosition(Transform stackRoot, int index, float spacing)
        {
            int itemsPerLayer = _spawnColumns * _spawnRows;
            int indexInLayer = index % itemsPerLayer;
            int layer = index / itemsPerLayer;

            float offsetRight = (indexInLayer % _spawnColumns - (_spawnColumns - 1) * 0.5f) * spacing;
            float offsetForward = (indexInLayer / _spawnColumns - (_spawnRows - 1) * 0.5f) * spacing;
            float offsetUp = _spawnHeight + layer * spacing;

            Vector3 position = stackRoot.position
                + stackRoot.right * offsetRight
                + stackRoot.forward * offsetForward
                + stackRoot.up * offsetUp;

            return position + UnityEngine.Random.insideUnitSphere * _spawnJitter;
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
