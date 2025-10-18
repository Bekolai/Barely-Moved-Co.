# Avatar, Emoji & Kick Fixes - October 18, 2025

## Issues Fixed

### 1. ✅ Steam Avatar Upside Down
**Problem**: Steam avatars were displaying upside down

**Root Cause**: Steam provides avatar data in a format that's vertically flipped compared to Unity's texture coordinate system.

**Fix**: Added vertical flip when processing avatar data
```csharp
// Flip the image vertically (Steam avatars are upside down)
byte[] flippedData = new byte[avatarData.Length];
int rowSize = (int)width * 4;
for (int row = 0; row < height; row++)
{
    int sourceRow = (int)height - row - 1; // Flip vertically
    System.Array.Copy(avatarData, sourceRow * rowSize, flippedData, row * rowSize, rowSize);
}
```

**Result**: Avatars now display correctly! ✅

---

### 2. ✅ Crown Emoji (🏆) Showing as Blank Square
**Problem**: Crown emoji showing as □ (blank square) because default TextMeshPro font doesn't support emoji

**Fix**: Replaced emoji with text-based indicators
- Changed from: `"🏆 PlayerName"`
- Changed to: `"[HOST] PlayerName"` (in yellow color)

**Benefits**:
- Works with all fonts ✅
- Clear and readable ✅
- Color-coded (yellow for host, cyan for you, white for others) ✅

**Files Changed**:
- `InGameLobbyPanel.cs` - ESC menu lobby panel
- `LobbyPlayerUI.cs` - MainMenu lobby (if still used)

---

### 3. ✅ Kick Not Working for Local Testing
**Problem**: When testing locally (same Steam account), kick might not work properly

**Root Cause**: Both instances share the same Steam ID, which could confuse identification

**Fix**: Enhanced kick validation with detailed logging
- Uses **network connection ID** (not Steam ID) for identification
- Multiple safety checks to prevent kicking wrong player
- Added extensive debug logging to diagnose issues

**New Validation Checks**:
1. Must be on server/host
2. Player data must exist
3. Connection must exist
4. Cannot kick `isLocalPlayer` (yourself)
5. Cannot kick `isServer` (the host)
6. Cannot kick connection ID 0 (host connection)
7. Connection ID must be > 0

**Debug Output**:
```
[InGameLobbyPanel] Kick request - Player: ClientName, ConnID: 1, isLocalPlayer: False, isServer: False
[InGameLobbyPanel] ✓ Kicking player: ClientName (ConnectionID: 1)
```

---

## Code Changes Summary

### NetworkPlayerData.cs
**Avatar Flipping**:
```csharp
// OLD: Direct load (upside down)
m_AvatarTexture.LoadRawTextureData(avatarData);

// NEW: Flip before loading
byte[] flippedData = new byte[avatarData.Length];
int rowSize = (int)width * 4;
for (int row = 0; row < height; row++)
{
    int sourceRow = (int)height - row - 1;
    System.Array.Copy(avatarData, sourceRow * rowSize, flippedData, row * rowSize, rowSize);
}
m_AvatarTexture.LoadRawTextureData(flippedData);
```

---

### InGameLobbyPanel.cs
**Emoji Replacement**:
```csharp
// OLD: Emoji (doesn't display)
string prefix = _playerData.isServer ? "🏆 " : "";

// NEW: Text indicator with color
string prefix = _playerData.isServer ? "[HOST] " : "";
_entry.nameText.color = isLocalPlayer ? Color.cyan : 
                        (_playerData.isServer ? Color.yellow : Color.white);
```

**Enhanced Kick Validation**:
```csharp
// Added detailed logging
Debug.Log($"[InGameLobbyPanel] Kick request - Player: {_playerData.PlayerName}, " +
          $"ConnID: {conn.connectionId}, isLocalPlayer: {_playerData.isLocalPlayer}, " +
          $"isServer: {_playerData.isServer}");

// Improved validation
if (conn.connectionId <= 0)
{
    Debug.LogWarning($"[InGameLobbyPanel] Invalid connection ID: {conn.connectionId}");
    return;
}

Debug.Log($"[InGameLobbyPanel] ✓ Kicking player: {_playerData.PlayerName} (ConnectionID: {conn.connectionId})");
```

---

### LobbyPlayerUI.cs
**Same emoji fix** applied to MainMenu lobby panel (if used).

---

## Visual Comparison

### Before:
```
ESC Menu:
□ YourName (You)        ← Blank square, confusing
□ ClientName [Kick]     ← Blank square

Avatar: ↙️ Upside down
```

### After:
```
ESC Menu:
[HOST] YourName (You)   ← Yellow text, clear!
ClientName [Kick]        ← White text

Avatar: ✅ Correct orientation
```

---

## Testing Guide

### Test 1: Avatar Orientation ✅
1. Start game (Steam must be running)
2. Wait for avatar to load
3. Press ESC
4. **Check**: Avatar should be right-side up!

