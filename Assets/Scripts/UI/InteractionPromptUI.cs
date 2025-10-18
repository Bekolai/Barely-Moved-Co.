using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace BarelyMoved.UI
{
    /// <summary>
    /// Simple UI prompt that shows when player is near an interactable object
    /// Automatically detects and displays the correct input button based on active device
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("UI Elements")]
        [SerializeField] private GameObject m_PromptPanel;
        [SerializeField] private TextMeshProUGUI m_PromptText;

        [Header("Settings")]
        [SerializeField] private string m_ActionName = "Interact";
        [SerializeField] private string m_PromptFormat = "Press {0} to Interact";
        #endregion

        #region Private Fields
        private InputAction m_InteractAction;
        private string m_LastDeviceLayout = "";
        private string m_CachedButtonName = "";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Hide();
            FindInteractAction();
        }

        private void OnEnable()
        {
            // Subscribe to device change events
            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
        }

        private void Update()
        {
            // Check if device has changed
            if (m_PromptPanel != null && m_PromptPanel.activeSelf)
            {
                UpdatePromptIfDeviceChanged();
            }
        }
        #endregion

        #region Input Detection
        /// <summary>
        /// Find the Interact action from the player's input
        /// </summary>
        private void FindInteractAction()
        {
            // Try to find the player's input component
            var playerInput = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                m_InteractAction = playerInput.actions.FindAction(m_ActionName);
                
                if (m_InteractAction == null)
                {
                    Debug.LogWarning($"[InteractionPromptUI] Could not find action '{m_ActionName}' in PlayerInput");
                }
            }
            else
            {
                Debug.LogWarning("[InteractionPromptUI] Could not find PlayerInput component in scene");
            }
        }

        /// <summary>
        /// Get the current input device layout (Keyboard, Gamepad, etc.)
        /// </summary>
        private string GetCurrentDeviceLayout()
        {
            var lastDevice = InputSystem.GetDevice<InputDevice>();
            
            // Check for gamepad
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            {
                return "Gamepad";
            }
            
            // Check for keyboard/mouse
            if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            {
                return "Keyboard";
            }
            
            if (Mouse.current != null && Mouse.current.wasUpdatedThisFrame)
            {
                return "Keyboard";
            }

            // Default to last known device
            if (Gamepad.current != null)
            {
                return "Gamepad";
            }

            return "Keyboard";
        }

        /// <summary>
        /// Get the display name for the interact button on the current device
        /// </summary>
        private string GetInteractButtonName()
        {
            if (m_InteractAction == null)
            {
                return "F"; // Fallback
            }

            string currentDevice = GetCurrentDeviceLayout();
            
            // Find the binding for the current device
            for (int i = 0; i < m_InteractAction.bindings.Count; i++)
            {
                var binding = m_InteractAction.bindings[i];
                
                // Check if this binding matches the current device
                bool isGamepadBinding = binding.groups.Contains("Gamepad");
                bool isKeyboardBinding = binding.groups.Contains("Keyboard");
                
                if (currentDevice == "Gamepad" && isGamepadBinding)
                {
                    return GetGamepadButtonDisplayName(binding.path);
                }
                else if (currentDevice == "Keyboard" && isKeyboardBinding)
                {
                    return GetKeyboardButtonDisplayName(binding.path);
                }
            }

            // Fallback
            return currentDevice == "Gamepad" ? "X" : "F";
        }

        /// <summary>
        /// Convert gamepad button path to user-friendly name
        /// </summary>
        private string GetGamepadButtonDisplayName(string _path)
        {
            if (_path.Contains("buttonWest")) return "X"; // Xbox X / PS Square
            if (_path.Contains("buttonSouth")) return "A"; // Xbox A / PS Cross
            if (_path.Contains("buttonEast")) return "B"; // Xbox B / PS Circle
            if (_path.Contains("buttonNorth")) return "Y"; // Xbox Y / PS Triangle
            if (_path.Contains("leftTrigger")) return "LT";
            if (_path.Contains("rightTrigger")) return "RT";
            if (_path.Contains("leftShoulder")) return "LB";
            if (_path.Contains("rightShoulder")) return "RB";
            
            return "Button";
        }

        /// <summary>
        /// Convert keyboard button path to user-friendly name
        /// </summary>
        private string GetKeyboardButtonDisplayName(string _path)
        {
            // Extract key name from path like "<Keyboard>/f"
            if (_path.Contains("/"))
            {
                string keyName = _path.Substring(_path.LastIndexOf('/') + 1);
                return keyName.ToUpper();
            }
            
            return _path;
        }

        /// <summary>
        /// Update prompt text if the input device has changed
        /// </summary>
        private void UpdatePromptIfDeviceChanged()
        {
            string currentDevice = GetCurrentDeviceLayout();
            
            if (currentDevice != m_LastDeviceLayout)
            {
                m_LastDeviceLayout = currentDevice;
                m_CachedButtonName = GetInteractButtonName();
                UpdatePromptText();
            }
        }

        /// <summary>
        /// Update the prompt text with the current button name
        /// </summary>
        private void UpdatePromptText()
        {
            if (m_PromptText != null)
            {
                string buttonName = string.IsNullOrEmpty(m_CachedButtonName) ? GetInteractButtonName() : m_CachedButtonName;
                m_PromptText.text = string.Format(m_PromptFormat, buttonName);
            }
        }

        /// <summary>
        /// Handle input action changes
        /// </summary>
        private void OnActionChange(object _obj, InputActionChange _change)
        {
            // Reset cached values when actions change
            if (_change == InputActionChange.BoundControlsChanged)
            {
                m_CachedButtonName = "";
                m_LastDeviceLayout = "";
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Show the interaction prompt with dynamic button detection
        /// </summary>
        public void Show()
        {
            if (m_PromptPanel != null)
            {
                m_PromptPanel.SetActive(true);
            }

            UpdatePromptIfDeviceChanged();
        }

        /// <summary>
        /// Show the interaction prompt with custom action text and dynamic button
        /// Example: ShowWithAction("to View Jobs") -> "Press E to View Jobs" or "Press X to View Jobs"
        /// </summary>
        public void ShowWithAction(string _actionText)
        {
            if (m_PromptPanel != null)
            {
                m_PromptPanel.SetActive(true);
            }

            // Update format with custom action
            m_PromptFormat = $"Press {{0}} {_actionText}";
            UpdatePromptIfDeviceChanged();
        }

        /// <summary>
        /// Show the interaction prompt with custom text (overrides dynamic detection)
        /// Use ShowWithAction() if you want dynamic button display
        /// </summary>
        public void Show(string _text)
        {
            if (m_PromptPanel != null)
            {
                m_PromptPanel.SetActive(true);
            }

            if (m_PromptText != null)
            {
                m_PromptText.text = _text;
            }
        }

        /// <summary>
        /// Hide the interaction prompt
        /// </summary>
        public void Hide()
        {
            if (m_PromptPanel != null)
            {
                m_PromptPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update the prompt text manually
        /// </summary>
        public void SetPromptText(string _text)
        {
            if (m_PromptText != null)
            {
                m_PromptText.text = _text;
            }
        }

        /// <summary>
        /// Set custom prompt format (use {0} as placeholder for button name)
        /// </summary>
        public void SetPromptFormat(string _format)
        {
            m_PromptFormat = _format;
            UpdatePromptText();
        }
        #endregion
    }
}

