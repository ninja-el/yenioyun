using System.Collections.Generic;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Yığın objelerinin doğduğu ve içinde kaldığı kutu alan. Alanın ölçüsü tek kaynaktır: hem
    /// doğma noktaları hem de objeleri içeride tutan görünmez duvarlar bu kutudan üretilir, böylece
    /// obje ölçeği değişince yığın alanın dışına taşmaz. Alan yalnızca Scene view'da görünür.
    /// </summary>
    public class StackArea : MonoBehaviour
    {
        // Alanın ölçüleri dünya birimidir; transform ölçeği hem duvarları hem doğma noktalarını
        // kaydırdığı için birden farklı ölçek uyarı ile bildirilir.
        private const float ScaleTolerance = 0.001f;

        // Boşluk ne kadar negatif olursa olsun her eksende objenin en az bu oranı kadar yer ayrılır.
        private const float MinClearanceRatio = 0.5f;

        // Obje bu altı dönüşten biriyle bir yüzü aşağı bakacak şekilde yatırılır.
        private static readonly Quaternion[] FaceDownRotations =
        {
            Quaternion.identity,
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(180f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, -90f),
        };

        /// <summary>Doğan objenin alana göre dönüşü.</summary>
        public enum SpawnRotation
        {
            Random,
            FaceDown,
        }

        [Tooltip("Alanın bu transform'a göre merkezi.")]
        [SerializeField] private Vector3 _center = new Vector3(0f, 3f, 0f);

        [Tooltip("Alanın iç ölçüsü. Objeler bu kutunun dışına çıkamaz.")]
        [SerializeField] private Vector3 _size = new Vector3(6f, 6f, 6f);

        [Tooltip("Üretilen görünmez duvarların kalınlığı. İnce duvarı hızlı obje delip geçebilir.")]
        [SerializeField, Min(0.01f)] private float _wallThickness = 1f;

        [Tooltip("Alanın üstü de kapatılsın mı? Kapalıysa objeler yukarıdan taşabilir.")]
        [SerializeField] private bool _hasCeiling = true;

        [Tooltip("Boş yer aranırken çakışma kontrolünün bakacağı layer'lar.")]
        [SerializeField] private LayerMask _occupantLayers = ~0;

        [Tooltip("Bir obje için boş nokta ararken denenecek rastgele aday sayısı. Her aday kendi dönüşüyle denenir. " +
            "Yüksek değer alan dolarken yer bulma şansını artırır; ilk dolumda aday bulunamazsa kalan objeler sırayla doğar.")]
        [SerializeField, Min(1)] private int _placementAttempts = 300;

        [Tooltip("Objelerin döndürülmüş collider kutuları arasında her eksende bırakılacak ek boşluk. 0'da kutular " +
            "değmeden en yakın durur. Negatif değer kutuları iç içe geçirir; objeler çarpışıp itişebilir.")]
        [SerializeField] private float _placementPadding = 0f;

        [Tooltip("Açıkken obje, denenen adaylar arasında boş olan en alçak noktaya konur; yığın tabandan başlayıp " +
            "sıkı bir öbek halinde doğar. Kapalıyken ilk boş aday alınır ve objeler tüm alana dağılır.")]
        [SerializeField] private bool _fillFromBottom = true;

        [Tooltip("Random: obje her yöne rastgele döner. FaceDown: obje bir yüzü aşağı bakacak şekilde yatar, " +
            "yalnızca dikey eksende rastgele döner; kutular düz durduğu için yığın daha sıkı olur.")]
        [SerializeField] private SpawnRotation _spawnRotation = SpawnRotation.FaceDown;

        [Tooltip("Tabandan doldururken adayların çekildiği bandın kalınlığı, objenin en uzun kenarı cinsinden. " +
            "Küçük değer alt katmanı daha sıkı doldurur ama bant daha sık yükselir.")]
        [SerializeField, Min(0.1f)] private float _bandHeightRatio = 1f;

        [Tooltip("Seçilen nokta boş kaldığı sürece bu adımla aşağı kaydırılıp alttaki boşluğa oturtulur; " +
            "objenin en uzun kenarı cinsinden. Küçük değer daha sıkı oturtur, daha çok kontrol yapar.")]
        [SerializeField, Min(0.01f)] private float _settleStepRatio = 0.05f;

        [Tooltip("Alanın Scene view'da çizileceği renk.")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);

        private readonly List<OrientedBox> _reservations = new List<OrientedBox>();

        // Tabandan doldururken bandın alt sınırı; objenin alt yüzünün alan tabanına göre yüksekliği.
        private float _placementFloor;

        private void Awake()
        {
            WarnOnScaledTransform();
            BuildWalls();
        }

        /// <summary>
        /// Yeni bir yerleştirme turu başlatır. Aynı turda doğan objelerin collider'ı henüz kapalı
        /// olduğu için çakışma kontrolü onları göremez; tur boyunca yerleri burada tutulur.
        /// </summary>
        public void BeginPlacement()
        {
            _reservations.Clear();
            _placementFloor = 0f;
        }

        /// <summary>
        /// Alanın içinde, verilen yarı ölçüdeki kutuya yer olan boş bir nokta ve dönüş bulup ayırır.
        /// Nokta collider merkezinin, dönüş objenin dünya uzayındaki hedefidir. Alan doluysa false
        /// döner; çağıran kalan objeleri bekletip boşluk açıldıkça yeniden denemelidir.
        /// </summary>
        public bool TryReserveSpot(Vector3 halfExtents, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            Vector3 padded = GetPaddedExtents(halfExtents);
            float longestEdge = 2f * Mathf.Max(padded.x, Mathf.Max(padded.y, padded.z));

            bool found = _fillFromBottom
                ? TryFindLowestSpot(padded, longestEdge, out Vector3 localSpot, out Quaternion localRotation)
                : TryFindSpotInBand(padded, 0f, _size.y, false, out localSpot, out localRotation);

            if (!found) { return false; }

            position = transform.TransformPoint(localSpot);
            rotation = transform.rotation * localRotation;
            _reservations.Add(new OrientedBox(position, padded, rotation));
            return true;
        }

        /// <summary>
        /// Verilen noktayı ve dönüşü, aynı turda ayrılmış başka bir yerle çakışmıyorsa ayırır. Shuffle'da
        /// rastgele yer bulamayan objeyi başka bir objenin boşalttığı yere göndermek için kullanılır.
        /// Nokta zaten bir objenin durduğu yer olduğundan duvar kontrolü yapılmaz; yeni dönüşte
        /// kutunun zemine ya da duvara biraz taşması olasıdır ve fizik açılınca çözülür.
        /// </summary>
        public bool TryReserveAt(Vector3 position, Vector3 halfExtents, Quaternion rotation)
        {
            OrientedBox box = new OrientedBox(position, GetPaddedExtents(halfExtents), rotation);
            if (IsReserved(box)) { return false; }

            _reservations.Add(box);
            return true;
        }

        /// <summary>Alanın dönüş ayarına göre dünya uzayında rastgele bir obje dönüşü.</summary>
        public Quaternion PickRotation()
        {
            return transform.rotation * PickLocalRotation();
        }

        // Bant tabandan yukarı yarım bant adımlarla kayar. Tabanı yalnızca yerleştirme başarılı
        // olunca kalıcı yükselir; sığmayan büyük obje sonraki küçük objelerin alt boşluklarını kapatmaz.
        private bool TryFindLowestSpot(Vector3 padded, float longestEdge, out Vector3 localSpot,
            out Quaternion localRotation)
        {
            float bandHeight = longestEdge * _bandHeightRatio;
            float floor = _placementFloor;

            while (floor < _size.y)
            {
                float ceiling = floor + bandHeight;

                if (TryFindSpotInBand(padded, floor, ceiling, true, out localSpot, out localRotation))
                {
                    localSpot = SettleDown(localSpot, localRotation, padded, longestEdge * _settleStepRatio);
                    _placementFloor = Mathf.Max(_placementFloor, floor);
                    return true;
                }

                if (ceiling >= _size.y) { break; }

                floor += bandHeight * 0.5f;
            }

            localSpot = Vector3.zero;
            localRotation = Quaternion.identity;
            return false;
        }

        // minBottom/maxBottom objenin alt yüzünün alan tabanına göre yüksekliğidir; dönen nokta ve
        // dönüş alanın yerel uzayındadır.
        private bool TryFindSpotInBand(Vector3 padded, float minBottom, float maxBottom, bool pickLowest,
            out Vector3 localSpot, out Quaternion localRotation)
        {
            localSpot = Vector3.zero;
            localRotation = Quaternion.identity;
            Vector3 halfSize = _size * 0.5f;
            float areaBottom = _center.y - halfSize.y;
            bool hasCandidate = false;
            float bestHeight = float.MaxValue;

            for (int attempt = 0; attempt < _placementAttempts; attempt++)
            {
                Quaternion candidateRotation = PickLocalRotation();
                Vector3 aligned = OrientedBox.GetAlignedExtents(padded, candidateRotation);
                Vector3 room = halfSize - aligned;

                if (room.x < 0f || room.y < 0f || room.z < 0f) { continue; }

                float maxCandidateBottom = Mathf.Min(maxBottom, _size.y - 2f * aligned.y);
                if (maxCandidateBottom < minBottom) { continue; }

                Vector3 candidate = new Vector3(
                    _center.x + Random.Range(-room.x, room.x),
                    areaBottom + Random.Range(minBottom, maxCandidateBottom) + aligned.y,
                    _center.z + Random.Range(-room.z, room.z));

                // Bulunandan yüksek aday kontrole bile girmez; tabandan doldururken maliyeti bu düşürür.
                if (hasCandidate && candidate.y >= bestHeight) { continue; }
                if (!IsFree(candidate, candidateRotation, padded)) { continue; }

                hasCandidate = true;
                bestHeight = candidate.y;
                localSpot = candidate;
                localRotation = candidateRotation;

                if (!pickLowest) { return true; }
            }

            return hasCandidate;
        }

        private Vector3 SettleDown(Vector3 localSpot, Quaternion localRotation, Vector3 padded, float step)
        {
            float alignedHeight = OrientedBox.GetAlignedExtents(padded, localRotation).y;
            float lowest = _center.y - _size.y * 0.5f + alignedHeight;

            while (localSpot.y - step >= lowest)
            {
                Vector3 lower = localSpot + Vector3.down * step;
                if (!IsFree(lower, localRotation, padded)) { break; }

                localSpot = lower;
            }

            return localSpot;
        }

        private bool IsFree(Vector3 localCenter, Quaternion localRotation, Vector3 padded)
        {
            Vector3 center = transform.TransformPoint(localCenter);
            Quaternion rotation = transform.rotation * localRotation;
            if (IsReserved(new OrientedBox(center, padded, rotation))) { return false; }

            return !Physics.CheckBox(center, padded, rotation, _occupantLayers, QueryTriggerInteraction.Ignore);
        }

        private Quaternion PickLocalRotation()
        {
            if (_spawnRotation == SpawnRotation.Random) { return Random.rotation; }

            Quaternion faceDown = FaceDownRotations[Random.Range(0, FaceDownRotations.Length)];
            return Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * faceDown;
        }

        // Negatif boşluk ince kenarları sıfırın altına itebilir; o zaman CheckBox negatif ölçü alır.
        // Her eksen bu yüzden objenin kendi ölçüsünün altına inse de pozitif kalır.
        private Vector3 GetPaddedExtents(Vector3 halfExtents)
        {
            return new Vector3(
                Mathf.Max(halfExtents.x + _placementPadding, halfExtents.x * MinClearanceRatio),
                Mathf.Max(halfExtents.y + _placementPadding, halfExtents.y * MinClearanceRatio),
                Mathf.Max(halfExtents.z + _placementPadding, halfExtents.z * MinClearanceRatio));
        }

        private bool IsReserved(in OrientedBox box)
        {
            for (int i = 0; i < _reservations.Count; i++)
            {
                if (_reservations[i].Intersects(box)) { return true; }
            }

            return false;
        }

        private void BuildWalls()
        {
            Vector3 half = _size * 0.5f;
            float offset = _wallThickness * 0.5f;
            Vector3 capSize = new Vector3(_size.x + _wallThickness * 2f, _wallThickness, _size.z + _wallThickness * 2f);

            AddWall(_center + Vector3.down * (half.y + offset), capSize);
            AddWall(_center + Vector3.left * (half.x + offset), new Vector3(_wallThickness, _size.y, _size.z));
            AddWall(_center + Vector3.right * (half.x + offset), new Vector3(_wallThickness, _size.y, _size.z));
            AddWall(_center + Vector3.back * (half.z + offset), new Vector3(_size.x, _size.y, _wallThickness));
            AddWall(_center + Vector3.forward * (half.z + offset), new Vector3(_size.x, _size.y, _wallThickness));

            if (!_hasCeiling) { return; }

            AddWall(_center + Vector3.up * (half.y + offset), capSize);
        }

        private void AddWall(Vector3 center, Vector3 size)
        {
            BoxCollider wall = gameObject.AddComponent<BoxCollider>();
            wall.center = center;
            wall.size = size;
        }

        private void WarnOnScaledTransform()
        {
            Vector3 scale = transform.lossyScale;

            if (Mathf.Abs(scale.x - 1f) < ScaleTolerance
                && Mathf.Abs(scale.y - 1f) < ScaleTolerance
                && Mathf.Abs(scale.z - 1f) < ScaleTolerance)
            {
                return;
            }

            Debug.LogWarning(
                "StackArea expects a transform scale of 1; resize the area with its size field instead.",
                this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireCube(_center, _size);

            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, _gizmoColor.a * 0.1f);
            Gizmos.DrawCube(_center, _size);

            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}
