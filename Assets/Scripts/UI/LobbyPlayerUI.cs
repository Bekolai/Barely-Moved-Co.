using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using BarelyMoved.Player;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Displays connected players in the lobby
    /// Shows player names with Steam integration, ready status, and connection info
    /// </summary>
    public class LobbyPlayerUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private Transform m_PlayerListContainer;
        [SerializeField] private GameObject m_PlayerSlotPrefab;
        [SerializeField] private TextMeshProUGUI m_PlayerCountText;
        [SerializeField] private int m_MaxPlayers = 4;
        #endregion

        #region Private Fields
        private List<PlayerSlotData> m_PlayerSlots = new List<PlayerSlotData>();
        private float m_UpdateInterval = 0.5f;
        private float m_TimeSinceLastUpdate = 0f;

        // Helper class to track slot data
        private class PlayerSlotData
        {
            public GameObject slotObject;
            public TextMeshProUGUI nameText;
            public RawImage avatarImage;
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializePlayerSlots();
            
            // Subscribe to player data updates
            NetworkPlayerData.OnPlayerDataUpdated += OnPlayerDataUpdated;
            
            // Subscribe to connection count updates from the tracker
            BarelyMoved.Network.NetworkConnectionTracker.OnConnectionCountUpdated += OnConnectionCountUpdated;
        }

        private void Update()
        {
            // Update player list periodically
            m_TimeSinceLastUpdate += Time.deltaTime;
            if (m_TimeSinceLastUpdate >= m_UpdateInterval)
            {
                m_TimeSinceLastUpdate = 0f;
                UpdatePlayerList();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from player data updates
            NetworkPlayerData.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            
            // Unsubscribe from connection count updates from the tracker
            BarelyMoved.Network.NetworkConnectionTracker.OnConnectionCountUpdated -= OnConnectionCountUpdated;
        }
        #endregion

        #region Initialization
        private void InitializePlayerSlots()
        {
            if (m_PlayerListContainer == null)
            {
                Debug.LogWarning("[LobbyPlayerUI] Player list container not assigned!");
                return;
            }

            // Clear existing slots
            foreach (Transform child in m_PlayerListContainer)
            {
                Destroy(child.gameObject);
            }
            m_PlayerSlots.Clear();

            // Create slots for max players
            for (int i = 0; i < m_MaxPlayers; i++)
            {
                CreatePlayerSlot(i);
            }
        }

        private void CreatePlayerSlot(int _slotIndex)
        {
            PlayerSlotData slotData = new PlayerSlotData();

            if (m_PlayerSlotPrefab == null)
            {
                // Create a simple slot with text and optional avatar
                GameObject slot = new GameObject($"PlayerSlot_{_slotIndex}");
                slot.transform.SetParent(m_PlayerListContainer);
                slot.transform.localScale = Vector3.one;

                // Add horizontal layout
                HorizontalLayoutGroup layout = slot.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 5, 5);

                // Create avatar placeholder (will be used if Steam avatar available)
                GameObject avatarObj = new GameObject("Avatar");
                avatarObj.transform.SetParent(slot.transform);
                RawImage avatarImage = avatarObj.AddComponent<RawImage>();
                RectTransform avatarRect = avatarObj.GetComponent<RectTransform>();
                avatarRect.sizeDelta = new Vector2(40, 40);
                slotData.avatarImage = avatarImage;
                avatarImage.gameObject.SetActive(false); // Hidden by default

                // Create text
                GameObject textObj = new GameObject("NameText");
                textObj.transform.SetParent(slot.transform);
                TextMeshProUGUI slotText = textObj.AddComponent<TextMeshProUGUI>();
                slotText.text = $"[Empty Slot {_slotIndex + 1}]";
                slotText.fontSize = 24;
                slotText.alignment = TextAlignmentOptions.Left;
                slotData.nameText = slotText;

                slotData.slotObject = slot;
                m_PlayerSlots.Add(slotData);
            }
            else
            {
                GameObject slot = Instantiate(m_PlayerSlotPrefab, m_PlayerListContainer);
                slot.name = $"PlayerSlot_{_slotIndex}";
                
                // Try to find text and avatar components in prefab
                slotData.slotObject = slot;
                slotData.nameText = slot.GetComponentInChildren<TextMeshProUGUI>();
                slotData.avatarImage = slot.GetComponentInChildren<RawImage>();
                
                m_PlayerSlots.Add(slotData);
            }
        }
        #endregion

        #region Player List Management
        private void UpdatePlayerList()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                // Not connected, clear all slots
                ClearAllSlots();
                UpdatePlayerCount(0);
                return;
            }

            // Try to get NetworkPlayerData components (only exists if players have spawned)
            NetworkPlayerData[] allPlayers = FindObjectsByType<NetworkPlayerData>(FindObjectsSortMode.None);
            
            // If no players spawned yet (like in MainMenu), use connection count instead
            if (allPlayers.Length == 0)
            {
                int connectionCount = GetConnectionCount();
                
                // Update slots with generic names
                for (int i = 0; i < m_PlayerSlots.Count; i++)
                {
                    if (i < connectionCount)
                    {
                        UpdateSlotGeneric(m_PlayerSlots[i], i, connectionCount);
                    }
                    else
                    {
                        UpdateSlot(m_PlayerSlots[i], null, i);
                    }
                }
                
                UpdatePlayerCount(connectionCount);
            }
            else
            {
                // Players have spawned, show actual player data
                for (int i = 0; i < m_PlayerSlots.Count; i++)
                {
                    if (i < allPlayers.Length)
                    {
                        UpdateSlot(m_PlayerSlots[i], allPlayers[i], i);
                    }
                    else
                    {
                        UpdateSlot(m_PlayerSlots[i], null, i);
                    }
                }

                UpdatePlayerCount(allPlayers.Length);
            }
        }

        private int GetConnectionCount()
        {
            // Use the NetworkManager's synced connection count
            var networkManager = BarelyMoved.Network.BarelyMovedNetworkManager.Instance;
            if (networkManager != null)
            {
                return networkManager.ConnectedPlayerCount;
            }
            
            return 0;
        }

        private void UpdateSlotGeneric(PlayerSlotData _slotData, int _index, int _totalConnections)
        {
            if (_slotData == null || _slotData.slotObject == null) return;

            // Only show slots that have players
            bool isHost = NetworkServer.active;
            
            // Show generic player info (no Steam data available yet)
            // For host: slot 0 is host
            // For client: they can only see themselves, so show themselves
            string playerName;
            string prefix;
            
            if (isHost)
            {
                // Host can see all players
                playerName = _index == 0 ? "Host" : $"Player {_index + 1}";
                prefix = _index == 0 ? "[HOST] " : "";
            }
            else
            {
                // Client can only see themselves, not as "Host"
                playerName = "You";
                prefix = "";
            }
            
            if (_slotData.nameText != null)
            {
                _slotData.nameText.text = $"{prefix}{playerName}";
                _slotData.nameText.color = Color.green;
            }

            // Hide avatar since we don't have player data yet
            if (_slotData.avatarImage != null)
            {
                _slotData.avatarImage.gameObject.SetActive(false);
            }
        }

        private void UpdateSlot(PlayerSlotData _slotData, NetworkPlayerData _playerData, int _index)
        {
            if (_slotData == null || _slotData.slotObject == null) return;

            if (_playerData != null)
            {
                // Show player info with Steam data
                string playerName = _playerData.PlayerName;
                
                // Check if this player is actually the host (has server active)
                bool isThisPlayerHost = _playerData.isServer;
                string prefix = isThisPlayerHost ? "[HOST] " : "";
                
                if (_slotData.nameText != null)
                {
                    _slotData.nameText.text = $"{prefix}{playerName}";
                    _slotData.nameText.color = isThisPlayerHost ? Color.yellow : Color.green;
                }

                // Show Steam avatar if available
                if (_slotData.avatarImage != null && _playerData.HasAvatar)
                {
                    _slotData.avatarImage.texture = _playerData.AvatarTexture;
                    _slotData.avatarImage.gameObject.SetActive(true);
                }
                else if (_slotData.avatarImage != null)
                {
                    _slotData.avatarImage.gameObject.SetActive(false);
                }
            }
            else
            {
                // Show empty slot
                if (_slotData.nameText != null)
                {
                    _slotData.nameText.text = $"[Empty Slot {_index + 1}]";
                    _slotData.nameText.color = Color.gray;
                }
                
                if (_slotData.avatarImage != null)
                {
                    _slotData.avatarImage.gameObject.SetActive(false);
                }
            }
        }

        private void ClearAllSlots()
        {
            for (int i = 0; i < m_PlayerSlots.Count; i++)
            {
                UpdateSlot(m_PlayerSlots[i], null, i);
            }
        }

        /// <summary>
        /// Called when player data is updated (name or avatar changed)
        /// </summary>
        private void OnPlayerDataUpdated(NetworkPlayerData _playerData)
        {
            Debug.Log($"[LobbyPlayerUI] Player data updated: {_playerData.PlayerName}");
            // Force immediate update
            UpdatePlayerList();
        }

        /// <summary>
        /// Called when connection count is updated from server
        /// </summary>
        private void OnConnectionCountUpdated(int _count)
        {
            Debug.Log($"[LobbyPlayerUI] Connection count updated: {_count}");
            // Force immediate update
            UpdatePlayerList();
        }

        private void UpdatePlayerCount(int _count)
        {
            if (m_PlayerCountText != null)
            {
                m_PlayerCountText.text = $"Players: {_count}/{m_MaxPlayers}";
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually refresh the player list
        /// </summary>
        public void RefreshPlayerList()
        {
            UpdatePlayerList();
        }

        /// <summary>
        /// Set the maximum number of players
        /// </summary>
        public void SetMaxPlayers(int _maxPlayers)
        {
            m_MaxPlayers = _maxPlayers;
            InitializePlayerSlots();
        }
        #endregion
    }
}

