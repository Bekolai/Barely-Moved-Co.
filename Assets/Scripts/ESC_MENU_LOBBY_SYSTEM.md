# ESC Menu Lobby System - Setup Guide

## Overview
This system removes the standalone lobby screen and implements a seamless "direct-to-gameplay" flow with lobby info accessible via ESC menu.

## New Flow

### Host Flow
```
Main Menu → Click "Host" → Directly to Prep Scene
                              ↓
                         In Prep Scene:
                         - Press ESC → See lobby panel
                         - Invite friends via Steam
                         - Friends join directly into prep
                         - Kick players if needed (host only)
```

### Client Flow
```
Main Menu → Click "Join" → Enter IP → Join
                                        ↓
                                   Directly to host's current scene
                                   (Prep or Level - wherever host is)
                                        ↓
                                   Press ESC → See lobby panel
```

## Key Features

✅ **No Blocking Lobby** - Host goes straight to prep scene
✅ **Dynamic Joining** - Clients can join anytime during prep or gameplay
✅ **ESC Menu Lobby** - Shows connected players with names, avatars, and kick option
✅ **Steam Integration** - Use Steam friend invites (if Steam is enabled)
✅ **Host Control** - Kick players via ESC menu
✅ **Seamless Experience** - No waiting in lobby, just play

---

## Components Created

### 1. PauseMenuManager.cs
**Purpose**: Manages the ESC/pause menu system

**Features**:
- Detects ESC key press
- Shows/hides pause menu
- Panel navigation (Lobby ↔ Settings)
- Return to main menu
- Quit game

**Location**: `Assets/Scripts/UI/PauseMenuManager.cs`

### 2. InGameLobbyPanel.cs
**Purpose**: Displays connected players in the ESC menu

**Features**:
- Shows all connected players
- Displays Steam names and avatars
- Kick button for host
- Real-time updates when players join/leave
- "🏆" crown for host
- "(You)" indicator for local player

**Location**: `Assets/Scripts/UI/InGameLobbyPanel.cs`

### 3. NetworkConnectionTracker.cs
**Purpose**: Syncs connection count from server to clients

**Features**:
- NetworkBehaviour with SyncVar
- Auto-spawned by server
- Fires events on count changes
- Singleton access

**Location**: `Assets/Scripts/Network/NetworkConnectionTracker.cs`

---

## Modified Components

### BarelyMovedNetworkManager.cs
**Added**:
- `KickPlayer(NetworkConnectionToClient conn)` method
- Connection tracker spawning and updates

### MainMenuManager.cs
**Changed**:
- `HostGame()` now goes directly to prep scene (no lobby wait)
- `OnClientConnectedToServer()` doesn't show lobby (clients join scene directly)

---

## Setup Instructions

### Step 1: Create Pause Menu UI (In Prep & Level Scenes)

1. **Create Pause Menu Root**
   ```
   Canvas
   └── PauseMenuRoot (GameObject)
       ├── PauseMenuManager (Component)
       ├── Background Panel (Image - semi-transparent black)
       └── ContentPanel
           ├── LobbyPanel
           │   ├── InGameLobbyPanel (Component)
           │   ├── Title: "Connected Players"
           │   ├── PlayerListContainer (Vertical Layout Group)
           │   ├── PlayerCountText (Text)
           │   └── Buttons (Resume, Settings, Main Menu)
           └── SettingsPanel
               └── (Your settings UI)
   ```

2. **Configure PauseMenuManager**
   - Assign `PauseMenuRoot` to the root GameObject
   - Assign `LobbyPanel` reference
   - Assign `SettingsPanel` reference
   - **Assign `PauseAction`**: Click the circle picker → Select `Player > Pause` (from Input System)
   - Set `PauseGameWhenOpen` to `false` (for multiplayer)

