using System;
using MatchPack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Ekrana dokunuşu yığın objesine çevirir. Sahne geçişi boyunca kapalıdır; kutuya uçmakta olan
    /// objenin collider'ı kapalı olduğu için raycast onu hedeflemez.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        /// <summary>Yığındaki bir objeye dokunulduğunda yayınlanır.</summary>
        public event Action<StackItem> OnItemTapped;

        [Tooltip("Raycast'in atılacağı kalıcı kamera.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Raycast'in çarpabileceği layer'lar.")]
        [SerializeField] private LayerMask _itemLayers = ~0;

        private InputAction _tapAction;

        /// <summary>Dokunuş algılaması açık mı? Sahne geçişinde SceneLoader kapatır.</summary>
        public bool IsEnabled { get; private set; } = true;

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

        private void HandleSceneTransitionChanged(bool isTransitioning)
        {
            IsEnabled = !isTransitioning;
        }

        private void HandleTapPerformed(InputAction.CallbackContext context)
        {
            if (!IsEnabled || Pointer.current == null) { return; }

            Ray ray = _camera.ScreenPointToRay(Pointer.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _itemLayers)) { return; }
            if (!hit.collider.TryGetComponent(out StackItem item)) { return; }

            OnItemTapped?.Invoke(item);
        }
    }
}
