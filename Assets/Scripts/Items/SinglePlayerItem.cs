using UnityEngine;

namespace BarelyMoved.Items
{
    /// <summary>
    /// Item that can be carried by a single player
    /// Smaller items like boxes, lamps, etc.
    /// </summary>
    public class SinglePlayerItem : GrabbableItem
    {
        #region Serialized Fields
        [Header("Single Player Settings")]
        [SerializeField] private Vector3 m_HoldOffset = new Vector3(0f, 1f, 0.5f);
        [SerializeField] private Vector3 m_HoldRotation = Vector3.zero;
        #endregion

        #region Properties
        public Vector3 HoldOffset => m_HoldOffset;
        public Vector3 HoldRotation => m_HoldRotation;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            m_Size = ItemSize.Small;
        }
        #endregion

        #if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw hold position preview
            if (m_GrabPoints != null && m_GrabPoints.Length > 0 && m_GrabPoints[0] != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 holdPos = m_GrabPoints[0].position + m_HoldOffset;
                Gizmos.DrawWireCube(holdPos, Vector3.one * 0.2f);
                Gizmos.DrawLine(m_GrabPoints[0].position, holdPos);
            }
        }
        #endif
    }
}

