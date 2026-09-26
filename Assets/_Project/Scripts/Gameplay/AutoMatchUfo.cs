using System;
using System.Collections;
using System.Collections.Generic;
using MatchPack.Core;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Auto-Match'in UFO'su. Kutuya ayrılmış objeleri çekim ışınıyla içine alır, kutunun üstüne
    /// uçar ve kutuyu tamamlar; kutu ayrılırken onunla birlikte yükselip küçülerek kaybolur.
    /// Hangi kutunun ve objelerin seçileceğine karar vermez, bu <see cref="AutoMatchBooster"/>'ın işidir.
    /// </summary>
    public class AutoMatchUfo : MonoBehaviour, IPoolable
    {
        private const float ShrinkExponent = 0.6f;
        private const float MinBeamHeight = 0.01f;

        private static readonly int CollectedId = Shader.PropertyToID("_Collected");

        [Header("Parçalar")]
        [Tooltip("Kameraya dönen UFO görseli.")]
        [SerializeField] private Transform _body;

        [Tooltip("Lambaları yakılan UFO görselinin renderer'ı.")]
        [SerializeField] private Renderer _bodyRenderer;

        [Tooltip("Zeminden UFO'ya uzanan çekim ışını. Mesh'i birim yüksekliktedir, tabanı pivottadır.")]
        [SerializeField] private Transform _beam;

        [Tooltip("Işın boyunca yükselip daralan halkalar.")]
        [SerializeField] private LineRenderer[] _rings;

        [Header("Duruş")]
        [Tooltip("UFO görselinin ölçeği.")]
        [SerializeField, Min(0.01f)] private float _bodyScale = 0.85f;

        [Tooltip("Obje toplarken UFO'nun objelerin ortasından yüksekliği. UFO bu yükseklikte ekranın ortasına iner.")]
        [SerializeField, Min(0f)] private float _collectHeight = 3f;

        [Tooltip("Obje toplarken ekranın ortasındaki duruş noktasına eklenen kayma (dünya ekseninde).")]
        [SerializeField] private Vector3 _collectOffset = new Vector3(0f, 0f, 1f);

        [Tooltip("Kutunun üstünde dururken kutu pivotundan yüksekliği.")]
        [SerializeField, Min(0f)] private float _boxHoverHeight = 1.2f;

        [Tooltip("Objelerin içine girdiği nokta; kameradan bakınca UFO merkezinin bu kadar altıdır.")]
        [SerializeField, Min(0f)] private float _intakeDepth = 0.73f;

        [Tooltip("UFO'nun ekrana girdiği nokta, toplama noktasına göre.")]
        [SerializeField] private Vector3 _entryOffset = new Vector3(5f, 2f, 0f);

        [Tooltip("Girişte UFO'nun başlangıç eğimi (derece). İnerken sıfıra döner.")]
        [SerializeField] private float _entryTilt = -15f;

        [Header("Zamanlama")]
        [Tooltip("Ekrana girip toplama noktasına inme süresi.")]
        [SerializeField, Min(0.01f)] private float _enterDuration = 0.65f;

        [Tooltip("Tek bir objenin UFO'ya çekilme süresi.")]
        [SerializeField, Min(0.01f)] private float _pullDuration = 0.72f;

        [Tooltip("Art arda çekilen objelerin başlangıçları arasındaki fark; her obje için bu aralıkta (en az, en çok) rastgele seçilir.")]
        [SerializeField] private Vector2 _pullStaggerRange = new Vector2(0.1f, 0.2f);

        [Tooltip("Toplama noktasından kutunun üstüne uçma süresi.")]
        [SerializeField, Min(0.01f)] private float _travelDuration = 1f;

        [Header("Hareket")]
        [Tooltip("Toplarken havada salınmanın yüksekliği.")]
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.035f;

        [Tooltip("Havada salınmanın hızı.")]
        [SerializeField, Min(0f)] private float _bobFrequency = 5f;

        [Tooltip("Toplarken sağa sola yatmanın açısı (derece).")]
        [SerializeField, Min(0f)] private float _swayAngle = 2f;

        [Tooltip("Sağa sola yatmanın hızı.")]
        [SerializeField, Min(0f)] private float _swayFrequency = 3f;

        [Tooltip("Çekilen objenin çizdiği sarmalın yarıçapı.")]
        [SerializeField, Min(0f)] private float _spiralRadius = 0.22f;

        [Tooltip("Çekilen objenin yol boyunca döndüğü açı (derece).")]
        [SerializeField] private Vector3 _pullSpin = new Vector3(25f, 220f, 15f);

        [Header("Işın")]
        [Tooltip("Işının tabandaki yarıçapı.")]
        [SerializeField, Min(0f)] private float _beamRadius = 1.5f;

        [Tooltip("Halkanın tabandaki ve tepedeki yarıçapı.")]
        [SerializeField] private Vector2 _ringRadius = new Vector2(1.45f, 0.1f);

        [Tooltip("Halkaların ekrandaki basıklığı (dikey yarıçap / yatay yarıçap).")]
        [SerializeField, Range(0f, 1f)] private float _ringFlatness = 0.3f;

        [Tooltip("Halkaların yükselme hızı (saniyede tur).")]
        [SerializeField, Min(0f)] private float _ringSpeed = 0.8f;

        [Tooltip("Halkanın tabandaki rengi. Yükseldikçe saydamlaşır.")]
        [SerializeField] private Color _ringColor = new Color(0.2f, 0.9f, 1f, 0.8f);

        private readonly List<StackItem> _items = new List<StackItem>();
        private readonly List<Pose> _startPoses = new List<Pose>();
        private readonly List<Vector3> _startScales = new List<Vector3>();
        private readonly List<float> _pullStarts = new List<float>();

        private MaterialPropertyBlock _properties;
        private Transform _camera;
        private Box _box;
        private Action<AutoMatchUfo> _onFinished;
        private Coroutine _routine;
        private float _tilt;
        private int _litLampCount;

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
            SetBeamVisible(false);
        }

        private void LateUpdate()
        {
            if (_camera == null) { return; }

            _body.rotation = _camera.rotation * Quaternion.Euler(0f, 0f, _tilt);
        }

        /// <summary>
        /// Objeleri toplayıp kutuyu tamamlar ve kutuyla birlikte kaybolur. Objelerin yuvası kutuda
        /// önceden ayrılmış ve objeler fizik dışına alınmış olmalıdır. Bitince
        /// <paramref name="onFinished"/> çağrılır; UFO'yu havuza iade etmek çağıranın işidir.
        /// </summary>
        public void Play(Box box, IReadOnlyList<StackItem> items, Camera camera, Action<AutoMatchUfo> onFinished)
        {
            _box = box;
            _onFinished = onFinished;
            _camera = camera != null ? camera.transform : null;

            _items.Clear();
            _startPoses.Clear();
            _startScales.Clear();
            _pullStarts.Clear();

            float pullStart = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                Transform itemTransform = items[i].transform;
                _items.Add(items[i]);
                _startPoses.Add(new Pose(itemTransform.position, itemTransform.rotation));
                _startScales.Add(itemTransform.localScale);
                _pullStarts.Add(pullStart);
                pullStart += UnityEngine.Random.Range(_pullStaggerRange.x, _pullStaggerRange.y);
            }

            // Kutuda zaten duran objelerin lambası baştan yanık gelir; UFO kalanları yakar.
            SetLitLamps(box.ItemCount - items.Count);
            _routine = StartCoroutine(PlayRoutine());
        }

        public void OnSpawned()
        {
            _tilt = 0f;
            _body.localScale = Vector3.one * _bodyScale;
            SetBeamVisible(false);
        }

        public void OnDespawned()
        {
            if (_routine != null) { StopCoroutine(_routine); }

            _routine = null;
            _box = null;
            _camera = null;
            _onFinished = null;
            _items.Clear();
            _startPoses.Clear();
            _startScales.Clear();
            _pullStarts.Clear();
            SetBeamVisible(false);
        }

        private IEnumerator PlayRoutine()
        {
            Vector3 center = GetItemsCenter();
            Vector3 hover = GetScreenCenterPoint(center.y + _collectHeight, center) + _collectOffset;

            yield return EnterRoutine(hover);
            yield return CollectRoutine(hover, center.y);

            if (IsBoxAvailable()) { yield return TravelRoutine(); }

            if (IsBoxAvailable())
            {
                // Ölçek kutu tamamlanmadan okunur; ayrılış kutuyu küçülttükçe UFO da aynı oranda küçülür.
                float boxScale = _box.transform.localScale.x;
                ConfirmItems();
                yield return FollowDepartureRoutine(boxScale);
            }

            Finish();
        }

        private IEnumerator EnterRoutine(Vector3 hover)
        {
            Vector3 entry = hover + _entryOffset;

            for (float time = 0f; time < _enterDuration; time += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, time / _enterDuration);
                transform.position = Vector3.Lerp(entry, hover, progress);
                _tilt = Mathf.Lerp(_entryTilt, 0f, progress);
                yield return null;
            }

            transform.position = hover;
            _tilt = 0f;
        }

        private IEnumerator CollectRoutine(Vector3 hover, float groundHeight)
        {
            SetBeamVisible(true);
            int capturedCount = 0;

            for (float time = 0f; capturedCount < _items.Count; time += Time.deltaTime)
            {
                transform.position = hover + Vector3.up * (Mathf.Sin(time * _bobFrequency) * _bobAmplitude);
                _tilt = Mathf.Sin(time * _swayFrequency) * _swayAngle;
                Vector3 intake = transform.position + GetScreenDown() * _intakeDepth;

                capturedCount = 0;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (PullItem(i, time, intake)) { capturedCount++; }
                }

                UpdateBeam(time, groundHeight, intake);
                yield return null;
            }

            SetBeamVisible(false);
            _tilt = 0f;
        }

        private IEnumerator TravelRoutine()
        {
            Vector3 start = transform.position;

            // Kutu bantta ilerlemeye devam eder; varış noktası her karede kutunun o anki yerinden okunur.
            for (float time = 0f; time < _travelDuration && IsBoxAvailable(); time += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(start, GetBoxHoverPoint(1f), Mathf.SmoothStep(0f, 1f, time / _travelDuration));
                yield return null;
            }
        }

        private IEnumerator FollowDepartureRoutine(float boxScale)
        {
            bool hasDeparted = false;

            // Kutu, oyuncunun ona uçurduğu son obje de varınca ayrılır; o ana kadar UFO kutunun üstünde bekler.
            while (IsBoxAvailable())
            {
                hasDeparted |= _box.IsDeparting;
                if (hasDeparted && !_box.IsDeparting) { break; }

                float ratio = boxScale > 0f ? _box.transform.localScale.x / boxScale : 0f;
                transform.position = GetBoxHoverPoint(ratio);
                _body.localScale = Vector3.one * (_bodyScale * ratio);
                yield return null;
            }
        }

        private bool PullItem(int index, float time, Vector3 intake)
        {
            StackItem item = _items[index];
            if (!item.gameObject.activeSelf) { return true; }

            float progress = Mathf.Clamp01((time - _pullStarts[index]) / _pullDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float angle = progress * Mathf.PI * 2f;
            Vector3 spiral = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (Mathf.Sin(progress * Mathf.PI) * _spiralRadius);

            item.transform.position = Vector3.Lerp(_startPoses[index].position, intake, eased) + spiral;
            item.transform.rotation = _startPoses[index].rotation * Quaternion.Euler(_pullSpin * progress);
            item.transform.localScale = _startScales[index] * Mathf.Pow(1f - eased, ShrinkExponent);

            if (progress < 1f) { return false; }

            // Obje kutuya uçmaz; kutunun yuva listesinde gizli kalır ve kutu havuza dönerken onunla iade edilir.
            item.gameObject.SetActive(false);
            SetLitLamps(_litLampCount + 1);
            return true;
        }

        private void ConfirmItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _box.ConfirmItem(_items[i]);
            }
        }

        private void Finish()
        {
            _routine = null;
            _onFinished?.Invoke(this);
        }

        // Işın 2D tasarlandığı için dünyada dikey değil, ekranda UFO'nun altına doğru uzanır; tabanı
        // bu doğrultunun obje yüksekliğine indiği yerdir.
        private void UpdateBeam(float time, float groundHeight, Vector3 intake)
        {
            Vector3 down = GetScreenDown();
            float drop = intake.y - groundHeight;
            float height = Mathf.Max(MinBeamHeight, down.y < -0.01f ? drop / -down.y : drop);
            Vector3 ground = intake + down * height;

            _beam.SetPositionAndRotation(ground, _camera != null ? _camera.rotation : Quaternion.identity);
            _beam.localScale = new Vector3(_beamRadius, height, _beamRadius);

            for (int i = 0; i < _rings.Length; i++)
            {
                UpdateRing(_rings[i], Mathf.Repeat(time * _ringSpeed + (float)i / _rings.Length, 1f), height);
            }
        }

        // Halka ekran düzleminde basık bir elipstir; kameradan bakınca 2D ışının üzerinde durur.
        private void UpdateRing(LineRenderer ring, float progress, float height)
        {
            float radius = Mathf.Lerp(_ringRadius.x, _ringRadius.y, progress);
            Color color = _ringColor;
            color.a *= 1f - progress;
            ring.startColor = color;
            ring.endColor = color;

            int pointCount = ring.positionCount;
            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / pointCount;
                var local = new Vector3(Mathf.Cos(angle) * radius, progress * height + Mathf.Sin(angle) * radius * _ringFlatness, 0f);
                ring.SetPosition(i, _beam.position + _beam.rotation * local);
            }
        }

        private void SetBeamVisible(bool isVisible)
        {
            _beam.gameObject.SetActive(isVisible);

            for (int i = 0; i < _rings.Length; i++)
            {
                _rings[i].gameObject.SetActive(isVisible);
            }
        }

        private void SetLitLamps(int count)
        {
            _litLampCount = count;
            _bodyRenderer.GetPropertyBlock(_properties);
            _properties.SetFloat(CollectedId, count);
            _bodyRenderer.SetPropertyBlock(_properties);
        }

        private Vector3 GetItemsCenter()
        {
            Vector3 sum = Vector3.zero;

            for (int i = 0; i < _startPoses.Count; i++)
            {
                sum += _startPoses[i].position;
            }

            return sum / Mathf.Max(1, _startPoses.Count);
        }

        private Vector3 GetScreenDown()
        {
            return _camera != null ? -_camera.up : Vector3.down;
        }

        // Ekranın ortasından geçen görüş ışınının verilen yükseklikteki noktası.
        private Vector3 GetScreenCenterPoint(float height, Vector3 fallback)
        {
            if (_camera == null || _camera.forward.y > -0.01f) { return fallback + Vector3.up * _collectHeight; }

            float distance = (height - _camera.position.y) / _camera.forward.y;
            return _camera.position + _camera.forward * distance;
        }

        private Vector3 GetBoxHoverPoint(float scaleRatio)
        {
            return _box.transform.position + Vector3.up * (_boxHoverHeight * scaleRatio);
        }

        // Level sökülürken kutu havuza dönmüş olabilir; UFO o durumda kutuya dokunmadan biter.
        private bool IsBoxAvailable()
        {
            return _box != null && _box.gameObject.activeInHierarchy;
        }
    }
}
