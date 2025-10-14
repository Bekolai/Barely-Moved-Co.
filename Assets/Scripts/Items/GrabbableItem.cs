using UnityEngine;
using Mirror;

namespace BarelyMoved.Items
{
	public interface ICarryController
	{
		void StartCarry(Vector3 _position, Quaternion _rotation);
		void UpdateTarget(Vector3 _position, Quaternion _rotation);
		void StopCarry(Vector3 _releaseVelocity);
		void ForceStopCarry();
	}

    /// <summary>
    /// Base class for all grabbable items in the game
    /// Handles network synchronization, damage tracking, and physics
    /// Server is authoritative for all physics simulation
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CarryPhysicsController))]
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

		#region Carry/Damage Tuning
		[Header("Carry & Damage Tuning")]
		[SerializeField, Range(0f, 1f)] private float m_DropOnImpactFraction = 0.5f; // drop if single-hit damage >= 50% of remaining value window
		[SerializeField] private float m_CarriedDamageMultiplier = 1f; // damage multiplier while carried
		[SerializeField] private float m_RecoilImpulseScale = 3f; // impulse applied to item on collision while carried
		#endregion

        #region Properties
        public ItemData Data => m_ItemData;
        public ItemSize Size => m_Size;
        public Transform[] GrabPoints => m_GrabPoints;
        public float CurrentValue => m_CurrentValue;
        public bool IsGrabbed => m_IsGrabbed;
        public bool IsBroken => m_CurrentValue <= m_ItemData.MinValue;
        public virtual bool CanBeGrabbed => !m_IsGrabbed && !IsBroken;

        private float m_LastReleaseTime;
        private bool m_WasThrown; // Track if item was thrown vs dropped
        private const float c_ReleaseGracePeriod = 0.5f; // Seconds of immunity after being released
        private const float c_MinDamageVelocity = 5f; // Minimum velocity to cause damage
        private const float c_ThrowDamageMultiplier = 2f; // Extra damage for thrown items
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
        /// <summary>
        /// Mark item as recently released (not thrown)
        /// </summary>
        [Server]
        public void MarkAsReleased()
        {
            m_LastReleaseTime = Time.time;
            m_WasThrown = false;
        }

        /// <summary>
        /// Mark item as thrown
        /// </summary>
        [Server]
        public void MarkAsThrown()
        {
            m_LastReleaseTime = Time.time;
            m_WasThrown = true;
        }
            
        protected virtual void Start()
        {
            // Server sets up physics
            if (isServer)
            {
                SetupPhysics();
                // Mark as released initially (for spawn grace period)
                MarkAsReleased();
            }
        }

		protected virtual void OnCollisionEnter(Collision _collision)
        {
            // Only server processes collisions
            if (!isServer) return;
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
			// Keep physics active while grabbed (Skyrim/REPO feel)
			m_Rigidbody.isKinematic = false;
			m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			
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

			// Ensure physics is enabled
			m_Rigidbody.isKinematic = false;
            m_Rigidbody.linearVelocity = _releaseVelocity;

            // Mark as released (not thrown)
            MarkAsReleased();

            Debug.Log($"[GrabbableItem] {gameObject.name} released");

			// Notify clients to clear carry state
			RpcOnReleased();
        }

        /// <summary>
        /// Called when thrown (Server only)
        /// </summary>
        [Server]
        public virtual void Throw(Vector3 _throwVelocity)
        {
			// Ensure any carry joint is disabled before throwing
			var controller = GetComponent<ICarryController>();
			if (controller != null)
			{
				controller.ForceStopCarry();
			}
			Release(_throwVelocity);

            // Mark as thrown for damage calculation
            MarkAsThrown();

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

            // Don't take damage during release grace period (for gentle drops)
            // BUT only if item was NOT thrown - thrown items can take damage immediately
            if (!m_WasThrown && Time.time - m_LastReleaseTime < c_ReleaseGracePeriod) return;

            // Don't take damage for low-velocity impacts (gentle collisions)
            if (impactVelocity < c_MinDamageVelocity) return;

			// Calculate base damage
			float damage = m_ItemData.CalculateDamage(impactVelocity);
			if (m_IsGrabbed)
			{
				damage *= m_CarriedDamageMultiplier;
			}

            // Apply extra damage if item was thrown
            if (m_WasThrown)
            {
                damage *= c_ThrowDamageMultiplier;
            }

			if (damage > 0f)
            {
                ApplyDamage(damage);

                // Optional: Spawn visual/audio feedback
                OnDamageReceived(damage, _collision.GetContact(0).point);

				// Apply recoil impulse while carried
				if (m_IsGrabbed)
				{
					var contact = _collision.GetContact(0);
					Vector3 recoil = -contact.normal * m_RecoilImpulseScale * Mathf.Clamp(impactVelocity, 0f, 20f);
					m_Rigidbody.AddForceAtPosition(recoil, contact.point, ForceMode.Impulse);
				}
            }

			// Forced drop on large single-hit damage while carried or any time
			float remainingWindow = Mathf.Max(0.0001f, (m_CurrentValue - m_ItemData.MinValue));
			if (damage >= m_DropOnImpactFraction * remainingWindow || IsBroken)
			{
				// Fully drop on server (disable carry + release with current velocity)
				ForceDrop(m_Rigidbody.linearVelocity);
			}
        }

		#region Carry Control
		/// <summary>
		/// Server-side forced drop helper (called by external systems)
		/// </summary>
		[Server]
		public void ForceDrop(Vector3 _releaseVelocity)
		{
			var controller = GetComponent<ICarryController>();
			if (controller != null)
			{
				controller.ForceStopCarry();
			}
			Release(_releaseVelocity);
		}
		#endregion

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

		[ClientRpc]
		protected void RpcOnReleased()
		{
			// Hook for clients to react to release (clears local carry on holders)
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

