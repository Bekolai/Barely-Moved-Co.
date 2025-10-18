using UnityEngine;
using Mirror;
using BarelyMoved.GameManagement;
using BarelyMoved.UI;
using System.Collections.Generic;

namespace BarelyMoved.Interactables
{
    /// <summary>
    /// Interactable zone that allows players to manually finish the level
    /// Shows confirmation dialog before completing
    /// </summary>
    public class LevelFinishZone : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Zone Settings")]
        [SerializeField] private Transform m_InteractionZone;
        [SerializeField] private Vector3 m_ZoneSize = new Vector3(2f, 2f, 2f);
        [SerializeField] private float m_InteractionRange = 3f;
        [SerializeField] private LayerMask m_PlayerLayer;

        [Header("Visual Feedback")]
        [SerializeField] private Color m_ZoneColor = new Color(0f, 0f, 1f, 0.3f);
        [SerializeField] private Renderer m_ZoneRenderer;

        [Header("UI")]
        [SerializeField] private GameObject m_InteractionPromptUI;

        #endregion

        #region Private Fields
        private HashSet<uint> m_PlayersInRange = new HashSet<uint>();
        private Collider m_TriggerCollider;
        private JobManager m_JobManager;
        #endregion

        #region Events
        public delegate void FinishZoneEventDelegate();
        public event FinishZoneEventDelegate OnPlayerEnteredRange;
        public event FinishZoneEventDelegate OnPlayerExitedRange;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_TriggerCollider = GetComponent<Collider>();
            
            if (m_TriggerCollider != null)
            {
                m_TriggerCollider.isTrigger = true;
            }

            SetupVisuals();

            // Hide interaction prompt initially
            if (m_InteractionPromptUI != null)
            {
                m_InteractionPromptUI.SetActive(false);
            }
        }

        private void Start()
        {
            m_JobManager = JobManager.Instance;
        }

        private void OnTriggerEnter(Collider _other)
        {
            if (!isServer) return;

            // Check if it's a player
            if (IsPlayerLayer(_other.gameObject.layer))
            {
                var player = _other.GetComponent<NetworkIdentity>();
                if (player != null)
                {
                    m_PlayersInRange.Add(player.netId);
                    RpcShowInteractionPrompt(player.netId, true);
                }
            }
        }

        private void OnTriggerExit(Collider _other)
        {
            if (!isServer) return;

            if (IsPlayerLayer(_other.gameObject.layer))
            {
                var player = _other.GetComponent<NetworkIdentity>();
                if (player != null)
                {
                    m_PlayersInRange.Remove(player.netId);
                    RpcShowInteractionPrompt(player.netId, false);
                }
            }
        }
        #endregion

        #region Setup
        private void SetupVisuals()
        {
            if (m_ZoneRenderer != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                m_ZoneRenderer.GetPropertyBlock(props);
                props.SetColor("_Color", m_ZoneColor);
                m_ZoneRenderer.SetPropertyBlock(props);
            }
        }
        #endregion

        #region Interaction
        /// <summary>
        /// Called when player presses interact button while in range
        /// Client calls this, which sends a Command to the server to validate and process
        /// </summary>
        public void TryInteract(uint _playerNetId)
        {
            // Send command to server to validate and process interaction
            CmdTryInteract(_playerNetId);
        }

        [Command(requiresAuthority = false)]
        private void CmdTryInteract(uint _playerNetId)
        {
            // Validate on server (where m_PlayersInRange is correctly populated)
            if (!m_PlayersInRange.Contains(_playerNetId))
            {
                Debug.LogWarning($"[LevelFinishZone] Player {_playerNetId} not in range!");
                return;
            }

            // Check if job is active
            if ((m_JobManager == null || !m_JobManager.JobActive) )
            {
                Debug.LogWarning("[LevelFinishZone] Cannot finish - job not active!");
                return;
            }

            // Show confirmation dialog
            RpcShowConfirmationDialog(_playerNetId);
        }

        /// <summary>
        /// Called when player confirms they want to finish
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdConfirmFinish()
        {
            if (m_JobManager == null)
            {
                Debug.LogError("[LevelFinishZone] JobManager not found!");
                return;
            }

            Debug.Log("[LevelFinishZone] Level finish confirmed by player");
            m_JobManager.ManualCompleteJob();
        }

        private bool IsPlayerLayer(int _layer)
        {
            return (m_PlayerLayer.value & (1 << _layer)) != 0;
        }
        #endregion

        #region Network Callbacks
        [ClientRpc]
        private void RpcShowInteractionPrompt(uint _playerNetId, bool _show)
        {
            // Only show for local player
            if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == _playerNetId)
            {
                if (m_InteractionPromptUI != null)
                {
                    m_InteractionPromptUI.SetActive(_show);
                }

                if (_show)
                {
                    OnPlayerEnteredRange?.Invoke();
                }
                else
                {
                    OnPlayerExitedRange?.Invoke();
                }
            }
        }

        [ClientRpc]
        private void RpcShowConfirmationDialog(uint _playerNetId)
        {
            // Only show for local player
            if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == _playerNetId)
            {   
               
                // This will be handled by the UI manager
                var confirmDialog = FindFirstObjectByType<LevelFinishConfirmationUI>();
                if (confirmDialog != null)
                {
                    confirmDialog.Show();
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Check if a player is in interaction range
        /// </summary>
        public bool IsPlayerInRange(uint _playerNetId)
        {
            return m_PlayersInRange.Contains(_playerNetId);
        }
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Transform zoneTransform = m_InteractionZone != null ? m_InteractionZone : transform;

            // Draw interaction zone
            Gizmos.color = m_ZoneColor;
            Gizmos.DrawCube(zoneTransform.position, m_ZoneSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(zoneTransform.position, m_ZoneSize);

            // Draw interaction range sphere
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(zoneTransform.position, m_InteractionRange);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_InteractionZone == null)
            {
                m_InteractionZone = transform;
            }
        }
        #endif
    }
}

