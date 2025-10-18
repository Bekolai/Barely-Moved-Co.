using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarelyMoved.GameManagement;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Main menu UI controller
    /// Handles main menu, lobby, and settings panels
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Main Menu Panel")]
        [SerializeField] private GameObject m_MainMenuPanel;
        [SerializeField] private Button m_HostButton;
        [SerializeField] private Button m_JoinButton;
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_QuitButton;

        [Header("Lobby Panel")]
        [SerializeField] private GameObject m_LobbyPanel;
        [SerializeField] private TextMeshProUGUI m_LobbyTitleText;
        [SerializeField] private TextMeshProUGUI m_PlayerCountText;
        [SerializeField] private Button m_StartGameButton;
        [SerializeField] private Button m_LeaveLobbyButton;
        [SerializeField] private Transform m_PlayerListContainer;

        [Header("Join Panel")]
        [SerializeField] private GameObject m_JoinPanel;
        [SerializeField] private TMP_InputField m_ServerAddressInput;
        [SerializeField] private Button m_ConfirmJoinButton;
        [SerializeField] private Button m_CancelJoinButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject m_SettingsPanel;
        [SerializeField] private Button m_CloseSettingsButton;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupButtons();
            ShowMainMenu();
        }

        private void OnEnable()
        {
            // Subscribe to manager events
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.OnLobbyCreated += OnLobbyCreated;
                MainMenuManager.Instance.OnLobbyJoined += OnLobbyJoined;
                MainMenuManager.Instance.OnLobbyLeft += OnLobbyLeft;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from manager events
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.OnLobbyCreated -= OnLobbyCreated;
                MainMenuManager.Instance.OnLobbyJoined -= OnLobbyJoined;
                MainMenuManager.Instance.OnLobbyLeft -= OnLobbyLeft;
            }
        }
        #endregion

        #region Setup
        private void SetupButtons()
        {
            // Main Menu buttons
            if (m_HostButton != null)
                m_HostButton.onClick.AddListener(OnHostButtonClicked);
            
            if (m_JoinButton != null)
                m_JoinButton.onClick.AddListener(OnJoinButtonClicked);
            
            if (m_SettingsButton != null)
                m_SettingsButton.onClick.AddListener(OnSettingsButtonClicked);
            
            if (m_QuitButton != null)
                m_QuitButton.onClick.AddListener(OnQuitButtonClicked);

            // Lobby buttons
            if (m_StartGameButton != null)
                m_StartGameButton.onClick.AddListener(OnStartGameButtonClicked);
            
            if (m_LeaveLobbyButton != null)
                m_LeaveLobbyButton.onClick.AddListener(OnLeaveLobbyButtonClicked);

            // Join panel buttons
            if (m_ConfirmJoinButton != null)
                m_ConfirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            
            if (m_CancelJoinButton != null)
                m_CancelJoinButton.onClick.AddListener(OnCancelJoinClicked);

            // Settings buttons
            if (m_CloseSettingsButton != null)
                m_CloseSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
        }
        #endregion

        #region UI Navigation
        public void ShowMainMenu()
        {
            SetPanelActive(m_MainMenuPanel, true);
            SetPanelActive(m_LobbyPanel, false);
            SetPanelActive(m_JoinPanel, false);
            SetPanelActive(m_SettingsPanel, false);
        }

        public void ShowLobby(bool _isHost)
        {
            SetPanelActive(m_MainMenuPanel, false);
            SetPanelActive(m_LobbyPanel, true);
            SetPanelActive(m_JoinPanel, false);
            SetPanelActive(m_SettingsPanel, false);

            // Update lobby UI
            if (m_LobbyTitleText != null)
            {
                m_LobbyTitleText.text = _isHost ? "Hosting Lobby" : "In Lobby";
            }

            // Only host can start game
            if (m_StartGameButton != null)
            {
                m_StartGameButton.gameObject.SetActive(_isHost);
            }

            UpdatePlayerCount();
        }

        public void ShowJoinPanel()
        {
            SetPanelActive(m_MainMenuPanel, false);
            SetPanelActive(m_JoinPanel, true);
            SetPanelActive(m_LobbyPanel, false);
            SetPanelActive(m_SettingsPanel, false);
        }

        public void ShowSettings()
        {
            SetPanelActive(m_SettingsPanel, true);
        }

        private void SetPanelActive(GameObject _panel, bool _active)
        {
            if (_panel != null)
            {
                _panel.SetActive(_active);
            }
        }
        #endregion

        #region Button Callbacks - Main Menu
        private void OnHostButtonClicked()
        {
            Debug.Log("[MainMenuUI] Host button clicked");
            
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.HostGame();
            }
        }

        private void OnJoinButtonClicked()
        {
            Debug.Log("[MainMenuUI] Join button clicked");
            ShowJoinPanel();
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("[MainMenuUI] Settings button clicked");
            ShowSettings();
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("[MainMenuUI] Quit button clicked");
            
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.QuitGame();
            }
        }
        #endregion

        #region Button Callbacks - Lobby
        private void OnStartGameButtonClicked()
        {
            Debug.Log("[MainMenuUI] Start game button clicked");
            
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.StartGame();
            }
        }

        private void OnLeaveLobbyButtonClicked()
        {
            Debug.Log("[MainMenuUI] Leave lobby button clicked");
            
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.LeaveLobby();
            }
        }
        #endregion

        #region Button Callbacks - Join
        private void OnConfirmJoinClicked()
        {
            Debug.Log("[MainMenuUI] Confirm join clicked");

            string address = m_ServerAddressInput != null ? m_ServerAddressInput.text : "localhost";
            
            if (string.IsNullOrEmpty(address))
            {
                address = "localhost";
            }

            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.JoinGame(address);
            }
        }

        private void OnCancelJoinClicked()
        {
            Debug.Log("[MainMenuUI] Cancel join clicked");
            ShowMainMenu();
        }
        #endregion

        #region Button Callbacks - Settings
        private void OnCloseSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Close settings clicked");
            ShowMainMenu();
        }
        #endregion

        #region Lobby Events
        private void OnLobbyCreated()
        {
            Debug.Log("[MainMenuUI] Lobby created");
            ShowLobby(true); // true = is host
        }

        private void OnLobbyJoined()
        {
            Debug.Log("[MainMenuUI] Lobby joined");
            ShowLobby(false); // false = is client
        }

        private void OnLobbyLeft()
        {
            Debug.Log("[MainMenuUI] Left lobby");
            ShowMainMenu();
        }
        #endregion

        #region Player List
        private void UpdatePlayerCount()
        {
            if (m_PlayerCountText != null)
            {
                int playerCount = 0;
                
                if (Mirror.NetworkServer.active)
                {
                    // If we're the server/host, count connections
                    playerCount = Mirror.NetworkServer.connections.Count;
                }
                else if (Mirror.NetworkClient.active)
                {
                    // If we're a client, we're at least 1
                    playerCount = 1;
                }
                
                m_PlayerCountText.text = $"Players: {playerCount}/4";
            }
        }

        /// <summary>
        /// Called periodically to update player count
        /// </summary>
        private void Update()
        {
            // Update player count if in lobby
            if (m_LobbyPanel != null && m_LobbyPanel.activeSelf)
            {
                UpdatePlayerCount();
            }
        }
        #endregion
    }
}

