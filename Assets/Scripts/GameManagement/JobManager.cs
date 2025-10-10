using UnityEngine;
using Mirror;
using BarelyMoved.Items;
using BarelyMoved.Interactables;
using System.Collections.Generic;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Manages the current moving job
    /// Tracks time, required items, scoring, etc.
    /// Server authoritative
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class JobManager : NetworkBehaviour
    {
        #region Singleton
        public static JobManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Job Settings")]
        [SerializeField] private float m_JobTimeLimit = 600f; // 10 minutes
        [SerializeField] private DeliveryZone m_DeliveryZone;

        [Header("Items")]
        [SerializeField] private List<GrabbableItem> m_RequiredItems = new List<GrabbableItem>();
        #endregion

        #region SyncVars
        [SyncVar] private float m_TimeRemaining;
        [SyncVar] private bool m_JobActive;
        [SyncVar] private float m_FinalScore;
        #endregion

        #region Properties
        public float TimeRemaining => m_TimeRemaining;
        public bool JobActive => m_JobActive;
        public int TotalItemsRequired => m_RequiredItems.Count;
        public int ItemsDelivered => m_DeliveryZone != null ? m_DeliveryZone.DeliveredItemCount : 0;
        public float CompletionPercentage => m_DeliveryZone != null ? m_DeliveryZone.GetCompletionPercentage(TotalItemsRequired) : 0f;
        public float FinalScore => m_FinalScore;
        #endregion

        #region Events
        public delegate void JobEventDelegate();
        public event JobEventDelegate OnJobStarted;
        public event JobEventDelegate OnJobCompleted;
        public event JobEventDelegate OnJobFailed;
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

            // Configure NetworkIdentity for server-only authority
            NetworkIdentity networkIdentity = GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                networkIdentity.serverOnly = true; // This object only exists on server
            }
        }

        private void Update()
        {
            if (!isServer || !m_JobActive) return;

            UpdateTimer();
        }
        #endregion

        #region Job Control
        [Server]
        public void StartJob()
        {
            if (m_JobActive)
            {
                Debug.LogWarning("[JobManager] Job already active!");
                return;
            }

            m_TimeRemaining = m_JobTimeLimit;
            m_JobActive = true;
            m_FinalScore = 0f;

            if (m_DeliveryZone != null)
            {
                m_DeliveryZone.ResetZone();
            }

            Debug.Log("[JobManager] Job started!");
            RpcOnJobStarted();
        }

        [Server]
        private void CompleteJob()
        {
            m_JobActive = false;
            CalculateFinalScore();

            Debug.Log($"[JobManager] Job completed! Score: {m_FinalScore:F2}");
            RpcOnJobCompleted(m_FinalScore);
        }

        [Server]
        private void FailJob()
        {
            m_JobActive = false;
            m_FinalScore = 0f;

            Debug.Log("[JobManager] Job failed - Time's up!");
            RpcOnJobFailed();
        }
        #endregion

        #region Timer
        private void UpdateTimer()
        {
            m_TimeRemaining -= Time.deltaTime;

            if (m_TimeRemaining <= 0f)
            {
                m_TimeRemaining = 0f;
                FailJob();
                return;
            }

            // Check completion
            if (ItemsDelivered >= TotalItemsRequired)
            {
                CompleteJob();
            }
        }
        #endregion

        #region Scoring
        private void CalculateFinalScore()
        {
            if (m_DeliveryZone == null)
            {
                m_FinalScore = 0f;
                return;
            }

            float totalValue = m_DeliveryZone.TotalValue;
            float timeBonus = m_TimeRemaining * 10f; // Bonus for finishing early
            
            m_FinalScore = totalValue + timeBonus;
        }
        #endregion

        #region Network Callbacks
        [ClientRpc]
        private void RpcOnJobStarted()
        {
            OnJobStarted?.Invoke();
        }

        [ClientRpc]
        private void RpcOnJobCompleted(float _score)
        {
            OnJobCompleted?.Invoke();
        }

        [ClientRpc]
        private void RpcOnJobFailed()
        {
            OnJobFailed?.Invoke();
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Start Job")]
        private void DebugStartJob()
        {
            if (isServer)
                StartJob();
            else
                Debug.LogWarning("Can only start job on server!");
        }

        [ContextMenu("Debug Job Info")]
        private void DebugJobInfo()
        {
            Debug.Log($"Job Active: {m_JobActive}");
            Debug.Log($"Time Remaining: {m_TimeRemaining:F1}s");
            Debug.Log($"Items: {ItemsDelivered}/{TotalItemsRequired}");
            Debug.Log($"Completion: {CompletionPercentage:F1}%");
        }
        #endif
    }
}

