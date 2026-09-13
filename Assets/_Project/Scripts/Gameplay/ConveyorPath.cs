using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Bandın kapalı turu. Sıralı waypoint'lerden bir çevrim kurar ve tur üzerindeki bir mesafeyi
    /// poza çevirir. Oyun mantığı bilmez; kutuları <see cref="Conveyor"/> yürütür.
    /// </summary>
    public class ConveyorPath : MonoBehaviour
    {
        [Tooltip("Turu çizen sıralı noktalar. Son nokta ilkine bağlanır, tur kapalıdır.")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("Turun bölüneceği eşit aralıklı slot sayısı.")]
        [SerializeField, Min(1)] private int _slotCount = 12;

        [Tooltip("Kutunun bant dışında doğduğu nokta.")]
        [SerializeField] private Transform _entryStart;

        [Tooltip("Kutunun banta katıldığı, tur üzerindeki nokta.")]
        [SerializeField] private Transform _entryPoint;

        [Tooltip("Tamamlanan kutunun banttan ayrılıp gittiği ve kaybolduğu nokta. Tur üzerinde değildir.")]
        [SerializeField] private Transform _exitPoint;

        [Tooltip("Kutunun tur zemininden yüksekliği.")]
        [SerializeField] private float _boxHeightOffset;

        [Tooltip("Turu, slotları ve çapaları Scene view'da çizer.")]
        [SerializeField] private bool _isGizmosEnabled = true;

        private Vector3[] _points;
        private float[] _segmentEnds;

        /// <summary>Turun kaç eşit slota bölündüğü. Kutular bu slotlara bağlanır.</summary>
        public int SlotCount => _slotCount;

        /// <summary>Turun toplam uzunluğu.</summary>
        public float TotalLength { get; private set; }

        /// <summary>Kutunun banta katıldığı noktanın tur üzerindeki mesafesi.</summary>
        public float EntryDistance { get; private set; }

        public Transform EntryStart => _entryStart;
        public Transform ExitPoint => _exitPoint;

        /// <summary>Tur kurulabildi mi? Eksik waypoint veya çapa varsa false.</summary>
        public bool IsValid { get; private set; }

        private void Awake()
        {
            if (!Rebuild())
            {
                Debug.LogError(
                    $"{name}: ConveyorPath is incomplete. It needs at least 3 waypoints and the " +
                    "EntryStart / EntryPoint / ExitPoint anchors.",
                    this);
            }
        }

        /// <summary>
        /// Waypoint konumlarını, tur uzunluğunu ve çapa mesafelerini yeniden hesaplar.
        /// Tur kurulamazsa false döner ve log basmaz; çağıran karar verir.
        /// </summary>
        public bool Rebuild()
        {
            IsValid = false;

            if (!HasRequiredReferences()) { return false; }

            CachePoints();
            EntryDistance = GetNearestDistance(_entryPoint.position);

            IsValid = TotalLength > 0f;
            return IsValid;
        }

        /// <summary>Tur üzerindeki bir mesafeyi dünya pozuna çevirir. Mesafe tur uzunluğuna göre sarılır.</summary>
        public void Evaluate(float distance, out Vector3 position, out Quaternion rotation)
        {
            distance = Mathf.Repeat(distance, TotalLength);

            int segment = 0;
            while (segment < _segmentEnds.Length - 1 && distance > _segmentEnds[segment])
            {
                segment++;
            }

            float segmentStart = segment == 0 ? 0f : _segmentEnds[segment - 1];
            float segmentLength = _segmentEnds[segment] - segmentStart;

            Vector3 from = _points[segment];
            Vector3 to = _points[(segment + 1) % _points.Length];
            float t = segmentLength > 0f ? (distance - segmentStart) / segmentLength : 0f;

            position = Vector3.Lerp(from, to, t) + Vector3.up * _boxHeightOffset;

            Vector3 direction = to - from;
            rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : Quaternion.identity;
        }

        /// <summary>Verilen slotun tur üzerindeki mesafesi. Offset bir tam turun kesridir.</summary>
        public float GetSlotDistance(int slotIndex, float beltOffset)
        {
            return ((float)slotIndex / _slotCount + beltOffset) * TotalLength;
        }

        private bool HasRequiredReferences()
        {
            if (_waypoints == null || _waypoints.Length < 3) { return false; }

            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) { return false; }
            }

            return _entryStart != null && _entryPoint != null && _exitPoint != null;
        }

        private void CachePoints()
        {
            if (_points == null || _points.Length != _waypoints.Length)
            {
                _points = new Vector3[_waypoints.Length];
                _segmentEnds = new float[_waypoints.Length];
            }

            for (int i = 0; i < _waypoints.Length; i++)
            {
                _points[i] = _waypoints[i].position;
            }

            float total = 0f;
            for (int i = 0; i < _points.Length; i++)
            {
                total += Vector3.Distance(_points[i], _points[(i + 1) % _points.Length]);
                _segmentEnds[i] = total;
            }

            TotalLength = total;
        }

        private float GetNearestDistance(Vector3 worldPoint)
        {
            float bestDistance = 0f;
            float bestSqrMagnitude = float.MaxValue;

            for (int i = 0; i < _points.Length; i++)
            {
                Vector3 from = _points[i];
                Vector3 to = _points[(i + 1) % _points.Length];
                Vector3 segment = to - from;
                float segmentSqrMagnitude = segment.sqrMagnitude;
                if (segmentSqrMagnitude <= 0f) { continue; }

                float t = Mathf.Clamp01(Vector3.Dot(worldPoint - from, segment) / segmentSqrMagnitude);
                Vector3 projected = from + segment * t;
                float sqrMagnitude = (worldPoint - projected).sqrMagnitude;

                if (sqrMagnitude >= bestSqrMagnitude) { continue; }

                bestSqrMagnitude = sqrMagnitude;
                float segmentStart = i == 0 ? 0f : _segmentEnds[i - 1];
                bestDistance = segmentStart + Mathf.Sqrt(segmentSqrMagnitude) * t;
            }

            return bestDistance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_isGizmosEnabled) { return; }

            // Oyun sırasında tur zaten kurulu; edit mode'da noktalar sürüklendikçe yeniden kurulur.
            if (!Application.isPlaying && !Rebuild()) { return; }
            if (!IsValid) { return; }

            Gizmos.color = Color.white;
            for (int i = 0; i < _points.Length; i++)
            {
                Gizmos.DrawLine(_points[i], _points[(i + 1) % _points.Length]);
            }

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _slotCount; i++)
            {
                Evaluate(GetSlotDistance(i, 0f), out Vector3 position, out Quaternion rotation);
                Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            }

            Evaluate(EntryDistance, out Vector3 entry, out _);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(_entryStart.position, entry);
            UnityEditor.Handles.Label(entry, "ENTRY");

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_exitPoint.position, 0.5f);
            UnityEditor.Handles.Label(_exitPoint.position, "EXIT");
        }
#endif
    }
}
