using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarelyMoved.Interactables;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Confirmation dialog that appears when player tries to finish the level
    /// </summary>
    public class LevelFinishConfirmationUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private GameObject m_DialogPanel;
        [SerializeField] private TextMeshProUGUI m_MessageText;
        [SerializeField] private Button m_ConfirmButton;
        [SerializeField] private Button m_CancelButton;

        [Header("Settings")]
        [SerializeField] private string m_ConfirmMessage = "Are you ready to finish the level and head back?";
        #endregion

        #region Private Fields
        private LevelFinishZone m_FinishZone;
        private CursorLockMode m_PreviousCursorLockMode;
        private bool m_PreviousCursorVisible;
        private BarelyMoved.Player.PlayerInputHandler m_LocalPlayerInput;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Setup buttons
            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (m_CancelButton != null)
            {
                m_CancelButton.onClick.AddListener(OnCancelClicked);
            }

            // Hide dialog initially
            Hide();
        }

        private void Start()
        {
            m_FinishZone = FindFirstObjectByType<LevelFinishZone>();
            FindLocalPlayerInput();
        }

        private void FindLocalPlayerInput()
        {
            // Find the local player's input handler
            var playerInputHandlers = FindObjectsByType<BarelyMoved.Player.PlayerInputHandler>(FindObjectsSortMode.None);
            foreach (var handler in playerInputHandlers)
            {
                if (handler.isLocalPlayer)
                {
                    m_LocalPlayerInput = handler;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (m_CancelButton != null)
            {
                m_CancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Show the confirmation dialog
        /// </summary>
        public void Show()
        {
            if (m_DialogPanel != null)
            {
                m_DialogPanel.SetActive(true);
            }

            if (m_MessageText != null)
            {
                m_MessageText.text = m_ConfirmMessage;
            }

            // Store current cursor state before changing it
            m_PreviousCursorLockMode = Cursor.lockState;
            m_PreviousCursorVisible = Cursor.visible;

            // Unlock cursor so player can click buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable local player input (only affects this player, not others in coop)
            if (m_LocalPlayerInput != null)
            {
                m_LocalPlayerInput.DisableInput();
            }
        }

        /// <summary>
        /// Hide the confirmation dialog
        /// </summary>
        public void Hide()
        {
            if (m_DialogPanel != null)
            {
                m_DialogPanel.SetActive(false);
            }

            // Restore previous cursor state
            Cursor.lockState = m_PreviousCursorLockMode;
            Cursor.visible = m_PreviousCursorVisible;

            // Re-enable local player input
            if (m_LocalPlayerInput != null)
            {
                m_LocalPlayerInput.EnableInput();
            }
        }
        #endregion

        #region Button Callbacks
        private void OnConfirmClicked()
        {
            Debug.Log("[LevelFinishConfirmationUI] Player confirmed finish");

            // Tell the finish zone to complete the level
            if (m_FinishZone != null)
            {
                m_FinishZone.CmdConfirmFinish();
            }

            Hide();
        }

        private void OnCancelClicked()
        {
            Debug.Log("[LevelFinishConfirmationUI] Player cancelled finish");
            Hide();
        }
        #endregion
    }
}

