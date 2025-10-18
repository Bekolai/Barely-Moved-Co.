using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using BarelyMoved.GameManagement;
using BarelyMoved.Player;

namespace BarelyMoved.Network
{
    /// <summary>
    /// Custom NetworkManager for Barely Moved Co. with Steam integration
    /// Handles host-client model where host simulates all physics
    /// Manages scene transitions and player spawning across different scenes
    /// </summary>
    public class BarelyMovedNetworkManager : NetworkManager
    {
        #region Singleton
        public static BarelyMovedNetworkManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Player Setup")]
        [SerializeField] private Transform[] m_PlayerSpawnPoints;
        [SerializeField] private int m_MaxPlayers = 4;

        [Header("Scene Setup")]
        [SerializeField] private bool m_SpawnPlayersInMainMenu = false; // Don't spawn in main menu
        [SerializeField] private bool m_AutoCreatePlayerInPrep = true; // Auto spawn in prep scene

        [Header("Network Tracking")]
        [SerializeField] private GameObject m_ConnectionTrackerPrefab;

        #endregion

        #region Private Fields
        private Dictionary<int, GameObject> m_ConnectedPlayers = new Dictionary<int, GameObject>();
        private int m_NextSpawnIndex = 0;
        private string m_CurrentSceneName;
        #endregion

        #region Properties
        public bool IsHost => NetworkServer.active && NetworkClient.active;
        public bool IsClient => NetworkClient.active && !NetworkServer.active;
        /// <summary>
        /// Gets the connected player count. Uses NetworkConnectionTracker for accurate syncing.
        /// </summary>
        public int ConnectedPlayerCount 
        { 
            get
            {
                if (NetworkServer.active)
                {
                    return NetworkServer.connections.Count;
                }
                else if (NetworkConnectionTracker.Instance != null)
                {
                    // Clients use synced connection count from tracker
                    return NetworkConnectionTracker.Instance.ConnectionCount;
                }
                else
                {
                    return 0;
                }
            }
        }
        public string CurrentSceneName => m_CurrentSceneName;
        #endregion

        #region Events
        public delegate void NetworkEventDelegate();
        public event NetworkEventDelegate OnClientConnectedToServer;
        #endregion

        #region Unity Lifecycle
        public override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void Start()
        {
            base.Start();
            
            #if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[BarelyMovedNetworkManager] SteamManager not initialized. This is normal for testing without Steam. Game will continue in offline/test mode.");
            }
            #endif
        }
        #endregion

