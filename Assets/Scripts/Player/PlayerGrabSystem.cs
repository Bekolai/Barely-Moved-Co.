using UnityEngine;
using Mirror;
using BarelyMoved.Items;
using System.Collections.Generic;

namespace BarelyMoved.Player
{
    /// <summary>
    /// Handles player grabbing, holding, and throwing items
    /// Sends commands to server for physics authority
    /// </summary>
    public class PlayerGrabSystem : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Detection")]
        [SerializeField] private float m_GrabRange = 2f;
        [SerializeField] private LayerMask m_GrabbableLayer;
        [SerializeField] private Transform m_GrabOrigin;

        [Header("Hold Settings")]
        [SerializeField] private Transform m_HoldPosition;
        [SerializeField] private float m_HoldDistance = 1.5f;
        [SerializeField] private float m_ThrowForce = 10f;
		[SerializeField] private float m_MinHoldDistance = 0.6f;
		[SerializeField] private float m_MaxHoldDistance = 3.0f;
		[SerializeField] private float m_VerticalAdjustSpeed = 0.01f; // meters per mouse Y unit
		[SerializeField] private float m_ScrollAdjustSpeed = 0.1f; // meters per scroll step

        [Header("Visual Feedback")]
        [SerializeField] private Color m_HighlightColor = Color.yellow;
        #endregion

        #region Private Fields
		private PlayerInputHandler m_InputHandler;
        private NetworkPlayerController m_PlayerController;
        
        private GrabbableItem m_CurrentlyHeldItem;
		private GrabbableItem m_NearbyItem;
		private float m_DefaultHoldDistance;
		private float m_DefaultVerticalOffset = 0f;
		private Dictionary<Collider, int> m_ItemColliderOriginalLayers = new Dictionary<Collider, int>();
        
        private Renderer m_HighlightedRenderer;
        private Color m_OriginalColor;
        #endregion

        #region Properties
        public bool IsHoldingItem => m_CurrentlyHeldItem != null;
        public GrabbableItem HeldItem => m_CurrentlyHeldItem;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_PlayerController = GetComponent<NetworkPlayerController>();

            if (m_GrabOrigin == null)
                m_GrabOrigin = transform;

