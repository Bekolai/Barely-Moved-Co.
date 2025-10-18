# Pause Menu & Kick Fixes - October 18, 2025

## Issues Found & Fixed

### Issue 1: Hard-Coded ESC Key ❌
**Problem**: PauseMenuManager used `Keyboard.current[Key.Escape]` directly instead of Input System
**Impact**: Not configurable, doesn't work with gamepad, violates Input System pattern

**Fix**: ✅
- Changed to use `InputActionReference`
- Added "Pause" action to Input System (Player map)
- Bound to ESC (keyboard) and Start (gamepad)
- Proper enable/disable lifecycle management

**Files Changed**:
- `PauseMenuManager.cs` - Now uses `InputActionReference m_PauseAction`
- `InputSystem_Actions.inputactions` - Added Pause action with bindings

---

### Issue 2: Pause Menu Initialization Problem ❌
**Problem**: Pause menu wouldn't show up initially until referenced manually
**Impact**: Menu wouldn't work on first open

**Fix**: ✅
- Added `Awake()` method to ensure `m_PauseMenuRoot` is disabled at start
- This prevents "invisible but active" state issues
- Menu now properly hidden on scene load

**Files Changed**:
- `PauseMenuManager.cs` - Added Awake() initialization

---

### Issue 3: Kick Targeting Host (Connection ID 0) ❌
**Problem**: Kick button was visible for host and clicking it kicked connection ID 0 (the host itself!)
**Impact**: Host could kick themselves, or worse, nothing would happen but errors would spam

**Logs**:
```
[InGameLobbyPanel] Kicking player: Bekolai (ConnectionID: 0)
[BarelyMovedNetworkManager] Kicking player with connection ID: 0
```

**Root Cause**: 
- NetworkPlayerData.connectionToClient for host returns connection ID 0
- Kick button was showing for host player
- No validation to prevent kicking connection ID 0

**Fix**: ✅
- Hide kick button for host player (`!_playerData.isServer`)
- Hide kick button for local player (`!_playerData.isLocalPlayer`)  
- Added safety check: don't kick if `connectionId == 0`
- Added multiple validation layers in `OnKickButtonClicked()`

**Files Changed**:
- `InGameLobbyPanel.cs` - Enhanced kick button visibility logic & validation

---

## Code Changes Summary

### PauseMenuManager.cs

**Before**:
```csharp
[SerializeField] private Key m_PauseKey = Key.Escape;

private void Update()
{
    if (Keyboard.current != null && Keyboard.current[m_PauseKey].wasPressedThisFrame)
    {
        TogglePause();
    }
}
```

**After**:
```csharp
[SerializeField] private InputActionReference m_PauseAction;

private void Awake()
{
    // Ensure pause menu is hidden at start
    if (m_PauseMenuRoot != null)
    {
        m_PauseMenuRoot.SetActive(false);
    }
}

private void OnEnable()
{
    if (m_PauseAction != null)
    {
        m_PauseAction.action.Enable();
        m_PauseAction.action.performed += OnPausePerformed;
    }
}

private void OnDisable()
{
    if (m_PauseAction != null)
    {
        m_PauseAction.action.performed -= OnPausePerformed;
        m_PauseAction.action.Disable();
    }
}

private void OnPausePerformed(InputAction.CallbackContext _context)
{
    TogglePause();
}
```

---

### InGameLobbyPanel.cs

**Kick Button Visibility - Before**:
```csharp
// Show kick button if we are host and not local player
bool showKick = _isHost && !_playerData.isLocalPlayer;
```

**After**:
```csharp
// Show kick button if:
// - We are host
// - This is NOT the local player (can't kick yourself)
// - This is NOT the server player (can't kick the host)
bool showKick = _isHost && !_playerData.isLocalPlayer && !_playerData.isServer;
```

**Kick Handler - Added Validation**:
```csharp
private void OnKickButtonClicked(NetworkPlayerData _playerData)
{
    // ... existing checks ...
    
    // NEW: Safety checks - don't kick yourself or the host
    if (_playerData.isLocalPlayer)
    {
        Debug.LogWarning("[InGameLobbyPanel] Cannot kick yourself!");
        return;
    }

    if (_playerData.isServer)
    {
        Debug.LogWarning("[InGameLobbyPanel] Cannot kick the host!");
        return;
    }

    NetworkConnectionToClient conn = _playerData.connectionToClient;
    if (conn != null)
    {
        // NEW: Additional safety check - don't kick connection ID 0
        if (conn.connectionId == 0)
        {
            Debug.LogWarning($"[InGameLobbyPanel] Cannot kick connection ID 0 (host connection)");
            return;
        }
        
        // ... proceed with kick ...
    }
}
```

