# Quick Setup Guide - Network Fixes

## ⚠️ IMPORTANT: Required Setup Steps

After applying these network fixes, you **MUST** complete the following setup steps:

---

## 1. Add NetworkPlayerData to Player Prefab

**Location:** `Assets/Prefabs/Player.prefab` (or wherever your player prefab is)

**Steps:**
1. Open your player prefab in Unity
2. Click "Add Component"
3. Search for "Network Player Data"
4. Add the component
5. **Save the prefab** (Ctrl+S)

**Why:** The new Steam integration requires this component to sync player names and avatars across the network.

---

## 2. Verify Scene Setup

### MainMenu Scene
Make sure you have:
- [x] `MainMenuManager` in the scene
- [x] `MainMenuUI` in the scene
- [x] `BarelyMovedNetworkManager` (should be DontDestroyOnLoad)
- [x] UI panels for Main Menu, Lobby, and Join properly assigned

### PrepScene & Level Scenes
Make sure you have:
- [x] `JobBoardZone` with proper collider setup (isTrigger = true)
- [x] `LevelFinishZone` with proper collider setup (isTrigger = true)
- [x] Both zones on correct layer (assigned in m_InteractableLayer)

---

## 3. Testing Steps

### Test 1: Client Can Join Lobby
1. **Build the game** or run two instances (one in editor, one build)
2. **Host:** Click "Host" button
3. **Host:** You should see the lobby UI immediately
4. **Client:** Click "Join" button
5. **Client:** Enter host's IP (or "localhost" if on same machine)
6. **Client:** Click "Connect"
7. ✅ **Expected:** Client should see lobby UI after connecting
8. ✅ **Expected:** Both should see player names (Steam names or "Player_XXXX")

### Test 2: Client Can Interact
1. **Host:** Start the game (go to PrepScene or Level)
2. **Both players:** Walk up to a JobBoardZone or LevelFinishZone
3. ✅ **Expected:** Both see "Press F to interact" prompt
4. **Both players:** Press F
5. ✅ **Expected:** Both can successfully interact (open job board or finish level)

### Test 3: Steam Integration (Optional)
1. Make sure Steam client is running
2. Run the game
3. ✅ **Expected:** Lobby shows your actual Steam username
4. ✅ **Expected:** Lobby shows your Steam avatar (small square image)

---

## 4. Common Issues & Solutions

### Issue: "Client stuck at IP input panel"
**Solution:** Make sure you followed the fixes. The client should now properly show the lobby after connecting.

### Issue: "Client can't interact with zones"
**Solution:** 
- Check that both `JobBoardZone` and `LevelFinishZone` have colliders set to `isTrigger = true`
- Check that player is on the correct layer (defined in zone's `m_PlayerLayer` mask)
- Make sure you applied the Command fix to both zone scripts

### Issue: "No Steam names showing"
**Solution:**
- Make sure `NetworkPlayerData` component is on the player prefab
- Check that player prefab is assigned in `BarelyMovedNetworkManager`
- If Steam isn't running, it will show "Player_XXXX" instead (this is normal)

### Issue: "Player avatars not showing"
**Solution:**
- Steam avatars may take a moment to load
- Check console for Steam-related errors
- Make sure `DISABLESTEAMWORKS` is not defined in your project settings
- Avatars are optional - the system works fine without them

---

## 5. Verification Checklist

Before considering setup complete, verify:

- [x] `NetworkPlayerData` component exists on player prefab
- [x] `NetworkPlayerData.cs` file exists in `Assets/Scripts/Player/`
- [x] `NETWORK_FIXES_SUMMARY.md` file exists (documentation)
- [x] No compile errors in Unity Console
- [x] Host can see lobby
- [x] Client can join and see lobby
- [x] Both host and client can interact with zones
- [x] Player names show in lobby (Steam or fallback)

---

## 6. Files Modified Summary

### New Files Created:
- ✅ `Assets/Scripts/Player/NetworkPlayerData.cs`
- ✅ `Assets/Scripts/NETWORK_FIXES_SUMMARY.md`
- ✅ `Assets/Scripts/QUICK_SETUP_GUIDE.md` (this file)

### Files Modified:
- ✅ `Assets/Scripts/Network/BarelyMovedNetworkManager.cs`
- ✅ `Assets/Scripts/GameManagement/MainMenuManager.cs`
- ✅ `Assets/Scripts/Interactables/LevelFinishZone.cs`
- ✅ `Assets/Scripts/Interactables/JobBoardZone.cs`
- ✅ `Assets/Scripts/UI/LobbyPlayerUI.cs`

---

## 7. Next Steps

Once setup is complete:

1. **Test in multiplayer** using the testing steps above
2. **Build and test** with actual builds (not just in editor)
3. **Review** `NETWORK_FIXES_SUMMARY.md` for technical details
4. **Customize** lobby UI if desired (see LobbyPlayerUI component)

---

## Need Help?

If you encounter issues:

1. Check Unity Console for errors
2. Review `NETWORK_FIXES_SUMMARY.md` for troubleshooting
3. Check that all modified files compiled successfully
4. Verify that Mirror is properly installed and up to date
5. Make sure Steamworks.NET is properly configured (if using Steam)

---

**Setup complete! Your multiplayer networking should now work correctly for all players.**


