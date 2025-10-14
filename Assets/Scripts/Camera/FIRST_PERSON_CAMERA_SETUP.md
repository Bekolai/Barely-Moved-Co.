# First-Person Camera Setup Guide

## 📋 Overview
This guide will help you set up the first-person camera system for your co-op game using Cinemachine 3 and Unity's new Input System.

---

## 🎯 What's Included

### New Scripts Created:
1. **FirstPersonCameraController.cs** - Handles first-person camera rotation and player orientation
2. **Updated NetworkPlayerController.cs** - Player now faces where the camera looks instead of movement direction

### Features:
- ✅ Mouse look with adjustable sensitivity
- ✅ Gamepad look support with separate sensitivity
- ✅ Vertical look clamping (prevents over-rotation)
- ✅ Player body rotates to match camera direction
- ✅ Movement is always relative to look direction (W = forward, S = backward, A/D = strafe)
- ✅ Network-ready (only works for local player)
- ✅ Cursor lock for proper first-person experience

---

## 🔧 Setup Instructions

### Step 1: Update Your Player Prefab

1. **Open your Player prefab** (Assets/Prefabs/Player.prefab)

2. **Add the FirstPersonCameraController component**:
   - Select the root GameObject
   - Click "Add Component"
   - Search for "First Person Camera Controller"
   - Add it

3. **Configure the FirstPersonCameraController**:
   
   **Camera Settings:**
   - **Virtual Camera**: Leave empty (auto-finds the scene camera)
   - **Camera Target**: Drag your existing CameraTarget child transform here
   
   **Look Sensitivity:**
   - **Mouse Sensitivity**: 2.0 (adjust to preference)
   - **Gamepad Sensitivity**: 3.0 (adjust to preference)
   
   **Look Constraints:**
   - **Min Vertical Angle**: -80° (how far down you can look)
   - **Max Vertical Angle**: 80° (how far up you can look)

4. **Save the prefab**

### Step 2: Setup Scene Camera

1. **Find or create Main Camera** in your scene

2. **Add CinemachineBrain component** (if not already present):
   - Select Main Camera
   - Add Component → Cinemachine → Cinemachine Brain
   - Set Update Method to "Smart Update" or "Late Update"

3. **Create Cinemachine Virtual Camera on Player Prefab**:
   - Open your Player prefab
   - Right-click on the Player root object → Cinemachine → Cinemachine Camera
   - Name it "FirstPersonCamera"
   - Make it a **child of the Player** (this is important!)

4. **Configure the CinemachineCamera**:
   - **Position**: Set to (0, 0, 0) - it will be at player origin
   - **Lens Settings**:
     - Field of View: 60-75 (typical FPS FOV)
   - **Body**: Select "Do Nothing" (we control position via parent)
   - **Aim**: Select "Do Nothing" (we control rotation via script)
   - **Priority**: Set to 10 or higher

### Step 3: Position the Camera Target

The Camera Target is already on your player prefab. Make sure it's positioned correctly:

1. **Select the CameraTarget child object** on your Player prefab
2. **Position it at head/eye height**:
   - Typical position: (0, 1.6, 0) for a standard character
   - Adjust Y value based on your character model height

### Step 4: Test Your Setup

1. **Enter Play Mode**
2. **Start a game** (Host or join via Steam)
3. **Test the controls**:
   - Move mouse to look around
   - W/S to move forward/backward
   - A/D to strafe left/right
   - Character should face where you're looking
   - Movement should feel like a standard FPS game

---

## ⚙️ Customization Options

### Adjusting Mouse Sensitivity
- **In-Game**: Modify `m_MouseSensitivity` on FirstPersonCameraController
- **Recommended Range**: 1.0 - 5.0
- Higher = faster camera movement

### Adjusting Gamepad Sensitivity
- **In-Game**: Modify `m_GamepadSensitivity` on FirstPersonCameraController
- **Recommended Range**: 2.0 - 6.0
- Gamepad typically needs higher sensitivity than mouse

### Changing Vertical Look Limits
- **Min Vertical Angle**: How far down you can look (negative values)
  - Default: -80°
  - Range: -90° to 0°
  
- **Max Vertical Angle**: How far up you can look (positive values)
  - Default: 80°
  - Range: 0° to 90°

### Unlocking the Cursor
If you need to unlock the cursor (for UI, pause menu, etc.):
```csharp
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;
```

To re-lock:
```csharp
Cursor.lockState = CursorLockMode.Locked;
Cursor.visible = false;
```

---

## 🐛 Troubleshooting

### Camera doesn't move when I move the mouse
- Check that the Look action is properly set up in InputSystem_Actions
- Verify PlayerInputHandler is on the player and enabled
- Make sure you're testing with the local player (not a remote client view)

### Player doesn't rotate with camera
- Ensure FirstPersonCameraController is on the same GameObject as NetworkPlayerController
- Check that Camera Target is assigned in FirstPersonCameraController

### Other players' cameras take over my view (IMPORTANT)
- **This is now fixed!** The script disables CinemachineCamera by default
- Only enables it for the local player in `OnStartLocalPlayer()`
- Each client only sees through their own camera
- If you still have issues:
  - Make sure you're using the latest FirstPersonCameraController script
  - Check that CinemachineCamera starts disabled in the prefab (script handles this)
  - Verify each player prefab instance has its own CinemachineCamera child

### Camera follows the wrong player
- This should auto-resolve. FirstPersonCameraController uses `OnStartLocalPlayer()` to only activate for your player
- The camera is explicitly disabled for remote players
- Verify NetworkIdentity has "Local Player Authority" checked

### Movement feels wrong
- The system uses camera-relative movement:
  - W = forward (direction you're facing)
  - S = backward
  - A = strafe left
  - D = strafe right
- This is standard FPS behavior

### Camera jumps or stutters
- Ensure CinemachineBrain Update Method is set to "Smart Update" or "Late Update"
- FirstPersonCameraController runs in LateUpdate() for smooth camera movement

---

## 📝 Notes

### Multiplayer Considerations
- Camera control only works for the local player
- Remote players see your character rotate to match where you're looking
- Camera Target position is synchronized via NetworkTransform

### Input System Integration
- Look input comes from the existing "Look" action in InputSystem_Actions
- Mouse uses delta input for smooth movement
- Gamepad uses stick input

### Performance
- Very lightweight system
- No raycasting or heavy calculations
- Single LateUpdate() call per frame per player

---

## 🎮 Controls Summary

**Keyboard & Mouse:**
- Move: WASD
- Look: Mouse Movement
- Sprint: Left Shift
- Jump: Space
- Interact: E
- Grab/Throw: E / Left Click

**Gamepad:**
- Move: Left Stick
- Look: Right Stick
- Sprint: L3 (Left Stick Press)
- Jump: A/Cross
- Interact: X/Square
- Grab/Throw: RT/R2 / LT/L2

---

## 🚀 Next Steps

You now have a fully functional first-person camera system! Consider adding:
- [ ] Head bob for more immersive movement
- [ ] Camera shake on landing/impacts
- [ ] Smooth camera transitions (crouching, etc.)
- [ ] Weapon/hand models positioned relative to camera
- [ ] Settings menu for sensitivity adjustment

---

## 📚 Related Files
- `FirstPersonCameraController.cs` - Camera controller script
- `NetworkPlayerController.cs` - Updated player movement
- `PlayerInputHandler.cs` - Input handling
- `InputSystem_Actions.inputactions` - Input mapping


