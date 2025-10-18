using UnityEngine;
using Mirror;
using BarelyMoved.Interactables;
using BarelyMoved.UI;

namespace BarelyMoved.Player
{
    /// <summary>
    /// Handles player interactions with interactable objects like finish zones
    /// Separate from grab system for clarity
    /// </summary>
    public class PlayerInteractionSystem : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Detection")]
        [SerializeField] private float m_InteractionRange = 3f;
        [SerializeField] private LayerMask m_InteractableLayer;
        [SerializeField] private Transform m_InteractionOrigin;
        [SerializeField] private InteractionPromptUI m_InteractionPromptUI;
        #endregion

        #region Private Fields
        private PlayerInputHandler m_InputHandler;
        private LevelFinishZone m_NearbyFinishZone;
        private JobBoardZone m_NearbyJobBoard;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_InteractionPromptUI = FindFirstObjectByType<InteractionPromptUI>();

            if (m_InteractionOrigin == null)
            {
                m_InteractionOrigin = transform;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            DetectNearbyInteractables();
            HandleInteractionInput();
            
            // Show prompt if near any interactable with dynamic button display
            if(m_NearbyFinishZone == null && m_NearbyJobBoard == null)
            {
                m_InteractionPromptUI?.Hide();
            }
            else if (m_NearbyFinishZone != null)
            {
                // Use ShowWithAction for dynamic button display
                m_InteractionPromptUI?.ShowWithAction("to Finish Level");
            }
            else if (m_NearbyJobBoard != null)
            {
                // Use ShowWithAction for dynamic button display
                m_InteractionPromptUI?.ShowWithAction("to View Jobs");
            }
        }
        #endregion

        #region Detection
        private void DetectNearbyInteractables()
        {
            // Check for interactables in range
            Collider[] colliders = Physics.OverlapSphere(m_InteractionOrigin.position, m_InteractionRange, m_InteractableLayer);
            
            m_NearbyFinishZone = null;
            m_NearbyJobBoard = null;

            foreach (var col in colliders)
            {
                // Check for Level Finish Zone
                LevelFinishZone finishZone = col.GetComponent<LevelFinishZone>();
                if (finishZone != null)
                {
                    m_NearbyFinishZone = finishZone;
                    break; // Prioritize finish zone if both are present
                }
                
                // Check for Job Board Zone
                JobBoardZone jobBoard = col.GetComponent<JobBoardZone>();
                if (jobBoard != null)
                {
                    m_NearbyJobBoard = jobBoard;
                }
            }
        }
        #endregion

        #region Input Handling
        private void HandleInteractionInput()
        {
            if (m_InputHandler == null) return;

            if (m_InputHandler.IsInteractPressed)
            {
                m_InputHandler.ConsumeInteractInput();

                // Prioritize finish zone over job board
                if (m_NearbyFinishZone != null)
                {
                    InteractWithFinishZone();
                }
                else if (m_NearbyJobBoard != null)
                {
                    InteractWithJobBoard();
                }
            }
        }

        private void InteractWithFinishZone()
        {
            if (m_NearbyFinishZone == null) return;

            Debug.Log("[PlayerInteractionSystem] Interacting with finish zone");
            m_NearbyFinishZone.TryInteract(netId);
        }

        private void InteractWithJobBoard()
        {
            if (m_NearbyJobBoard == null) return;

            Debug.Log("[PlayerInteractionSystem] Interacting with job board");
            m_NearbyJobBoard.TryInteract(netId);
        }
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (m_InteractionOrigin == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(m_InteractionOrigin.position, m_InteractionRange);
        }
        #endif
    }
}

