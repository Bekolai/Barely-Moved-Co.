using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace BarelyMoved.Camera
{
    /// <summary>
    /// Manages AAA-style third-person camera with orbital controls
    /// Handles smooth following, collision avoidance, and input-based rotation
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        #region Singleton
        public static CameraManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera m_VirtualCamera;
        [SerializeField] private CinemachineThirdPersonFollow m_ThirdPersonFollow;
        [SerializeField] private CinemachineOrbitalFollow m_OrbitalFollow;
        [SerializeField] private CinemachineInputAxisController m_InputAxisController;
        [SerializeField] private CinemachineCollisionImpulseSource m_CollisionImpulse;

        [Header("Camera Settings")]
        [SerializeField] private float m_CameraDistance = 5f;
        [SerializeField] private float m_MinCameraDistance = 2f;
        [SerializeField] private float m_MaxCameraDistance = 10f;
        [SerializeField] private float m_CameraHeight = 2f;
        [SerializeField] private float m_ZoomSpeed = 5f;
        #endregion

        #region Private Fields
        private Transform m_CurrentTarget;

        // Camera rotation tracking
        private float m_CameraYaw; // Horizontal rotation around target
        private float m_CameraPitch; // Vertical rotation

        // Input handling
        private Vector2 m_LookInput;
        private float m_ZoomInput;

        // Camera state
        private bool m_IsControllingCamera = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeCamera();
        }

        private void Start()
        {
            ApplyCameraSettings();
            EnableCameraControl();
        }

        private void Update()
        {
            HandleCameraInput();
        }

        private void OnEnable()
        {
            // Subscribe to input events when enabled
            if (m_IsControllingCamera)
            {
                EnableCameraControl();
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from input events when disabled
            DisableCameraControl();
        }
        #endregion

        #region Initialization
        private void InitializeCamera()
        {
            if (m_VirtualCamera == null)
            {
                m_VirtualCamera = GetComponentInChildren<CinemachineCamera>();

                if (m_VirtualCamera == null)
                {
                    Debug.LogError("[CameraManager] No CinemachineCamera found!");
                    return;
                }
            }

            // Get or add required Cinemachine components
            SetupCinemachineComponents();

            // Initialize camera angles to look slightly down at player
            m_CameraPitch = -15f; // Slight downward angle
            m_CameraYaw = 0f;
        }

        private void SetupCinemachineComponents()
        {
            // Get or add ThirdPersonFollow component for basic following behavior
            m_ThirdPersonFollow = m_VirtualCamera.GetComponent<CinemachineThirdPersonFollow>();
            if (m_ThirdPersonFollow == null)
            {
                m_ThirdPersonFollow = m_VirtualCamera.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            }

            // Get or add OrbitalFollow component for orbital camera movement
            m_OrbitalFollow = m_VirtualCamera.GetComponent<CinemachineOrbitalFollow>();
            if (m_OrbitalFollow == null)
            {
                m_OrbitalFollow = m_VirtualCamera.gameObject.AddComponent<CinemachineOrbitalFollow>();
            }

            // Get or add InputAxisController component for input handling
            m_InputAxisController = m_VirtualCamera.GetComponent<CinemachineInputAxisController>();
            if (m_InputAxisController == null)
            {
                m_InputAxisController = m_VirtualCamera.gameObject.AddComponent<CinemachineInputAxisController>();
            }

            // Get or add CollisionImpulseSource for camera shake
            m_CollisionImpulse = m_VirtualCamera.GetComponent<CinemachineCollisionImpulseSource>();
            if (m_CollisionImpulse == null)
            {
                m_CollisionImpulse = m_VirtualCamera.gameObject.AddComponent<CinemachineCollisionImpulseSource>();
            }
        }

        private void ApplyCameraSettings()
        {
            if (m_ThirdPersonFollow != null)
            {
                // Configure third person follow for basic camera behavior
                m_ThirdPersonFollow.CameraDistance = m_CameraDistance;
                m_ThirdPersonFollow.VerticalArmLength = m_CameraHeight;
                m_ThirdPersonFollow.CameraSide = 1f; // Right side
                m_ThirdPersonFollow.Damping = new Vector3(0.1f, 0.1f, 0.1f); // X, Y, Z damping
            }

            if (m_OrbitalFollow != null)
            {
                // Configure orbital follow for orbital camera movement
                // Set initial camera position for orbital movement
                m_OrbitalFollow.HorizontalAxis.Value = 0f; // Start at default position
                m_OrbitalFollow.VerticalAxis.Value = -15f; // Slight downward angle
            }

            // Configure InputAxisController for input handling
            if (m_InputAxisController != null)
            {
                // The input axis controller will handle mouse input for camera rotation
                // We don't need to configure it manually - it responds to Cinemachine input channels
            }
        }
        #endregion

        #region Camera Control
        /// <summary>
        /// Enable camera control with input
        /// </summary>
        public void EnableCameraControl()
        {
            m_IsControllingCamera = true;
            // Input handling is managed through the Input System
        }

        /// <summary>
        /// Disable camera control
        /// </summary>
        public void DisableCameraControl()
        {
            m_IsControllingCamera = false;
        }

        private void HandleCameraInput()
        {
            if (!m_IsControllingCamera || m_CurrentTarget == null) return;

            // Get look input from Input System
            var lookAction = InputSystem.actions.FindAction("Look");
            if (lookAction != null)
            {
                m_LookInput = lookAction.ReadValue<Vector2>();
            }

            // Get zoom input from Input System (mouse wheel)
            var zoomAction = InputSystem.actions.FindAction("Zoom");
            if (zoomAction != null)
            {
                m_ZoomInput = zoomAction.ReadValue<float>();
            }
            else
            {
                // Fallback: try to read mouse scroll wheel directly from Input System
                // This might not work perfectly, but it's better than crashing
                try
                {
                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        m_ZoomInput = mouse.scroll.ReadValue().y * 0.1f; // Scale down the scroll value
                    }
                }
                catch
                {
                    m_ZoomInput = 0f;
                }
            }
        }

        private void LateUpdate()
        {
            // Update camera rotation in LateUpdate to ensure it happens after all other updates
            UpdateCameraRotation();
        }

        private void UpdateCameraRotation()
        {
            if (!m_IsControllingCamera || m_CurrentTarget == null) return;

            // In Cinemachine 3.x, input is handled by the CinemachineInputAxisController component
            // The input axis controller automatically reads from the Input System actions

            // Apply zoom input
            if (Mathf.Abs(m_ZoomInput) > 0.01f)
            {
                float newDistance = m_CameraDistance - m_ZoomInput * m_ZoomSpeed;
                SetCameraDistance(Mathf.Clamp(newDistance, m_MinCameraDistance, m_MaxCameraDistance));
            }
        }

        /// <summary>
        /// Reset camera to default position behind player
        /// </summary>
        public void ResetCameraPosition()
        {
            // Reset camera angles for next target assignment
            m_CameraYaw = 0f;
            m_CameraPitch = -15f; // Slight downward angle

            if (m_OrbitalFollow != null)
            {
                m_OrbitalFollow.HorizontalAxis.Value = m_CameraYaw;
                m_OrbitalFollow.VerticalAxis.Value = m_CameraPitch;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the camera to follow a target (usually local player)
        /// </summary>
        /// <param name="_target">Transform to follow</param>
        public void SetFollowTarget(Transform _target)
        {
            if (_target == null)
            {
                Debug.LogWarning("[CameraManager] Attempted to set null target!");
                return;
            }

            m_CurrentTarget = _target;

            if (m_VirtualCamera != null)
            {
                m_VirtualCamera.Follow = _target;
                m_VirtualCamera.LookAt = _target;

                // Reset camera position when switching targets
                ResetCameraPosition();

                Debug.Log($"[CameraManager] Camera now following: {_target.name}");
            }
        }

        /// <summary>
        /// Clear the camera target
        /// </summary>
        public void ClearTarget()
        {
            m_CurrentTarget = null;

            if (m_VirtualCamera != null)
            {
                m_VirtualCamera.Follow = null;
                m_VirtualCamera.LookAt = null;
            }
        }

        /// <summary>
        /// Adjust camera distance at runtime
        /// </summary>
        public void SetCameraDistance(float _distance)
        {
            m_CameraDistance = _distance;

            if (m_ThirdPersonFollow != null)
            {
                m_ThirdPersonFollow.CameraDistance = _distance;
            }
        }

        /// <summary>
        /// Set camera height offset
        /// </summary>
        public void SetCameraHeight(float _height)
        {
            m_CameraHeight = _height;

            if (m_ThirdPersonFollow != null)
            {
                m_ThirdPersonFollow.VerticalArmLength = _height;
            }
        }

        /// <summary>
        /// Get current camera distance
        /// </summary>
        public float GetCameraDistance()
        {
            return m_CameraDistance;
        }

        /// <summary>
        /// Check if camera is currently controlling
        /// </summary>
        public bool IsControllingCamera()
        {
            return m_IsControllingCamera;
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug Camera Info")]
        private void DebugCameraInfo()
        {
            Debug.Log($"Current Target: {(m_CurrentTarget != null ? m_CurrentTarget.name : "None")}");
            Debug.Log($"Camera Distance: {m_CameraDistance}");
            Debug.Log($"Camera Yaw: {m_CameraYaw}");
            Debug.Log($"Camera Pitch: {m_CameraPitch}");
            Debug.Log($"Virtual Camera: {(m_VirtualCamera != null ? "Active" : "Null")}");
            Debug.Log($"Controlling Camera: {m_IsControllingCamera}");
        }

        [ContextMenu("Reset Camera Position")]
        private void ResetCameraPositionEditor()
        {
            ResetCameraPosition();
        }
        #endif
    }
}

