using UnityEngine;
using Mirror;
using BarelyMoved.Items;
using System.Collections.Generic;

namespace BarelyMoved.Interactables
{
    /// <summary>
    /// Delivery zone where items must be delivered to complete the job
    /// Tracks delivered items and their condition
    /// Server authoritative
    /// </summary>
    public class DeliveryZone : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Zone Settings")]
        [SerializeField] private Transform m_DropZone;
        [SerializeField] private Vector3 m_ZoneSize = new Vector3(5f, 3f, 5f);
        [SerializeField] private LayerMask m_ItemLayer;

        [Header("Visual Feedback")]
        [SerializeField] private Color m_ZoneColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Renderer m_ZoneRenderer;
        #endregion

        #region Private Fields
        private HashSet<GrabbableItem> m_ItemsInZone = new HashSet<GrabbableItem>();
        private Collider m_TriggerCollider;
        #endregion

        #region SyncVars
        [SyncVar(hook = nameof(OnDeliveredItemCountChanged))] private int m_DeliveredItemCount;
        [SyncVar(hook = nameof(OnTotalValueChanged))] private float m_TotalValue;
        #endregion

        #region Properties
        public int DeliveredItemCount => m_DeliveredItemCount;
        public float TotalValue => m_TotalValue;
        public List<GrabbableItem> DeliveredItems => new List<GrabbableItem>(m_ItemsInZone);
        #endregion

