using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Game sahnesinin dışarıya açtığı tek referans noktası. Sahne bir kez yüklenip hiç
    /// boşaltılmadığı için kök obje hep aktiftir; menüye dönüldüğünde yalnızca 3B level içeriği
    /// gizlenir, oyunun tek kamerası açık kalır.
    /// </summary>
    public class LevelContext : MonoBehaviour
    {
        [Tooltip("Taşıyıcı bandın ve kutuların parent'ı.")]
        [SerializeField] private Transform _conveyorRoot;

        [Tooltip("Yığın objelerinin doğduğu ve içinde kaldığı alan.")]
        [SerializeField] private StackArea _stackArea;

        [Tooltip("Oyunun tek kamerası. Menüde de bu kamera render eder, bu yüzden hiç kapanmaz.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Kameranın level oynanırken duracağı pozisyon.")]
        [SerializeField] private Transform _cameraAnchor;

        [Tooltip("Kutuların üzerinde döndüğü bant turu.")]
        [SerializeField] private ConveyorPath _conveyorPath;

        public Transform ConveyorRoot => _conveyorRoot;
        public StackArea StackArea => _stackArea;
        public Camera Camera => _camera;
        public Transform CameraAnchor => _cameraAnchor;
        public ConveyorPath ConveyorPath => _conveyorPath;

        private void Awake()
        {
            if (_camera == null)
            {
                Debug.LogError(
                    "LevelContext has no camera assigned; add the camera to GameScene and bind it here.",
                    this);
            }
        }

        /// <summary>
        /// 3B level içeriğini açıp kapatır. Kamera bilerek dışarıda bırakılmıştır: menüde de
        /// ekranı o render eder.
        /// </summary>
        public void SetContentActive(bool isActive)
        {
            if (_conveyorRoot != null) { _conveyorRoot.gameObject.SetActive(isActive); }
            if (_stackArea != null) { _stackArea.gameObject.SetActive(isActive); }
        }
    }
}
