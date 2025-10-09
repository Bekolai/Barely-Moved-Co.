using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using BarelyMoved.GameManagement;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Main game HUD displaying job info, timer, score, etc.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI m_TimerText;
        [SerializeField] private TextMeshProUGUI m_ItemCountText;
        [SerializeField] private TextMeshProUGUI m_ScoreText;
        [SerializeField] private Slider m_ProgressBar;
        
        [Header("Connection Info")]
        [SerializeField] private TextMeshProUGUI m_ConnectionStatusText;
        [SerializeField] private GameObject m_ConnectionPanel;
        #endregion

        #region Private Fields
        private JobManager m_JobManager;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            m_JobManager = JobManager.Instance;
            
            if (m_JobManager == null)
            {
                Debug.LogWarning("[GameHUD] No JobManager found in scene!");
            }
        }

        private void Update()
        {
            UpdateHUD();
            UpdateConnectionStatus();
        }
        #endregion

        #region Update Methods
        private void UpdateHUD()
        {
            if (m_JobManager == null) return;

            // Timer
            if (m_TimerText != null)
            {
                float time = m_JobManager.TimeRemaining;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                m_TimerText.text = $"{minutes:00}:{seconds:00}";
                
                // Change color if running out of time
                if (time < 60f)
                {
                    m_TimerText.color = Color.red;
                }
                else
                {
                    m_TimerText.color = Color.white;
                }
            }

            // Item count
            if (m_ItemCountText != null)
            {
                m_ItemCountText.text = $"Items: {m_JobManager.ItemsDelivered} / {m_JobManager.TotalItemsRequired}";
            }

            // Progress bar
            if (m_ProgressBar != null)
            {
                m_ProgressBar.value = m_JobManager.CompletionPercentage / 100f;
            }

            // Score (only show when job is complete)
            if (m_ScoreText != null && !m_JobManager.JobActive)
            {
                m_ScoreText.text = $"Final Score: ${m_JobManager.FinalScore:F2}";
                m_ScoreText.gameObject.SetActive(true);
            }
            else if (m_ScoreText != null)
            {
                m_ScoreText.gameObject.SetActive(false);
            }
        }

        private void UpdateConnectionStatus()
        {
            if (m_ConnectionStatusText == null) return;

            if (NetworkServer.active && NetworkClient.active)
            {
                m_ConnectionStatusText.text = "HOST";
                m_ConnectionStatusText.color = Color.green;
            }
            else if (NetworkClient.active)
            {
                m_ConnectionStatusText.text = "CLIENT";
                m_ConnectionStatusText.color = Color.cyan;
            }
            else
            {
                m_ConnectionStatusText.text = "NOT CONNECTED";
                m_ConnectionStatusText.color = Color.red;
            }

            // Show/hide connection panel based on status
            if (m_ConnectionPanel != null)
            {
                m_ConnectionPanel.SetActive(!NetworkClient.active);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Called by UI button to create lobby and host
        /// </summary>
        public void OnHostButtonClicked()
        {
            Network.SteamLobbyManager.Instance?.CreateLobby();
        }

        /// <summary>
        /// Called by UI button to invite friends
        /// </summary>
        public void OnInviteFriendsButtonClicked()
        {
            Network.SteamLobbyManager.Instance?.InviteFriends();
        }

        /// <summary>
        /// Called by UI button to leave game
        /// </summary>
        public void OnLeaveButtonClicked()
        {
            Network.BarelyMovedNetworkManager.Instance?.LeaveGame();
        }
        #endregion
    }
}

