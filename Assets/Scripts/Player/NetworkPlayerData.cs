using UnityEngine;
using Mirror;
using Steamworks;

namespace BarelyMoved.Player
{
    /// <summary>
    /// Stores and syncs player data across the network
    /// Includes Steam integration for nicknames and avatars
    /// </summary>
    public class NetworkPlayerData : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("Player Info")]
        [SerializeField] private string m_DefaultPlayerName = "Player";
        #endregion

        #region Private Fields
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        private string m_PlayerName = "";

        [SyncVar(hook = nameof(OnSteamIdChanged))]
        private ulong m_SteamId = 0;

        // Avatar texture is not synced directly, but fetched on each client based on SteamID
        private Texture2D m_AvatarTexture;
        private bool m_AvatarLoaded = false;
        
        #if !DISABLESTEAMWORKS
        private Callback<AvatarImageLoaded_t> m_AvatarImageLoadedCallback;
        #endif
        #endregion

        #region Properties
        public string PlayerName => string.IsNullOrEmpty(m_PlayerName) ? m_DefaultPlayerName : m_PlayerName;
        public ulong SteamId => m_SteamId;
        public Texture2D AvatarTexture => m_AvatarTexture;
        public bool HasAvatar => m_AvatarLoaded && m_AvatarTexture != null;
        #endregion

        #region Events
        public delegate void PlayerDataDelegate(NetworkPlayerData _playerData);
        public static event PlayerDataDelegate OnPlayerDataUpdated;
        #endregion

        #region Unity Lifecycle
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // Set up Steam callback
            #if !DISABLESTEAMWORKS
            if (SteamManager.Initialized)
            {
                m_AvatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
                
                string steamName = SteamFriends.GetPersonaName();
                CSteamID steamId = SteamUser.GetSteamID();

                Debug.Log($"[NetworkPlayerData] Local player Steam name: {steamName}, ID: {steamId}");
                
                // Send to server
                CmdSetPlayerData(steamName, steamId.m_SteamID);
            }
            else
            #endif
            {
                // Use default name if Steam not available
                string fallbackName = $"Player_{Random.Range(1000, 9999)}";
                Debug.Log($"[NetworkPlayerData] Steam not available, using fallback name: {fallbackName}");
                CmdSetPlayerData(fallbackName, 0);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Set up Steam callback for non-local players too
            #if !DISABLESTEAMWORKS
            if (SteamManager.Initialized && m_AvatarImageLoadedCallback == null)
            {
                m_AvatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
            }
            #endif

            // Try to load avatar if Steam ID is available
            if (m_SteamId != 0)
            {
                LoadSteamAvatar(m_SteamId);
            }
        }
        #endregion

        #region Network Commands
        [Command]
        private void CmdSetPlayerData(string _playerName, ulong _steamId)
        {
            m_PlayerName = _playerName;
            m_SteamId = _steamId;

            Debug.Log($"[NetworkPlayerData] Server set player data: {m_PlayerName}, SteamID: {m_SteamId}");
        }
        #endregion

        #region SyncVar Hooks
        private void OnPlayerNameChanged(string _oldName, string _newName)
        {
            Debug.Log($"[NetworkPlayerData] Player name changed from '{_oldName}' to '{_newName}'");
            
            // Load avatar if Steam ID is available and not already loaded
            if (m_SteamId != 0 && !m_AvatarLoaded)
            {
                LoadSteamAvatar(m_SteamId);
            }

            // Notify listeners
            OnPlayerDataUpdated?.Invoke(this);
        }

        private void OnSteamIdChanged(ulong _oldId, ulong _newId)
        {
            Debug.Log($"[NetworkPlayerData] Steam ID changed from {_oldId} to {_newId}");
            
            // Load avatar when Steam ID is set
            if (_newId != 0 && !m_AvatarLoaded)
            {
                LoadSteamAvatar(_newId);
            }
        }
        #endregion

        #region Steam Avatar
        /// <summary>
        /// Load Steam avatar from Steam ID
        /// </summary>
        private void LoadSteamAvatar(ulong _steamId)
        {
            #if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("[NetworkPlayerData] Cannot load avatar - Steam not initialized");
                return;
            }

            if (_steamId == 0)
            {
                Debug.LogWarning("[NetworkPlayerData] Cannot load avatar - Steam ID is 0");
                return;
            }

            CSteamID steamId = new CSteamID(_steamId);
            
            Debug.Log($"[NetworkPlayerData] Attempting to load avatar for Steam ID: {_steamId}");
            
            // Get the large avatar handle
            int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);
            
            Debug.Log($"[NetworkPlayerData] Avatar handle: {avatarHandle} (-1=loading, 0=no avatar, >0=valid)");
            
            if (avatarHandle == -1)
            {
                Debug.Log($"[NetworkPlayerData] Avatar not yet loaded for Steam ID: {_steamId}, waiting for callback...");
                // Avatar not yet loaded, callback will handle it when ready
                return;
            }
            else if (avatarHandle == 0)
            {
                Debug.LogWarning($"[NetworkPlayerData] No avatar available for Steam ID: {_steamId}");
                return;
            }

            // Process the avatar
            ProcessAvatarImage(avatarHandle, _steamId);
            #else
            Debug.LogWarning("[NetworkPlayerData] Steam is disabled, cannot load avatar");
            #endif
        }

        #if !DISABLESTEAMWORKS
        /// <summary>
        /// Called when a Steam avatar is loaded
        /// </summary>
        private void OnAvatarImageLoaded(AvatarImageLoaded_t _callback)
        {
            // Check if this callback is for our Steam ID
            if (_callback.m_steamID.m_SteamID != m_SteamId)
            {
                return; // Not for us
            }

            Debug.Log($"[NetworkPlayerData] Avatar callback received for Steam ID: {m_SteamId}, handle: {_callback.m_iImage}");

            // Process the loaded avatar
            if (_callback.m_iImage > 0)
            {
                ProcessAvatarImage(_callback.m_iImage, m_SteamId);
            }
        }

        /// <summary>
        /// Process an avatar image from a handle
        /// </summary>
        private void ProcessAvatarImage(int _avatarHandle, ulong _steamId)
        {
            // Get avatar dimensions
            if (!SteamUtils.GetImageSize(_avatarHandle, out uint width, out uint height))
            {
                Debug.LogError($"[NetworkPlayerData] Failed to get avatar size for Steam ID: {_steamId}");
                return;
            }

            Debug.Log($"[NetworkPlayerData] Avatar size: {width}x{height}");

            // Get avatar RGBA data
            byte[] avatarData = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(_avatarHandle, avatarData, (int)(width * height * 4)))
            {
                Debug.LogError($"[NetworkPlayerData] Failed to get avatar data for Steam ID: {_steamId}");
                return;
            }

            // Create texture
            m_AvatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            
            // Flip the image vertically (Steam avatars are upside down)
            byte[] flippedData = new byte[avatarData.Length];
            int rowSize = (int)width * 4;
            for (int row = 0; row < height; row++)
            {
                int sourceRow = (int)height - row - 1; // Flip vertically
                System.Array.Copy(avatarData, sourceRow * rowSize, flippedData, row * rowSize, rowSize);
            }
            
            m_AvatarTexture.LoadRawTextureData(flippedData);
            m_AvatarTexture.Apply();
            m_AvatarLoaded = true;

            Debug.Log($"[NetworkPlayerData] Successfully loaded Steam avatar for {m_PlayerName} ({_steamId})");

            // Notify listeners
            OnPlayerDataUpdated?.Invoke(this);
        }
        #endif

        /// <summary>
        /// Public method to get player name by connection ID
        /// </summary>
        public static string GetPlayerNameByConnectionId(int _connectionId)
        {
            // Find all NetworkPlayerData components
            NetworkPlayerData[] allPlayerData = FindObjectsByType<NetworkPlayerData>(FindObjectsSortMode.None);
            
            foreach (var playerData in allPlayerData)
            {
                if (playerData.connectionToClient != null && playerData.connectionToClient.connectionId == _connectionId)
                {
                    return playerData.PlayerName;
                }
            }

            return $"Player {_connectionId + 1}";
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            // Clean up avatar texture
            if (m_AvatarTexture != null)
            {
                Destroy(m_AvatarTexture);
            }
        }
        #endregion
    }
}