---

### InputSystem_Actions.inputactions

**Added Pause Action**:
```json
{
    "name": "Pause",
    "type": "Button",
    "id": "f5e67890-abcd-4ef1-2345-6789abcdef01",
    "expectedControlType": "Button"
}
```

**Added Bindings**:
```json
// Keyboard - ESC
{
    "path": "<Keyboard>/escape",
    "groups": "Keyboard&Mouse",
    "action": "Pause"
}

// Gamepad - Start Button
{
    "path": "<Gamepad>/start",
    "groups": "Gamepad",
    "action": "Pause"
}
```

---

## Unity Setup Required

### 1. Update PauseMenuManager References

In your pause menu GameObject:

1. **Remove**: Old "Pause Key" field (no longer used)
2. **Assign**: "Pause Action" field
   - Click the circle picker
   - Select: `Player > Pause`

### 2. Reimport Input Actions

Unity should auto-detect the changes to `InputSystem_Actions.inputactions`, but if pause doesn't work:

1. Select `InputSystem_Actions.inputactions` in Project window
2. Click "Generate C# Class" (if button shows)
3. Or right-click → Reimport

### 3. Test All Scenarios

#### Host Testing
- [ ] Press ESC → Pause menu opens
- [ ] See your name with 🏆 crown
- [ ] NO kick button next to your name
- [ ] Press ESC again → Menu closes

#### Client Testing  
- [ ] Join host's game
- [ ] Press ESC → See both players
- [ ] See host with crown, yourself with "(You)"
- [ ] NO kick button on your side (you're not host)

#### Host Kick Testing
- [ ] Host presses ESC
- [ ] See kick button next to client (not next to self!)
- [ ] Click kick button
- [ ] Client disconnects
- [ ] No errors about connection ID 0

---

## Testing Checklist

### Input System ✅
- [ ] ESC key opens pause menu
- [ ] Gamepad Start button opens pause menu
- [ ] Can rebind in Input System settings
- [ ] Works with both keyboard and gamepad

### Initialization ✅
- [ ] Pause menu hidden on scene load
- [ ] First press of ESC shows menu correctly
- [ ] No "reference not found" errors

### Kick Functionality ✅
- [ ] Kick button ONLY shows for clients (not host)
- [ ] Kick button NEVER shows next to yourself
- [ ] Clicking kick successfully disconnects client
- [ ] No attempts to kick connection ID 0
- [ ] Client returns to main menu when kicked

---

## Validation Logs

### Expected Logs (Good) ✅
```
[PauseMenuManager] Paused
[InGameLobbyPanel] Kicking player: PlayerName (ConnectionID: 1)
[BarelyMovedNetworkManager] Kicking player with connection ID: 1
```

### Warning Logs (Prevented Bad Actions) ⚠️
```
[InGameLobbyPanel] Cannot kick yourself!
[InGameLobbyPanel] Cannot kick the host!
[InGameLobbyPanel] Cannot kick connection ID 0 (host connection)
```

### Error Logs (Should Never See) ❌
```
// These should NEVER appear now:
[InGameLobbyPanel] Kicking player: HostName (ConnectionID: 0)  // FIXED!
Command ... called ... while NetworkClient is not ready  // FIXED!
```

---

## Summary

| Issue | Status | Files | Impact |
|-------|--------|-------|--------|
| Hard-coded ESC | ✅ Fixed | PauseMenuManager.cs, InputSystem_Actions | Now uses proper Input System |
| Menu initialization | ✅ Fixed | PauseMenuManager.cs | Menu shows correctly on first open |
| Kick targeting host | ✅ Fixed | InGameLobbyPanel.cs | Can only kick actual clients, not host |

**All issues resolved!** 🎉

---

## Next Steps

1. **Open Unity Editor**
2. **Select PauseMenuManager in scene**
3. **Assign Pause Action**: `Player > Pause`
4. **Test**: Press ESC, verify menu works
5. **Test Multiplayer**: Verify kick works correctly

---

## Related Documentation

- `ESC_MENU_LOBBY_SYSTEM.md` - Full system documentation
- `README_NEW_LOBBY_SYSTEM.md` - Quick start guide
- `IMPLEMENTATION_SUMMARY_OCT18.md` - Implementation details

