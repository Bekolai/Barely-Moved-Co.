using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace BarelyMoved.Items
{
    /// <summary>
    /// Item that requires TWO players to carry
    /// Furniture like couches, fridges, etc.
    /// Both players must coordinate movement
    /// </summary>
    public class DualPlayerItem : GrabbableItem
    {
        #region Serialized Fields
        [Header("Dual Player Settings")]
        [SerializeField] private Transform m_FrontGrabPoint;
        [SerializeField] private Transform m_BackGrabPoint;
        [SerializeField] private float m_MovementSpeed = 2f;
        [SerializeField] private float m_RotationSpeed = 2f;
        #endregion

        #region SyncVars
        [SyncVar] private uint m_FrontPlayerID;
        [SyncVar] private uint m_BackPlayerID;
        #endregion

        #region Properties
        public Transform FrontGrabPoint => m_FrontGrabPoint;
        public Transform BackGrabPoint => m_BackGrabPoint;
        public bool HasBothPlayers => m_FrontPlayerID != 0 && m_BackPlayerID != 0;
        public override bool CanBeGrabbed => !IsBroken; // Can grab even if one player is holding
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            m_Size = ItemSize.Large;
            
            // Setup grab points array
            if (m_FrontGrabPoint != null && m_BackGrabPoint != null)
            {
                m_GrabPoints = new Transform[] { m_FrontGrabPoint, m_BackGrabPoint };
            }
        }

        private void Update()
        {
            // Only server simulates movement
            if (!isServer) return;
            
            // If both players are grabbing, item is held in place by grab system
            // Movement is handled by averaging player positions
        }
        #endregion

        #region Grab/Release Override
        [Server]
        public override bool TryGrab(uint _playerNetID)
        {
            if (IsBroken)
                return false;

            // Assign to first available slot
            if (m_FrontPlayerID == 0)
            {
                m_FrontPlayerID = _playerNetID;
                Debug.Log($"[DualPlayerItem] Front player {_playerNetID} grabbed {gameObject.name}");
            }
            else if (m_BackPlayerID == 0)
            {
                m_BackPlayerID = _playerNetID;
                Debug.Log($"[DualPlayerItem] Back player {_playerNetID} grabbed {gameObject.name}");
            }
            else
            {
                // Both slots occupied
                return false;
            }

            // Update grabbed state
            m_IsGrabbed = HasBothPlayers;
            
            if (HasBothPlayers)
            {
                m_Rigidbody.isKinematic = true;
                Debug.Log($"[DualPlayerItem] {gameObject.name} now held by both players!");
            }
            else
            {
                // Only one player, keep physics active but add constraint
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            return true;
        }

        [Server]
        public override void Release(Vector3 _releaseVelocity)
        {
            // Clear both players
            m_FrontPlayerID = 0;
            m_BackPlayerID = 0;
            
            base.Release(_releaseVelocity);
        }

        /// <summary>
        /// Release a specific player from the item
        /// </summary>
        [Server]
        public void ReleasePlayer(uint _playerNetID)
        {
            if (m_FrontPlayerID == _playerNetID)
            {
                m_FrontPlayerID = 0;
                Debug.Log($"[DualPlayerItem] Front player {_playerNetID} released {gameObject.name}");
            }
            else if (m_BackPlayerID == _playerNetID)
            {
                m_BackPlayerID = 0;
                Debug.Log($"[DualPlayerItem] Back player {_playerNetID} released {gameObject.name}");
            }

            // Update state
            m_IsGrabbed = HasBothPlayers;
            
            if (!HasBothPlayers)
            {
                m_Rigidbody.isKinematic = false;
                
                // If one player still holding, don't drop completely
                if (m_FrontPlayerID != 0 || m_BackPlayerID != 0)
                {
                    m_Rigidbody.linearVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Get which grab point a player should use
        /// </summary>
        public Transform GetGrabPointForPlayer(uint _playerNetID)
        {
            if (m_FrontPlayerID == _playerNetID)
                return m_FrontGrabPoint;
            else if (m_BackPlayerID == _playerNetID)
                return m_BackGrabPoint;
            
            // Assign to first available
            if (m_FrontPlayerID == 0)
                return m_FrontGrabPoint;
            else if (m_BackPlayerID == 0)
                return m_BackGrabPoint;
            
            return null;
        }
        #endregion

        #region Position Update
        /// <summary>
        /// Update item position based on both players' positions
        /// </summary>
        [Server]
        public void UpdateDualPlayerPosition(Vector3 _frontPlayerPos, Vector3 _backPlayerPos)
        {
            if (!HasBothPlayers) return;

            // Position item between both players
            Vector3 centerPos = (_frontPlayerPos + _backPlayerPos) / 2f;
            
            // Calculate rotation based on player positions
            Vector3 direction = (_frontPlayerPos - _backPlayerPos).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smooth movement
            transform.position = Vector3.Lerp(transform.position, centerPos, m_MovementSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_RotationSpeed * Time.deltaTime);
        }
        #endregion

        #if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw connection between grab points
            if (m_FrontGrabPoint != null && m_BackGrabPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(m_FrontGrabPoint.position, m_BackGrabPoint.position);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(m_FrontGrabPoint.position, 0.15f);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(m_BackGrabPoint.position, 0.15f);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Auto-find grab points by name
            if (m_FrontGrabPoint == null)
            {
                Transform front = transform.Find("GrabPoint_Front");
                if (front != null) m_FrontGrabPoint = front;
            }

            if (m_BackGrabPoint == null)
            {
                Transform back = transform.Find("GrabPoint_Back");
                if (back != null) m_BackGrabPoint = back;
            }
        }
        #endif
    }
}

