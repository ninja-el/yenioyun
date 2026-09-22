using System;
using System.Collections.Generic;
using MatchPack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Ekrana dokunuşu yığın objelerine çevirir. Prob ilk çarptığı objede durmaz; ışın boyunca
    /// sıralanmış birkaç adayı birden toplar ve hangisinin oynanacağına karar vermeyi dinleyene
    /// bırakır. Sahne geçişi boyunca kapalıdır; kutuya uçmakta olan objenin collider'ı kapalı
    /// olduğu için prob onu hedeflemez.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// Bir dokunuşun ham sonucu. DebugManager'ın gizmo çizimi buna dayanır, oyun mantığı
        /// bu veriyi kullanmaz. <see cref="HasHit"/> false ise isabet yoktur.
        /// </summary>
        public readonly struct TapProbe
        {
            public TapProbe(Ray ray, RaycastHit hit, bool isBoxCast, Vector3 halfExtents, Quaternion orientation)
            {
                Ray = ray;
                Hit = hit;
                IsBoxCast = isBoxCast;
                HalfExtents = halfExtents;
                Orientation = orientation;
            }

            public Ray Ray { get; }
            public RaycastHit Hit { get; }

            /// <summary>Prob kutu ile mi atıldı? False ise ince ışın kullanılmıştır.</summary>
            public bool IsBoxCast { get; }

            /// <summary>Kutu probun yarım ölçüsü. Işın modunda anlamsızdır.</summary>
            public Vector3 HalfExtents { get; }

            /// <summary>Kutu probun duruşu; kutu ışına dik durur. Işın modunda anlamsızdır.</summary>
            public Quaternion Orientation { get; }

            public bool HasHit => Hit.collider != null;
        }

        public static InputManager Instance { get; private set; }

        // Kutu probun ışın yönündeki kalınlığı. Sıfıra yakın tutulur ki prob yalnızca yanlara paylı
        // olsun; sıfır verilince BoxCast dejenere kutuyla hiçbir şeye çarpmıyor.
        private const float ProbeThickness = 0.01f;

        // NonAlloc cast'ler isabetleri mesafeye göre sıralamaz ve tampon dolunca gerisini atar;
        // en yakınları kaçırmamak için tampon istenen aday sayısından bilerek büyük tutulur.
        private const int ProbeBufferSize = 16;

        /// <summary>
        /// Dokunuşun çarptığı yığın objeleri, yakından uzağa sıralı. Liste her dokunuşta yeniden
        /// kullanılır; dinleyen taraf saklamamalı, çağrı içinde tüketmelidir.
        /// </summary>
        public event Action<IReadOnlyList<StackItem>> OnItemsTapped;

        /// <summary>Her dokunuşun ham prob sonucu; prob hiçbir şeye çarpmasa da yayınlanır.</summary>
        public event Action<TapProbe> OnTapProbed;

        [Tooltip("Probun çarpabileceği layer'lar.")]
        [SerializeField] private LayerMask _itemLayers = ~0;

        [Tooltip("Dokunuş kutu prob (BoxCast) ile aransın mı? Kapalıysa ince ışın (Raycast) kullanılır.")]
        [SerializeField] private bool _isBoxCastEnabled = true;

        [Tooltip("Kutu probun dünya birimi cinsinden kenar uzunluğu. Büyüdükçe küçük kaymalar affedilir.")]
        [SerializeField, Min(0.01f)] private float _boxCastWidth = 0.3f;

        [Tooltip("Probun tek dokunuşta toplayacağı en fazla obje adedi. 1 verilince yalnızca en yakın obje aday olur.")]
        [SerializeField, Min(1)] private int _maxProbeHits = 2;

        private readonly List<StackItem> _candidates = new List<StackItem>();
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[ProbeBufferSize];

        private InputAction _tapAction;

        /// <summary>Dokunuş algılaması açık mı? Sahne geçişinde SceneLoader kapatır.</summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>Prob kutu modunda mı çalışıyor? Debug ekranı bunu yazar.</summary>
        public bool IsBoxCastEnabled => _isBoxCastEnabled;

        /// <summary>Kutu probun kenar uzunluğu. Debug ekranı bunu yazar.</summary>
        public float BoxCastWidth => _boxCastWidth;

        /// <summary>Tek dokunuşta toplanan en fazla aday sayısı. Debug ekranı bunu yazar.</summary>
        public int MaxProbeHits => _maxProbeHits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _tapAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
            _tapAction.performed += HandleTapPerformed;
            _tapAction.Enable();
        }

        private void Start()
        {
            // SceneLoader.Instance Awake'te atandığı için abonelik Start'a bırakıldı.
            SceneLoader.Instance.OnSceneTransitionChanged += HandleSceneTransitionChanged;
        }

        private void OnDestroy()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnSceneTransitionChanged -= HandleSceneTransitionChanged;
            }

            if (_tapAction != null)
            {
                _tapAction.performed -= HandleTapPerformed;
                _tapAction.Dispose();
            }

            if (Instance == this) { Instance = null; }
        }

        /// <summary>
        /// Dokunuş algılamasını dışarıdan açar veya kapatır. Booster satın alma paneli gibi oyunun
        /// durduğu anlarda kapatılır; sahne geçişini SceneLoader ayrıca yönetir.
        /// </summary>
        public void SetInputEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        private void HandleSceneTransitionChanged(bool isTransitioning)
        {
            IsEnabled = !isTransitioning;
        }

        private void HandleTapPerformed(InputAction.CallbackContext context)
        {
            if (!IsEnabled || Pointer.current == null) { return; }

            Camera camera = GetLevelCamera();
            if (camera == null) { return; }

            TapProbe probe = Probe(camera, Pointer.current.position.ReadValue());
            OnTapProbed?.Invoke(probe);

            if (_candidates.Count == 0) { return; }

            OnItemsTapped?.Invoke(_candidates);
        }

        // Kamera GameScene'de durduğu için Inspector'dan bağlanamaz (sahneler arası referans
        // tutulamaz); her dokunuşta LevelContext üzerinden okunur.
        private static Camera GetLevelCamera()
        {
            if (SceneLoader.Instance == null || SceneLoader.Instance.ActiveLevel == null) { return null; }

            return SceneLoader.Instance.ActiveLevel.Camera;
        }

        private TapProbe Probe(Camera camera, Vector2 screenPosition)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);

            if (!_isBoxCastEnabled)
            {
                int rayHitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, Mathf.Infinity, _itemLayers);
                CollectCandidates(rayHitCount);

                return new TapProbe(ray, GetNearestHit(rayHitCount), false, Vector3.zero, Quaternion.identity);
            }

            Quaternion orientation = Quaternion.LookRotation(ray.direction, camera.transform.up);
            Vector3 halfExtents = new Vector3(_boxCastWidth * 0.5f, _boxCastWidth * 0.5f, ProbeThickness);

            int boxHitCount = Physics.BoxCastNonAlloc(
                ray.origin,
                halfExtents,
                ray.direction,
                _hitBuffer,
                orientation,
                Mathf.Infinity,
                _itemLayers);

            CollectCandidates(boxHitCount);

            return new TapProbe(ray, GetNearestHit(boxHitCount), true, halfExtents, orientation);
        }

        /// <summary>
        /// Tampondaki isabetleri mesafeye göre sıralar ve en fazla <see cref="_maxProbeHits"/> ayrı
        /// objeyi aday listesine yazar. Aynı objenin birden fazla collider'ı bir kez sayılır.
        /// </summary>
        private void CollectCandidates(int hitCount)
        {
            _candidates.Clear();
            if (hitCount <= 0) { return; }

            Array.Sort(_hitBuffer, 0, hitCount, HitDistanceComparer.Instance);

            for (int i = 0; i < hitCount && _candidates.Count < _maxProbeHits; i++)
            {
                // Collider obje prefabının alt objesinde olabilir; StackItem her zaman kökte durur.
                StackItem item = _hitBuffer[i].collider.GetComponentInParent<StackItem>();
                if (item == null || _candidates.Contains(item)) { continue; }

                _candidates.Add(item);
            }
        }

        // Sıralama CollectCandidates içinde yapıldığı için en yakın isabet tamponun başındadır.
        private RaycastHit GetNearestHit(int hitCount)
        {
            return hitCount > 0 ? _hitBuffer[0] : default;
        }

        private class HitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();

            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }
    }
}