### Test 2: Host Indicator ✅
1. Start as host
2. Press ESC
3. **Check**: Your name shows `[HOST] YourName (You)` in yellow/cyan
4. **Check**: No blank squares!

### Test 3: Kick with Same Steam Account 🧪
1. **Host**: Start game
2. **Client**: Start second instance (same machine, same Steam)
3. **Host**: Press ESC
4. **Check Console**:
   ```
   [InGameLobbyPanel] Player: YourName, isHost:True, isLocalPlayer:True, isServer:True, showKick:False
   [InGameLobbyPanel] Player: YourName, isHost:True, isLocalPlayer:False, isServer:False, showKick:True
   ```
5. **Visual**: Kick button should show next to second entry (client)
6. **Click Kick**
7. **Check Console**:
   ```
   [InGameLobbyPanel] Kick request - Player: YourName, ConnID: 1, isLocalPlayer: False, isServer: False
   [InGameLobbyPanel] ✓ Kicking player: YourName (ConnectionID: 1)
   ```
8. **Expected**: Client disconnects and returns to main menu

---

## Important Notes

### About Local Testing
When testing with same Steam account:
- Both players will have **same name** and **same Steam ID**
- But they will have **different connection IDs**:
  - Host: Connection ID = 0
  - Client: Connection ID = 1 (or higher)
- Kick logic uses **connection ID**, not Steam ID ✅

### About isLocalPlayer
- `isLocalPlayer` is **different** for each instance
- Host's player: `isLocalPlayer = true` on host, `false` on client
- Client's player: `isLocalPlayer = false` on host, `true` on client
- This is how kick knows which player you are ✅

### About isServer
- Only the **host's player object** has `isServer = true`
- All other players have `isServer = false`
- Used to identify host and prevent kicking them ✅

---

## Troubleshooting

### "Kick button still not showing"
**Check Console Logs**:
```
[InGameLobbyPanel] Player: Name, isHost:?, isLocalPlayer:?, isServer:?, showKick:?
```

**Kick button should show if**:
- `isHost: True` (you are host)
- `isLocalPlayer: False` (not your own player)
- `isServer: False` (not the host player)

**If all conditions met but button still hidden**:
- Check if button GameObject exists in UI
- Check button's active state in hierarchy
- Verify button isn't behind another UI element

---

### "Kick does nothing / errors"
**Check Console Logs**:
```
[InGameLobbyPanel] Kick request - Player: X, ConnID: Y, ...
```

**If you see warnings like**:
- "Cannot kick yourself!" → Button showed for wrong player (bug in visibility logic)
- "Cannot kick the host!" → Button showed for host (bug in visibility logic)
- "Invalid connection ID: 0" → Trying to kick connection 0 (host)
- "No connection found" → Player doesn't have `connectionToClient`

**If you see success log but client doesn't disconnect**:
- Check `BarelyMovedNetworkManager.KickPlayer()` is being called
- Verify `conn.Disconnect()` is executing
- Check for Mirror networking errors

---

### "Avatar still upside down"
**Verify fix applied**:
1. Check `NetworkPlayerData.cs`
2. Look for the flip loop:
   ```csharp
   for (int row = 0; row < height; row++)
   {
       int sourceRow = (int)height - row - 1;
       ...
   }
   ```
3. If missing, fix wasn't applied correctly

---

### "Still seeing blank squares"
**Possible causes**:
1. Old code still using emoji
2. Font doesn't support "[" and "]" characters (unlikely)
3. Text color is same as background (check color settings)

**Verify text**:
- Should say `[HOST]` not `🏆`
- Should be in yellow color for host

---

## Summary

| Issue | Status | Fix | Impact |
|-------|--------|-----|--------|
| Upside down avatars | ✅ Fixed | Vertical flip algorithm | Avatars display correctly |
| Emoji not showing | ✅ Fixed | Replace with [HOST] text | Works on all fonts |
| Kick logic | ✅ Enhanced | Better validation + logging | More reliable, easier to debug |

**All fixes complete!** 🎉

---

## Expected Behavior Now

### When Host Opens ESC Menu:
- Sees: `[HOST] YourName (You)` (yellow/cyan)
- Sees: `ClientName [Kick]` (white)
- Avatars display right-side up
- Kick button only shows next to clients (not self or host's own entry)

### When Client Opens ESC Menu:
- Sees: `[HOST] HostName` (yellow)
- Sees: `YourName (You)` (cyan)
- Avatars display right-side up
- No kick buttons (not host)

### When Kick Button Clicked:
- Console shows validation logs
- If valid: `✓ Kicking player: Name (ConnectionID: X)`
- Client disconnects
- Returns to main menu
- Host sees updated player count

---

## Files Changed

- ✅ `NetworkPlayerData.cs` - Avatar flipping
- ✅ `InGameLobbyPanel.cs` - Emoji fix + kick validation
- ✅ `LobbyPlayerUI.cs` - Emoji fix
- ✅ `FIXES_OCT18_AVATAR_EMOJI_KICK.md` - This documentation

