using DG.Tweening;
using MatchPack.Core;
using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Yığındaki tek obje. Yığın fiziksel olduğu için obje bir rigidbody taşır; havuza dönerken
    /// hızı sıfırlanmazsa bir sonraki kullanımda eski momentumuyla geri gelir.
    /// </summary>
    public class StackItem : MonoBehaviour, IPoolable
    {
        [Tooltip("Dokunuş raycast'inin çarpacağı collider.")]
        [SerializeField] private Collider _collider;

        [Tooltip("Yığındaki fiziksel davranışı sağlayan rigidbody.")]
        [SerializeField] private Rigidbody _rigidbody;

        private float _boundingRadius;
        private RigidbodyInterpolation _interpolation;

        public ItemType Type { get; private set; }

        /// <summary>Objenin herhangi bir dönüşte kaplayabileceği yarıçap. Doğma aralığı bundan hesaplanır.</summary>
        public float BoundingRadius => _boundingRadius;

        /// <summary>Obje fiziksel olarak durulmuş mu? Yığının oturduğunu anlamak için kullanılır.</summary>
        public bool IsResting => _rigidbody.IsSleeping();

        private void Awake()
        {
            // Collider prefab'ta açık ve obje dönmemişken ölçülür; sonradan rotasyon bounds'u bozar.
            _boundingRadius = _collider.bounds.extents.magnitude;
            _interpolation = _rigidbody.interpolation;
        }

        /// <summary>Objeyi bir tipe hazırlar. Havuzdan alındıktan sonra çağrılır.</summary>
        public void Setup(ItemType type)
        {
            Type = type;
        }

        /// <summary>
        /// Objeyi fiziğe katar veya fizik dışına alır. Kutuya uçarken kapatılır; açık kalırsa
        /// yerçekimi tween ile kavga eder ve uçan obje yığını iter.
        /// </summary>
        public void SetSimulated(bool isSimulated)
        {
            if (!isSimulated && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            // Interpolasyon yalnızca fizik objeyi sürerken doğrudur; kapalıyken transform'a yazılan
            // her poz bir sonraki karede rigidbody'nin eski pozuyla geri alınır.
            _rigidbody.interpolation = isSimulated ? _interpolation : RigidbodyInterpolation.None;
            _rigidbody.isKinematic = !isSimulated;
            _collider.enabled = isSimulated;
        }

        /// <summary>
        /// Objeyi rigidbody ile birlikte ışınlar. Yalnızca transform'a yazmak yetmez: fizik motoru
        /// pozu bir sonraki sync'e kadar eski değerinde tutar ve objeyi havuzdaki konumuna geri çeker.
        /// Çağrı öncesi obje <see cref="SetSimulated"/> ile fizik dışına alınmış olmalıdır.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        public void OnSpawned()
        {
            // Havuzdan çıkan obje havuz kökünün konumundadır; fizik ancak çağıran onu yerleştirdikten sonra açılır.
            SetSimulated(false);
        }

        public void OnDespawned()
        {
            transform.DOKill();
            SetSimulated(false);
            Type = null;
        }
    }
}