3. **Configure InGameLobbyPanel**
   - Assign `PlayerListContainer` (Transform with Vertical Layout Group)
   - Assign `PlayerCountText` (TextMeshPro Text)
   - Set `MaxPlayers` to 4 (or your game's max)
   - Optional: Assign `PlayerEntryPrefab` (or leave null for auto-creation)

4. **Make PauseMenuRoot inactive by default**
   - Set `PauseMenuRoot` GameObject to inactive
   - It will show when ESC is pressed

### Step 2: Create Player Entry Prefab (Optional)

If you want custom styling for player entries:

1. **Create PlayerEntryPrefab**
   ```
   PlayerEntry (GameObject with Horizontal Layout Group)
   ├── Avatar (RawImage - 40x40)
   ├── NameText (TextMeshProUGUI - 200 width)
   └── KickButton (Button - 80x30)
       └── Text: "Kick" (red color)
   ```

2. **Assign to InGameLobbyPanel**
   - Assign prefab to `PlayerEntryPrefab` field

**OR** leave null and it will auto-generate player entries.

### Step 3: Test the System

#### Testing Host Flow
1. **Start Play Mode**
2. **Click "Host" in Main Menu**
   - Should immediately load Prep Scene
   - Should see "Connection tracker spawned" in console
3. **Press ESC in Prep Scene**
   - Should see pause menu with lobby panel
   - Should show "Players: 1/4"
   - Should show your name with crown "🏆 YourName (You)"
   - Kick button should NOT show for yourself

#### Testing Client Join
1. **Build and run a second instance** (or use ParrelSync)
2. **Click "Join" and enter host IP**
3. **Client should load directly into Prep Scene**
   - No lobby screen shown
   - Join directly into gameplay
4. **Press ESC on Client**
   - Should see "Players: 2/4"
   - Should show "✓ YourName (You)"
   - Should see host with crown "🏆 HostName"
   - No kick button on client
5. **Press ESC on Host**
   - Should see "Players: 2/4"
   - Should see both players
   - Should see kick button next to client's name

#### Testing Kick Functionality
1. **As host, press ESC**
2. **Click "Kick" button next to a client**
3. **Client should be disconnected**
   - Client returns to main menu
   - Host sees updated player count

---

## UI Layout Example

### ESC Menu - Lobby Panel
```
┌─────────────────────────────────────────┐
│           CONNECTED PLAYERS             │
├─────────────────────────────────────────┤
│  Players: 3/4                           │
├─────────────────────────────────────────┤
│  [Avatar] 🏆 HostName (You)             │
│  [Avatar] ✓ Player2           [Kick]   │
│  [Avatar] ✓ Player3           [Kick]   │
├─────────────────────────────────────────┤
│  [Resume]  [Settings]  [Main Menu]      │
└─────────────────────────────────────────┘
```

---

## Steam Integration

### Inviting Friends (Host)
1. Host is in Prep Scene
2. Open Steam overlay (Shift+Tab)
3. Right-click friend → "Invite to Game"
4. Friend joins directly into Prep Scene
5. Both can press ESC to see lobby

### Requirements
- Steam must be initialized
- `steam_appid.txt` must be present
- Steamworks.NET transport enabled

---

## Architecture

### Connection Flow
```
Server (Host)
    ↓ [Spawns NetworkConnectionTracker]
    ↓ [Players connect]
    ↓ [Updates tracker.ConnectionCount]
    ↓ [SyncVar replicates to all clients]
    ↓
Clients
    ↓ [Receive SyncVar update]
    ↓ [Event fires: OnConnectionCountUpdated]
    ↓ [InGameLobbyPanel refreshes]
    ↓ [UI updates to show correct count]
```

### Kick Flow
```
Host presses Kick button
    ↓ [InGameLobbyPanel.OnKickButtonClicked()]
    ↓ [BarelyMovedNetworkManager.KickPlayer()]
    ↓ [NetworkConnectionToClient.Disconnect()]
    ↓
Client is disconnected
    ↓ [Client returns to main menu]
    ↓ [Host's tracker updates]
    ↓ [All remaining clients update their UI]
```

---

## Troubleshooting

### "Players: 0/4" shown on clients
**Cause**: NetworkConnectionTracker not spawned or not synced
**Fix**: Check console for "Connection tracker spawned" message. Ensure server is active.

### Kick button visible for local player
**Cause**: isLocalPlayer check failing
**Fix**: Ensure player prefab has NetworkIdentity with "Local Player Authority" enabled

### ESC menu not showing
**Cause**: PauseMenuRoot not assigned or Input System not detecting ESC
**Fix**: 
- Check PauseMenuManager has `m_PauseMenuRoot` assigned
- Verify Input System is installed
- Check console for "[PauseMenuManager] Paused" message

### Players don't see each other's names
**Cause**: NetworkPlayerData not synced or Steam names not fetched
**Fix**:
- Ensure player prefab has NetworkPlayerData component
- Check NetworkIdentity is on player prefab
- Verify Steam is initialized (if using Steam)

### Clients stuck in MainMenu after joining
**Cause**: Mirror scene sync not working
**Fix**:
- Ensure NetworkManager has scene sync enabled
- Check host successfully transitioned to prep scene
- Verify both scenes are in Build Settings

---

## Files Summary

### New Files
- ✅ `Assets/Scripts/UI/PauseMenuManager.cs`
- ✅ `Assets/Scripts/UI/InGameLobbyPanel.cs`
- ✅ `Assets/Scripts/Network/NetworkConnectionTracker.cs`
- ✅ `Assets/Scripts/ESC_MENU_LOBBY_SYSTEM.md` (this file)

### Modified Files
- 🔄 `Assets/Scripts/Network/BarelyMovedNetworkManager.cs` (added KickPlayer)
- 🔄 `Assets/Scripts/GameManagement/MainMenuManager.cs` (skip lobby, go to prep)

### Obsolete Files (Can be removed)
- ❌ `Assets/Scripts/CONNECTION_SYNC_SETUP.md` (old system)
- ❌ `Assets/Scripts/SYNC_FIX_OCT18.md` (old fix)

---

## Next Steps

### Recommended Enhancements
1. **Add player ready status** (checkboxes in ESC menu)
2. **Add chat system** (simple text chat in ESC menu)
3. **Add Steam friend list UI** (invite friends from in-game UI)
4. **Add player roles** (assign colors or roles in lobby)
5. **Add auto-kick on AFK** (kick after X minutes of inactivity)

### Integration with Existing Systems
- ✅ Works with `GameStateManager` for scene transitions
- ✅ Works with `NetworkPlayerData` for Steam names/avatars
- ✅ Works with existing prep scene and level scenes
- ✅ Compatible with Steam invites

---

## Quick Reference

| Action | Key/Button | Who Can Do It |
|--------|-----------|---------------|
| Open ESC Menu | ESC | Everyone |
| Resume Game | ESC again or Resume button | Everyone |
| Kick Player | Kick button in lobby panel | Host only |
| Return to Main Menu | Main Menu button in ESC menu | Everyone |
| Invite Friend | Steam Overlay (Shift+Tab) | Host |

---

## Support & Feedback

If you encounter issues:
1. Check console logs for errors
2. Verify all components are assigned in Inspector
3. Test in standalone build (not just editor)
4. Check Steam is initialized (if using Steam features)
5. Ensure both host and client are using same build version

