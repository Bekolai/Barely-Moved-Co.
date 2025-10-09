using UnityEngine;

namespace BarelyMoved.Items
{
    /// <summary>
    /// Data container for item value and damage tracking
    /// Can be used as ScriptableObject for item definitions
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Barely Moved/Item Data")]
    public class ItemData : ScriptableObject
    {
        #region Serialized Fields
        [Header("Item Info")]
        [SerializeField] private string m_ItemName = "Item";
        [SerializeField, TextArea] private string m_Description = "";

        [Header("Value")]
        [SerializeField] private float m_BaseValue = 100f;
        [SerializeField] private float m_MinValue = 0f;

        [Header("Damage Settings")]
        [SerializeField] private float m_DamagePerCollision = 10f;
        [SerializeField] private float m_CollisionThreshold = 2f; // Minimum impact velocity to cause damage
        [SerializeField] private bool m_IsFragile = false;
        [SerializeField, Range(0f, 1f)] private float m_FragileDamageMultiplier = 2f;

        [Header("Physics")]
        [SerializeField] private float m_Mass = 1f;
        #endregion

        #region Properties
        public string ItemName => m_ItemName;
        public string Description => m_Description;
        public float BaseValue => m_BaseValue;
        public float MinValue => m_MinValue;
        public float DamagePerCollision => m_DamagePerCollision;
        public float CollisionThreshold => m_CollisionThreshold;
        public bool IsFragile => m_IsFragile;
        public float FragileDamageMultiplier => m_FragileDamageMultiplier;
        public float Mass => m_Mass;
        #endregion

        #region Public Methods
        /// <summary>
        /// Calculate damage from collision velocity
        /// </summary>
        public float CalculateDamage(float _impactVelocity)
        {
            if (_impactVelocity < m_CollisionThreshold)
                return 0f;

            float damage = m_DamagePerCollision * (_impactVelocity / m_CollisionThreshold);
            
            if (m_IsFragile)
            {
                damage *= m_FragileDamageMultiplier;
            }

            return damage;
        }
        #endregion
    }
}

