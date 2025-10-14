using UnityEngine;
using Steamworks;
using Mirror;
using System;

namespace BarelyMoved.Network
{
    /// <summary>
    /// Manages Steam lobbies for co-op gameplay
    /// Handles lobby creation, invites, and joining through Steam
    /// </summary>
    public class SteamLobbyManager : MonoBehaviour
    {
        #region Constants
        private const string c_LobbyTypeKey = "LobbyType";
        private const string c_LobbyTypeValue = "BarelyMovedCo";
        #endregion

        #region Build Configuration
        /// <summary>
        /// Check if we're in a build that should disable Steam (for testing)
        /// </summary>
        private bool ShouldDisableSteam()
        {
            // Always disable Steam in builds for testing
            // In builds, Steam typically isn't available, so we run in offline mode
            return true;
        }
        #endregion

        #region Singleton
        public static SteamLobbyManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Lobby Settings")]
        [SerializeField] private int m_MaxLobbyMembers = 4;
        #endregion

        #region Private Fields
        private CSteamID m_CurrentLobbyID;
        private bool m_IsInLobby = false;
        
        // Steam Callbacks
        private Callback<LobbyCreated_t> m_LobbyCreatedCallback;
        private Callback<GameLobbyJoinRequested_t> m_JoinRequestCallback;
        private Callback<LobbyEnter_t> m_LobbyEnteredCallback;
        #endregion

        #region Properties
        public bool IsInLobby => m_IsInLobby;
        public CSteamID CurrentLobbyID => m_CurrentLobbyID;
        #endregion

        #region Events
        public event Action<CSteamID> OnLobbyCreated;
        public event Action<CSteamID> OnLobbyEntered;
        public event Action OnLobbyLeft;
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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            #if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SteamLobbyManager] SteamManager not initialized. This is normal for testing without Steam. Game will continue in offline/test mode.");
                return;
            }

