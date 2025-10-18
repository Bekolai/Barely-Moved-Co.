using UnityEngine;
using TMPro;
using BarelyMoved.GameManagement;
using Mirror;

namespace BarelyMoved.Interactables
{
    /// <summary>
    /// Simple UI binder to display a DeliveryZone's live stats (count and total value).
    /// Attach to a world-space canvas or screen-space HUD and assign references.
    /// </summary>
    public class DeliveryZoneUI : NetworkBehaviour
    {
        [SerializeField] private DeliveryZone m_Zone;
        [SerializeField] private TMPro.TMP_Text m_CountText;
        [SerializeField] private TMPro.TMP_Text m_ValueText;


        
        private void OnEnable()
        {
            if(m_Zone == null){
                m_Zone = FindFirstObjectByType<DeliveryZone>();
            }
            if (m_Zone != null)
            {
                m_Zone.OnZoneStatsChanged += HandleStatsChanged;
                // Initialize with current values
                HandleStatsChanged(m_Zone.DeliveredItemCount, m_Zone.TotalValue);
            }
        }
        
        private void OnDisable()
        {
            if (m_Zone != null)
            {
                m_Zone.OnZoneStatsChanged -= HandleStatsChanged;
            }
        }

        private void HandleStatsChanged(int count, float total)
        {
            if (m_CountText != null)
            {
                m_CountText.text = $"Items: {count}";
            }
            if (m_ValueText != null)
            {
                m_ValueText.text = $"Value: {total:0}";
            }
        }
    }
}


