using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using BarelyMoved.Network;
namespace BarelyMoved.UI
{
    /// <summary>
    /// Shows disconnection messages when clients get kicked or lose connection
    /// </summary>
    public class DisconnectionMessageUI : MonoBehaviour
    {
        #region Singleton
        public static DisconnectionMessageUI Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private GameObject m_MessagePanel;
        [SerializeField] private TextMeshProUGUI m_MessageText;
        [SerializeField] private Button m_OkButton;

        [Header("Messages")]
        [SerializeField] private string m_KickedMessage = "You have been kicked from the game.";
        [SerializeField] private string m_ConnectionLostMessage = "Connection to server lost.";
        #endregion

        #region Private Fields
        private CursorLockMode m_PreviousCursorLockMode;
        private bool m_PreviousCursorVisible;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Log("[DisconnectionMessageUI] Awake called");

            if (Instance != null && Instance != this)
            {
                Debug.Log("[DisconnectionMessageUI] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
          /*   DontDestroyOnLoad(gameObject); */

            Debug.Log("[DisconnectionMessageUI] Instance created and set to DontDestroyOnLoad");

            // Setup button
            if (m_OkButton != null)
            {
                m_OkButton.onClick.AddListener(OnOkClicked);
                Debug.Log("[DisconnectionMessageUI] OK button listener added");
            }
            else
            {
                Debug.LogWarning("[DisconnectionMessageUI] OK button is null!");
            }

            // Hide panel initially
            Hide();

            // Ensure cursor is visible in main menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[DisconnectionMessageUI] Awake completed");
        }

        private void OnEnable()
        {
            Debug.Log("[DisconnectionMessageUI] OnEnable called");
        }

        void Start()
        {
            if(BarelyMovedNetworkManager.Instance.isDisconnected){
                if(BarelyMovedNetworkManager.Instance.m_WasKicked){
                    ShowKickedMessage();
                    BarelyMovedNetworkManager.Instance.isDisconnected = false;
                    BarelyMovedNetworkManager.Instance.m_WasKicked = false;
                }
                else{
                    ShowConnectionLostMessage();    
                    BarelyMovedNetworkManager.Instance.isDisconnected = false;
                    BarelyMovedNetworkManager.Instance.m_WasKicked = false;
                }
            }
        }
        private void OnDestroy()
        {
            // Clean up button listener
            if (m_OkButton != null)
            {
                m_OkButton.onClick.RemoveListener(OnOkClicked);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Show disconnection message for kicked player
        /// </summary>
        public void ShowKickedMessage()
        {
            Debug.Log("[DisconnectionMessageUI] ShowKickedMessage called");
            ShowMessage(m_KickedMessage);
        }

        /// <summary>
        /// Show disconnection message for connection loss
        /// </summary>
        public void ShowConnectionLostMessage()
        {
            Debug.Log("[DisconnectionMessageUI] ShowConnectionLostMessage called");
            ShowMessage(m_ConnectionLostMessage);
        }

        /// <summary>
        /// Test method to show popup manually
        /// </summary>
        public void TestShowPopup()
        {
            Debug.Log("[DisconnectionMessageUI] TestShowPopup called");
            ShowMessage("Test popup message!");
        }

        /// <summary>
        /// Show custom disconnection message
        /// </summary>
        public void ShowMessage(string _message)
        {
            Debug.Log($"[DisconnectionMessageUI] ShowMessage called with: {_message}");
            Debug.Log($"[DisconnectionMessageUI] MessagePanel: {m_MessagePanel}, MessageText: {m_MessageText}, OkButton: {m_OkButton}");

            if (m_MessagePanel != null)
            {
                m_MessagePanel.SetActive(true);
                Debug.Log($"[DisconnectionMessageUI] MessagePanel activated");
            }
            else
            {
                Debug.LogError("[DisconnectionMessageUI] MessagePanel is null!");
            }

            if (m_MessageText != null)
            {
                m_MessageText.text = _message;
                Debug.Log($"[DisconnectionMessageUI] MessageText set to: {_message}");
            }
            else
            {
                Debug.LogError("[DisconnectionMessageUI] MessageText is null!");
            }

            // Store current cursor state before changing it
            m_PreviousCursorLockMode = Cursor.lockState;
            m_PreviousCursorVisible = Cursor.visible;

            // Unlock cursor so player can click button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log($"[DisconnectionMessageUI] Showing message: {_message}");
        }

        /// <summary>
        /// Hide the message panel
        /// </summary>
        public void Hide()
        {
            if (m_MessagePanel != null)
            {
                m_MessagePanel.SetActive(false);
            }

            // For main menu, always ensure cursor is visible and unlocked
            // Don't restore previous state as it might be from game scene
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[DisconnectionMessageUI] Message hidden, cursor set to visible/unlocked for main menu");
        }
        #endregion

        #region Button Callbacks
        private void OnOkClicked()
        {
            Debug.Log("[DisconnectionMessageUI] Player acknowledged disconnection message");

            // Return to main menu
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "MainMenu")
            {
                Debug.Log("[DisconnectionMessageUI] Loading MainMenu scene...");
                SceneManager.LoadScene("MainMenu");
            }

            Hide();
        }
        #endregion
    }
}