        #region Server Callbacks
        public override void OnStartServer()
        {
            base.OnStartServer();
            m_CurrentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[BarelyMovedNetworkManager] Server started in scene: {m_CurrentSceneName}. Host is authoritative for physics.");
            
            // Spawn connection tracker if not already spawned
            SpawnConnectionTracker();
            
            // Important: Clear onlineScene to prevent auto scene change when hosting
            // Scene changes will be controlled by GameStateManager instead
            if (string.IsNullOrEmpty(onlineScene) || onlineScene == "MainMenu")
            {
                Debug.Log("[BarelyMovedNetworkManager] Staying in MainMenu - waiting for manual scene transition");
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient _conn)
        {
            // Check if we should spawn player in current scene
            string sceneName = SceneManager.GetActiveScene().name;
            
            // Don't spawn players in main menu, only in prep and level scenes
            if (sceneName == "MainMenu" && !m_SpawnPlayersInMainMenu)
            {
                Debug.Log($"[BarelyMovedNetworkManager] Player {_conn.connectionId} connected but not spawning in MainMenu");
                
                // Still update connection tracker even if player isn't spawned
                UpdateConnectionTracker();
                return;
            }

            // Get spawn position
            Transform spawnPoint = GetSpawnPointForCurrentScene();
            
            // Instantiate player
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Add to dictionary
            m_ConnectedPlayers[_conn.connectionId] = player;
            
            // Spawn for connection
            NetworkServer.AddPlayerForConnection(_conn, player);
            
            Debug.Log($"[BarelyMovedNetworkManager] Player {_conn.connectionId} spawned in {sceneName}. Total players: {m_ConnectedPlayers.Count}");
            
            // Update connection tracker
            UpdateConnectionTracker();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient _conn)
        {
            // Remove from dictionary
            if (m_ConnectedPlayers.ContainsKey(_conn.connectionId))
            {
                m_ConnectedPlayers.Remove(_conn.connectionId);
                Debug.Log($"[BarelyMovedNetworkManager] Player {_conn.connectionId} left. Remaining: {m_ConnectedPlayers.Count}");
            }
            
            base.OnServerDisconnect(_conn);
            
            // Update connection tracker
            UpdateConnectionTracker();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            m_ConnectedPlayers.Clear();
            m_NextSpawnIndex = 0;
            Debug.Log("[BarelyMovedNetworkManager] Server stopped.");

        }
        #endregion

        #region Client Callbacks
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("[BarelyMovedNetworkManager] Connected to server.");
            
            // Notify listeners (e.g., MainMenuManager) that client connected
            OnClientConnectedToServer?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();

            Debug.Log("[BarelyMovedNetworkManager] Disconnected from server.");

            // Return to main menu when disconnected (kicked or connection lost)
            // Only if we're NOT the host (host can disconnect without leaving scene)
            // Don't reload if already in MainMenu
            if (!NetworkServer.active)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                if (currentScene != "MainMenu")
                {
                    Debug.Log("[BarelyMovedNetworkManager] Client disconnected - returning to main menu...");
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Start hosting a game (host = server + client)
        /// </summary>
        public void StartHosting()
        {
            StartHosting(false);
        }

        /// <summary>
        /// Start hosting a game with optional Steam bypass
        /// </summary>
        /// <param name="_bypassSteam">If true, starts hosting without Steam integration</param>
        public void StartHosting(bool _bypassSteam)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                Debug.LogWarning("[BarelyMovedNetworkManager] Already connected!");
                return;
            }

            // Set max connections before starting host
            maxConnections = m_MaxPlayers;

            // Use NetworkManager's StartHost() which handles transport setup properly
            StartHost();

            Debug.Log($"[BarelyMovedNetworkManager] Started hosting with max players: {m_MaxPlayers}, Steam bypass: {_bypassSteam}");
        }

        /// <summary>
        /// Join a game as a client
        /// </summary>
        /// <param name="_address">Server address to connect to</param>
        public void JoinGame(string _address)
        {
            if (NetworkClient.active)
            {
                Debug.LogWarning("[BarelyMovedNetworkManager] Already connected!");
                return;
            }

            networkAddress = _address;
            StartClient();
            
            Debug.Log($"[BarelyMovedNetworkManager] Joining game at {_address}");
        }

        /// <summary>
        /// Stop hosting or disconnect from server
        /// </summary>
        public void LeaveGame()
        {
            if (NetworkServer.active)
            {
                StopHost();
            }
            else if (NetworkClient.active)
            {
                StopClient();
            }

            m_ConnectedPlayers.Clear();
            m_NextSpawnIndex = 0;
        }