        #region Events
        public delegate void ZoneStatsChangedDelegate(int _count, float _totalValue);
        public event ZoneStatsChangedDelegate OnZoneStatsChanged;
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
        }

        private float m_NextValidationTime;
        private const float c_ValidationInterval = 0.25f;

        private void Update()
        {
            if (!isServer) return;
            if (Time.time < m_NextValidationTime) return;
            m_NextValidationTime = Time.time + c_ValidationInterval;
            ValidateMembership();
        }

        private void OnTriggerEnter(Collider _other)
        {
            if (!isServer) return;

            GrabbableItem item = _other.GetComponent<GrabbableItem>();
            
            TryAddItem(item);
        }

        private void OnTriggerStay(Collider _other)
        {
            // Handle cases where item is dropped while already inside the zone
            if (!isServer) return;

            GrabbableItem item = _other.GetComponent<GrabbableItem>();
            TryAddItem(item);
        }

        private void OnTriggerExit(Collider _other)
        {
            if (!isServer) return;
            GrabbableItem item = _other.GetComponent<GrabbableItem>();
            if (item == null) return;
            if (m_ItemsInZone.Remove(item))
            {
                RecalculateTotals();
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

        #region Delivery & Tracking

        [Server]
        private void TryAddItem(GrabbableItem _item)
        {
            if (_item == null) return;
            if (!IsLayerAllowed(_item.gameObject.layer)) return;
            if (!IsEligible(_item)) return;
            if (m_ItemsInZone.Add(_item))
            {
                // Optional VFX/SFX notify on first entry
                RpcOnItemDelivered(_item.netId, _item.CurrentValue);
                RecalculateTotals();
            }
        }

        [Server]
        private void ValidateMembership()
        {
            bool changed = false;

            // Remove invalid entries
            if (m_ItemsInZone.Count > 0)
            {
                var snapshot = new List<GrabbableItem>(m_ItemsInZone);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var item = snapshot[i];
                    if (item == null || !IsEligible(item) || !IsInsideZone(item))
                    {
                        m_ItemsInZone.Remove(item);
                        changed = true;
                    }
                }
            }

            // Discover missed items (e.g., destroyed colliders skip exits)
            Transform zoneTransform = m_DropZone != null ? m_DropZone : transform;
            Collider[] overlaps = Physics.OverlapBox(zoneTransform.position, m_ZoneSize * 0.5f, zoneTransform.rotation, m_ItemLayer);
            for (int i = 0; i < overlaps.Length; i++)
            {
                var item = overlaps[i].GetComponent<GrabbableItem>();
                if (item != null && IsEligible(item))
                {
                    if (m_ItemsInZone.Add(item)) changed = true;
                }
            }

            if (changed)
            {
                RecalculateTotals();
            }
            else
            {
                // Even without set membership change, values of items may have changed
                // (damage, breaks). Recompute visible totals from current snapshot.
                RecalculateTotals();
            }
        }

        [Server]
        private void RecalculateTotals()
        {
            int count = 0;
            float total = 0f;
            if (m_ItemsInZone.Count > 0)
            {
                var snapshot = new List<GrabbableItem>(m_ItemsInZone);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var item = snapshot[i];
                    if (item == null) continue;
                    if (!IsEligible(item)) continue;
                    if (!IsInsideZone(item)) continue;
                    count++;
                    total += item.CurrentValue;
                }
            }

            m_DeliveredItemCount = count;
            m_TotalValue = total;
        }

        private bool IsLayerAllowed(int layer)
        {
            int mask = m_ItemLayer.value;
            return (mask & (1 << layer)) != 0;
        }

        private bool IsEligible(GrabbableItem item)
        {
            if (item == null) return false;
            if (item.IsGrabbed) return false;
            if (item.IsBroken) return false;
            if (!item.gameObject.activeInHierarchy) return false;
            return true;
        }

        private bool IsInsideZone(GrabbableItem item)
        {
            Transform zoneTransform = m_DropZone != null ? m_DropZone : transform;
            Vector3 halfExtents = m_ZoneSize * 0.5f;
            // Quick physics check using bounds vs overlap box
            var cols = item.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null || !col.enabled) continue;
                if (Physics.ComputePenetration(
                    col, col.transform.position, col.transform.rotation,
                    m_TriggerCollider, zoneTransform.position, zoneTransform.rotation,
                    out _, out _))
                {
                    return true;
                }
            }
            // Fallback to OverlapBox check on item position
            return Physics.OverlapBox(zoneTransform.position, halfExtents, zoneTransform.rotation, m_ItemLayer).Length > 0;
        }

        [ClientRpc]
        private void RpcOnItemDelivered(uint _itemNetId, float _value)
        {
            if (NetworkClient.spawned.TryGetValue(_itemNetId, out NetworkIdentity itemIdentity))
            {
                GrabbableItem item = itemIdentity.GetComponent<GrabbableItem>();
                
                // Play delivery VFX/SFX here
                Debug.Log($"[DeliveryZone] Client received delivery notification for {item.name}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reset the delivery zone (for new jobs)
        /// </summary>
        [Server]
        public void ResetZone()
        {
            m_ItemsInZone.Clear();
            m_DeliveredItemCount = 0;
            m_TotalValue = 0f;
            
            Debug.Log("[DeliveryZone] Zone reset");
        }

        /// <summary>
        /// Check if a specific item has been delivered
        /// </summary>
        public bool IsItemDelivered(GrabbableItem _item)
        {
            return _item != null && m_ItemsInZone.Contains(_item);
        }

        /// <summary>
        /// Get delivery completion percentage
        /// </summary>
        public float GetCompletionPercentage(int _totalRequiredItems)
        {
            if (_totalRequiredItems <= 0) return 0f;
            return (float)m_DeliveredItemCount / _totalRequiredItems * 100f;
        }
        #endregion

        #region SyncVar Hooks
        private void OnDeliveredItemCountChanged(int _old, int _new)
        {
            OnZoneStatsChanged?.Invoke(_new, m_TotalValue);
        }

        private void OnTotalValueChanged(float _old, float _new)
        {
            OnZoneStatsChanged?.Invoke(m_DeliveredItemCount, _new);
        }
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Transform zoneTransform = m_DropZone != null ? m_DropZone : transform;

            Gizmos.color = m_ZoneColor;
            Gizmos.DrawCube(zoneTransform.position, m_ZoneSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(zoneTransform.position, m_ZoneSize);
        }

        protected override void OnValidate()
        {
            if (m_DropZone == null)
            {
                m_DropZone = transform;
            }
        }
        #endif
    }
}

