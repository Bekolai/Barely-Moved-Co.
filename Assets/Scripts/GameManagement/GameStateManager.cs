using UnityEngine;
using Mirror;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Manages the overall game state and scene flow
    /// Persists across all scenes via DontDestroyOnLoad
    /// </summary>
    public class GameStateManager : NetworkBehaviour
    {
        #region Constants
        public const string c_MainMenuSceneName = "MainMenu";
        public const string c_PrepSceneName = "PrepScene";
        public const string c_LevelSceneName = "SampleScene";
        #endregion

        #region Singleton
        public static GameStateManager Instance { get; private set; }
        #endregion

        #region Enums
        public enum GameState
        {
            MainMenu,
            PrepHub,
            InLevel,
            Loading
        }
        #endregion

        #region SyncVars
        [SyncVar(hook = nameof(OnGameStateChanged))]
        private GameState m_CurrentState = GameState.MainMenu;
        #endregion

        #region Serialized Fields
        [Header("Scene Names")]
        [SerializeField] private string m_MainMenuSceneName = c_MainMenuSceneName;
        [SerializeField] private string m_PrepSceneName = c_PrepSceneName;
        [SerializeField] private string m_LevelSceneName = c_LevelSceneName;

        [Header("Settings")]
        [SerializeField] private bool m_AutoStartInPrepForTesting = false;
        #endregion

        #region Properties
        public GameState CurrentState => m_CurrentState;
        public string MainMenuSceneName => m_MainMenuSceneName;
        public string PrepSceneName => m_PrepSceneName;
        public string LevelSceneName => m_LevelSceneName;
        public bool IsInMainMenu => m_CurrentState == GameState.MainMenu;
        public bool IsInPrepHub => m_CurrentState == GameState.PrepHub;
        public bool IsInLevel => m_CurrentState == GameState.InLevel;
        public bool IsLoading => m_CurrentState == GameState.Loading;
        #endregion

        #region Events
        public delegate void GameStateDelegate(GameState _newState);
        public event GameStateDelegate OnStateChanged;
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

            Debug.Log("[GameStateManager] Initialized");
        }

        private void Start()
        {
            // For testing: skip main menu if in prep scene
            if (m_AutoStartInPrepForTesting && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == m_PrepSceneName)
            {
                if (isServer)
                {
                    SetGameState(GameState.PrepHub);
                }
            }
        }
        #endregion

        #region State Management
        /// <summary>
        /// Set the game state (Server only)
        /// </summary>
        [Server]
        public void SetGameState(GameState _newState)
        {
            if (m_CurrentState == _newState)
            {
                Debug.LogWarning($"[GameStateManager] Already in state: {_newState}");
                return;
            }

            Debug.Log($"[GameStateManager] State change: {m_CurrentState} -> {_newState}");
            m_CurrentState = _newState;
        }

        private void OnGameStateChanged(GameState _oldState, GameState _newState)
        {
            Debug.Log($"[GameStateManager] State changed on client: {_oldState} -> {_newState}");
            OnStateChanged?.Invoke(_newState);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Transition from Main Menu to Prep Scene (after lobby ready)
        /// </summary>
        [Server]
        public void TransitionToPrep()
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[GameStateManager] Only server can transition scenes!");
                return;
            }

            Debug.Log("[GameStateManager] Transitioning to Prep Scene...");
            SetGameState(GameState.Loading);
            NetworkManager.singleton.ServerChangeScene(m_PrepSceneName);
        }

        /// <summary>
        /// Transition from Prep to Level Scene (when starting a job)
        /// </summary>
        [Server]
        public void TransitionToLevel()
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[GameStateManager] Only server can transition scenes!");
                return;
            }

            Debug.Log("[GameStateManager] Transitioning to Level Scene...");
            SetGameState(GameState.Loading);
            NetworkManager.singleton.ServerChangeScene(m_LevelSceneName);
        }

        /// <summary>
        /// Transition from Level back to Prep Scene (after job complete)
        /// </summary>
        [Server]
        public void TransitionBackToPrep()
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[GameStateManager] Only server can transition scenes!");
                return;
            }

            Debug.Log("[GameStateManager] Returning to Prep Scene...");
            SetGameState(GameState.Loading);
            NetworkManager.singleton.ServerChangeScene(m_PrepSceneName);
        }

        /// <summary>
        /// Called when a scene finishes loading (from NetworkManager callback)
        /// </summary>
        [Server]
        public void OnSceneLoaded(string _sceneName)
        {
            Debug.Log($"[GameStateManager] Scene loaded: {_sceneName}");

            if (_sceneName == m_MainMenuSceneName)
            {
                SetGameState(GameState.MainMenu);
            }
            else if (_sceneName == m_PrepSceneName)
            {
                SetGameState(GameState.PrepHub);
            }
            else if (_sceneName == m_LevelSceneName)
            {
                SetGameState(GameState.InLevel);
                
                // Start the job automatically when level loads
                if (JobManager.Instance != null)
                {
                    JobManager.Instance.StartJob();
                }
            }
        }
        #endregion

        #if UNITY_EDITOR
        [ContextMenu("Debug State Info")]
        private void DebugStateInfo()
        {
            Debug.Log($"Current State: {m_CurrentState}");
            Debug.Log($"Is Server: {isServer}");
            Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }
        #endif
    }
}

