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

        [Header("Visual Feedback")]
        [SerializeField] private Color m_HighlightColor = Color.yellow;
        #endregion

        #region Private Fields
        private PlayerInputHandler m_InputHandler;
        private NetworkPlayerController m_PlayerController;
        
        private GrabbableItem m_CurrentlyHeldItem;
        private GrabbableItem m_NearbyItem;
        
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
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            DetectNearbyItems();
            HandleGrabInput();
            UpdateHeldItemPosition();
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
        private void HandleGrabInput()
        {
            // Grab/Drop
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

            // Throw
            if (m_InputHandler.IsThrowPressed && IsHoldingItem)
            {
                m_InputHandler.ConsumeThrowInput();
                ThrowItem();
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
        }

        private void ThrowItem()
        {
            if (m_CurrentlyHeldItem == null) return;

            Vector3 throwVelocity = m_GrabOrigin.forward * m_ThrowForce + m_PlayerController.Velocity;
            CmdThrowItem(m_CurrentlyHeldItem.netId, throwVelocity);
        }

        private void UpdateHeldItemPosition()
        {
            if (!IsHoldingItem) return;
            if (m_CurrentlyHeldItem == null) return;

            // Calculate hold position
            Vector3 targetPosition = m_HoldPosition != null 
                ? m_HoldPosition.position 
                : m_GrabOrigin.position + m_GrabOrigin.forward * m_HoldDistance;

            // Send to server to update
            CmdUpdateHeldItemPosition(m_CurrentlyHeldItem.netId, targetPosition, m_GrabOrigin.rotation);
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
                item.Release(_velocity);
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

            item.Throw(_throwVelocity);
            RpcOnItemDropped();
        }

        [Command]
        private void CmdUpdateHeldItemPosition(uint _itemNetId, Vector3 _position, Quaternion _rotation)
        {
            NetworkIdentity itemIdentity = NetworkServer.spawned[_itemNetId];
            if (itemIdentity == null) return;

            GrabbableItem item = itemIdentity.GetComponent<GrabbableItem>();
            if (item == null) return;

            item.UpdatePosition(_position, _rotation);
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
        }

        [ClientRpc]
        private void RpcOnItemDropped()
        {
            if (!isLocalPlayer) return;

            m_CurrentlyHeldItem = null;
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

