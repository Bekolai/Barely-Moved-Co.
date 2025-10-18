using UnityEngine;
using Mirror;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Manages the prep/hub scene
    /// Where players select jobs, buy upgrades, and prepare for missions
    /// </summary>
    public class PrepSceneManager : NetworkBehaviour
    {
        #region Singleton
        public static PrepSceneManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Scene Setup")]
        [SerializeField] private Transform[] m_PlayerSpawnPoints;
        [SerializeField] private Transform m_DefaultSpawnPoint;

        [Header("Interactables")]
        [SerializeField] private GameObject m_JobBoard;
        [SerializeField] private GameObject m_UpgradeShop;
        [SerializeField] private GameObject m_CustomizationStation;

        [Header("Player Settings")]
        [SerializeField] private bool m_AutoSpawnPlayers = true;
        #endregion

        #region Private Fields
        private int m_NextSpawnIndex = 0;
        #endregion

        #region Properties
        public bool IsReady => NetworkServer.active || NetworkClient.active;
        #endregion

        #region Events
        public delegate void PrepSceneEventDelegate();
        public event PrepSceneEventDelegate OnSceneReady;
        public event PrepSceneEventDelegate OnJobStarted;
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
            Debug.Log("[PrepSceneManager] Initialized");
        }

        private void Start()
        {
            InitializeScene();
        }
        #endregion

        #region Initialization
        private void InitializeScene()
        {
            Debug.Log("[PrepSceneManager] Initializing prep scene...");

            // Update game state if on server
            if (isServer && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnSceneLoaded(GameStateManager.c_PrepSceneName);
            }

            // Setup cursor for prep scene - should be unlocked for social hub
            SetupCursor();

            OnSceneReady?.Invoke();
        }

        private void SetupCursor()
        {
            // In prep scene, cursor should be unlocked for free movement
            // Players can interact with job board and walk around
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[PrepSceneManager] Cursor set to locked mode for gameplay");
        }
        #endregion

        #region Job Management
        /// <summary>
        /// Start a job (transition to level scene)
        /// Only host can initiate
        /// </summary>
        public void StartJob()
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[PrepSceneManager] Only host can start jobs!");
                CmdRequestStartJob();
                return;
            }

            StartJobInternal();
        }

        [Command(requiresAuthority = false)]
        private void CmdRequestStartJob()
        {
            StartJobInternal();
        }

        [Server]
        private void StartJobInternal()
        {
            Debug.Log("[PrepSceneManager] Starting job, transitioning to level scene...");
            
            OnJobStarted?.Invoke();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.TransitionToLevel();
            }
        }
        #endregion

        #region Spawn Management
        /// <summary>
        /// Get the next spawn point for a player
        /// </summary>
        public Transform GetSpawnPoint()
        {
            // Try assigned spawn points first
            if (m_PlayerSpawnPoints != null && m_PlayerSpawnPoints.Length > 0)
            {
                // Make sure the spawn point is valid
                if (m_NextSpawnIndex < m_PlayerSpawnPoints.Length && m_PlayerSpawnPoints[m_NextSpawnIndex] != null)
                {
                    Transform spawnPoint = m_PlayerSpawnPoints[m_NextSpawnIndex];
                    m_NextSpawnIndex = (m_NextSpawnIndex + 1) % m_PlayerSpawnPoints.Length;
                    return spawnPoint;
                }
            }

            // Try default spawn point
            if (m_DefaultSpawnPoint != null)
            {
                Debug.LogWarning("[PrepSceneManager] Using default spawn point");
                return m_DefaultSpawnPoint;
            }

            // Last resort: use self position (safe fallback)
            Debug.LogWarning("[PrepSceneManager] No spawn points assigned! Using PrepSceneManager position");
            return transform;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Show the job board UI
        /// </summary>
        public void ShowJobBoard()
        {
            Debug.Log("[PrepSceneManager] Opening job board...");
            // This will be handled by JobSelectionUI
        }

        /// <summary>
        /// Show the upgrade shop UI
        /// </summary>
        public void ShowUpgradeShop()
        {
            Debug.Log("[PrepSceneManager] Opening upgrade shop...");
            // TODO: Implement upgrade shop UI
        }

        /// <summary>
        /// Show the customization station UI
        /// </summary>
        public void ShowCustomization()
        {
            Debug.Log("[PrepSceneManager] Opening customization...");
            // TODO: Implement customization UI
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug Scene Info")]
        private void DebugSceneInfo()
        {
            Debug.Log($"Is Server: {isServer}");
            Debug.Log($"Is Client: {isClient}");
            Debug.Log($"Network Active: {NetworkServer.active || NetworkClient.active}");
            Debug.Log($"Spawn Points: {(m_PlayerSpawnPoints != null ? m_PlayerSpawnPoints.Length : 0)}");
        }

        private void OnDrawGizmos()
        {
            // Draw spawn points
            if (m_PlayerSpawnPoints != null)
            {
                for (int i = 0; i < m_PlayerSpawnPoints.Length; i++)
                {
                    if (m_PlayerSpawnPoints[i] != null)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(m_PlayerSpawnPoints[i].position, 0.5f);
                        Gizmos.DrawLine(m_PlayerSpawnPoints[i].position, m_PlayerSpawnPoints[i].position + Vector3.up * 2f);
                    }
                }
            }
        }
        #endif
    }
}

