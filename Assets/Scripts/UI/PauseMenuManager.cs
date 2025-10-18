using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using BarelyMoved.Player;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Manages the pause/ESC menu shown during gameplay
    /// Shows lobby panel with connected players and kick options
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Menu Panels")]
        [SerializeField] private GameObject m_PauseMenuRoot;
        [SerializeField] private GameObject m_LobbyPanel;
        [SerializeField] private GameObject m_SettingsPanel;
        
        [Header("Input")]
        [SerializeField] private InputActionReference m_PauseAction;
        
        [Header("Settings")]
        [SerializeField] private bool m_PauseGameWhenOpen = false; // Set false for multiplayer
        [SerializeField] private bool m_LockPlayerInput = true; // Lock player movement during pause
        #endregion

        #region Private Fields
        private bool m_IsPaused = false;
        private bool m_ShowingSettings = false;
        private PlayerInputHandler m_LocalPlayerInput;
        #endregion

        #region Properties
        public bool IsPaused => m_IsPaused;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure pause menu is hidden at start
            if (m_PauseMenuRoot != null)
            {
                m_PauseMenuRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Enable and subscribe to pause action
            if (m_PauseAction != null)
            {
                m_PauseAction.action.Enable();
                m_PauseAction.action.performed += OnPausePerformed;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from pause action
            if (m_PauseAction != null)
            {
                m_PauseAction.action.performed -= OnPausePerformed;
                m_PauseAction.action.Disable();
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext _context)
        {
            TogglePause();
        }
        #endregion

        #region Pause Control
        /// <summary>
        /// Toggle pause menu on/off
        /// </summary>
        public void TogglePause()
        {
            if (m_IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// Show pause menu
        /// </summary>
        public void Pause()
        {
            m_IsPaused = true;
            
            if (m_PauseMenuRoot != null)
            {
                m_PauseMenuRoot.SetActive(true);
            }

            // Show lobby panel by default
            ShowLobbyPanel();

            // Optionally pause game (usually false in multiplayer)
            if (m_PauseGameWhenOpen)
            {
                Time.timeScale = 0f;
            }

            // Lock player input if enabled
            if (m_LockPlayerInput)
            {
                LockPlayerInput();
            }

            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[PauseMenuManager] Paused");
        }

        /// <summary>
        /// Hide pause menu and resume game
        /// </summary>
        public void Resume()
        {
            m_IsPaused = false;
            
            if (m_PauseMenuRoot != null)
            {
                m_PauseMenuRoot.SetActive(false);
            }

            // Restore time scale
            if (m_PauseGameWhenOpen)
            {
                Time.timeScale = 1f;
            }

            // Unlock player input if it was locked
            if (m_LockPlayerInput)
            {
                UnlockPlayerInput();
            }

            // Restore cursor lock (if using first person or locked cursor gameplay)
            // For third-person might want to keep cursor visible
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;

            Debug.Log("[PauseMenuManager] Resumed");
        }
        #endregion

        #region Input Locking
        /// <summary>
        /// Lock local player input
        /// </summary>
        private void LockPlayerInput()
        {
            // Find local player if not cached
            if (m_LocalPlayerInput == null)
            {
                NetworkPlayerData[] allPlayers = FindObjectsByType<NetworkPlayerData>(FindObjectsSortMode.None);
                foreach (var player in allPlayers)
                {
                    if (player.isLocalPlayer)
                    {
                        m_LocalPlayerInput = player.GetComponent<PlayerInputHandler>();
                        break;
                    }
                }
            }

            // Disable input
            if (m_LocalPlayerInput != null)
            {
                m_LocalPlayerInput.DisableInput();
                Debug.Log("[PauseMenuManager] Player input locked");
            }
            else
            {
                Debug.LogWarning("[PauseMenuManager] Could not find local player to lock input");
            }
        }

        /// <summary>
        /// Unlock local player input
        /// </summary>
        private void UnlockPlayerInput()
        {
            if (m_LocalPlayerInput != null)
            {
                m_LocalPlayerInput.EnableInput();
                Debug.Log("[PauseMenuManager] Player input unlocked");
            }
        }
        #endregion

        #region Panel Navigation
        /// <summary>
        /// Show the lobby panel with connected players
        /// </summary>
        public void ShowLobbyPanel()
        {
            if (m_LobbyPanel != null)
            {
                m_LobbyPanel.SetActive(true);
            }

            if (m_SettingsPanel != null)
            {
                m_SettingsPanel.SetActive(false);
            }

            m_ShowingSettings = false;
        }

        /// <summary>
        /// Show settings panel
        /// </summary>
        public void ShowSettings()
        {
            if (m_LobbyPanel != null)
            {
                m_LobbyPanel.SetActive(false);
            }

            if (m_SettingsPanel != null)
            {
                m_SettingsPanel.SetActive(true);
            }

            m_ShowingSettings = true;
        }

        /// <summary>
        /// Return to main menu (disconnect and load main menu scene)
        /// </summary>
        public void ReturnToMainMenu()
        {
            var networkManager = BarelyMoved.Network.BarelyMovedNetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.LeaveGame();
            }

            // Resume time scale before leaving
            Time.timeScale = 1f;

            // Load main menu scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Quit the game application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[PauseMenuManager] Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion
    }
}

