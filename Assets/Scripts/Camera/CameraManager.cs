using UnityEngine;
using Unity.Cinemachine;

namespace BarelyMoved.Camera
{
    /// <summary>
    /// Manages Cinemachine camera for the local player
    /// Handles smooth third-person following
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        #region Singleton
        public static CameraManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera m_VirtualCamera;
        [SerializeField] private CinemachinePositionComposer m_FramingTransposer;

        [Header("Camera Settings")]
        [SerializeField] private float m_CameraDistance = 5f;
        #endregion

        #region Private Fields
        private Transform m_CurrentTarget;
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

            // Get PositionComposer component
            m_FramingTransposer = m_VirtualCamera.GetComponent<CinemachinePositionComposer>();

            if (m_FramingTransposer == null)
            {
                Debug.LogWarning("[CameraManager] No PositionComposer found on camera!");
                return;
            }
        }

        private void ApplyCameraSettings()
        {
            if (m_FramingTransposer == null) return;

            m_FramingTransposer.CameraDistance = m_CameraDistance;
            // Note: LookaheadTime may not be available in CinemachinePositionComposer in v3.x
            // m_FramingTransposer.LookaheadTime = m_LookAheadTime;
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

            if (m_FramingTransposer != null)
            {
                m_FramingTransposer.CameraDistance = _distance;
            }
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug Camera Info")]
        private void DebugCameraInfo()
        {
            Debug.Log($"Current Target: {(m_CurrentTarget != null ? m_CurrentTarget.name : "None")}");
            Debug.Log($"Camera Distance: {m_CameraDistance}");
            Debug.Log($"Virtual Camera: {(m_VirtualCamera != null ? "Active" : "Null")}");
        }
        #endif
    }
}

