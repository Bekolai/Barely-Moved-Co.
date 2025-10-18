using UnityEngine;

namespace BarelyMoved.GameManagement
{
    /// <summary>
    /// Stores level completion data to persist between scenes
    /// Singleton that survives scene transitions
    /// </summary>
    public class LevelResultsData : MonoBehaviour
    {
        #region Singleton
        public static LevelResultsData Instance { get; private set; }
        #endregion

        #region Level Results Data
        [Header("Money")]
        public float MoneyEarned;
        public float MoneyDeducted;
        public float NetProfit;

        [Header("Performance")]
        public float TimeRemaining;
        public float TimeTaken;
        public int ItemsDelivered;
        public int ItemsBroken;
        public int TotalItems;

        [Header("Breakdown")]
        public float BasePayment;
        public float TimeBonus;
        public float ItemDamageDeductions;
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Store level results
        /// </summary>
        public void SetResults(float _basePayment, float _timeBonus, float _damageDeductions, 
                               float _timeRemaining, float _timeTaken, 
                               int _itemsDelivered, int _itemsBroken, int _totalItems)
        {
            BasePayment = _basePayment;
            TimeBonus = _timeBonus;
            ItemDamageDeductions = _damageDeductions;

            MoneyEarned = _basePayment + _timeBonus;
            MoneyDeducted = _damageDeductions;
            NetProfit = MoneyEarned - MoneyDeducted;

            TimeRemaining = _timeRemaining;
            TimeTaken = _timeTaken;
            ItemsDelivered = _itemsDelivered;
            ItemsBroken = _itemsBroken;
            TotalItems = _totalItems;

            Debug.Log($"[LevelResultsData] Results stored - Net Profit: ${NetProfit:F2}");
        }

        /// <summary>
        /// Clear stored results
        /// </summary>
        public void ClearResults()
        {
            MoneyEarned = 0f;
            MoneyDeducted = 0f;
            NetProfit = 0f;
            TimeRemaining = 0f;
            TimeTaken = 0f;
            ItemsDelivered = 0;
            ItemsBroken = 0;
            TotalItems = 0;
            BasePayment = 0f;
            TimeBonus = 0f;
            ItemDamageDeductions = 0f;
        }
        #endregion
    }
}