            InitializeSteamCallbacks();
            #endif
        }

        private void OnDestroy()
        {
            if (m_IsInLobby)
            {
                LeaveLobby();
            }
        }
        #endregion

        #region Initialization
        private void InitializeSteamCallbacks()
        {
            m_LobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCallback);
            m_JoinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequestCallback);
            m_LobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnteredCallback);
            
            Debug.Log("[SteamLobbyManager] Steam callbacks initialized.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new Steam lobby and starts hosting
        /// </summary>
        public void CreateLobby()
        {
            // Check if we should disable Steam for testing
            if (ShouldDisableSteam())
            {
                Debug.Log("[SteamLobbyManager] Steam disabled for testing - starting direct host mode");
                BarelyMovedNetworkManager.Instance?.StartHosting(true); // Bypass Steam
                return;
            }

            #if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SteamLobbyManager] Cannot create lobby - Steam not initialized. This is normal for testing without Steam.");
                return;
            }

            if (m_IsInLobby)
            {
                Debug.LogWarning("[SteamLobbyManager] Already in a lobby!");
                return;
            }

            Debug.Log("[SteamLobbyManager] Creating lobby...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, m_MaxLobbyMembers);
            #else
            Debug.LogWarning("[SteamLobbyManager] Steamworks is disabled. This is normal for testing without Steam.");
            #endif
        }

        /// <summary>
        /// Join a lobby by Steam ID
        /// </summary>
        public void JoinLobby(CSteamID _lobbyID)
        {
            #if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[SteamLobbyManager] Cannot join lobby - Steam not initialized. This is normal for testing without Steam.");
                return;
            }

            Debug.Log($"[SteamLobbyManager] Joining lobby {_lobbyID}...");
            SteamMatchmaking.JoinLobby(_lobbyID);
            #else
            Debug.LogWarning("[SteamLobbyManager] Steamworks is disabled. This is normal for testing without Steam.");
            #endif
        }

        /// <summary>
        /// Leave the current lobby
        /// </summary>
        public void LeaveLobby()
        {
            #if !DISABLESTEAMWORKS
            if (!m_IsInLobby)
            {
                Debug.LogWarning("[SteamLobbyManager] Not in a lobby!");
                return;
            }

            Debug.Log("[SteamLobbyManager] Leaving lobby...");
            SteamMatchmaking.LeaveLobby(m_CurrentLobbyID);
            
            m_IsInLobby = false;
            m_CurrentLobbyID = CSteamID.Nil;
            
            OnLobbyLeft?.Invoke();
            #endif
        }

        /// <summary>
        /// Invite Steam friends to the lobby (opens Steam overlay)
        /// </summary>
        public void InviteFriends()
        {
            #if !DISABLESTEAMWORKS
            if (!m_IsInLobby)
            {
                Debug.LogWarning("[SteamLobbyManager] Cannot invite - not in a lobby!");
                return;
            }

            SteamFriends.ActivateGameOverlayInviteDialog(m_CurrentLobbyID);
            Debug.Log("[SteamLobbyManager] Opening Steam friend invite dialog...");
            #endif
        }

        /// <summary>
        /// Get lobby member count
        /// </summary>
        public int GetLobbyMemberCount()
        {
            #if !DISABLESTEAMWORKS
            if (!m_IsInLobby) return 0;
            return SteamMatchmaking.GetNumLobbyMembers(m_CurrentLobbyID);
            #else
            return 0;
            #endif
        }
        #endregion

        #region Steam Callbacks
        private void OnLobbyCreatedCallback(LobbyCreated_t _callback)
        {
            if (_callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"[SteamLobbyManager] Failed to create lobby! Result: {_callback.m_eResult}. This is normal for testing without Steam.");
                return;
            }

            m_CurrentLobbyID = new CSteamID(_callback.m_ulSteamIDLobby);
            m_IsInLobby = true;

            // Set lobby metadata
            SteamMatchmaking.SetLobbyData(m_CurrentLobbyID, c_LobbyTypeKey, c_LobbyTypeValue);
            
            Debug.Log($"[SteamLobbyManager] Lobby created successfully: {m_CurrentLobbyID}");
            
            // Start hosting the game (bypass Steam if in test mode)
            bool bypassSteam = false;
            #if UNITY_EDITOR
            // In editor, we can bypass Steam for testing
            bypassSteam = true;
            #endif
            BarelyMovedNetworkManager.Instance?.StartHosting(bypassSteam);
            
            OnLobbyCreated?.Invoke(m_CurrentLobbyID);
        }

        private void OnJoinRequestCallback(GameLobbyJoinRequested_t _callback)
        {
            Debug.Log($"[SteamLobbyManager] Join request received for lobby: {_callback.m_steamIDLobby}");
            JoinLobby(_callback.m_steamIDLobby);
        }

        private void OnLobbyEnteredCallback(LobbyEnter_t _callback)
        {
            m_CurrentLobbyID = new CSteamID(_callback.m_ulSteamIDLobby);
            m_IsInLobby = true;

            Debug.Log($"[SteamLobbyManager] Entered lobby: {m_CurrentLobbyID}");

            // If not host, get host's Steam ID and connect
            CSteamID hostID = SteamMatchmaking.GetLobbyOwner(m_CurrentLobbyID);
            
            if (hostID.m_SteamID == SteamUser.GetSteamID().m_SteamID)
            {
                Debug.Log("[SteamLobbyManager] You are the host!");
            }
            else
            {
                Debug.Log($"[SteamLobbyManager] Joining host {hostID}...");
                
                // Connect to host using Steam P2P or their address
                // For Mirror with Steam Transport, this is handled automatically
                // But we can trigger the connection here if needed
                string hostAddress = hostID.m_SteamID.ToString();
                BarelyMovedNetworkManager.Instance?.JoinGame(hostAddress);
            }
            
            OnLobbyEntered?.Invoke(m_CurrentLobbyID);
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug Lobby Info")]
        private void DebugLobbyInfo()
        {
            Debug.Log($"In Lobby: {m_IsInLobby}");
            Debug.Log($"Lobby ID: {m_CurrentLobbyID}");
            Debug.Log($"Member Count: {GetLobbyMemberCount()}");
        }
        #endif
    }
}

