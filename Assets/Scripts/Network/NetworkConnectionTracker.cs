using UnityEngine;
using Mirror;

namespace BarelyMoved.Network
{
    /// <summary>
    /// NetworkBehaviour component that syncs connection count from server to all clients
    /// This is needed because NetworkManager cannot use SyncVars or ClientRpc
    /// </summary>
    public class NetworkConnectionTracker : NetworkBehaviour
    {
        #region Singleton
        public static NetworkConnectionTracker Instance { get; private set; }
        #endregion

        #region SyncVars
        [SyncVar(hook = nameof(OnConnectionCountChanged))]
        private int m_ConnectionCount = 0;
        #endregion

        #region Events
        public delegate void ConnectionCountDelegate(int _count);
        public static event ConnectionCountDelegate OnConnectionCountUpdated;
        #endregion

        #region Properties
        public int ConnectionCount => m_ConnectionCount;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the connection count (server only)
        /// </summary>
        [Server]
        public void UpdateConnectionCount(int _count)
        {
            if (m_ConnectionCount != _count)
            {
                m_ConnectionCount = _count;
                Debug.Log($"[NetworkConnectionTracker] Connection count updated: {_count}");
            }
        }
        #endregion

        #region SyncVar Hooks
        private void OnConnectionCountChanged(int _oldValue, int _newValue)
        {
            Debug.Log($"[NetworkConnectionTracker] Connection count changed from {_oldValue} to {_newValue}");
            OnConnectionCountUpdated?.Invoke(_newValue);
        }
        #endregion
    }
}

