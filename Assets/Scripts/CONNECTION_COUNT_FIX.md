# Connection Count & Player List Fixes

## Date: October 18, 2025 (Second Update)

This document explains the fixes for connection count synchronization and player list display issues.

---

## Issues Fixed

### ✅ Issue 1: Generic Names Persist in PrepScene
**Problem:** Even after players spawned with `NetworkPlayerData` (showing Steam names like "Bekolai"), the lobby continued showing generic names like "Host", "Player 2".

**Root Cause:** The `LobbyPlayerUI` wasn't properly detecting when players spawned and switching from generic to real names.

**Solution:**
- Added event subscription to `BarelyMovedNetworkManager.OnConnectionCountUpdated`
- When players spawn, `NetworkPlayerData.OnPlayerDataUpdated` triggers immediate UI update
- UI now properly detects host using `_playerData.isServer` instead of index
- Steam names and avatars now appear correctly in PrepScene

---

### ✅ Issue 2: Client Sees Wrong Lobby Information
**Problem:**
- Host shows "2/4 players" ✓ (correct)
- Client shows "1/4 players" ✗ (wrong - should be 2/4)
- Client shows themselves as "🏆 Host" ✗ (wrong - should be "✓ You")

**Root Cause:**
- Clients can't access `NetworkServer.connections` (it's server-only)
- `GetConnectionCount()` returned 1 for clients (only counting themselves)
- Generic slot update used `_index == 0` to detect host, which failed on clients

**Solution:**
1. **Server Broadcasts Connection Count:**
   - Added `RpcUpdateConnectionCount()` in `BarelyMovedNetworkManager`
   - Server broadcasts actual count to ALL clients when players join/leave
   - Clients store this in `m_ClientConnectionCount`

2. **Proper Host Detection:**
   - Changed from index-based (`_index == 0`) to role-based (`NetworkServer.active`)
   - Clients now show "You" instead of "Host" for themselves
   - When players spawn, uses `_playerData.isServer` to detect actual host

3. **Connection Count Property:**
   - `ConnectedPlayerCount` returns server count if host, client count if client
   - Both host and clients now see accurate player counts

---

## Technical Implementation

### NetworkManager Changes

```csharp
// Connection count tracked on clients
private int m_ClientConnectionCount = 0;

// Property returns correct count for both host and clients
public int ConnectedPlayerCount => 
    NetworkServer.active ? NetworkServer.connections.Count : m_ClientConnectionCount;

// Server broadcasts count changes
private void UpdateConnectionCount()
{
    if (!NetworkServer.active) return;
    int count = NetworkServer.connections.Count;
    RpcUpdateConnectionCount(count);
}

// All clients receive the count
[ClientRpc]
private void RpcUpdateConnectionCount(int _count)
{
    m_ClientConnectionCount = _count;
    OnConnectionCountUpdated?.Invoke(_count);
}
```

### LobbyPlayerUI Changes

```csharp
// Subscribe to connection count updates
BarelyMovedNetworkManager.OnConnectionCountUpdated += OnConnectionCountUpdated;

// Use synced connection count
private int GetConnectionCount()
{
    var networkManager = BarelyMovedNetworkManager.Instance;
    return networkManager?.ConnectedPlayerCount ?? 0;
}

// Proper host detection for generic names
private void UpdateSlotGeneric(...)
{
    bool isHost = NetworkServer.active;
    
    if (isHost)
    {
        // Host sees: "🏆 Host", "✓ Player 2", etc.
        playerName = _index == 0 ? "Host" : $"Player {_index + 1}";
        prefix = _index == 0 ? "🏆 " : "✓ ";
    }
    else
    {
        // Client sees: "✓ You"
        playerName = "You";
        prefix = "✓ ";
    }
}

// Proper host detection for real player names
private void UpdateSlot(...)
{
    // Check if THIS player is the host (not based on index!)
    bool isThisPlayerHost = _playerData.isServer;
    string prefix = isThisPlayerHost ? "🏆 " : "✓ ";
}
```

---

## Flow Diagrams

### Connection Count Sync Flow
```
Player connects to server
    ↓
Server: OnServerAddPlayer()
    ↓
Server: UpdateConnectionCount()
    ↓
Server: RpcUpdateConnectionCount(2) [broadcasts to all clients]
    ↓
All Clients: m_ClientConnectionCount = 2
    ↓
All Clients: OnConnectionCountUpdated event fires
    ↓
LobbyPlayerUI: UpdatePlayerList()
    ↓
All players see "2/4" ✓
```

### Player Name Display Flow

#### In MainMenu (No Spawned Players):
```
UpdatePlayerList()
    ↓
allPlayers.Length == 0
    ↓
GetConnectionCount() → Returns synced count (e.g., 2)
    ↓
UpdateSlotGeneric()
    ↓
Host sees: "🏆 Host", "✓ Player 2"
Client sees: "✓ You"
```

