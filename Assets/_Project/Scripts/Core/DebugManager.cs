using System.Collections.Generic;
using MatchPack.Gameplay;
using MatchPack.Meta;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatchPack.Core
{
    /// <summary>
    /// Dokunuşları ve level durumunu Scene view'da gizmo olarak çizer, ayrıca test kısayollarını
    /// işletir. Oyun mantığına hiç dokunmaz; yalnızca event dinler ve var olan public API'yi çağırır.
    /// </summary>
    public class DebugManager : MonoBehaviour
    {
        private enum TapOutcome
        {
            NoHit,
            Unresolved,
            Matched,
            Missed
        }

        private struct TapRecord
        {
            public Ray Ray;
            public Vector3 Point;
            public Vector3 ProbeEnd;
            public bool IsBoxCast;
            public Vector3 HalfExtents;
            public Quaternion Orientation;
            public string TypeId;
            public TapOutcome Outcome;
            public float ExpireTime;
        }

        public static DebugManager Instance { get; private set; }

        [Tooltip("Gizmo çizimini açar. Kapalıyken dokunuşlar kaydedilmez ve hiçbir şey çizilmez.")]
        [SerializeField] private bool _isGizmosEnabled = true;

        [Tooltip("Aynı anda ekranda tutulacak dokunuş sayısı.")]
        [SerializeField, Min(1)] private int _tapHistoryCount = 5;

        [Tooltip("Bir dokunuş çiziminin ekranda kalma süresi (saniye).")]
        [SerializeField, Min(0.1f)] private float _tapLifetimeSeconds = 3f;

        [Tooltip("Isabet noktasına çizilen kürenin yarıçapı.")]
        [SerializeField, Min(0.01f)] private float _hitMarkerRadius = 0.12f;

        [Tooltip("Isabet olmayan probun çizileceği uzunluk.")]
        [SerializeField, Min(1f)] private float _missedRayLength = 40f;

        [Tooltip("Görseli olmayan kutuların yerine çizilecek tel kutunun ölçüsü.")]
        [SerializeField] private Vector3 _boxMarkerSize = Vector3.one;

        [Tooltip("Eşleşen dokunuşun rengi.")]
        [SerializeField] private Color _matchedColor = Color.green;

        [Tooltip("Uygun kutu bulunamayan dokunuşun rengi.")]
        [SerializeField] private Color _missedColor = Color.red;

        [Tooltip("Objeye değen ama MatchResolver'a ulaşmayan dokunuşun rengi.")]
        [SerializeField] private Color _unresolvedColor = Color.yellow;

        [Tooltip("Banttaki kutu işaretlerinin rengi.")]
        [SerializeField] private Color _boxMarkerColor = Color.cyan;

        [Header("Test kısayolları")]
        [Tooltip("Klavye kısayollarını açar. Yayın sürümünde kapatılır.")]
        [SerializeField] private bool _areDebugKeysEnabled = true;

        [Tooltip("Leveli anında kazandıran tuş.")]
        [SerializeField] private Key _winKey = Key.F1;

        [Tooltip("Leveli anında kaybettiren tuş.")]
        [SerializeField] private Key _loseKey = Key.F2;

        [Tooltip("Sınırsız canı açıp kapatan tuş.")]
        [SerializeField] private Key _infiniteLivesKey = Key.F3;

        [Tooltip("Gold ekleyen tuş.")]
        [SerializeField] private Key _addGoldKey = Key.F4;

        [Tooltip("Bir can ekleyen tuş.")]
        [SerializeField] private Key _addLifeKey = Key.F5;

        [Tooltip("Gold tuşuna basıldığında eklenecek miktar.")]
        [SerializeField, Min(1)] private int _debugGoldAmount = 1000;

        [Tooltip("Sınırsız can tuşuna basıldığında verilecek süre (saat).")]
        [SerializeField, Min(0.1f)] private float _debugInfiniteLivesHours = 24f;

        private readonly List<TapRecord> _taps = new List<TapRecord>();
        private readonly List<Box> _boxes = new List<Box>();

        private ItemStack _itemStack;
        private Conveyor _conveyor;
        private MatchResolver _matchResolver;
        private bool _hasLevelComponents;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Diğer manager'ların aksine kendi objesine sahip değil; yalnızca bileşen silinir.
                Destroy(this);
                return;
            }

            Instance = this;

            // Bileşenler aynı objede durduğu için Inspector bağlaması gerekmez.
            _itemStack = GetComponent<ItemStack>();
            _conveyor = GetComponent<Conveyor>();
            _matchResolver = GetComponent<MatchResolver>();
            _hasLevelComponents = _itemStack != null && _conveyor != null && _matchResolver != null;

            if (!_hasLevelComponents)
            {
                Debug.LogError(
                    "DebugManager must sit on the same GameObject as ItemStack, Conveyor and MatchResolver.",
                    this);
            }
        }

        private void Start()
        {
            InputManager.Instance.OnTapProbed += HandleTapProbed;
            SceneLoader.Instance.OnBeforeLevelTeardown += HandleBeforeLevelTeardown;

            if (!_hasLevelComponents) { return; }

            _matchResolver.OnItemMatched += HandleItemMatched;
            _matchResolver.OnItemMissed += HandleItemMissed;
            _conveyor.OnBoxSpawned += HandleBoxSpawned;
            _conveyor.OnBoxFilled += HandleBoxFilled;
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnTapProbed -= HandleTapProbed;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnBeforeLevelTeardown -= HandleBeforeLevelTeardown;
            }

            if (_hasLevelComponents)
            {
                _matchResolver.OnItemMatched -= HandleItemMatched;
                _matchResolver.OnItemMissed -= HandleItemMissed;
                _conveyor.OnBoxSpawned -= HandleBoxSpawned;
                _conveyor.OnBoxFilled -= HandleBoxFilled;
            }

            if (Instance == this) { Instance = null; }
        }

        private void Update()
        {
            ProcessDebugKeys();

            // Time.timeScale sıfırlansa bile çizimler yaşlanmalı.
            while (_taps.Count > 0 && Time.unscaledTime >= _taps[0].ExpireTime)
            {
                _taps.RemoveAt(0);
            }
        }

        /// <summary>Oynanan leveli anında kazandırır. Level sonu panelini test etmek içindir.</summary>
        public void TriggerWin()
        {
            if (GameManager.Instance == null) { return; }

            GameManager.Instance.CompleteLevel();
        }

        /// <summary>Oynanan leveli anında kaybettirir. Level sonu panelini test etmek içindir.</summary>
        public void TriggerLose()
        {
            if (GameManager.Instance == null) { return; }

            GameManager.Instance.FailLevel();
        }

        /// <summary>Sınırsız canı açar, açıksa kapatır. Market panelini test etmek içindir.</summary>
        public void ToggleInfiniteLives()
        {
            if (EconomyManager.Instance == null) { return; }

            if (EconomyManager.Instance.HasInfiniteLives)
            {
                EconomyManager.Instance.ClearInfiniteLives();
                return;
            }

            EconomyManager.Instance.GrantInfiniteLives(_debugInfiniteLivesHours);
        }

        /// <summary>Test için gold ekler.</summary>
        public void AddDebugGold()
        {
            if (EconomyManager.Instance == null) { return; }

            EconomyManager.Instance.AddGold(_debugGoldAmount);
        }

        /// <summary>Test için bir can ekler.</summary>
        public void AddDebugLife()
        {
            if (EconomyManager.Instance == null) { return; }

            EconomyManager.Instance.AddLives(1);
        }

        private void ProcessDebugKeys()
        {
            if (!_areDebugKeysEnabled || Keyboard.current == null) { return; }

            if (Keyboard.current[_winKey].wasPressedThisFrame) { TriggerWin(); }
            if (Keyboard.current[_loseKey].wasPressedThisFrame) { TriggerLose(); }
            if (Keyboard.current[_infiniteLivesKey].wasPressedThisFrame) { ToggleInfiniteLives(); }
            if (Keyboard.current[_addGoldKey].wasPressedThisFrame) { AddDebugGold(); }
            if (Keyboard.current[_addLifeKey].wasPressedThisFrame) { AddDebugLife(); }
        }

        private void HandleTapProbed(InputManager.TapProbe probe)
        {
            if (!_isGizmosEnabled) { return; }

            StackItem item = probe.HasHit ? probe.Hit.collider.GetComponentInParent<StackItem>() : null;
            Ray ray = probe.Ray;
            Vector3 missedEnd = ray.origin + ray.direction * _missedRayLength;

            TapRecord record = new TapRecord
            {
                Ray = ray,
                Point = probe.HasHit ? probe.Hit.point : missedEnd,
                // Kutu prob çarptığı yerde durur; kutunun o andaki merkezi isabet noktasının değil,
                // ışın ekseninin üzerindedir. Kutu bu yüzden hit.point'e değil buraya çizilir.
                ProbeEnd = probe.HasHit ? ray.origin + ray.direction * probe.Hit.distance : missedEnd,
                IsBoxCast = probe.IsBoxCast,
                HalfExtents = probe.HalfExtents,
                Orientation = probe.Orientation,
                TypeId = item != null && item.Type != null ? item.Type.Id : null,
                Outcome = probe.HasHit ? TapOutcome.Unresolved : TapOutcome.NoHit,
                ExpireTime = Time.unscaledTime + _tapLifetimeSeconds
            };

            while (_taps.Count >= _tapHistoryCount) { _taps.RemoveAt(0); }
            _taps.Add(record);
        }

        private void HandleItemMatched(StackItem item)
        {
            SetLastOutcome(TapOutcome.Matched);
        }

        private void HandleItemMissed(StackItem item)
        {
            SetLastOutcome(TapOutcome.Missed);
        }

        private void SetLastOutcome(TapOutcome outcome)
        {
            if (!_isGizmosEnabled || _taps.Count == 0) { return; }

            int lastIndex = _taps.Count - 1;
            TapRecord record = _taps[lastIndex];
            record.Outcome = outcome;
            _taps[lastIndex] = record;
        }

        private void HandleBoxSpawned(Box box)
        {
            if (!_isGizmosEnabled) { return; }

            _boxes.Add(box);
        }

        private void HandleBoxFilled(Box box)
        {
            _boxes.Remove(box);
        }

        private void HandleBeforeLevelTeardown()
        {
            _taps.Clear();
            _boxes.Clear();
        }

