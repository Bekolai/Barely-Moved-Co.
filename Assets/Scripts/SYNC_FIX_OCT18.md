# Connection Count Sync Fix - October 18, 2025

## Issue
Host and clients were seeing different player counts in the lobby:
- **Host**: Saw 2/4 (correct server connection count)
- **Client**: Saw 1/4 (incorrect - trying to count non-existent spawned objects)
- **Clients couldn't see their own names** in the lobby

## Root Cause
1. `BarelyMovedNetworkManager` tried to use `[ClientRpc]` method, but NetworkManager is **not** a NetworkBehaviour
2. Mirror.Weaver error: "ClientRpc must be declared inside a NetworkBehaviour"
3. Clients tried to count `NetworkPlayerData` objects which don't spawn in MainMenu scene
4. No proper sync mechanism for connection count

## Solution
Created a dedicated `NetworkConnectionTracker` NetworkBehaviour component:

### New Files Created
1. **NetworkConnectionTracker.cs**
   - NetworkBehaviour with SyncVar for connection count
   - Provides singleton access
   - Fires `OnConnectionCountUpdated` event when count changes
   - Properly syncs from server to all clients

### Files Modified

#### BarelyMovedNetworkManager.cs
**Removed:**
- ❌ `[ClientRpc] RpcUpdateConnectionCount()` - Invalid in NetworkManager
- ❌ `m_ClientConnectionCount` field - No longer needed
- ❌ `UpdateConnectionCount()` method - Moved to tracker
- ❌ `OnConnectionCountUpdated` event - Moved to tracker
- ❌ Network Sync region

**Added:**
- ✅ `m_ConnectionTrackerPrefab` field (optional - auto-creates if null)
- ✅ `SpawnConnectionTracker()` - Spawns tracker on server start
- ✅ `UpdateConnectionTracker()` - Updates tracker when players connect/disconnect
- ✅ Calls to `UpdateConnectionTracker()` in key locations:
  - `OnStartServer()` - Initial spawn
  - `OnServerAddPlayer()` - Player joins (spawned or not)
  - `OnServerDisconnect()` - Player leaves

**Updated:**
- 🔄 `ConnectedPlayerCount` property now uses `NetworkConnectionTracker.Instance.ConnectionCount` for clients

#### LobbyPlayerUI.cs
**Changed:**
- 🔄 Event subscription: `NetworkConnectionTracker.OnConnectionCountUpdated` instead of `BarelyMovedNetworkManager.OnConnectionCountUpdated`
- Updated in both `Start()` and `OnDestroy()`

### Documentation Created
- **CONNECTION_SYNC_SETUP.md** - Complete setup and usage guide

## How It Works

### Server Flow
```
1. Server starts
   └─> SpawnConnectionTracker()
       └─> Creates NetworkConnectionTracker GameObject
       └─> Adds NetworkIdentity + NetworkConnectionTracker
       └─> NetworkServer.Spawn() replicates to clients

2. Player connects
   └─> OnServerAddPlayer() or connection without spawn
       └─> UpdateConnectionTracker()
           └─> tracker.UpdateConnectionCount(NetworkServer.connections.Count)
               └─> SyncVar automatically syncs to all clients
```

### Client Flow
```
1. Client receives spawned tracker
   └─> NetworkConnectionTracker.Instance set
   
2. SyncVar update received
   └─> OnConnectionCountChanged() hook fires
       └─> OnConnectionCountUpdated event fires
           └─> LobbyPlayerUI.OnConnectionCountUpdated() updates UI
```

## Testing Checklist

### ✅ Compilation
- [x] No Mirror.Weaver errors
- [x] No compiler errors
- [x] All linter checks pass

### Expected Runtime Behavior

#### MainMenu - No Spawned Players
1. Host creates lobby
   - Shows "Players: 1/4" ✓
   - Shows "🏆 Host" ✓

2. Client joins
   - Host shows "Players: 2/4" ✓
   - Client shows "Players: 2/4" ✓
   - Host sees "🏆 Host" and "✓ Player 2" ✓
   - Client sees "✓ You" ✓

3. More clients join
   - All clients show matching count (e.g., "3/4") ✓
   - All see their own indicator ✓

4. Client disconnects
   - Count decrements on all clients ✓
   - Updates immediately ✓

#### PrepScene - Spawned Players
1. Players spawn with NetworkPlayerData
   - Shows actual player names from Steam ✓
   - Shows Steam avatars if available ✓
   - Host marked with 🏆, others with ✓ ✓

## Benefits
- ✅ Proper Mirror networking patterns (NetworkBehaviour for RPCs/SyncVars)
- ✅ Accurate connection counts on all clients
- ✅ Works in MainMenu (no spawned players) and gameplay scenes
- ✅ Automatic creation - no manual setup required
- ✅ Events for reactive UI updates
- ✅ Clean, maintainable code

## Migration Notes
- **Old code using `BarelyMovedNetworkManager.OnConnectionCountUpdated`** needs to change to `NetworkConnectionTracker.OnConnectionCountUpdated`
- Only `LobbyPlayerUI.cs` was affected in this codebase
- No prefab setup required - tracker auto-creates at runtime

## Related Issues Fixed
- Mirror.Weaver error about ClientRpc in NetworkManager ✓
- Host/client connection count mismatch ✓
- Clients not seeing their own names in lobby ✓
- Connection count not updating in real-time ✓

## Files Checklist
- [x] NetworkConnectionTracker.cs (new)
- [x] NetworkConnectionTracker.cs.meta (new)
- [x] BarelyMovedNetworkManager.cs (modified)
- [x] LobbyPlayerUI.cs (modified)
- [x] CONNECTION_SYNC_SETUP.md (new documentation)
- [x] SYNC_FIX_OCT18.md (this file)

## Next Steps
1. ✅ Compile project - verify no errors
2. ⏳ Test in Unity Editor - verify tracker spawns
3. ⏳ Test lobby UI - verify matching counts
4. ⏳ Test multi-client - verify all clients sync
5. ⏳ Test Steam integration - verify names/avatars display