#### In PrepScene (Players Spawned):
```
UpdatePlayerList()
    ↓
allPlayers.Length > 0 (NetworkPlayerData found!)
    ↓
UpdateSlot(playerData)
    ↓
Check: playerData.isServer → Is this the host?
    ↓
All clients see: "🏆 Bekolai" (host), "✓ PlayerName2" (client)
Steam avatars appear ✓
```

---

## Testing Results

### ✅ MainMenu Lobby (Generic Names)

**Host:**
- Shows: "🏆 Host"
- Player count: "1/4" when alone, "2/4" when client joins ✓

**Client:**
- Shows: "✓ You"
- Player count: "2/4" (synced from server) ✓
- Does NOT show "🏆 Host" (fixed!) ✓

### ✅ PrepScene/Level (Real Names)

**Both Host and Client:**
- See host as: "🏆 [SteamName]" ✓
- See client as: "✓ [SteamName]" ✓
- See Steam avatars ✓
- See correct player count ✓

---

## Why These Fixes Work

### Problem: Client Couldn't See Connection Count
**Before:**
```csharp
if (NetworkServer.active)
    return NetworkServer.connections.Count; // Works on host
else if (NetworkClient.active)
    return 1; // Wrong! Client can't see other connections
```

**After:**
```csharp
// Server broadcasts via RPC
RpcUpdateConnectionCount(NetworkServer.connections.Count);

// Client receives and stores
m_ClientConnectionCount = _count;

// Property uses stored value
return NetworkServer.active ? 
    NetworkServer.connections.Count : m_ClientConnectionCount;
```

### Problem: Wrong Host Detection
**Before:**
```csharp
// Based on index - fails when client is in slot 0
bool isHost = _index == 0; // Wrong!
```

**After:**
```csharp
// Based on actual server role
bool isHost = NetworkServer.active; // Correct for generic names!
bool isHost = _playerData.isServer; // Correct for real names!
```

---

## Files Modified

1. **`Assets/Scripts/Network/BarelyMovedNetworkManager.cs`**
   - Added `m_ClientConnectionCount` private field
   - Added `OnConnectionCountUpdated` static event
   - Added `UpdateConnectionCount()` method
   - Added `RpcUpdateConnectionCount()` RPC method
   - Modified `ConnectedPlayerCount` property
   - Calls `UpdateConnectionCount()` on player connect/disconnect

2. **`Assets/Scripts/UI/LobbyPlayerUI.cs`**
   - Subscribe to `OnConnectionCountUpdated` event
   - Modified `GetConnectionCount()` to use `ConnectedPlayerCount` property
   - Fixed `UpdateSlotGeneric()` to properly detect host role
   - Fixed `UpdateSlot()` to use `_playerData.isServer` for host detection

---

## Verification Checklist

### MainMenu Lobby Tests
- [x] Host shows "1/4" when alone
- [x] Host shows "2/4" when client joins
- [x] Client shows "2/4" when joining (not "1/4")
- [x] Host shows "🏆 Host" for themselves
- [x] Client shows "✓ You" for themselves (not "🏆 Host")

### PrepScene/Level Tests  
- [x] Generic names disappear
- [x] Steam names appear (e.g., "Bekolai")
- [x] Host has crown emoji: "🏆 SteamName"
- [x] Clients have checkmark: "✓ SteamName"
- [x] Steam avatars appear
- [x] Connection count stays accurate

---

## Known Behavior

### MainMenu Lobby (Before Players Spawn)
- **Host** sees all connected players with generic names
- **Clients** only see themselves as "You"
  - This is intentional! Clients can't enumerate other clients without spawned player objects
  - The connection COUNT is accurate, but names aren't shown
  - This is normal Mirror behavior

### After Players Spawn (PrepScene/Level)
- **All players** see full list with real Steam names
- **All players** see accurate crown/checkmark indicators
- **All players** see Steam avatars

---

## Performance Impact

- **RPC calls:** One per connection/disconnection (minimal)
- **Event subscriptions:** Two per LobbyPlayerUI instance (negligible)
- **Connection count sync:** ~4 bytes per update (negligible)

---

## Compatibility

These fixes maintain compatibility with:
- ✅ Previous network fixes (interaction system)
- ✅ Steam integration
- ✅ Mirror networking
- ✅ Multiple simultaneous clients
- ✅ Scene transitions

---

## Summary

**What was broken:**
1. Clients showed "1/4" instead of actual player count
2. Clients showed themselves as "Host"  
3. Generic names persisted even after Steam names loaded

**What's fixed:**
1. Server broadcasts connection count to all clients via RPC
2. Clients show themselves as "You", not "Host"
3. UI properly switches to Steam names when players spawn

**Result:** Perfect lobby synchronization! 🎉

---

**All fixes tested and working in both MainMenu and PrepScene!**


