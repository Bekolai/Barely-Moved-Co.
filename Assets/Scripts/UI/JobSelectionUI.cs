using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarelyMoved.GameManagement;
using Mirror;

namespace BarelyMoved.UI
{
    /// <summary>
    /// UI for selecting jobs in the prep scene
    /// Shows available jobs with rewards and difficulty
    /// </summary>
    public class JobSelectionUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private GameObject m_JobBoardPanel;
        [SerializeField] private TextMeshProUGUI m_JobTitleText;
        [SerializeField] private TextMeshProUGUI m_JobDescriptionText;
        [SerializeField] private TextMeshProUGUI m_JobRewardText;
        [SerializeField] private TextMeshProUGUI m_JobDifficultyText;
        [SerializeField] private Button m_StartJobButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Job Selection")]
        [SerializeField] private Button[] m_JobButtons;

        [Header("Temporary Job Data")]
        [SerializeField] private string[] m_JobTitles = new string[] { "Apartment Move", "Office Relocation", "Mansion Moving" };
        [SerializeField] private string[] m_JobDescriptions = new string[] 
        {
            "Help a family move to a new apartment. Moderate furniture, tight stairs.",
            "Relocate an entire office. Lots of boxes, some fragile equipment.",
            "Move a wealthy client's mansion. High-value items, multiple floors!"
        };
        [SerializeField] private int[] m_JobRewards = new int[] { 1000, 2500, 5000 };
        #endregion

        #region Private Fields
        private int m_SelectedJobIndex = 0;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupButtons();
            HideJobBoard();
        }

        private void Start()
        {
            // Set default job selection
            if (m_JobButtons != null && m_JobButtons.Length > 0)
            {
                SelectJob(0);
            }
        }
        #endregion

        #region Setup
        private void SetupButtons()
        {
            // Setup job buttons
            if (m_JobButtons != null)
            {
                for (int i = 0; i < m_JobButtons.Length; i++)
                {
                    int index = i; // Capture for closure
                    if (m_JobButtons[i] != null)
                    {
                        m_JobButtons[i].onClick.AddListener(() => OnJobButtonClicked(index));
                    }
                }
            }

            // Setup control buttons
            if (m_StartJobButton != null)
            {
                m_StartJobButton.onClick.AddListener(OnStartJobClicked);
            }

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(OnCloseClicked);
            }
        }
        #endregion

        #region UI Control
        /// <summary>
        /// Show the job board UI
        /// </summary>
        public void ShowJobBoard()
        {
            if (m_JobBoardPanel != null)
            {
                m_JobBoardPanel.SetActive(true);
            }

            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player input
            DisableLocalPlayerInput();

            UpdateJobDisplay();
        }

        /// <summary>
        /// Hide the job board UI
        /// </summary>
        public void HideJobBoard()
        {
            if (m_JobBoardPanel != null)
            {
                m_JobBoardPanel.SetActive(false);
            }

            // Lock cursor back for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Re-enable player input
            EnableLocalPlayerInput();
        }
        #endregion

        #region Job Selection
        private void SelectJob(int _index)
        {
            if (_index < 0 || _index >= m_JobTitles.Length)
            {
                Debug.LogWarning($"[JobSelectionUI] Invalid job index: {_index}");
                return;
            }

            m_SelectedJobIndex = _index;
            UpdateJobDisplay();
        }

        private void UpdateJobDisplay()
        {
            if (m_SelectedJobIndex < 0 || m_SelectedJobIndex >= m_JobTitles.Length)
                return;

            // Update job info
            if (m_JobTitleText != null)
            {
                m_JobTitleText.text = m_JobTitles[m_SelectedJobIndex];
            }

            if (m_JobDescriptionText != null)
            {
                m_JobDescriptionText.text = m_JobDescriptions[m_SelectedJobIndex];
            }

            if (m_JobRewardText != null)
            {
                m_JobRewardText.text = $"Reward: ${m_JobRewards[m_SelectedJobIndex]}";
            }

            if (m_JobDifficultyText != null)
            {
                string difficulty = m_SelectedJobIndex == 0 ? "Easy" : (m_SelectedJobIndex == 1 ? "Medium" : "Hard");
                m_JobDifficultyText.text = $"Difficulty: {difficulty}";
            }

            // Only host can start job
            if (m_StartJobButton != null)
            {
                bool isHost = NetworkServer.active;
                m_StartJobButton.interactable = isHost;
                
                // Update button text if not host
                var buttonText = m_StartJobButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && !isHost)
                {
                    buttonText.text = "Waiting for Host...";
                }
                else if (buttonText != null)
                {
                    buttonText.text = "Start Job";
                }
            }
        }
        #endregion

        #region Button Callbacks
        private void OnJobButtonClicked(int _jobIndex)
        {
            Debug.Log($"[JobSelectionUI] Selected job {_jobIndex}");
            SelectJob(_jobIndex);
        }

        private void OnStartJobClicked()
        {
            Debug.Log($"[JobSelectionUI] Starting job: {m_JobTitles[m_SelectedJobIndex]}");

            // Only host can start
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[JobSelectionUI] Only host can start jobs!");
                return;
            }

            HideJobBoard();

            // Trigger job start through prep scene manager
            if (PrepSceneManager.Instance != null)
            {
                PrepSceneManager.Instance.StartJob();
            }
        }

        private void OnCloseClicked()
        {
            Debug.Log("[JobSelectionUI] Closing job board");
            HideJobBoard();
        }
        #endregion

        #region Player Input Management
        private void DisableLocalPlayerInput()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                var inputHandler = localPlayer.GetComponent<BarelyMoved.Player.PlayerInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.DisableInput();
                }
            }
        }

        private void EnableLocalPlayerInput()
        {
            var localPlayer = NetworkClient.localPlayer;
            if (localPlayer != null)
            {
                var inputHandler = localPlayer.GetComponent<BarelyMoved.Player.PlayerInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.EnableInput();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Toggle job board visibility
        /// </summary>
        public void ToggleJobBoard()
        {
            if (m_JobBoardPanel != null && m_JobBoardPanel.activeSelf)
            {
                HideJobBoard();
            }
            else
            {
                ShowJobBoard();
            }
        }
        #endregion
    }
}

