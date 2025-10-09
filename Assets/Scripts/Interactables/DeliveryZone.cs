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
        private List<GrabbableItem> m_DeliveredItems = new List<GrabbableItem>();
        private Collider m_TriggerCollider;
        #endregion

        #region SyncVars
        [SyncVar] private int m_DeliveredItemCount;
        [SyncVar] private float m_TotalValue;
        #endregion

        #region Properties
        public int DeliveredItemCount => m_DeliveredItemCount;
        public float TotalValue => m_TotalValue;
        public List<GrabbableItem> DeliveredItems => new List<GrabbableItem>(m_DeliveredItems);
        #endregion

        #region Events
        public delegate void ItemDeliveredDelegate(GrabbableItem _item, float _value);
        public event ItemDeliveredDelegate OnItemDelivered;
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

        private void OnTriggerEnter(Collider _other)
        {
            if (!isServer) return;

            GrabbableItem item = _other.GetComponent<GrabbableItem>();
            
            if (item != null && !item.IsGrabbed && !m_DeliveredItems.Contains(item))
            {
                DeliverItem(item);
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

        #region Delivery
        [Server]
        private void DeliverItem(GrabbableItem _item)
        {
            if (_item == null || m_DeliveredItems.Contains(_item))
                return;

            float itemValue = _item.CurrentValue;
            
            m_DeliveredItems.Add(_item);
            m_DeliveredItemCount = m_DeliveredItems.Count;
            m_TotalValue += itemValue;

            Debug.Log($"[DeliveryZone] Item delivered: {_item.name}, Value: {itemValue:F2}, Total: {m_TotalValue:F2}");

            // Disable item physics once delivered
            if (_item.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
            }

            // Notify clients
            RpcOnItemDelivered(_item.netId, itemValue);

            OnItemDelivered?.Invoke(_item, itemValue);
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
            m_DeliveredItems.Clear();
            m_DeliveredItemCount = 0;
            m_TotalValue = 0f;
            
            Debug.Log("[DeliveryZone] Zone reset");
        }

        /// <summary>
        /// Check if a specific item has been delivered
        /// </summary>
        public bool IsItemDelivered(GrabbableItem _item)
        {
            return m_DeliveredItems.Contains(_item);
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

