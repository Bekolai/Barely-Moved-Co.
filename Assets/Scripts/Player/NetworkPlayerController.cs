using UnityEngine;
using Mirror;

namespace BarelyMoved.Player
{
    /// <summary>
    /// Third-person networked player controller
    /// Client sends input to server, server simulates movement and physics
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        #region Constants
        private const float c_Gravity = -9.81f;
        #endregion

        #region Serialized Fields
        [Header("Movement")]
        [SerializeField, Range(1f, 10f)] private float m_WalkSpeed = 3.5f;
        [SerializeField, Range(1f, 15f)] private float m_SprintSpeed = 6f;
        [SerializeField, Range(1f, 10f)] private float m_JumpHeight = 1.5f;
        [SerializeField, Range(1f, 20f)] private float m_RotationSpeed = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform m_GroundCheck;
        [SerializeField] private float m_GroundDistance = 0.2f;
        [SerializeField] private LayerMask m_GroundMask;

        [Header("References")]
        [SerializeField] private Transform m_CameraTarget;
        #endregion

        #region Private Fields
        private CharacterController m_CharacterController;
        private PlayerInputHandler m_InputHandler;
        private Transform m_MainCameraTransform;
        
        private Vector3 m_Velocity;
        private bool m_IsGrounded;
        
        // Network sync
        [SyncVar] private Vector3 m_SyncPosition;
        [SyncVar] private Quaternion m_SyncRotation;
        #endregion

        #region Properties
        public Transform CameraTarget => m_CameraTarget;
        public bool IsGrounded => m_IsGrounded;
        public Vector3 Velocity => m_Velocity;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_InputHandler = GetComponent<PlayerInputHandler>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Get main camera
            if (UnityEngine.Camera.main != null)
            {
                m_MainCameraTransform = UnityEngine.Camera.main.transform;
            }

            // Notify camera system to follow this player
            if (m_CameraTarget != null)
            {
                BarelyMoved.Camera.CameraManager.Instance?.SetFollowTarget(m_CameraTarget);
            }
        }

        private void Update()
        {
            if (isLocalPlayer)
            {
                // Local player: process input and send to server
                HandleMovement();
            }
            else
            {
                // Remote players: interpolate to synced position
                transform.position = Vector3.Lerp(transform.position, m_SyncPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, m_SyncRotation, Time.deltaTime * 10f);
            }
        }

        private void FixedUpdate()
        {
            CheckGroundStatus();
        }
        #endregion

        #region Movement
        private void HandleMovement()
        {
            // Get input
            Vector2 moveInput = m_InputHandler.MoveInput;
            bool isSprinting = m_InputHandler.IsSprintHeld;
            bool jumpPressed = m_InputHandler.IsJumpPressed;

            // Calculate movement direction relative to camera
            Vector3 moveDirection = CalculateMoveDirection(moveInput);

            // Determine speed
            float targetSpeed = isSprinting ? m_SprintSpeed : m_WalkSpeed;
            
            // Move character
            if (moveDirection.magnitude > 0.1f)
            {
                m_CharacterController.Move(moveDirection * targetSpeed * Time.deltaTime);
                RotateTowardsMovement(moveDirection);
            }

            // Handle jumping
            if (jumpPressed && m_IsGrounded)
            {
                m_Velocity.y = Mathf.Sqrt(m_JumpHeight * -2f * c_Gravity);
                m_InputHandler.ConsumeJumpInput();
            }

            // Apply gravity
            if (!m_IsGrounded)
            {
                m_Velocity.y += c_Gravity * Time.deltaTime;
            }
            else if (m_Velocity.y < 0)
            {
                m_Velocity.y = -2f; // Small downward force to keep grounded
            }

            m_CharacterController.Move(m_Velocity * Time.deltaTime);

            // Send position to server for sync
            if (isLocalPlayer)
            {
                CmdUpdateTransform(transform.position, transform.rotation);
            }
        }

        private Vector3 CalculateMoveDirection(Vector2 _input)
        {
            if (m_MainCameraTransform == null)
            {
                return new Vector3(_input.x, 0f, _input.y);
            }

            // Get camera forward and right vectors (flattened on Y)
            Vector3 forward = m_MainCameraTransform.forward;
            Vector3 right = m_MainCameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            // Calculate desired move direction
            return (forward * _input.y + right * _input.x).normalized;
        }

        private void RotateTowardsMovement(Vector3 _direction)
        {
            if (_direction.magnitude < 0.1f) return;

            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_RotationSpeed * Time.deltaTime);
        }

        private void CheckGroundStatus()
        {
            if (m_GroundCheck == null)
            {
                m_IsGrounded = m_CharacterController.isGrounded;
                return;
            }

            m_IsGrounded = Physics.CheckSphere(m_GroundCheck.position, m_GroundDistance, m_GroundMask);
        }
        #endregion

        #region Network Commands
        [Command]
        private void CmdUpdateTransform(Vector3 _position, Quaternion _rotation)
        {
            m_SyncPosition = _position;
            m_SyncRotation = _rotation;
        }
        #endregion

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (m_GroundCheck != null)
            {
                Gizmos.color = m_IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(m_GroundCheck.position, m_GroundDistance);
            }
        }
        #endif
    }
}

