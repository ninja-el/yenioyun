using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Bandın kapalı turu. Waypoint'lerden bir çevrim kurar, aralarını yumuşatıp örneklere böler ve
    /// tur üzerindeki bir mesafeyi poza çevirir. Oyun mantığı bilmez; kutuları <see cref="Conveyor"/>
    /// yürütür.
    /// </summary>
    public class ConveyorPath : MonoBehaviour
    {
        // Centripetal Catmull-Rom. Waypoint aralıkları eşit olmadığında (uzun kenar + kısa köşe)
        // düzgün parametreleme köşelerde taşma yapar; bu değer onu engeller.
        private const float CurveAlpha = 0.5f;

        [Tooltip("Turu çizen sıralı noktalar. Son nokta ilkine bağlanır, tur kapalıdır.")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("Turun bölüneceği eşit aralıklı slot sayısı.")]
        [SerializeField, Min(1)] private int _slotCount = 12;

        [Tooltip("İki waypoint arasının kaç parçaya bölüneceği. 1 = köşeler keskin, arttıkça yumuşar.")]
        [SerializeField, Min(1)] private int _smoothingSamples = 12;

        [Tooltip("Kutunun bant dışında doğduğu nokta. Tur üzerinde değildir.")]
        [SerializeField] private Transform _entryStart;

        [Tooltip("Kutunun banta katıldığı, tur üzerindeki nokta.")]
        [SerializeField] private Transform _entryPoint;

        [Tooltip("Tamamlanan kutunun banttan ayrılıp gittiği ve kaybolduğu nokta. Tur üzerinde değildir.")]
        [SerializeField] private Transform _exitPoint;

        [Tooltip("Kutunun tur zemininden yüksekliği.")]
        [SerializeField] private float _boxHeightOffset;


        [Tooltip("Turu, slotları ve çapaları Scene view'da çizer.")]
        [SerializeField] private bool _isGizmosEnabled = true;

        private Vector3[] _corners;
        private Vector3[] _samples;
        private float[] _sampleEnds;

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
                    $"{name}: ConveyorPath is incomplete. It needs at least 3 distinct waypoints and " +
                    "the EntryStart / EntryPoint / ExitPoint anchors.",
                    this);
            }
        }

        /// <summary>
        /// Waypoint konumlarını, eğriyi, tur uzunluğunu ve giriş mesafesini yeniden hesaplar.
        /// Tur kurulamazsa false döner ve log basmaz; çağıran karar verir.
        /// </summary>
        public bool Rebuild()
        {
            IsValid = false;

            if (!HasRequiredReferences()) { return false; }
            if (!CacheCorners()) { return false; }

            BuildSamples();
            EntryDistance = GetNearestDistance(_entryPoint.position);

            IsValid = TotalLength > 0f;
            return IsValid;
        }

        /// <summary>Tur üzerindeki bir mesafeyi dünya pozuna çevirir. Mesafe tur uzunluğuna göre sarılır.</summary>
        public void Evaluate(float distance, out Vector3 position, out Quaternion rotation)
        {
            distance = Mathf.Repeat(distance, TotalLength);

            int step = FindStep(distance);
            float stepStart = step == 0 ? 0f : _sampleEnds[step - 1];
            float stepLength = _sampleEnds[step] - stepStart;
            float t = stepLength > 0f ? (distance - stepStart) / stepLength : 0f;

            Vector3 from = _samples[step];
            Vector3 to = _samples[(step + 1) % _samples.Length];

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

        private bool CacheCorners()
        {
            // Üst üste binen noktalar sıfır uzunlukta parça üretir ve eğri orada yön bilgisini
            // kaybeder. Tur zaten kapalı olduğu için ilkiyle aynı yere konan son nokta da elenir.
            int count = 0;
            Vector3[] unique = new Vector3[_waypoints.Length];

            for (int i = 0; i < _waypoints.Length; i++)
            {
                Vector3 point = _waypoints[i].position;
                if (count > 0 && (point - unique[count - 1]).sqrMagnitude <= Mathf.Epsilon) { continue; }

                unique[count] = point;
                count++;
            }

            if (count > 1 && (unique[0] - unique[count - 1]).sqrMagnitude <= Mathf.Epsilon) { count--; }
            if (count < 3) { return false; }

            if (_corners == null || _corners.Length != count) { _corners = new Vector3[count]; }
            System.Array.Copy(unique, _corners, count);

            return true;
        }

        private void BuildSamples()
        {
            int cornerCount = _corners.Length;
            int sampleCount = cornerCount * _smoothingSamples;

            if (_samples == null || _samples.Length != sampleCount)
            {
                _samples = new Vector3[sampleCount];
                _sampleEnds = new float[sampleCount];
            }

            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 p0 = _corners[(i - 1 + cornerCount) % cornerCount];
                Vector3 p1 = _corners[i];
                Vector3 p2 = _corners[(i + 1) % cornerCount];
                Vector3 p3 = _corners[(i + 2) % cornerCount];

                for (int s = 0; s < _smoothingSamples; s++)
                {
                    _samples[i * _smoothingSamples + s] = Spline(p0, p1, p2, p3, (float)s / _smoothingSamples);
                }
            }

            float total = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                total += Vector3.Distance(_samples[i], _samples[(i + 1) % sampleCount]);
                _sampleEnds[i] = total;
            }

            TotalLength = total;
        }

        private int FindStep(float distance)
        {
            int low = 0;
            int high = _sampleEnds.Length - 1;

            while (low < high)
            {
                int middle = (low + high) / 2;
                if (_sampleEnds[middle] < distance) { low = middle + 1; } else { high = middle; }
            }

            return low;
        }

        private float GetNearestDistance(Vector3 worldPoint)
        {
            float bestDistance = 0f;
            float bestSqrMagnitude = float.MaxValue;

            for (int i = 0; i < _samples.Length; i++)
            {
                Vector3 from = _samples[i];
                Vector3 step = _samples[(i + 1) % _samples.Length] - from;
                float stepSqrMagnitude = step.sqrMagnitude;
                if (stepSqrMagnitude <= 0f) { continue; }

                float t = Mathf.Clamp01(Vector3.Dot(worldPoint - from, step) / stepSqrMagnitude);
                float sqrMagnitude = (worldPoint - (from + step * t)).sqrMagnitude;
                if (sqrMagnitude >= bestSqrMagnitude) { continue; }

                bestSqrMagnitude = sqrMagnitude;
                float stepStart = i == 0 ? 0f : _sampleEnds[i - 1];
                bestDistance = stepStart + Mathf.Sqrt(stepSqrMagnitude) * t;
            }

            return bestDistance;
        }

        private static Vector3 Spline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t0 = 0f;
            float t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), CurveAlpha);
            float t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), CurveAlpha);
            float t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), CurveAlpha);

            float time = Mathf.Lerp(t1, t2, t);

            Vector3 a1 = Remap(p0, p1, t0, t1, time);
            Vector3 a2 = Remap(p1, p2, t1, t2, time);
            Vector3 a3 = Remap(p2, p3, t2, t3, time);
            Vector3 b1 = Remap(a1, a2, t0, t2, time);
            Vector3 b2 = Remap(a2, a3, t1, t3, time);

            return Remap(b1, b2, t1, t2, time);
        }

        private static Vector3 Remap(Vector3 from, Vector3 to, float fromTime, float toTime, float time)
        {
            if (Mathf.Approximately(fromTime, toTime)) { return from; }

            return Vector3.LerpUnclamped(from, to, (time - fromTime) / (toTime - fromTime));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_isGizmosEnabled) { return; }

            // Oyun sırasında tur zaten kurulu; edit mode'da noktalar sürüklendikçe yeniden kurulur.
            if (!Application.isPlaying && !Rebuild()) { return; }
            if (!IsValid) { return; }

            Gizmos.color = Color.white;
            for (int i = 0; i < _samples.Length; i++)
            {
                Gizmos.DrawLine(_samples[i], _samples[(i + 1) % _samples.Length]);
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _corners.Length; i++)
            {
                Gizmos.DrawWireSphere(_corners[i], 0.12f);
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
