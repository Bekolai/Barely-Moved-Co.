using UnityEngine;
using Mirror;

namespace BarelyMoved.Items
{
    /// <summary>
    /// Base class for all grabbable items in the game
    /// Handles network synchronization, damage tracking, and physics
    /// Server is authoritative for all physics simulation
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableItem : NetworkBehaviour
    {
        #region Enums
        public enum ItemSize
        {
            Small,      // 1 player can carry
            Large       // 2 players required
        }
        #endregion

        #region Serialized Fields
        [Header("Item Configuration")]
        [SerializeField] protected ItemData m_ItemData;
        [SerializeField] protected ItemSize m_Size = ItemSize.Small;
        
        [Header("Grab Points")]
        [SerializeField] protected Transform[] m_GrabPoints;
        [SerializeField] protected Transform m_VisualRoot;
        #endregion

        #region Protected Fields
        protected Rigidbody m_Rigidbody;
        protected Collider[] m_Colliders;
        #endregion

        #region SyncVars
        [SyncVar] protected float m_CurrentValue;
        [SyncVar] protected bool m_IsGrabbed;
        [SyncVar] protected uint m_GrabbedByPlayerID; // NetworkIdentity netId
        #endregion

        #region Properties
        public ItemData Data => m_ItemData;
        public ItemSize Size => m_Size;
        public Transform[] GrabPoints => m_GrabPoints;
        public float CurrentValue => m_CurrentValue;
        public bool IsGrabbed => m_IsGrabbed;
        public bool IsBroken => m_CurrentValue <= m_ItemData.MinValue;
        public virtual bool CanBeGrabbed => !m_IsGrabbed && !IsBroken;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Colliders = GetComponentsInChildren<Collider>();
            
            // Set initial value
            if (m_ItemData != null)
            {
                m_CurrentValue = m_ItemData.BaseValue;
            }
        }

        protected virtual void Start()
        {
            // Server sets up physics
            if (isServer)
            {
                SetupPhysics();
            }
        }

        protected virtual void OnCollisionEnter(Collision _collision)
        {
            // Only server processes collisions
            if (!isServer) return;
            if (m_IsGrabbed) return; // Don't take damage while being carried
            
            ProcessCollisionDamage(_collision);
        }
        #endregion

        #region Physics Setup
        protected virtual void SetupPhysics()
        {
            if (m_ItemData != null)
            {
                m_Rigidbody.mass = m_ItemData.Mass;
            }
        }
        #endregion

        #region Grab/Release
        /// <summary>
        /// Called when a player grabs this item (Server only)
        /// </summary>
        [Server]
        public virtual bool TryGrab(uint _playerNetID)
        {
            if (!CanBeGrabbed)
            {
                return false;
            }

            m_IsGrabbed = true;
            m_GrabbedByPlayerID = _playerNetID;
            
            // Disable physics while grabbed
            m_Rigidbody.isKinematic = true;
            
            Debug.Log($"[GrabbableItem] {gameObject.name} grabbed by player {_playerNetID}");
            return true;
        }

        /// <summary>
        /// Called when a player releases this item (Server only)
        /// </summary>
        [Server]
        public virtual void Release(Vector3 _releaseVelocity)
        {
            m_IsGrabbed = false;
            m_GrabbedByPlayerID = 0;
            
            // Re-enable physics
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.linearVelocity = _releaseVelocity;
            
            Debug.Log($"[GrabbableItem] {gameObject.name} released");
        }

        /// <summary>
        /// Called when thrown (Server only)
        /// </summary>
        [Server]
        public virtual void Throw(Vector3 _throwVelocity)
        {
            Release(_throwVelocity);
            
            // Add extra force for throw
            m_Rigidbody.AddForce(_throwVelocity, ForceMode.Impulse);
            
            Debug.Log($"[GrabbableItem] {gameObject.name} thrown with force {_throwVelocity.magnitude}");
        }
        #endregion

        #region Damage System
        protected virtual void ProcessCollisionDamage(Collision _collision)
        {
            if (m_ItemData == null) return;

            float impactVelocity = _collision.relativeVelocity.magnitude;
            float damage = m_ItemData.CalculateDamage(impactVelocity);

            if (damage > 0f)
            {
                ApplyDamage(damage);
                
                // Optional: Spawn visual/audio feedback
                OnDamageReceived(damage, _collision.GetContact(0).point);
            }
        }

        [Server]
        public void ApplyDamage(float _damage)
        {
            m_CurrentValue = Mathf.Max(m_ItemData.MinValue, m_CurrentValue - _damage);
            
            Debug.Log($"[GrabbableItem] {gameObject.name} took {_damage} damage. Value: {m_CurrentValue}");

            if (IsBroken)
            {
                OnItemBroken();
            }
        }

        protected virtual void OnDamageReceived(float _damage, Vector3 _hitPoint)
        {
            // Override in derived classes for VFX/SFX
        }

        protected virtual void OnItemBroken()
        {
            Debug.Log($"[GrabbableItem] {gameObject.name} is broken!");
            
            // Optional: Trigger broken state visuals
            RpcOnItemBroken();
        }

        [ClientRpc]
        protected virtual void RpcOnItemBroken()
        {
            // Client-side broken state (VFX, SFX, etc.)
        }
        #endregion

        #region Network Synchronization
        /// <summary>
        /// Update item transform on server (called by grab system)
        /// </summary>
        [Server]
        public void UpdatePosition(Vector3 _position, Quaternion _rotation)
        {
            if (m_IsGrabbed)
            {
                transform.position = _position;
                transform.rotation = _rotation;
            }
        }
        #endregion

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            // Auto-find grab points if not set
            if (m_GrabPoints == null || m_GrabPoints.Length == 0)
            {
                Transform grabPointsParent = transform.Find("GrabPoints");
                if (grabPointsParent != null)
                {
                    m_GrabPoints = grabPointsParent.GetComponentsInChildren<Transform>();
                }
            }
        }

        protected virtual void OnDrawGizmos()
        {
            // Draw grab points
            if (m_GrabPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in m_GrabPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.1f);
                    }
                }
            }
        }
        #endif
    }
}

