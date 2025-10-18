using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections.Generic;
using BarelyMoved.Player;
using BarelyMoved.Network;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Displays connected players in the pause menu lobby panel
    /// Shows player names, avatars, and kick buttons (host only)
    /// </summary>
    public class InGameLobbyPanel : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private Transform m_PlayerListContainer;
        [SerializeField] private GameObject m_PlayerEntryPrefab;
        [SerializeField] private TextMeshProUGUI m_PlayerCountText;
        [SerializeField] private int m_MaxPlayers = 4;
        #endregion

        #region Private Fields
        private List<PlayerEntry> m_PlayerEntries = new List<PlayerEntry>();
        private float m_UpdateInterval = 0.5f;
        private float m_TimeSinceLastUpdate = 0f;

        // Helper class to track player entry UI
        private class PlayerEntry
        {
            public GameObject entryObject;
            public TextMeshProUGUI nameText;
            public RawImage avatarImage;
            public Button kickButton;
            public NetworkPlayerData playerData;
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            // Subscribe to events
            NetworkPlayerData.OnPlayerDataUpdated += OnPlayerDataUpdated;
            NetworkConnectionTracker.OnConnectionCountUpdated += OnConnectionCountUpdated;
            
            // Immediately refresh when shown
            RefreshPlayerList();
        }

        private void Update()
        {
            // Periodically refresh player list
            m_TimeSinceLastUpdate += Time.deltaTime;
            if (m_TimeSinceLastUpdate >= m_UpdateInterval)
            {
                m_TimeSinceLastUpdate = 0f;
                RefreshPlayerList();
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            NetworkPlayerData.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            NetworkConnectionTracker.OnConnectionCountUpdated -= OnConnectionCountUpdated;
        }
        #endregion

        #region Player List Management
        /// <summary>
        /// Refresh the entire player list
        /// </summary>
        public void RefreshPlayerList()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                ClearPlayerList();
                UpdatePlayerCount(0);
                return;
            }

            // Get all NetworkPlayerData objects (spawned players)
            NetworkPlayerData[] allPlayers = FindObjectsByType<NetworkPlayerData>(FindObjectsSortMode.None);
            
            bool isHost = NetworkServer.active;
            
            // Update or create entries
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (i < m_PlayerEntries.Count)
                {
                    UpdatePlayerEntry(m_PlayerEntries[i], allPlayers[i], isHost);
                }
                else
                {
                    CreatePlayerEntry(allPlayers[i], isHost);
                }
            }

            // Remove excess entries
            while (m_PlayerEntries.Count > allPlayers.Length)
            {
                RemovePlayerEntry(m_PlayerEntries.Count - 1);
            }

            UpdatePlayerCount(allPlayers.Length);
        }

        /// <summary>
        /// Create a new player entry UI element
        /// </summary>
        private void CreatePlayerEntry(NetworkPlayerData _playerData, bool _isHost)
        {
            PlayerEntry entry = new PlayerEntry();
            entry.playerData = _playerData;

            if (m_PlayerEntryPrefab != null)
            {
                // Instantiate from prefab
                GameObject entryObj = Instantiate(m_PlayerEntryPrefab, m_PlayerListContainer);
                entry.entryObject = entryObj;
                
                // Find components
                entry.nameText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                entry.avatarImage = entryObj.GetComponentInChildren<RawImage>();
                entry.kickButton = entryObj.GetComponentInChildren<Button>();
            }
            else
            {
                // Create entry from scratch
                GameObject entryObj = new GameObject($"PlayerEntry_{_playerData.PlayerName}");
                entryObj.transform.SetParent(m_PlayerListContainer);
                entryObj.transform.localScale = Vector3.one;
                entry.entryObject = entryObj;

                // Add horizontal layout
                HorizontalLayoutGroup layout = entryObj.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 10f;
                layout.padding = new RectOffset(10, 10, 5, 5);
                layout.childControlWidth = false;
                layout.childControlHeight = false;

                // Create avatar
                GameObject avatarObj = new GameObject("Avatar");
                avatarObj.transform.SetParent(entryObj.transform);
                RawImage avatarImage = avatarObj.AddComponent<RawImage>();
                RectTransform avatarRect = avatarObj.GetComponent<RectTransform>();
                avatarRect.sizeDelta = new Vector2(40, 40);
                entry.avatarImage = avatarImage;

                // Create name text
                GameObject nameObj = new GameObject("NameText");
                nameObj.transform.SetParent(entryObj.transform);
                TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
                nameText.fontSize = 20;
                nameText.alignment = TextAlignmentOptions.Left;
                RectTransform nameRect = nameObj.GetComponent<RectTransform>();
                nameRect.sizeDelta = new Vector2(200, 40);
                entry.nameText = nameText;

                // Create kick button (host only)
                GameObject kickObj = new GameObject("KickButton");
                kickObj.transform.SetParent(entryObj.transform);
                Button kickButton = kickObj.AddComponent<Button>();
                RectTransform kickRect = kickObj.GetComponent<RectTransform>();
                kickRect.sizeDelta = new Vector2(80, 30);
                
                // Add button text
                GameObject buttonTextObj = new GameObject("Text");
                buttonTextObj.transform.SetParent(kickObj.transform);
                TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
                buttonText.text = "Kick";
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
                buttonText.color = Color.red;
                RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
                buttonTextRect.anchorMin = Vector2.zero;
                buttonTextRect.anchorMax = Vector2.one;
                buttonTextRect.sizeDelta = Vector2.zero;
                
                entry.kickButton = kickButton;
            }

            // Setup kick button callback
            if (entry.kickButton != null)
            {
                entry.kickButton.onClick.AddListener(() => OnKickButtonClicked(entry.playerData));
            }

            m_PlayerEntries.Add(entry);
            UpdatePlayerEntry(entry, _playerData, _isHost);
        }

        /// <summary>
        /// Update an existing player entry
        /// </summary>
        private void UpdatePlayerEntry(PlayerEntry _entry, NetworkPlayerData _playerData, bool _isHost)
        {
            if (_entry == null || _playerData == null) return;

            _entry.playerData = _playerData;

            // Update name
            if (_entry.nameText != null)
            {
                // Check if this is the host connection (connection ID 0)
                bool isHostConnection = _playerData.connectionToClient != null && 
                                       _playerData.connectionToClient.connectionId == 0;
                
                string prefix = isHostConnection ? "[HOST] " : ""; // Host indicator
                bool isLocalPlayer = _playerData.isLocalPlayer;
                string suffix = isLocalPlayer ? " (You)" : "";
                
                _entry.nameText.text = $"{prefix}{_playerData.PlayerName}{suffix}";
                _entry.nameText.color = isLocalPlayer ? Color.cyan : (isHostConnection ? Color.yellow : Color.white);
            }

            // Update avatar
            if (_entry.avatarImage != null)
            {
                if (_playerData.HasAvatar)
                {
                    _entry.avatarImage.texture = _playerData.AvatarTexture;
                    _entry.avatarImage.gameObject.SetActive(true);
                }
                else
                {
                    _entry.avatarImage.gameObject.SetActive(false);
                }
            }

            // Update kick button visibility
            if (_entry.kickButton != null)
            {
                // Show kick button if:
                // - We are host
                // - This is NOT the local player (can't kick yourself)
                // - This is NOT connection ID 0 (can't kick the host connection)
                
                // Check connection ID to identify host (connection ID 0)
                bool isHostConnection = _playerData.connectionToClient != null && 
                                       _playerData.connectionToClient.connectionId == 0;
                
                bool showKick = _isHost && !_playerData.isLocalPlayer && !isHostConnection;
                _entry.kickButton.gameObject.SetActive(showKick);
                
                int connId = _playerData.connectionToClient != null ? _playerData.connectionToClient.connectionId : -1;
                Debug.Log($"[InGameLobbyPanel] Player: {_playerData.PlayerName}, ConnID:{connId}, isHost:{_isHost}, isLocalPlayer:{_playerData.isLocalPlayer}, isServer:{_playerData.isServer}, isHostConn:{isHostConnection}, showKick:{showKick}");
            }
        }

        /// <summary>
        /// Remove a player entry at index
        /// </summary>
        private void RemovePlayerEntry(int _index)
        {
            if (_index < 0 || _index >= m_PlayerEntries.Count) return;

            PlayerEntry entry = m_PlayerEntries[_index];
            if (entry.entryObject != null)
            {
                Destroy(entry.entryObject);
            }

            m_PlayerEntries.RemoveAt(_index);
        }

        /// <summary>
        /// Clear all player entries
        /// </summary>
        private void ClearPlayerList()
        {
            foreach (var entry in m_PlayerEntries)
            {
                if (entry.entryObject != null)
                {
                    Destroy(entry.entryObject);
                }
            }
            m_PlayerEntries.Clear();
        }

        /// <summary>
        /// Update the player count text
        /// </summary>
        private void UpdatePlayerCount(int _count)
        {
            if (m_PlayerCountText != null)
            {
                m_PlayerCountText.text = $"Players: {_count}/{m_MaxPlayers}";
            }
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// Called when player data is updated
        /// </summary>
        private void OnPlayerDataUpdated(NetworkPlayerData _playerData)
        {
            RefreshPlayerList();
        }

        /// <summary>
        /// Called when connection count changes
        /// </summary>
        private void OnConnectionCountUpdated(int _count)
        {
            RefreshPlayerList();
        }

        /// <summary>
        /// Called when kick button is clicked
        /// </summary>
        private void OnKickButtonClicked(NetworkPlayerData _playerData)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[InGameLobbyPanel] Only host can kick players");
                return;
            }

            if (_playerData == null)
            {
                Debug.LogWarning("[InGameLobbyPanel] Cannot kick - player data is null");
                return;
            }

            // Get the connection ID from the player
            NetworkConnectionToClient conn = _playerData.connectionToClient;
            if (conn == null)
            {
                Debug.LogWarning($"[InGameLobbyPanel] Cannot kick {_playerData.PlayerName} - no connection found");
                return;
            }

            // Log for debugging
            Debug.Log($"[InGameLobbyPanel] Kick request - Player: {_playerData.PlayerName}, ConnID: {conn.connectionId}, isLocalPlayer: {_playerData.isLocalPlayer}");

            // Safety checks - don't kick yourself
             if (_playerData.isLocalPlayer)
            {
                Debug.LogWarning("[InGameLobbyPanel] Cannot kick yourself!");
                return;
            } 

            // Don't kick connection ID 0 (host connection)
            if (conn.connectionId == 0)
            {
                Debug.LogWarning($"[InGameLobbyPanel] Cannot kick connection ID 0 (host connection)");
                return;
            } 

            // Connection ID must be valid (greater than 0 for clients)
            if (conn.connectionId < 0)
            {
                Debug.LogWarning($"[InGameLobbyPanel] Invalid connection ID: {conn.connectionId}");
                return;
            }

            Debug.Log($"[InGameLobbyPanel] ✓ Kicking player: {_playerData.PlayerName} (ConnectionID: {conn.connectionId})");
            
            var networkManager = BarelyMovedNetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.KickPlayer(conn);
            }
            else
            {
                Debug.LogError("[InGameLobbyPanel] NetworkManager instance not found!");
            }
        }
        #endregion
    }
}

