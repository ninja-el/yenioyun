using System;
using System.Collections.Generic;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Bandın kutu karuseli. Tek bir offset değerini ilerletir; kutular tur üzerindeki sanal
    /// slotlara bağlıdır ve konumlarını slot belirler, kendileri hareket hesaplamaz. Kutu giriş
    /// noktasından banta katılır; tamamlanınca banttan ayrılıp çıkış noktasına gider ve kaybolur.
    /// </summary>
    public class Conveyor : MonoBehaviour
    {
        private enum SlotState
        {
            Empty,
            Entering,
            Riding
        }

        private struct LeavingBox
        {
            public Box Box;
            public Vector3 From;
            public float Progress;
        }

        public event Action<Box> OnBoxSpawned;
        public event Action<Box> OnBoxFilled;
        public event Action OnAllBoxesCompleted;

        [Tooltip("Kutu kapasitesinin ve bant sürelerinin okunduğu config.")]
        [SerializeField] private GameConfig _config;

        [Tooltip("Kutu prefab'ları. Kutular sırayla bu listeden alınır, liste sonuna gelince başa döner.")]
        [SerializeField] private GameObject[] _boxPrefabs;

        private readonly Queue<ItemType> _boxQueue = new Queue<ItemType>();
        private readonly Queue<GameObject> _jokerQueue = new Queue<GameObject>();
        private readonly List<ItemType> _queueBuffer = new List<ItemType>();
        private readonly List<LeavingBox> _leavingBoxes = new List<LeavingBox>();

        private ConveyorPath _path;
        private Box[] _slotBoxes;
        private SlotState[] _slotStates;
        private float[] _slotProgress;

        private float _beltOffset;
        private float _speedScale = 1f;
        private float _entryTimer;
        private int _capacity;
        private int _dispatchedBoxCount;
        private bool _isRunning;

        /// <summary>Henüz banta girmemiş kutu sayısı.</summary>
        public int QueuedBoxCount => _boxQueue.Count;

        /// <summary>Bant çalışıyor mu? Level bitince veya bant boşaltılınca false olur.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Level'in kutularını kurar ve bandı çalıştırır.</summary>
        public void Build(LevelData level, ConveyorPath path)
        {
            Clear();

            if (path == null || !path.IsValid)
            {
                Debug.LogError("Conveyor.Build received an invalid ConveyorPath; the belt will not run.", this);
                return;
            }

            if (_boxPrefabs == null || _boxPrefabs.Length == 0)
            {
                Debug.LogError("Conveyor has no box prefabs assigned; the belt will not run.", this);
                return;
            }

            _path = path;
            _capacity = Mathf.Min(level.ConveyorCapacity, _path.SlotCount);

            _slotBoxes = new Box[_path.SlotCount];
            _slotStates = new SlotState[_path.SlotCount];
            _slotProgress = new float[_path.SlotCount];

            BuildQueue(level);

            _beltOffset = 0f;
            _entryTimer = 0f;
            _speedScale = 1f;
            _dispatchedBoxCount = 0;
            _isRunning = true;
        }

        /// <summary>Bant hızını ölçekler. Time Freeze booster'ı 0 vererek bandı durdurur.</summary>
        public void SetSpeedScale(float scale)
        {
            _speedScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// Bantta bu tipe uygun, girişini tamamlamış ve yuvası kalmış bir kutu varsa döner. Tipi
        /// belirlenmemiş joker kutu yalnızca aynı tipten normal kutu bulunamadığında aday olur;
        /// bu çağrı hiçbir kutunun durumunu değiştirmez.
        /// </summary>
        public bool TryGetBoxFor(ItemType type, out Box box)
        {
            box = null;
            if (_slotBoxes == null || type == null) { return false; }

            Box joker = null;

            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotStates[i] != SlotState.Riding) { continue; }

                Box candidate = _slotBoxes[i];
                if (candidate == null || candidate.IsFilled) { continue; }

                if (candidate.Type == type)
                {
                    box = candidate;
                    return true;
                }

                if (joker == null && candidate.IsJoker && !candidate.IsTypeLocked && HasBoxCredit(type))
                {
                    joker = candidate;
                }
            }

            box = joker;
            return box != null;
        }

        /// <summary>
        /// Objeye uygun kutuyu bulup yuvasını ona ayırır. Joker kutu seçildiyse tipini burada alır.
        /// Uygun kutu yoksa veya yuva ayrılamazsa false döner.
        /// </summary>
        public bool TryReserveBox(StackItem item, out Box box, out Transform slot)
        {
            slot = null;
            box = null;

            if (item == null || !TryGetBoxFor(item.Type, out Box candidate)) { return false; }
            if (!candidate.IsTypeLocked && !LockJokerBox(candidate, item.Type)) { return false; }
            if (!candidate.TryAddItem(item, out slot)) { return false; }

            box = candidate;
            return true;
        }

        /// <summary>Bantta duran, tipi belli ve hâlâ obje kabul eden kutuları verilen listeye yazar.</summary>
        public void CollectFillableBoxes(List<Box> buffer)
        {
            buffer.Clear();
            if (_slotBoxes == null) { return; }

            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotStates[i] != SlotState.Riding) { continue; }

                Box box = _slotBoxes[i];
                if (box == null || box.IsFilled || !box.IsTypeLocked) { continue; }

                buffer.Add(box);
            }
        }

        /// <summary>
        /// Joker kutuyu bant sırasına alır; bir sonraki boş slot giriş noktasını geçtiğinde banta
        /// katılır. Level'in kutu kuyruğundan gelmediği için kapasite kuralı girişini engellemez.
        /// </summary>
        public bool TryQueueJokerBox(GameObject prefab)
        {
            if (!_isRunning || prefab == null) { return false; }

            _jokerQueue.Enqueue(prefab);
            return true;
        }

        /// <summary>Bandı boşaltır; bantta ve çıkışta olan kutuları havuza iade eder.</summary>
        public void Clear()
        {
            _isRunning = false;

            if (_slotBoxes != null)
            {
                for (int i = 0; i < _slotBoxes.Length; i++)
                {
                    if (_slotBoxes[i] == null) { continue; }

                    ReleaseBox(_slotBoxes[i]);
                    _slotBoxes[i] = null;
                    _slotStates[i] = SlotState.Empty;
                    _slotProgress[i] = 0f;
                }
            }

            for (int i = 0; i < _leavingBoxes.Count; i++)
            {
                ReleaseBox(_leavingBoxes[i].Box);
            }

            _leavingBoxes.Clear();
            _boxQueue.Clear();
            _jokerQueue.Clear();
            _queueBuffer.Clear();
            _path = null;
        }

        private void Update()
        {
            if (!_isRunning) { return; }

            float previousOffset = _beltOffset;
            float step = _config.BeltSpeed * _speedScale * Time.deltaTime / _path.SlotCount;
            _beltOffset = Mathf.Repeat(_beltOffset + step, 1f);

            float travelled = step * _path.TotalLength;

            HandleEntries(previousOffset, travelled);
            UpdateSlotBoxes();
            UpdateLeavingBoxes();
        }

        private void HandleEntries(float previousOffset, float travelled)
        {
            if (_entryTimer > 0f) { _entryTimer -= Time.deltaTime; }

            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotStates[i] != SlotState.Empty) { continue; }
                if (_entryTimer > 0f) { return; }

                bool hasJokerBox = _jokerQueue.Count > 0;
                if (!hasJokerBox && !CanDispatch()) { return; }
                if (!HasCrossed(i, previousOffset, travelled, _path.EntryDistance)) { continue; }

                if (hasJokerBox)
                {
                    DispatchJokerBox(i);
                    continue;
                }

                DispatchBox(i);
            }
        }

        private bool CanDispatch()
        {
            // 01-Oyun-Ozeti.md kuralı "yığında kalan obje sayısı banttaki boş yuvadan fazlaysa
            // yeni kutu gelir" der. Toplam obje targetBoxCount * kapasite olduğu için kalan obje
            // daima kuyruktaki kutuların kapasitesi + banttaki boş yuvaya eşittir; kural bu yüzden
            // "kuyrukta kutu var mı" kontrolüne indirgenir.
            if (_boxQueue.Count == 0) { return false; }

            return GetFillableBoxCount() < _capacity;
        }

        private void DispatchBox(int slotIndex)
        {
            GameObject prefab = _boxPrefabs[_dispatchedBoxCount % _boxPrefabs.Length];
            GameObject instance = PoolManager.Instance.Get(prefab);
            if (instance == null) { return; }

            _dispatchedBoxCount++;
            PlaceBox(slotIndex, instance.GetComponent<Box>(), _boxQueue.Dequeue());
        }

        private void DispatchJokerBox(int slotIndex)
        {
            GameObject instance = PoolManager.Instance.Get(_jokerQueue.Peek());
            if (instance == null) { return; }

            _jokerQueue.Dequeue();
            Box box = instance.GetComponent<Box>();

            if (!box.IsJoker)
            {
                Debug.LogError("Conveyor was given a joker box prefab whose Box is not marked as joker.", this);
            }

            PlaceBox(slotIndex, box, null);
        }

        private void PlaceBox(int slotIndex, Box box, ItemType type)
        {
            box.Setup(type);
            box.OnBoxFilled += HandleBoxCompleted;
            box.transform.SetPositionAndRotation(_path.EntryStart.position, box.BaseRotation);

            _slotBoxes[slotIndex] = box;
            _slotStates[slotIndex] = SlotState.Entering;
            _slotProgress[slotIndex] = 0f;
            _entryTimer = _config.BoxEntryDelay;

            OnBoxSpawned?.Invoke(box);
        }

        // Joker kutu banta fazladan bir kutu ekler. Level'in obje sayısı kutu hedefinin tam katı
        // olduğu için, joker bir tipe kilitlenirken aynı tipten doldurulmamış bir kutu iptal
        // edilmezse level sonunda objesi kalmayan bir kutu bantta dönmeye devam ederdi.
        private bool LockJokerBox(Box joker, ItemType type)
        {
            if (!ConsumeBoxCredit(type)) { return false; }

            joker.Setup(type);
            return true;
        }

        private bool HasBoxCredit(ItemType type)
        {
            return _boxQueue.Contains(type) || FindEmptyBoxSlot(type) >= 0;
        }

        private bool ConsumeBoxCredit(ItemType type)
        {
            if (TryRemoveQueuedBox(type)) { return true; }

            int slotIndex = FindEmptyBoxSlot(type);
            if (slotIndex < 0) { return false; }

            DetachBox(slotIndex);
            return true;
        }

        private bool TryRemoveQueuedBox(ItemType type)
        {
            _queueBuffer.Clear();
            bool isRemoved = false;

            while (_boxQueue.Count > 0)
            {
                ItemType queued = _boxQueue.Dequeue();

                if (!isRemoved && queued == type)
                {
                    isRemoved = true;
                    continue;
                }

                _queueBuffer.Add(queued);
            }

            for (int i = 0; i < _queueBuffer.Count; i++)
            {
                _boxQueue.Enqueue(_queueBuffer[i]);
            }

            _queueBuffer.Clear();
            return isRemoved;
        }

        private int FindEmptyBoxSlot(ItemType type)
        {
            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotStates[i] == SlotState.Empty) { continue; }

                Box box = _slotBoxes[i];
                if (box == null || box.IsJoker || !box.IsEmpty || box.Type != type) { continue; }

                return i;
            }

            return -1;
        }

        private void UpdateSlotBoxes()
        {
            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                Box box = _slotBoxes[i];
                if (box == null) { continue; }

                // Kutu tur boyunca dönmez; sabit yönünü korur ki üzerindeki ikon her zaman okunsun.
                // Bu yüzden yolun rotasyonu kullanılmaz, yalnızca konum yazılır.
                _path.Evaluate(_path.GetSlotDistance(i, _beltOffset), out Vector3 position, out _);
                position += Vector3.up * box.BaseHeight;

                if (_slotStates[i] == SlotState.Entering)
                {
                    _slotProgress[i] += Time.deltaTime / Mathf.Max(_config.BoxEntryDuration, Mathf.Epsilon);

                    if (_slotProgress[i] >= 1f)
                    {
                        _slotProgress[i] = 1f;
                        _slotStates[i] = SlotState.Riding;
                    }

                    position = Vector3.Lerp(_path.EntryStart.position, position, _slotProgress[i]);
                }

                box.transform.position = position;
            }
        }

        private void UpdateLeavingBoxes()
        {
            for (int i = _leavingBoxes.Count - 1; i >= 0; i--)
            {
                LeavingBox leaving = _leavingBoxes[i];
                leaving.Progress += Time.deltaTime / Mathf.Max(_config.BoxExitDuration, Mathf.Epsilon);

                if (leaving.Progress >= 1f)
                {
                    _leavingBoxes.RemoveAt(i);
                    ReleaseBox(leaving.Box);
                    CheckCompletion();
                    continue;
                }

                leaving.Box.transform.position = Vector3.Lerp(
                    leaving.From,
                    _path.ExitPoint.position,
                    leaving.Progress);

                _leavingBoxes[i] = leaving;
            }
        }

        private void DetachBox(int slotIndex)
        {
            Box box = _slotBoxes[slotIndex];

            _slotBoxes[slotIndex] = null;
            _slotStates[slotIndex] = SlotState.Empty;
            _slotProgress[slotIndex] = 0f;

            _leavingBoxes.Add(new LeavingBox { Box = box, From = box.transform.position, Progress = 0f });
        }

        private void HandleBoxCompleted(Box box)
        {
            // Tamamlanan kutu turu beklemez; anında banttan ayrılıp çıkış noktasına gider.
            int slotIndex = FindSlot(box);
            if (slotIndex >= 0) { DetachBox(slotIndex); }

            OnBoxFilled?.Invoke(box);
        }

        private void CheckCompletion()
        {
            if (_boxQueue.Count > 0 || _jokerQueue.Count > 0 || _leavingBoxes.Count > 0) { return; }

            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotBoxes[i] != null) { return; }
            }

            _isRunning = false;
            OnAllBoxesCompleted?.Invoke();
        }

        private bool HasCrossed(int slotIndex, float previousOffset, float travelled, float targetDistance)
        {
            if (travelled <= 0f) { return false; }

            float previousDistance = _path.GetSlotDistance(slotIndex, previousOffset);
            return Mathf.Repeat(targetDistance - previousDistance, _path.TotalLength) <= travelled;
        }

        /// <summary>
        /// Bandın kapasitesine sayılan kutular: henüz doldurulabilir olanlar. Oyuncu bir kutunun
        /// üçüncü objesini gönderdiği anda o kutu kapasiteden düşer ve yenisinin yolu açılır.
        /// </summary>
        private int GetFillableBoxCount()
        {
            int count = 0;

            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotBoxes[i] != null && !_slotBoxes[i].IsFilled) { count++; }
            }

            return count;
        }

        private int FindSlot(Box box)
        {
            for (int i = 0; i < _slotBoxes.Length; i++)
            {
                if (_slotBoxes[i] == box) { return i; }
            }

            return -1;
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

        private void ReleaseBox(Box box)
        {
            box.OnBoxFilled -= HandleBoxCompleted;
            PoolManager.Instance.Release(box.gameObject);
        }
    }
}