        /// <summary>
        /// Kick a player from the server (host only)
        /// </summary>
        /// <param name="_conn">The connection to kick</param>
        public void KickPlayer(NetworkConnectionToClient _conn)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[BarelyMovedNetworkManager] Only host can kick players!");
                return;
            }

            if (_conn == null)
            {
                Debug.LogError("[BarelyMovedNetworkManager] Cannot kick - connection is null!");
                return;
            }

            Debug.Log($"[BarelyMovedNetworkManager] Kicking player with connection ID {_conn.connectionId}");

            // Disconnect the client - Mirror will handle cleanup automatically
            _conn.Disconnect();
        }
        #endregion

        #region Scene Callbacks
        public override void OnServerSceneChanged(string _sceneName)
        {
            base.OnServerSceneChanged(_sceneName);
            
            m_CurrentSceneName = _sceneName;
            m_NextSpawnIndex = 0; // Reset spawn index for new scene
            
            Debug.Log($"[BarelyMovedNetworkManager] Scene changed to: {_sceneName}");

            // Notify GameStateManager
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnSceneLoaded(_sceneName);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Spawn the connection tracker on server
        /// </summary>
        private void SpawnConnectionTracker()
        {
            if (!NetworkServer.active) return;
            
            // Check if tracker already exists
            if (NetworkConnectionTracker.Instance != null)
            {
                Debug.Log("[BarelyMovedNetworkManager] Connection tracker already exists");
                UpdateConnectionTracker();
                return;
            }
            
            // Spawn connection tracker prefab if assigned
            if (m_ConnectionTrackerPrefab != null)
            {
                GameObject tracker = Instantiate(m_ConnectionTrackerPrefab);
                NetworkServer.Spawn(tracker);
                Debug.Log("[BarelyMovedNetworkManager] Connection tracker spawned");
                UpdateConnectionTracker();
            }
            else
            {
                // Create tracker from scratch if no prefab assigned
                GameObject tracker = new GameObject("NetworkConnectionTracker");
                tracker.AddComponent<NetworkIdentity>();
                tracker.AddComponent<NetworkConnectionTracker>();
                NetworkServer.Spawn(tracker);
                Debug.Log("[BarelyMovedNetworkManager] Connection tracker created and spawned (no prefab assigned)");
                UpdateConnectionTracker();
            }
        }
        
        /// <summary>
        /// Update the connection tracker with current connection count
        /// </summary>
        private void UpdateConnectionTracker()
        {
            if (!NetworkServer.active) return;
            
            if (NetworkConnectionTracker.Instance != null)
            {
                int count = NetworkServer.connections.Count;
                NetworkConnectionTracker.Instance.UpdateConnectionCount(count);
            }
        }
        
        /// <summary>
        /// Get spawn point for current scene
        /// Checks scene-specific managers first (PrepSceneManager, etc.)
        /// Falls back to local spawn points or default position
        /// </summary>
        private Transform GetSpawnPointForCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            // Check for PrepSceneManager in prep scene
            if (sceneName == "PrepScene")
            {
                var prepManager = FindFirstObjectByType<PrepSceneManager>();
                if (prepManager != null)
                {
                    Transform prepSpawn = prepManager.GetSpawnPoint();
                    if (prepSpawn != null)
                    {
                        Debug.Log("[BarelyMovedNetworkManager] Using PrepSceneManager spawn point");
                        return prepSpawn;
                    }
                }
            }

            // Fall back to local spawn points if available
            if (m_PlayerSpawnPoints != null && m_PlayerSpawnPoints.Length > 0)
            {
                // Make sure the spawn point at index is valid
                if (m_NextSpawnIndex < m_PlayerSpawnPoints.Length && m_PlayerSpawnPoints[m_NextSpawnIndex] != null)
                {
                    Transform spawnPoint = m_PlayerSpawnPoints[m_NextSpawnIndex];
                    m_NextSpawnIndex = (m_NextSpawnIndex + 1) % m_PlayerSpawnPoints.Length;
                    return spawnPoint;
                }
            }

            // Last resort: use default position (0,1,0) to avoid spawning in floor
            Debug.LogWarning($"[BarelyMovedNetworkManager] No spawn points configured for {sceneName}! Using default position Vector3(0, 1, 0).");
            
            // Create a temporary spawn point at safe position
            GameObject tempSpawn = new GameObject("TempSpawnPoint");
            tempSpawn.transform.position = new Vector3(0, 1, 0);
            return tempSpawn.transform;
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug Connection Info")]
        private void DebugConnectionInfo()
        {
            Debug.Log($"Server Active: {NetworkServer.active}");
            Debug.Log($"Client Active: {NetworkClient.active}");
            Debug.Log($"Connected Players: {m_ConnectedPlayers.Count}");
            Debug.Log($"Max Connections: {maxConnections}");
        }
        #endif
    }
}

