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
        private InputAction m_InteractAction;
        #endregion

        #region Properties
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsSprintHeld { get; private set; }
        public bool IsGrabPressed { get; private set; }
        public bool IsThrowPressed { get; private set; }
        public bool IsInteractPressed { get; private set; }
        public bool IsAdjustHeld { get; private set; } // RMB held
        public float ScrollDelta { get; private set; } // Mouse scroll Y per frame
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
            if (m_PlayerInput == null)
            {
                Debug.LogError("[PlayerInputHandler] PlayerInput component is null!");
                return;
            }

            if (m_PlayerInput.actions == null)
            {
                Debug.LogError("[PlayerInputHandler] InputActionAsset is not assigned to PlayerInput!");
                return;
            }

            var actionMap = m_PlayerInput.currentActionMap;
            if (actionMap == null)
            {
                Debug.LogError("[PlayerInputHandler] Current action map is null!");
                return;
            }

            m_MoveAction = actionMap.FindAction("Move");
            m_LookAction = actionMap.FindAction("Look");
            m_JumpAction = actionMap.FindAction("Jump");
            m_SprintAction = actionMap.FindAction("Sprint");
            m_GrabAction = actionMap.FindAction("Grab");
            m_ThrowAction = actionMap.FindAction("Throw");
            m_InteractAction = actionMap.FindAction("Interact");

            // Check if required actions were found
            if (m_MoveAction == null) Debug.LogWarning("[PlayerInputHandler] Move action not found!");
            if (m_LookAction == null) Debug.LogWarning("[PlayerInputHandler] Look action not found!");
            if (m_JumpAction == null) Debug.LogWarning("[PlayerInputHandler] Jump action not found!");
            if (m_SprintAction == null) Debug.LogWarning("[PlayerInputHandler] Sprint action not found!");
            if (m_GrabAction == null) Debug.LogWarning("[PlayerInputHandler] Grab action not found!");
            if (m_ThrowAction == null) Debug.LogWarning("[PlayerInputHandler] Throw action not found!");
            if (m_InteractAction == null) Debug.LogWarning("[PlayerInputHandler] Interact action not found!");

            // Subscribe to input events
            if (m_JumpAction != null)
                m_JumpAction.performed += OnJumpPerformed;
            
            if (m_ThrowAction != null)
                m_ThrowAction.performed += OnThrowPerformed;
            
            if (m_GrabAction != null)
                m_GrabAction.performed += OnGrabPerformed;
            
            if (m_InteractAction != null)
                m_InteractAction.performed += OnInteractPerformed;
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

            // Direct mouse access for RMB + scroll (new input system)
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                IsAdjustHeld = UnityEngine.InputSystem.Mouse.current.rightButton.isPressed;
                var scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue();
                ScrollDelta = scroll.y;
            }
            else
            {
                IsAdjustHeld = false;
                ScrollDelta = 0f;
            }
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

        private void OnInteractPerformed(InputAction.CallbackContext _context)
        {
            if (!isLocalPlayer) return;
            IsInteractPressed = true;
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
        /// Consume interact input (call after processing)
        /// </summary>
        public void ConsumeInteractInput()
        {
            IsInteractPressed = false;
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

