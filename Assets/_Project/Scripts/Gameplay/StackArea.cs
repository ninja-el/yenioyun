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
        private struct Reservation
        {
            public Vector3 Position;
            public float Radius;
        }

        // Alanın ölçüleri dünya birimidir; transform ölçeği hem duvarları hem doğma noktalarını
        // kaydırdığı için birden farklı ölçek uyarı ile bildirilir.
        private const float ScaleTolerance = 0.001f;

        // Boşluk ne kadar negatif olursa olsun obje yarıçapının en az bu oranı kadar yer ayrılır.
        private const float MinClearanceRatio = 0.5f;

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

        [Tooltip("Bir obje için boş nokta ararken denenecek rastgele aday sayısı. Yüksek değer alan dolarken " +
            "yer bulma şansını artırır; ilk dolumda bir aday bulunamazsa kalan objeler sırayla doğar.")]
        [SerializeField, Min(1)] private int _placementAttempts = 300;

        [Tooltip("Objelerin sınır küreleri arasında ve duvar diplerinde bırakılacak ek boşluk. Küre collider'ı tamamen " +
            "sardığı için 0'da objeler değmeden en yakın durur. Negatif değer küreleri iç içe geçirir; objeler çarpışıp itişebilir.")]
        [SerializeField] private float _placementPadding = 0f;

        [Tooltip("Açıkken obje, denenen adaylar arasında boş olan en alçak noktaya konur; yığın tabandan başlayıp " +
            "sıkı bir öbek halinde doğar. Kapalıyken ilk boş aday alınır ve objeler tüm alana dağılır.")]
        [SerializeField] private bool _fillFromBottom = true;

        [Tooltip("Alanın Scene view'da çizileceği renk.")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);

        private readonly List<Reservation> _reservations = new List<Reservation>();

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
        }

        /// <summary>
        /// Alanın içinde, verilen yarıçapa yer olan boş bir nokta bulup ayırır. Alan doluysa false
        /// döner; çağıran kalan objeleri bekletip boşluk açıldıkça yeniden denemelidir.
        /// </summary>
        public bool TryReserveSpot(float radius, out Vector3 position)
        {
            position = Vector3.zero;

            float clearance = GetClearance(radius);
            Vector3 extents = _size * 0.5f - Vector3.one * clearance;

            if (extents.x < 0f || extents.y < 0f || extents.z < 0f) { return false; }

            bool hasCandidate = false;
            float bestHeight = float.MaxValue;

            for (int attempt = 0; attempt < _placementAttempts; attempt++)
            {
                Vector3 localCandidate = _center + new Vector3(
                    Random.Range(-extents.x, extents.x),
                    Random.Range(-extents.y, extents.y),
                    Random.Range(-extents.z, extents.z));

                // Bulunandan yüksek aday kontrole bile girmez; tabandan doldururken maliyeti bu düşürür.
                if (hasCandidate && localCandidate.y >= bestHeight) { continue; }

                Vector3 candidate = transform.TransformPoint(localCandidate);
                if (IsReserved(candidate, clearance)) { continue; }
                if (Physics.CheckSphere(candidate, clearance, _occupantLayers, QueryTriggerInteraction.Ignore)) { continue; }

                hasCandidate = true;
                bestHeight = localCandidate.y;
                position = candidate;

                if (!_fillFromBottom) { break; }
            }

            if (!hasCandidate) { return false; }

            _reservations.Add(new Reservation { Position = position, Radius = clearance });
            return true;
        }

        /// <summary>
        /// Verilen noktayı, aynı turda ayrılmış başka bir yerle çakışmıyorsa ayırır. Shuffle'da
        /// rastgele yer bulamayan objeyi başka bir objenin boşalttığı yere göndermek için kullanılır.
        /// Nokta zaten bir objenin durduğu yer olduğundan duvar kontrolü yapılmaz; yerde duran
        /// objenin küresi zemine değdiği için o kontrol her noktayı reddederdi.
        /// </summary>
        public bool TryReserveAt(Vector3 position, float radius)
        {
            float clearance = GetClearance(radius);
            if (IsReserved(position, clearance)) { return false; }

            _reservations.Add(new Reservation { Position = position, Radius = clearance });
            return true;
        }

        // Negatif boşluk küçük objelerde mesafeyi sıfırın altına itebilir; o zaman obje duvarın içinde
        // doğar ve CheckSphere negatif yarıçap alır. Mesafe bu yüzden objenin yarıçapının altına inse de pozitif kalır.
        private float GetClearance(float radius)
        {
            return Mathf.Max(radius + _placementPadding, radius * MinClearanceRatio);
        }

        private bool IsReserved(Vector3 candidate, float clearance)
        {
            for (int i = 0; i < _reservations.Count; i++)
            {
                Reservation reservation = _reservations[i];
                float minimumDistance = reservation.Radius + clearance;

                if ((reservation.Position - candidate).sqrMagnitude < minimumDistance * minimumDistance)
                {
                    return true;
                }
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
