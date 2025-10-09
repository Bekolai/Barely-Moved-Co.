using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

namespace BarelyMoved.Player
{
    /// <summary>
    /// Handles player input using Unity's new Input System
    /// Only processes input for the local player
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputHandler : NetworkBehaviour
    {
        #region Private Fields
        private PlayerInput m_PlayerInput;
        private InputAction m_MoveAction;
        private InputAction m_LookAction;
        private InputAction m_JumpAction;
        private InputAction m_SprintAction;
        private InputAction m_GrabAction;
        private InputAction m_ThrowAction;
        #endregion

        #region Properties
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsSprintHeld { get; private set; }
        public bool IsGrabPressed { get; private set; }
        public bool IsThrowPressed { get; private set; }
        public bool IsInteractPressed { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            m_PlayerInput = GetComponent<PlayerInput>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            InitializeInputActions();
            EnableInput();
        }

        private void OnDestroy()
        {
            DisableInput();
        }
        #endregion

        #region Input System
        private void InitializeInputActions()
        {
            var actionMap = m_PlayerInput.currentActionMap;

            m_MoveAction = actionMap.FindAction("Move");
            m_LookAction = actionMap.FindAction("Look");
            m_JumpAction = actionMap.FindAction("Jump");
            m_SprintAction = actionMap.FindAction("Sprint");
            m_GrabAction = actionMap.FindAction("Grab");
            m_ThrowAction = actionMap.FindAction("Throw");

            // Subscribe to input events
            if (m_JumpAction != null)
                m_JumpAction.performed += OnJumpPerformed;
            
            if (m_ThrowAction != null)
                m_ThrowAction.performed += OnThrowPerformed;
            
            if (m_GrabAction != null)
                m_GrabAction.performed += OnGrabPerformed;
        }

        private void Update()
        {
            // Only process input for local player
            if (!isLocalPlayer) return;

            UpdateInputValues();
        }

        private void UpdateInputValues()
        {
            MoveInput = m_MoveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            LookInput = m_LookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            IsSprintHeld = m_SprintAction?.IsPressed() ?? false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext _context)
        {
            if (!isLocalPlayer) return;
            IsJumpPressed = true;
        }

        private void OnGrabPerformed(InputAction.CallbackContext _context)
        {
            if (!isLocalPlayer) return;
            IsGrabPressed = true;
        }

        private void OnThrowPerformed(InputAction.CallbackContext _context)
        {
            if (!isLocalPlayer) return;
            IsThrowPressed = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Consume jump input (call after processing)
        /// </summary>
        public void ConsumeJumpInput()
        {
            IsJumpPressed = false;
        }

        /// <summary>
        /// Consume grab input (call after processing)
        /// </summary>
        public void ConsumeGrabInput()
        {
            IsGrabPressed = false;
        }

        /// <summary>
        /// Consume throw input (call after processing)
        /// </summary>
        public void ConsumeThrowInput()
        {
            IsThrowPressed = false;
        }

        /// <summary>
        /// Enable player input
        /// </summary>
        public void EnableInput()
        {
            if (isLocalPlayer)
            {
                m_PlayerInput.ActivateInput();
            }
        }

        /// <summary>
        /// Disable player input
        /// </summary>
        public void DisableInput()
        {
            if (m_PlayerInput != null)
            {
                m_PlayerInput.DeactivateInput();
            }
        }
        #endregion
    }
}

