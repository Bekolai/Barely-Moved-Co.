using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarelyMoved.GameManagement;
using UnityEngine.SceneManagement;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Displays job completion results in the prep scene
    /// Shows money earned, deductions, and performance stats
    /// </summary>
    public class JobCompleteUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private GameObject m_ResultsPanel;
        [SerializeField] private TextMeshProUGUI m_TitleText;
        
        [Header("Money Display")]
        [SerializeField] private TextMeshProUGUI m_MoneyEarnedText;
        [SerializeField] private TextMeshProUGUI m_MoneyDeductedText;
        [SerializeField] private TextMeshProUGUI m_NetProfitText;

        [Header("Breakdown")]
        [SerializeField] private TextMeshProUGUI m_BasePaymentText;
        [SerializeField] private TextMeshProUGUI m_TimeBonusText;
        [SerializeField] private TextMeshProUGUI m_DamageDeductionsText;

        [Header("Performance Stats")]
        [SerializeField] private TextMeshProUGUI m_TimeText;
        [SerializeField] private TextMeshProUGUI m_ItemsDeliveredText;
        [SerializeField] private TextMeshProUGUI m_ItemsBrokenText;

        [Header("Buttons")]
        [SerializeField] private Button m_ContinueButton;

        [Header("Animation")]
        [SerializeField] private float m_CountUpDuration = 1.5f;
        [SerializeField] private AnimationCurve m_CountUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        #endregion

        #region Private Fields
        private LevelResultsData m_ResultsData;
        private bool m_IsAnimating;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (m_ContinueButton != null)
            {
                m_ContinueButton.onClick.AddListener(OnContinueClicked);
            }

            // Initially hide the panel
            if (m_ResultsPanel != null)
            {
                m_ResultsPanel.SetActive(false);
            }
        }

        private void Start()
        {
            m_ResultsData = LevelResultsData.Instance;

            if (m_ResultsData != null)
            {
                ShowResults();
            }
            else
            {
                Debug.LogWarning("[JobCompleteUI] No LevelResultsData found - hiding results panel");
                if (m_ResultsPanel != null)
                {
                    m_ResultsPanel.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_ContinueButton != null)
            {
                m_ContinueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }
        #endregion

        #region Display Results
        private void ShowResults()
        {
            if (m_ResultsData == null) return;

            if (m_ResultsPanel != null)
            {
                m_ResultsPanel.SetActive(true);
            }

            if (m_TitleText != null)
            {
                m_TitleText.text = "JOB COMPLETE!";
            }

            // Start animated count-up
            StartCoroutine(AnimateResults());
        }

        private System.Collections.IEnumerator AnimateResults()
        {
            m_IsAnimating = true;
            float elapsed = 0f;

            // Cache final values
            float finalBasePayment = m_ResultsData.BasePayment;
            float finalTimeBonus = m_ResultsData.TimeBonus;
            float finalMoneyEarned = m_ResultsData.MoneyEarned;
            float finalDamageDeductions = m_ResultsData.ItemDamageDeductions;
            float finalMoneyDeducted = m_ResultsData.MoneyDeducted;
            float finalNetProfit = m_ResultsData.NetProfit;

            while (elapsed < m_CountUpDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / m_CountUpDuration);
                float curveValue = m_CountUpCurve.Evaluate(progress);

                // Animate money values
                UpdateMoneyDisplay(
                    finalBasePayment * curveValue,
                    finalTimeBonus * curveValue,
                    finalMoneyEarned * curveValue,
                    finalDamageDeductions * curveValue,
                    finalMoneyDeducted * curveValue,
                    finalNetProfit * curveValue
                );

                yield return null;
            }

            // Ensure final values are exact
            UpdateMoneyDisplay(
                finalBasePayment,
                finalTimeBonus,
                finalMoneyEarned,
                finalDamageDeductions,
                finalMoneyDeducted,
                finalNetProfit
            );

            // Display performance stats (no animation)
            UpdatePerformanceStats();

            m_IsAnimating = false;
        }

        private void UpdateMoneyDisplay(float _basePayment, float _timeBonus, float _moneyEarned,
                                       float _damageDeductions, float _moneyDeducted, float _netProfit)
        {
            // Breakdown
            if (m_BasePaymentText != null)
            {
                m_BasePaymentText.text = $"Base Payment: ${_basePayment:F2}";
            }

            if (m_TimeBonusText != null)
            {
                m_TimeBonusText.text = $"Time Bonus: +${_timeBonus:F2}";
            }

            if (m_DamageDeductionsText != null)
            {
                m_DamageDeductionsText.text = $"Damage Penalty: -${_damageDeductions:F2}";
            }

            // Totals
            if (m_MoneyEarnedText != null)
            {
                m_MoneyEarnedText.text = $"+${_moneyEarned:F2}";
                m_MoneyEarnedText.color = Color.green;
            }

            if (m_MoneyDeductedText != null)
            {
                m_MoneyDeductedText.text = $"-${_moneyDeducted:F2}";
                m_MoneyDeductedText.color = Color.red;
            }

            if (m_NetProfitText != null)
            {
                m_NetProfitText.text = $"${_netProfit:F2}";
                m_NetProfitText.color = _netProfit >= 0 ? Color.green : Color.red;
            }
        }

        private void UpdatePerformanceStats()
        {
            if (m_ResultsData == null) return;

            // Time
            if (m_TimeText != null)
            {
                int minutes = Mathf.FloorToInt(m_ResultsData.TimeTaken / 60f);
                int seconds = Mathf.FloorToInt(m_ResultsData.TimeTaken % 60f);
                m_TimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }

            // Items delivered
            if (m_ItemsDeliveredText != null)
            {
                m_ItemsDeliveredText.text = $"Items Delivered: {m_ResultsData.ItemsDelivered}/{m_ResultsData.TotalItems}";
            }

            // Items broken
            if (m_ItemsBrokenText != null)
            {
                m_ItemsBrokenText.text = $"Items Broken: {m_ResultsData.ItemsBroken}";
                m_ItemsBrokenText.color = m_ResultsData.ItemsBroken > 0 ? Color.yellow : Color.white;
            }
        }
        #endregion

        #region Button Callbacks
        private void OnContinueClicked()
        {
            Debug.Log("[JobCompleteUI] Continue clicked");

            // Clear results data
            if (m_ResultsData != null)
            {
                m_ResultsData.ClearResults();
            }

            // Hide panel
            if (m_ResultsPanel != null)
            {
                m_ResultsPanel.SetActive(false);
            }

            // Here you could trigger other prep scene activities
            // For now, just close the panel
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually show results (if not shown automatically)
        /// </summary>
        public void ShowResultsPanel()
        {
            if (m_ResultsData != null)
            {
                ShowResults();
            }
        }

        /// <summary>
        /// Skip animation and show final results immediately
        /// </summary>
        public void SkipAnimation()
        {
            if (m_IsAnimating)
            {
                StopAllCoroutines();
                UpdateMoneyDisplay(
                    m_ResultsData.BasePayment,
                    m_ResultsData.TimeBonus,
                    m_ResultsData.MoneyEarned,
                    m_ResultsData.ItemDamageDeductions,
                    m_ResultsData.MoneyDeducted,
                    m_ResultsData.NetProfit
                );
                UpdatePerformanceStats();
                m_IsAnimating = false;
            }
        }
        #endregion
    }
}

