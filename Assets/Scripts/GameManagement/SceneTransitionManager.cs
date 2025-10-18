using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Manages scene transitions and passes data between scenes
    /// Handles job completion and transition to prep scene
    /// Integrates with GameStateManager for proper 3-scene flow
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        #region Singleton
        public static SceneTransitionManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Scene Names")]
        [SerializeField] private string m_MainMenuSceneName = "MainMenu";
        [SerializeField] private string m_PrepSceneName = "PrepScene";
        [SerializeField] private string m_LevelSceneName = "SampleScene";

        [Header("Transition Settings")]
        [SerializeField] private float m_TransitionDelay = 2f;

        [Header("Payment Settings")]
        [SerializeField] private float m_BasePayment = 1000f;
        [SerializeField] private float m_TimeBonus_PerSecond = 10f;
        [SerializeField] private float m_DamageDeduction_PerItem = 50f;
        #endregion

        #region Private Fields
        private bool m_IsTransitioning;
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

        private void OnEnable()
        {
            // Subscribe to job completion event
            if (JobManager.Instance != null)
            {
                JobManager.Instance.OnJobCompleted += HandleJobCompleted;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from job completion event
            if (JobManager.Instance != null)
            {
                JobManager.Instance.OnJobCompleted -= HandleJobCompleted;
            }
        }
        #endregion

        #region Job Completion
        private void HandleJobCompleted()
        {
            Debug.Log("[SceneTransitionManager] Job completed - preparing transition to prep scene");

            if (!m_IsTransitioning)
            {
                StartCoroutine(TransitionToPrepScene());
            }
        }

        private IEnumerator TransitionToPrepScene()
        {
            m_IsTransitioning = true;

            // Calculate and store results
            CalculateAndStoreResults();

            // Wait for transition delay
            yield return new WaitForSeconds(m_TransitionDelay);

            // Load prep scene
            LoadPrepScene();

            m_IsTransitioning = false;
        }

        private void CalculateAndStoreResults()
        {
            JobManager jobManager = JobManager.Instance;
            if (jobManager == null)
            {
                Debug.LogError("[SceneTransitionManager] JobManager not found!");
                return;
            }

            // Get or create LevelResultsData
            LevelResultsData resultsData = LevelResultsData.Instance;
            if (resultsData == null)
            {
                GameObject resultsGO = new GameObject("LevelResultsData");
                resultsData = resultsGO.AddComponent<LevelResultsData>();
            }

            // Calculate payment
            float basePayment = m_BasePayment;
            float timeBonus = jobManager.TimeRemaining * m_TimeBonus_PerSecond;
            
            // Calculate deductions (you can expand this based on your damage system)
            int itemsBroken = jobManager.TotalItemsRequired - jobManager.ItemsDelivered;
            float damageDeductions = itemsBroken * m_DamageDeduction_PerItem;

            // Calculate time taken
            float jobTimeLimit = 600f; // You might want to get this from JobManager
            float timeTaken = jobTimeLimit - jobManager.TimeRemaining;

            // Store results
            resultsData.SetResults(
                basePayment,
                timeBonus,
                damageDeductions,
                jobManager.TimeRemaining,
                timeTaken,
                jobManager.ItemsDelivered,
                itemsBroken,
                jobManager.TotalItemsRequired
            );

            Debug.Log($"[SceneTransitionManager] Results calculated and stored");
        }
        #endregion

        #region Scene Loading
        /// <summary>
        /// Load the main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            Debug.Log($"[SceneTransitionManager] Loading main menu scene: {m_MainMenuSceneName}");

            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(m_MainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(m_MainMenuSceneName);
            }
        }

        /// <summary>
        /// Load the prep scene
        /// </summary>
        public void LoadPrepScene()
        {
            Debug.Log($"[SceneTransitionManager] Loading prep scene: {m_PrepSceneName}");

            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(m_PrepSceneName);
            }
            else
            {
                SceneManager.LoadScene(m_PrepSceneName);
            }
        }

        /// <summary>
        /// Load the level scene
        /// </summary>
        public void LoadLevelScene()
        {
            Debug.Log($"[SceneTransitionManager] Loading level scene: {m_LevelSceneName}");

            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(m_LevelSceneName);
            }
            else
            {
                SceneManager.LoadScene(m_LevelSceneName);
            }
        }

        /// <summary>
        /// Load a specific scene by name
        /// </summary>
        public void LoadScene(string _sceneName)
        {
            Debug.Log($"[SceneTransitionManager] Loading scene: {_sceneName}");

            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(_sceneName);
            }
            else
            {
                SceneManager.LoadScene(_sceneName);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually trigger transition to prep scene (for testing)
        /// </summary>
        [ContextMenu("Test Transition to Prep")]
        public void TestTransitionToPrep()
        {
            if (!m_IsTransitioning)
            {
                StartCoroutine(TransitionToPrepScene());
            }
        }
        #endregion
    }
}

