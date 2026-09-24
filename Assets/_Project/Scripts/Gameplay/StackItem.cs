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
        private Bounds _baseBounds;
        private Vector3 _baseScale;
        private RigidbodyInterpolation _interpolation;
        private Sequence _moveSequence;
        private float _scaleMultiplier = 1f;

        public ItemType Type { get; private set; }

        /// <summary>
        /// Objenin herhangi bir dönüşte kaplayabileceği yarıçap. Doğma aralığı bundan hesaplanır;
        /// bölüme özel boyut çarpanını içerir.
        /// </summary>
        public float BoundingRadius => _boundingRadius * _scaleMultiplier;

        /// <summary>
        /// Görselin, obje dönmemiş ve prefab ölçeğindeyken kapladığı hacim; objenin parent uzayında.
        /// Kutu, objeyi yuvasına sığdırırken bunu kullanır. Bölüm çarpanını içermez.
        /// </summary>
        public Bounds BaseBounds => _baseBounds;

        /// <summary>Prefab'taki yerel ölçek.</summary>
        public Vector3 BaseScale => _baseScale;

        /// <summary>Obje fiziksel olarak durulmuş mu? Yığının oturduğunu anlamak için kullanılır.</summary>
        public bool IsResting => _rigidbody.IsSleeping();

        /// <summary><see cref="MoveTo"/> ile başlatılan taşıma sürüyor mu?</summary>
        public bool IsMoving => _moveSequence != null;

        private void Awake()
        {
            // Collider prefab'ta açık ve obje dönmemişken ölçülür; sonradan rotasyon bounds'u bozar.
            _boundingRadius = _collider.bounds.extents.magnitude;
            _baseScale = transform.localScale;
            _baseBounds = CalculateBaseBounds();
            _interpolation = _rigidbody.interpolation;
        }

        // Renderer'ların yerel bounds köşeleri objenin yerel uzayına taşınır, sonra prefab ölçeğiyle
        // çarpılır; böylece sonuç objenin o anki konum ve rotasyonundan bağımsızdır.
        private Bounds CalculateBaseBounds()
        {
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer meshRenderer = renderers[i];
                if (!(meshRenderer is MeshRenderer) && !(meshRenderer is SkinnedMeshRenderer)) { continue; }

                Matrix4x4 toLocal = worldToLocal * meshRenderer.transform.localToWorldMatrix;
                Bounds local = meshRenderer.localBounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));
                    point = Vector3.Scale(toLocal.MultiplyPoint3x4(point), _baseScale);

                    if (hasBounds)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            if (!hasBounds)
            {
                Debug.LogWarning($"{name} has no mesh renderer; box fitting falls back to a unit size.", this);
                bounds = new Bounds(Vector3.zero, _baseScale);
            }

            return bounds;
        }

        /// <summary>
        /// Objeyi bir tipe ve bölüme özel boyut çarpanına hazırlar. Havuzdan alındıktan sonra,
        /// yığında yer ayrılmadan önce çağrılır; ayrılacak yer çarpanlı boyuta göre hesaplanır.
        /// </summary>
        public void Setup(ItemType type, float scaleMultiplier)
        {
            Type = type;
            _scaleMultiplier = scaleMultiplier;
            transform.localScale = _baseScale * scaleMultiplier;
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

        public void OnSpawned()
        {
            // Havuzdan çıkan obje havuz kökünün konumundadır; fizik ancak çağıran onu yerleştirdikten sonra açılır.
            SetSimulated(false);
            transform.localScale = _baseScale;
            _scaleMultiplier = 1f;
        }

        public void OnDespawned()
        {
            StopMove();
            transform.DOKill();
            SetSimulated(false);

            // Yuvaya dünya ölçeği korunarak parent edilen objenin yerel ölçeği değişir; havuza kendi
            // ölçeğiyle dönmezse bir sonraki kullanımda yığına o boyutla doğar.
            transform.localScale = _baseScale;
            _scaleMultiplier = 1f;
            Type = null;
        }
    }
}
