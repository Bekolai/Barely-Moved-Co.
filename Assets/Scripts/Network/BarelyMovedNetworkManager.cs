using UnityEngine;
using Mirror;
using Steamworks;
using System.Collections.Generic;

namespace BarelyMoved.Network
{
    /// <summary>
    /// Custom NetworkManager for Barely Moved Co. with Steam integration
    /// Handles host-client model where host simulates all physics
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

        #endregion

        #region Private Fields
        private Dictionary<int, GameObject> m_ConnectedPlayers = new Dictionary<int, GameObject>();
        private int m_NextSpawnIndex = 0;
        #endregion

        #region Properties
        public bool IsHost => NetworkServer.active && NetworkClient.active;
        public bool IsClient => NetworkClient.active && !NetworkServer.active;
        public int ConnectedPlayerCount => m_ConnectedPlayers.Count;
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
                Debug.LogError("[BarelyMovedNetworkManager] SteamManager not initialized! Ensure SteamManager is in the scene.");
            }
            #endif
        }
        #endregion

        #region Server Callbacks
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[BarelyMovedNetworkManager] Server started. Host is authoritative for physics.");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient _conn)
        {
            // Get spawn position
            Transform spawnPoint = GetNextSpawnPoint();
            
            // Instantiate player
            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            // Add to dictionary
            m_ConnectedPlayers[_conn.connectionId] = player;
            
            // Spawn for connection
            NetworkServer.AddPlayerForConnection(_conn, player);
            
            Debug.Log($"[BarelyMovedNetworkManager] Player {_conn.connectionId} joined. Total players: {m_ConnectedPlayers.Count}");
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
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("[BarelyMovedNetworkManager] Disconnected from server.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Start hosting a game (host = server + client)
        /// </summary>
        public void StartHosting()
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

            Debug.Log("[BarelyMovedNetworkManager] Started hosting with max players: " + m_MaxPlayers);
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
        #endregion

        #region Private Methods
        private Transform GetNextSpawnPoint()
        {
            if (m_PlayerSpawnPoints == null || m_PlayerSpawnPoints.Length == 0)
            {
                Debug.LogWarning("[BarelyMovedNetworkManager] No spawn points assigned! Using default position.");
                return transform;
            }

            Transform spawnPoint = m_PlayerSpawnPoints[m_NextSpawnIndex];
            m_NextSpawnIndex = (m_NextSpawnIndex + 1) % m_PlayerSpawnPoints.Length;
            
            return spawnPoint;
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

