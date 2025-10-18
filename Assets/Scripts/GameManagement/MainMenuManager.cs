using UnityEngine;
using Mirror;
using BarelyMoved.Network;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Manages the main menu scene
    /// Handles initialization and lobby setup
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        #region Singleton
        public static MainMenuManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private UI.MainMenuUI m_MainMenuUIComponent;

        [Header("Networking")]
        [SerializeField] private BarelyMovedNetworkManager m_NetworkManager;
        #endregion

        #region Private Fields
        private bool m_IsInLobby;
        #endregion

        #region Properties
        public bool IsInLobby => m_IsInLobby;
        #endregion

        #region Events
        public delegate void MenuEventDelegate();
        public event MenuEventDelegate OnLobbyCreated;
        public event MenuEventDelegate OnLobbyJoined;
        public event MenuEventDelegate OnLobbyLeft;
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

            // Find network manager if not assigned
            if (m_NetworkManager == null)
            {
                m_NetworkManager = FindFirstObjectByType<BarelyMovedNetworkManager>();
            }

            // Find UI component if not assigned
            if (m_MainMenuUIComponent == null)
            {
                m_MainMenuUIComponent = FindFirstObjectByType<UI.MainMenuUI>();
            }

            // Subscribe to network events
            if (m_NetworkManager != null)
            {
                m_NetworkManager.OnClientConnectedToServer += OnClientConnectedToServer;
            }

            ShowMainMenu();
        }

        private void Start()
        {
            // Ensure we're not connected when in main menu
            if (NetworkServer.active || NetworkClient.active)
            {
                Debug.LogWarning("[MainMenuManager] Already connected, disconnecting...");
                if (m_NetworkManager != null)
                {
                    m_NetworkManager.LeaveGame();
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from network events
            if (m_NetworkManager != null)
            {
                m_NetworkManager.OnClientConnectedToServer -= OnClientConnectedToServer;
            }
        }
        #endregion

        #region UI Navigation
        /// <summary>
        /// Show the main menu UI
        /// </summary>
        public void ShowMainMenu()
        {
            if (m_MainMenuUIComponent != null)
            {
                m_MainMenuUIComponent.ShowMainMenu();
            }
            
            m_IsInLobby = false;
        }

        /// <summary>
        /// Show the lobby UI
        /// </summary>
        public void ShowLobby()
        {
            bool isHost = NetworkServer.active;
            
            if (m_MainMenuUIComponent != null)
            {
                m_MainMenuUIComponent.ShowLobby(isHost);
            }
            
            m_IsInLobby = true;
        }

        /// <summary>
        /// Show the settings UI
        /// </summary>
        public void ShowSettings()
        {
            if (m_MainMenuUIComponent != null)
            {
                m_MainMenuUIComponent.ShowSettings();
            }
        }

        /// <summary>
        /// Return to main menu from settings
        /// </summary>
        public void CloseSettings()
        {
            ShowMainMenu();
        }
        #endregion

        #region Lobby Management
        /// <summary>
        /// Host a new game - goes directly to prep scene
        /// </summary>
        public void HostGame()
        {
            if (m_NetworkManager == null)
            {
                Debug.LogError("[MainMenuManager] NetworkManager not found!");
                return;
            }

            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[MainMenuManager] GameStateManager not found!");
                return;
            }

            Debug.Log("[MainMenuManager] Hosting game and transitioning to prep scene...");
            
            // Start hosting
            m_NetworkManager.StartHosting();
            
            // Go directly to prep scene (no lobby wait)
            GameStateManager.Instance.TransitionToPrep();
            
            OnLobbyCreated?.Invoke();
        }

        /// <summary>
        /// Join an existing game
        /// </summary>
        /// <param name="_address">Server address to connect to</param>
        public void JoinGame(string _address = "localhost")
        {
            if (m_NetworkManager == null)
            {
                Debug.LogError("[MainMenuManager] NetworkManager not found!");
                return;
            }

            Debug.Log($"[MainMenuManager] Attempting to join game at {_address}...");
            m_NetworkManager.JoinGame(_address);
            
            // Don't show lobby immediately - wait for OnClientConnectedToServer callback
        }

        /// <summary>
        /// Leave the current lobby
        /// </summary>
        public void LeaveLobby()
        {
            if (m_NetworkManager == null)
            {
                Debug.LogError("[MainMenuManager] NetworkManager not found!");
                return;
            }

            Debug.Log("[MainMenuManager] Leaving lobby...");
            m_NetworkManager.LeaveGame();
            
            ShowMainMenu();
            OnLobbyLeft?.Invoke();
        }

        /// <summary>
        /// Start the game (transition to prep scene)
        /// Only the host can do this
        /// </summary>
        public void StartGame()
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[MainMenuManager] Only host can start game!");
                return;
            }

            if (GameStateManager.Instance == null)
            {
                Debug.LogError("[MainMenuManager] GameStateManager not found!");
                return;
            }

            Debug.Log("[MainMenuManager] Starting game, transitioning to prep scene...");
            GameStateManager.Instance.TransitionToPrep();
        }
        #endregion

        #region Network Event Handlers
        /// <summary>
        /// Called when client successfully connects to server
        /// Client will automatically be transitioned to the host's current scene by Mirror
        /// </summary>
        private void OnClientConnectedToServer()
        {
            Debug.Log("[MainMenuManager] Client connected to server");
            OnLobbyJoined?.Invoke();
            
            // Note: Client will automatically transition to host's scene (prep or level)
            // No need to show lobby - they join directly into gameplay
        }
        #endregion

        #region Application
        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenuManager] Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion
    }
}

