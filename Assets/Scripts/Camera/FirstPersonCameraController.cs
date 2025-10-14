using UnityEngine;
using Unity.Cinemachine;
using Mirror;
using BarelyMoved.Player;

namespace BarelyMoved.Camera
{
    /// <summary>
    /// First-person camera controller using Cinemachine 3
    /// Handles camera rotation based on player input
    /// Only active for local player
    /// </summary>
    public class FirstPersonCameraController : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera m_VirtualCamera;
        [SerializeField] private Transform m_CameraTarget;
        
        [Header("Look Sensitivity")]
        [SerializeField, Range(0.1f, 10f)] private float m_MouseSensitivity = 2f;
        [SerializeField, Range(0.1f, 10f)] private float m_GamepadSensitivity = 3f;
        
        [Header("Look Constraints")]
        [SerializeField, Range(-90f, 0f)] private float m_MinVerticalAngle = -80f;
        [SerializeField, Range(0f, 90f)] private float m_MaxVerticalAngle = 80f;
        #endregion

        #region Private Fields
        private PlayerInputHandler m_InputHandler;
        private float m_CameraYaw = 0f;
        private float m_CameraPitch = 0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_InputHandler = GetComponent<PlayerInputHandler>();
            
            if (m_InputHandler == null)
            {
                Debug.LogError("[FirstPersonCameraController] PlayerInputHandler not found!");
            }

            // Find the Cinemachine camera early if it's a child
            if (m_VirtualCamera == null)
            {
                m_VirtualCamera = GetComponentInChildren<CinemachineCamera>();
            }

            // IMPORTANT: Disable the camera by default
            // It will only be enabled for the local player in OnStartLocalPlayer
            if (m_VirtualCamera != null)
            {
                m_VirtualCamera.enabled = false;
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // Find or setup Cinemachine camera
            if (m_VirtualCamera == null)
            {
                // First try to find camera as a child (preferred setup)
                m_VirtualCamera = GetComponentInChildren<CinemachineCamera>();
                
                // If not found as child, try to find in scene
                if (m_VirtualCamera == null)
                {
                    m_VirtualCamera = FindFirstObjectByType<CinemachineCamera>();
                }
                
                if (m_VirtualCamera == null)
                {
                    Debug.LogError("[FirstPersonCameraController] No CinemachineCamera found! Add one as a child of the player or in the scene.");
                    return;
                }
            }

            // CRITICAL: Enable the camera ONLY for the local player
            // This prevents remote players' cameras from taking over your view
            m_VirtualCamera.enabled = true;

            // If camera is a child, position it at the camera target height
            if (m_VirtualCamera.transform.parent == transform && m_CameraTarget != null)
            {
                m_VirtualCamera.transform.localPosition = new Vector3(0, m_CameraTarget.localPosition.y, 0);
            }
            else if (m_CameraTarget != null)
            {
                // If camera is in scene (not as child), set follow targets
                m_VirtualCamera.Follow = m_CameraTarget;
                m_VirtualCamera.LookAt = m_CameraTarget;
            }

            // Initialize camera rotation from player's current rotation
            m_CameraYaw = transform.eulerAngles.y;
            m_CameraPitch = 0f;

            // Lock cursor for first-person view
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnStopLocalPlayer()
        {
            base.OnStopLocalPlayer();

            // Disable camera when this player is no longer the local player
            if (m_VirtualCamera != null)
            {
                m_VirtualCamera.enabled = false;
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer) return;

            HandleCameraRotation();
        }
        #endregion

        #region Camera Control
        private void HandleCameraRotation()
        {
            if (m_InputHandler == null || m_VirtualCamera == null) return;

            Vector2 lookInput = m_InputHandler.LookInput;

            // Determine if using mouse or gamepad based on input magnitude
            float sensitivity = lookInput.magnitude > 1f ? m_GamepadSensitivity : m_MouseSensitivity;

            // Calculate rotation deltas
            float lookX = lookInput.x * sensitivity * Time.deltaTime;
            float lookY = lookInput.y * sensitivity * Time.deltaTime;

            // Update yaw (horizontal rotation)
            m_CameraYaw += lookX;

            // Update pitch (vertical rotation) with clamping
            m_CameraPitch -= lookY; // Invert Y for standard FPS controls
            m_CameraPitch = Mathf.Clamp(m_CameraPitch, m_MinVerticalAngle, m_MaxVerticalAngle);

            // Apply pitch rotation directly to the Cinemachine camera transform
            // This ensures vertical look works correctly
            if (m_VirtualCamera != null)
            {
                m_VirtualCamera.transform.localRotation = Quaternion.Euler(m_CameraPitch, 0f, 0f);
            }

            // Apply full rotation to camera target (for other systems that might need it)
            if (m_CameraTarget != null)
            {
                m_CameraTarget.rotation = Quaternion.Euler(m_CameraPitch, m_CameraYaw, 0f);
            }

            // Rotate the player body to match camera yaw (facing direction)
            transform.rotation = Quaternion.Euler(0f, m_CameraYaw, 0f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the current camera forward direction (flattened on Y axis)
        /// </summary>
        public Vector3 GetCameraForward()
        {
            if (m_CameraTarget == null) return transform.forward;

            Vector3 forward = m_CameraTarget.forward;
            forward.y = 0f;
            return forward.normalized;
        }

        /// <summary>
        /// Get the current camera right direction (flattened on Y axis)
        /// </summary>
        public Vector3 GetCameraRight()
        {
            if (m_CameraTarget == null) return transform.right;

            Vector3 right = m_CameraTarget.right;
            right.y = 0f;
            return right.normalized;
        }

        /// <summary>
        /// Get camera yaw angle
        /// </summary>
        public float GetCameraYaw() => m_CameraYaw;

        /// <summary>
        /// Get camera pitch angle
        /// </summary>
        public float GetCameraPitch() => m_CameraPitch;
        #endregion

        #region Debug
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Ensure min is always less than max
            if (m_MinVerticalAngle > 0f) m_MinVerticalAngle = 0f;
            if (m_MaxVerticalAngle < 0f) m_MaxVerticalAngle = 0f;
        }
#endif
        #endregion
    }
}

