using MatchPack.Gameplay;
using UnityEngine;

namespace MatchPack.Core
{
    /// <summary>
    /// Game sahnesinin dışarıya açtığı tek referans noktası. Kök obje sahnede pasif kayıtlıdır;
    /// eski level boşaltıldıktan sonra SceneLoader tarafından aktifleştirilir.
    /// </summary>
    public class LevelContext : MonoBehaviour
    {
        [Tooltip("Taşıyıcı bandın ve kutuların parent'ı.")]
        [SerializeField] private Transform _conveyorRoot;

        [Tooltip("Yığın objelerinin parent'ı.")]
        [SerializeField] private Transform _stackRoot;

        [Tooltip("Kalıcı kameranın bu level için hedefleyeceği pozisyon.")]
        [SerializeField] private Transform _cameraAnchor;

        [Tooltip("Kutuların üzerinde döndüğü bant turu.")]
        [SerializeField] private ConveyorPath _conveyorPath;

        public Transform ConveyorRoot => _conveyorRoot;
        public Transform StackRoot => _stackRoot;
        public Transform CameraAnchor => _cameraAnchor;
        public ConveyorPath ConveyorPath => _conveyorPath;

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        /// <summary>Sahne boşaltılmadan hemen önce çağrılır.</summary>
        public void Teardown()
        {
            gameObject.SetActive(false);
        }
    }
}