#if UNITY_EDITOR
        private readonly System.Text.StringBuilder _statusBuilder = new System.Text.StringBuilder();

        private void OnDrawGizmos()
        {
            if (!_isGizmosEnabled || !Application.isPlaying) { return; }

            DrawStatus();
            DrawBoxes();
            DrawTaps();
        }

        private void DrawStatus()
        {
            _statusBuilder.Clear();

            _statusBuilder.Append("state: ")
                .Append(GameManager.Instance != null ? GameManager.Instance.State.ToString() : "no GameManager");

            _statusBuilder.Append("\ninput: ")
                .Append(InputManager.Instance == null ? "no InputManager"
                    : InputManager.Instance.IsEnabled ? "enabled" : "DISABLED");

            if (InputManager.Instance != null)
            {
                _statusBuilder.Append(InputManager.Instance.IsBoxCastEnabled
                    ? $"  probe: boxcast {InputManager.Instance.BoxCastWidth:0.00}"
                    : "  probe: raycast")
                    .Append($" x{InputManager.Instance.MaxProbeHits}");
            }

            if (!_hasLevelComponents)
            {
                UnityEditor.Handles.Label(transform.position, _statusBuilder.ToString());
                return;
            }

            _statusBuilder.Append("\nstack: ")
                .Append(_itemStack.IsSettled ? "settled" : "NOT SETTLED")
                .Append(" (").Append(_itemStack.Items.Count).Append(" items)");

            _statusBuilder.Append("\nboxes on belt: ").Append(_boxes.Count)
                .Append("  queued: ").Append(_conveyor.QueuedBoxCount);

            for (int i = 0; i < _boxes.Count; i++)
            {
                Box box = _boxes[i];
                _statusBuilder.Append("\n  slot ").Append(i).Append(": ")
                    .Append(box.Type != null ? box.Type.Id : "null")
                    .Append(box.IsFilled ? " [full]" : string.Empty);
            }

            UnityEditor.Handles.Label(transform.position, _statusBuilder.ToString());
        }

        private void DrawBoxes()
        {
            Gizmos.color = _boxMarkerColor;

            for (int i = 0; i < _boxes.Count; i++)
            {
                Box box = _boxes[i];
                if (box == null) { continue; }

                Vector3 position = box.transform.position;
                Gizmos.DrawWireCube(position, _boxMarkerSize);
                UnityEditor.Handles.Label(position + Vector3.up * _boxMarkerSize.y, box.Type != null ? box.Type.Id : "null");
            }
        }

        private void DrawTaps()
        {
            for (int i = 0; i < _taps.Count; i++)
            {
                TapRecord tap = _taps[i];

                Gizmos.color = GetOutcomeColor(tap.Outcome);
                Gizmos.DrawLine(tap.Ray.origin, tap.ProbeEnd);
                Gizmos.DrawSphere(tap.Point, _hitMarkerRadius);

                if (tap.IsBoxCast) { DrawProbeBox(tap); }

                UnityEditor.Handles.Label(tap.Point, GetOutcomeLabel(tap));
            }
        }

        private static void DrawProbeBox(TapRecord tap)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(tap.ProbeEnd, tap.Orientation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, tap.HalfExtents * 2f);
            Gizmos.matrix = previousMatrix;
        }

        private Color GetOutcomeColor(TapOutcome outcome)
        {
            switch (outcome)
            {
                case TapOutcome.Matched: return _matchedColor;
                case TapOutcome.Unresolved: return _unresolvedColor;
                default: return _missedColor;
            }
        }

        private static string GetOutcomeLabel(TapRecord tap)
        {
            switch (tap.Outcome)
            {
                case TapOutcome.NoHit: return "NO HIT (probe hit nothing on the item layers)";
                case TapOutcome.Unresolved: return $"{tap.TypeId} - NOT RESOLVED (MatchResolver guard blocked it)";
                case TapOutcome.Matched: return $"{tap.TypeId} - MATCHED";
                default: return $"{tap.TypeId} - NO MATCHING BOX";
            }
        }
#endif
    }
}
