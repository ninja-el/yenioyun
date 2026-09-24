using System;
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
        private Vector3 _baseScale;
        private RigidbodyInterpolation _interpolation;
        private Sequence _moveSequence;

        public ItemType Type { get; private set; }

        /// <summary>Objenin herhangi bir dönüşte kaplayabileceği yarıçap. Doğma aralığı bundan hesaplanır.</summary>
        public float BoundingRadius => _boundingRadius;

        /// <summary>Obje fiziksel olarak durulmuş mu? Yığının oturduğunu anlamak için kullanılır.</summary>
        public bool IsResting => _rigidbody.IsSleeping();

        /// <summary><see cref="MoveTo"/> ile başlatılan taşıma sürüyor mu?</summary>
        public bool IsMoving => _moveSequence != null;

        private void Awake()
        {
            // Collider prefab'ta açık ve obje dönmemişken ölçülür; sonradan rotasyon bounds'u bozar.
            _boundingRadius = _collider.bounds.extents.magnitude;
            _baseScale = transform.localScale;
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

        /// <summary>
        /// Objeyi verilen poza verilen sürede kaydırır; varınca rigidbody'yi de o poza oturtur ve
        /// <paramref name="onArrived"/>'ı çağırır. Çağrı öncesi obje <see cref="SetSimulated"/> ile
        /// fizik dışına alınmış olmalıdır; collider kapalı olduğu için yolda başka objeye çarpmaz.
        /// </summary>
        public void MoveTo(Vector3 position, Quaternion rotation, float duration, Ease ease, Action onArrived)
        {
            StopMove();

            _moveSequence = DOTween.Sequence()
                .Join(transform.DOMove(position, duration).SetEase(ease))
                .Join(transform.DORotateQuaternion(rotation, duration).SetEase(ease))
                .OnComplete(() =>
                {
                    _moveSequence = null;
                    Teleport(position, rotation);
                    onArrived?.Invoke();
                });
        }

        /// <summary>Süren taşımayı yarıda keser. Varış bildirimi yapılmaz.</summary>
        public void StopMove()
        {
            _moveSequence?.Kill();
            _moveSequence = null;
        }

        /// <summary>
        /// Kutuya oturma yaylanması: dikey ve yatay eksen ayrı genlik ve sürelerle, sönümlenerek
        /// büyüyüp küçülür. Yatay eksen dikeyin tersi yönde başlar; biri uzarken diğeri incelir.
        /// Obje sonunda animasyon başındaki boyutuna döner.
        /// </summary>
        public void PlaySettleWobble(
            float verticalAmount, float verticalDuration,
            float horizontalAmount, float horizontalDuration,
            int oscillations)
        {
            // Obje yuvaya dünya ölçeği korunarak parent edildiği için taban, prefab ölçeği değil
            // o anki yerel ölçektir.
            Vector3 baseScale = transform.localScale;

            PlayScaleOscillation(verticalAmount, verticalDuration, oscillations, factor =>
            {
                Vector3 scale = transform.localScale;
                scale.y = baseScale.y * factor;
                transform.localScale = scale;
            });

            PlayScaleOscillation(-horizontalAmount, horizontalDuration, oscillations, factor =>
            {
                Vector3 scale = transform.localScale;
                scale.x = baseScale.x * factor;
                scale.z = baseScale.z * factor;
                transform.localScale = scale;
            });
        }

        private void PlayScaleOscillation(float amount, float duration, int oscillations, Action<float> applyFactor)
        {
            float angularSpeed = Mathf.PI * 2f * oscillations;

            // Hedef transform olarak işaretlenir ki OnDespawned'daki DOKill yarım kalan yaylanmayı da kessin.
            DOVirtual.Float(0f, 1f, duration, t =>
                {
                    float damping = (1f - t) * (1f - t);
                    applyFactor(1f + amount * Mathf.Sin(t * angularSpeed) * damping);
                })
                .SetEase(Ease.Linear)
                .SetTarget(transform);
        }

        public void OnSpawned()
        {
            // Havuzdan çıkan obje havuz kökünün konumundadır; fizik ancak çağıran onu yerleştirdikten sonra açılır.
            SetSimulated(false);
            transform.localScale = _baseScale;
        }

        public void OnDespawned()
        {
            StopMove();
            transform.DOKill();
            SetSimulated(false);

            // Kutuya oturan obje yaylanmanın ortasında olabilir; havuza kendi ölçeğiyle dönmezse bir
            // sonraki kullanımda yığına o boyutla doğar.
            transform.localScale = _baseScale;
            Type = null;
        }
    }
}
