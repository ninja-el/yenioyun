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
        // Temas normali bu değerden dik ise obje bir şeyin üstünde duruyor sayılır (~60° eğime kadar).
        private const float SupportNormalMinY = 0.5f;

        [Tooltip("Dokunuş raycast'inin çarpacağı collider.")]
        [SerializeField] private Collider _collider;

        [Tooltip("Yığındaki fiziksel davranışı sağlayan rigidbody.")]
        [SerializeField] private Rigidbody _rigidbody;

        private Vector3 _boundsExtents;
        private Vector3 _localBoundsCenter;
        private Bounds _baseBounds;
        private Vector3 _baseScale;
        private RigidbodyInterpolation _interpolation;
        private Sequence _moveSequence;
        private float _scaleMultiplier = 1f;

        private bool _canAutoFreeze;
        private bool _isFrozen;
        private bool _hasSupport;
        private float _restTime;
        private float _freezeMaxSpeedSqr;
        private float _freezeMaxAngularSpeedSqr;
        private float _freezeDelay;

        public ItemType Type { get; private set; }

        /// <summary>Obje yerine oturduğu için fiziği donduruldu mu? Donmuş obje itilmez ama collider'ı açıktır.</summary>
        public bool IsFrozen => _isFrozen;

        /// <summary>
        /// Collider'ın, obje dönmemişken objenin kendi eksenlerinde kapladığı kutunun yarı ölçüsü.
        /// <see cref="BoundsCenter"/> etrafında obje ile birlikte döner; yığında yer bundan ayrılır.
        /// Bölüme özel boyut çarpanını içerir.
        /// </summary>
        public Vector3 BoundsExtents => _boundsExtents * _scaleMultiplier;

        /// <summary>Collider'ın merkezi, dünya uzayında. Model pivotları çoğunlukla tabanda olduğu için pivotla aynı değildir.</summary>
        public Vector3 BoundsCenter => transform.TransformPoint(_localBoundsCenter);

        /// <summary>
        /// Görselin, obje dönmemiş ve prefab ölçeğindeyken kapladığı hacim; objenin parent uzayında.
        /// Kutu, objeyi yuvasına sığdırırken bunu kullanır. Bölüm çarpanını içermez.
        /// </summary>
        public Bounds BaseBounds => _baseBounds;

        /// <summary>Prefab'taki yerel ölçek.</summary>
        public Vector3 BaseScale => _baseScale;

        /// <summary>Obje fiziksel olarak durulmuş mu? Yığının oturduğunu anlamak için kullanılır.</summary>
        public bool IsResting => _isFrozen || _rigidbody.IsSleeping();

        /// <summary><see cref="MoveTo"/> ile başlatılan taşıma sürüyor mu?</summary>
        public bool IsMoving => _moveSequence != null;

        private void Awake()
        {
            // Collider prefab'ta açık ve obje dönmemişken ölçülür; sonradan rotasyon bounds'u bozar.
            _boundsExtents = _collider.bounds.extents;
            _localBoundsCenter = transform.InverseTransformPoint(_collider.bounds.center);
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

            _isFrozen = false;
            _hasSupport = false;
            _restTime = 0f;
            if (!isSimulated) { _canAutoFreeze = false; }
        }

        /// <summary>
        /// Fizikteki obje, altında bir destekle verilen hızların altında <paramref name="delay"/> saniye
        /// kalınca dondurulur (kinematic olur, collider açık kalır); böylece üstündeki objelerin baskısıyla
        /// itilmez. <see cref="SetSimulated"/>(true) sonrası çağrılır; fizik kapanınca kural düşer.
        /// </summary>
        public void EnableAutoFreeze(float maxSpeed, float maxAngularSpeed, float delay)
        {
            _freezeMaxSpeedSqr = maxSpeed * maxSpeed;
            _freezeMaxAngularSpeedSqr = maxAngularSpeed * maxAngularSpeed;
            _freezeDelay = delay;
            _restTime = 0f;
            _canAutoFreeze = true;
        }

        /// <summary>Donmuş objeyi yeniden fiziğe bırakır; oturunca kural onu tekrar dondurur.</summary>
        public void Unfreeze()
        {
            if (!_isFrozen) { return; }

            _isFrozen = false;
            _hasSupport = false;
            _restTime = 0f;
            _rigidbody.isKinematic = false;
            _rigidbody.interpolation = _interpolation;
            _rigidbody.WakeUp();
        }

        private void Freeze()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.isKinematic = true;
            _isFrozen = true;
        }

        // Temas bilgisi fizik adımından sonra gelir; burada bir önceki adımın teması okunup sıfırlanır.
        private void FixedUpdate()
        {
            if (!_canAutoFreeze || _isFrozen || _rigidbody.isKinematic) { return; }

            bool isSlow = _rigidbody.linearVelocity.sqrMagnitude <= _freezeMaxSpeedSqr
                && _rigidbody.angularVelocity.sqrMagnitude <= _freezeMaxAngularSpeedSqr;

            // Uyuyan rigidbody temas bildirmez; yerçekimine rağmen uyuyabildiyse bir şeyin üstündedir.
            bool isSupported = _hasSupport || _rigidbody.IsSleeping();

            _restTime = isSlow && isSupported ? _restTime + Time.fixedDeltaTime : 0f;
            _hasSupport = false;

            if (_restTime >= _freezeDelay) { Freeze(); }
        }

        private void OnCollisionEnter(Collision collision)
        {
            DetectSupport(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            DetectSupport(collision);
        }

        private void DetectSupport(Collision collision)
        {
            if (_hasSupport || !_canAutoFreeze) { return; }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y >= SupportNormalMinY)
                {
                    _hasSupport = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Collider merkezi verilen noktaya gelecek şekilde, verilen rotasyonda pivotun durması gereken
        /// konum. Yığında yer <see cref="BoundsCenter"/> üzerinden ayrıldığı için obje buna göre konur.
        /// </summary>
        public Vector3 GetPivotForCenter(Vector3 center, Quaternion rotation)
        {
            return center - rotation * Vector3.Scale(_localBoundsCenter, transform.lossyScale);
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