			// Cache defaults so each grab starts from a clean baseline
			m_DefaultHoldDistance = m_HoldDistance;
        }

		private void Update()
        {
            if (!isLocalPlayer) return;

            DetectNearbyItems();
			HandleGrabInput();
			UpdateCarryTargetPose();
			CheckForcedDropState();
        }
        #endregion

        #region Detection
        private void DetectNearbyItems()
        {
            // Clear previous highlight
            ClearHighlight();

            // Raycast or sphere cast for nearby items
            RaycastHit hit;
            if (Physics.Raycast(m_GrabOrigin.position, m_GrabOrigin.forward, out hit, m_GrabRange, m_GrabbableLayer))
            {
                GrabbableItem item = hit.collider.GetComponent<GrabbableItem>();
                
                if (item != null && item.CanBeGrabbed && !IsHoldingItem)
                {
                    m_NearbyItem = item;
                    HighlightItem(item);
                }
                else
                {
                    m_NearbyItem = null;
                }
            }
            else
            {
                // Sphere check as fallback
                Collider[] colliders = Physics.OverlapSphere(m_GrabOrigin.position, m_GrabRange, m_GrabbableLayer);
                
                if (colliders.Length > 0)
                {
                    GrabbableItem closest = null;
                    float closestDistance = float.MaxValue;

                    foreach (var col in colliders)
                    {
                        GrabbableItem item = col.GetComponent<GrabbableItem>();
                        if (item != null && item.CanBeGrabbed && !IsHoldingItem)
                        {
                            float distance = Vector3.Distance(m_GrabOrigin.position, item.transform.position);
                            if (distance < closestDistance)
                            {
                                closest = item;
                                closestDistance = distance;
                            }
                        }
                    }

                    if (closest != null)
                    {
                        m_NearbyItem = closest;
                        HighlightItem(closest);
                    }
                    else
                    {
                        m_NearbyItem = null;
                    }
                }
                else
                {
                    m_NearbyItem = null;
                }
            }
        }

        private void HighlightItem(GrabbableItem _item)
        {
            if (_item == null) return;

            Renderer renderer = _item.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                m_HighlightedRenderer = renderer;
                m_OriginalColor = renderer.material.color;
                renderer.material.color = m_HighlightColor;
            }
        }

        private void ClearHighlight()
        {
            if (m_HighlightedRenderer != null)
            {
                m_HighlightedRenderer.material.color = m_OriginalColor;
                m_HighlightedRenderer = null;
            }
        }
        #endregion

        #region Input Handling
        private float m_LastInputTime = 0f;
        private const float c_InputBufferTime = 0.1f; // Minimum time between input processing

        private void HandleGrabInput()
        {
            // Prevent rapid input processing that might cause conflicts
            if (Time.time - m_LastInputTime < c_InputBufferTime) return;
            m_LastInputTime = Time.time;

            // Process inputs in priority order to avoid conflicts
            // Priority: Throw > Drop > Grab

            // Handle throw input first - consume immediately if pressed
            if (m_InputHandler.IsThrowPressed)
            {
                m_InputHandler.ConsumeThrowInput();

                if (IsHoldingItem)
                {
                    // Throw item (only if holding one)
                    ThrowItem();
                    return; // Don't process other inputs this frame
                }
                // If not holding item, throw input is consumed and ignored
                // This prevents throw from being "queued" until an item is grabbed
            }

            // Grab/Drop (medium priority)
            if (m_InputHandler.IsGrabPressed)
            {
                m_InputHandler.ConsumeGrabInput();

                if (IsHoldingItem)
                {
                    // Drop item
                    DropItem();
                }
                else if (m_NearbyItem != null)
                {
                    // Grab item
                    GrabItem(m_NearbyItem);
                }
            }
        }
        #endregion

        #region Grab/Drop/Throw
        private void GrabItem(GrabbableItem _item)
        {
            if (_item == null) return;

            // Check item type
			if (_item is SinglePlayerItem)
            {
                CmdGrabSinglePlayerItem(_item.netId);
            }
            else if (_item is DualPlayerItem)
            {
                CmdGrabDualPlayerItem(_item.netId);
            }
        }

        private void DropItem()
        {
            if (m_CurrentlyHeldItem == null) return;

            Vector3 dropVelocity = m_PlayerController.Velocity;
			CmdDropItem(m_CurrentlyHeldItem.netId, dropVelocity);

			// Restore collision with player locally (client-side immediate)
			RestorePlayerItemCollisions();
        }

        private void ThrowItem()
        {
            if (m_CurrentlyHeldItem == null) return;

			Vector3 throwVelocity = m_GrabOrigin.forward * m_ThrowForce + m_PlayerController.Velocity;
			CmdThrowItem(m_CurrentlyHeldItem.netId, throwVelocity);

			// Locally clear collisions/layer now
			RestorePlayerItemCollisions();
			SetCarriedItemLayer(false);
        }

		private float m_CurrentVerticalOffset = 0f;
		private void UpdateCarryTargetPose()
		{
			if (!IsHoldingItem) return;
			if (m_CurrentlyHeldItem == null) return;

			Vector3 basePosition = (m_HoldPosition != null
				? m_HoldPosition.position
				: m_GrabOrigin.position) + m_GrabOrigin.forward * m_HoldDistance;
			Quaternion targetRotation = m_GrabOrigin.rotation;

			// Adjustments: RMB for vertical offset using mouse Y (LookInput.y), scroll for hold distance
			if (m_InputHandler.IsAdjustHeld)
			{
				m_CurrentVerticalOffset += -m_InputHandler.LookInput.y * m_VerticalAdjustSpeed; // invert for natural feel
				m_CurrentVerticalOffset = Mathf.Clamp(m_CurrentVerticalOffset, -0.8f, 0.8f);
			}
			if (Mathf.Abs(m_InputHandler.ScrollDelta) > 0.01f)
			{
				m_HoldDistance = Mathf.Clamp(m_HoldDistance + m_InputHandler.ScrollDelta * m_ScrollAdjustSpeed, m_MinHoldDistance, m_MaxHoldDistance);
			}

			Vector3 targetPosition = basePosition + Vector3.up * m_CurrentVerticalOffset;

			CmdUpdateCarryTarget(m_CurrentlyHeldItem.netId, targetPosition, targetRotation);
		}
        #endregion

        #region Network Commands
        [Command]
        private void CmdGrabSinglePlayerItem(uint _itemNetId)
        {
            NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
            if (itemIdentity == null) return;

            SinglePlayerItem item = itemIdentity.GetComponent<SinglePlayerItem>();
            if (item == null || !item.CanBeGrabbed) return;

            if (item.TryGrab(netId))
            {
				// Start carry on server via controller
				Vector3 targetPosition = m_HoldPosition != null
					? m_HoldPosition.position
					: m_GrabOrigin.position + m_GrabOrigin.forward * m_HoldDistance;
				Quaternion targetRotation = m_GrabOrigin.rotation;
                ICarryController controller = null;
                var mbs = itemIdentity.GetComponents<MonoBehaviour>();
                for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
                if (controller != null) { controller.StartCarry(targetPosition, targetRotation); }
				RpcOnItemGrabbed(_itemNetId);
            }
        }

        [Command]
        private void CmdGrabDualPlayerItem(uint _itemNetId)
        {
            NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
            if (itemIdentity == null) return;

            DualPlayerItem item = itemIdentity.GetComponent<DualPlayerItem>();
            if (item == null) return;

            if (item.TryGrab(netId))
            {
                RpcOnItemGrabbed(_itemNetId);
            }
        }

        [Command]
        private void CmdDropItem(uint _itemNetId, Vector3 _velocity)
        {
            NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
            if (itemIdentity == null) return;

            GrabbableItem item = itemIdentity.GetComponent<GrabbableItem>();
            if (item == null) return;

            // Check if dual-player item
            if (item is DualPlayerItem dualItem)
            {
				dualItem.ReleasePlayer(netId);
            }
            else
            {
				ICarryController controller = null;
				var mbs = itemIdentity.GetComponents<MonoBehaviour>();
				for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
				if (controller != null) { controller.StopCarry(_velocity); }
				else { item.Release(_velocity); }
            }

            RpcOnItemDropped();
        }

        [Command]
        private void CmdThrowItem(uint _itemNetId, Vector3 _throwVelocity)
        {
            NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
            if (itemIdentity == null) return;

			GrabbableItem item = itemIdentity.GetComponent<GrabbableItem>();
            if (item == null) return;

			// Ensure carry is disabled before throw
			ICarryController controller = null;
			var mbs = itemIdentity.GetComponents<MonoBehaviour>();
			for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
			if (controller != null) { controller.ForceStopCarry(); }

			item.Throw(_throwVelocity);
            RpcOnItemDropped();
        }

		[Command]
		private void CmdStartCarry(uint _itemNetId, Vector3 _position, Quaternion _rotation)
		{
			NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
			if (itemIdentity == null) return;
            ICarryController controller = null;
            var mbs = itemIdentity.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
            if (controller == null) return;
            controller.StartCarry(_position, _rotation);
		}

		[Command]
		private void CmdUpdateCarryTarget(uint _itemNetId, Vector3 _position, Quaternion _rotation)
		{
			NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
			if (itemIdentity == null) return;
            ICarryController controller = null;
            var mbs = itemIdentity.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
            if (controller == null) return;
            controller.UpdateTarget(_position, _rotation);
		}

		[Command]
		private void CmdStopCarry(uint _itemNetId, Vector3 _releaseVelocity)
		{
			NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
			if (itemIdentity == null) return;
            ICarryController controller = null;
            var mbs = itemIdentity.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++) { if (mbs[i] is ICarryController c) { controller = c; break; } }
            if (controller == null) return;
            controller.StopCarry(_releaseVelocity);
		}
        #endregion

        #region Network Callbacks
        [ClientRpc]
        private void RpcOnItemGrabbed(uint _itemNetId)
        {
            if (!isLocalPlayer) return;

            NetworkIdentity itemIdentity = NetworkClient.spawned[_itemNetId];
            if (itemIdentity == null) return;

			m_CurrentlyHeldItem = itemIdentity.GetComponent<GrabbableItem>();
            ClearHighlight();

			// Reset hold parameters on each grab to avoid carrying over buggy state
			m_HoldDistance = m_DefaultHoldDistance;
			m_CurrentVerticalOffset = m_DefaultVerticalOffset;

			// Ignore collisions between player and held item locally
			IgnorePlayerItemCollisions();

			// Move item colliders to CarriedItem layer if available to avoid CC blocking
			SetCarriedItemLayer(true);
        }

        [ClientRpc]
		private void RpcOnItemDropped()
        {
            if (!isLocalPlayer) return;

			// Restore before clearing reference
			RestorePlayerItemCollisions();
			SetCarriedItemLayer(false);
			// Reset parameters after any drop
			m_HoldDistance = m_DefaultHoldDistance;
			m_CurrentVerticalOffset = m_DefaultVerticalOffset;
			m_CurrentlyHeldItem = null;
			m_InputHandler.ConsumeGrabInput();
        }

		private void CheckForcedDropState()
		{
			if (!IsHoldingItem) return;
			if (m_CurrentlyHeldItem == null) return;
			if (!m_CurrentlyHeldItem.IsGrabbed)
			{
				// Item was dropped/broken server-side; clear local carry state
				RestorePlayerItemCollisions();
				SetCarriedItemLayer(false);
				// Reset to defaults on forced drop as well
				m_HoldDistance = m_DefaultHoldDistance;
				m_CurrentVerticalOffset = m_DefaultVerticalOffset;
				m_CurrentlyHeldItem = null;
			}
		}

		private void IgnorePlayerItemCollisions()
		{
			if (m_CurrentlyHeldItem == null) return;
			var itemColliders = m_CurrentlyHeldItem.GetComponentsInChildren<Collider>();
			var playerColliders = GetComponentsInChildren<Collider>();
			for (int i = 0; i < playerColliders.Length; i++)
			{
				for (int j = 0; j < itemColliders.Length; j++)
				{
					if (playerColliders[i] != null && itemColliders[j] != null)
					{
						Physics.IgnoreCollision(playerColliders[i], itemColliders[j], true);
					}
				}
			}
		}

		private void RestorePlayerItemCollisions()
		{
			if (m_CurrentlyHeldItem == null) return;
			var itemColliders = m_CurrentlyHeldItem.GetComponentsInChildren<Collider>();
			var playerColliders = GetComponentsInChildren<Collider>();
			for (int i = 0; i < playerColliders.Length; i++)
			{
				for (int j = 0; j < itemColliders.Length; j++)
				{
					if (playerColliders[i] != null && itemColliders[j] != null)
					{
						Physics.IgnoreCollision(playerColliders[i], itemColliders[j], false);
					}
				}
			}
		}

		private void SetCarriedItemLayer(bool _enable)
		{
			if (m_CurrentlyHeldItem == null) return;
			int carriedLayer = LayerMask.NameToLayer("CarriedItem");
			if (carriedLayer == -1) return; // Layer not defined, skip
			var itemColliders = m_CurrentlyHeldItem.GetComponentsInChildren<Collider>(true);
			if (_enable)
			{
				m_ItemColliderOriginalLayers.Clear();
				for (int i = 0; i < itemColliders.Length; i++)
				{
					var col = itemColliders[i];
					if (col == null) continue;
					m_ItemColliderOriginalLayers[col] = col.gameObject.layer;
					col.gameObject.layer = carriedLayer;
				}
			}
			else
			{
				foreach (var kv in m_ItemColliderOriginalLayers)
				{
					if (kv.Key != null)
					{
						kv.Key.gameObject.layer = kv.Value;
					}
				}
				m_ItemColliderOriginalLayers.Clear();
			}
		}
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (m_GrabOrigin == null) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(m_GrabOrigin.position, m_GrabRange);
            Gizmos.DrawRay(m_GrabOrigin.position, m_GrabOrigin.forward * m_GrabRange);

            if (m_HoldPosition != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(m_HoldPosition.position, 0.2f);
            }
        }
        #endif
    }
}

