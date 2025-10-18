# Connection Count Sync Setup Guide

## Overview
The `NetworkConnectionTracker` is a NetworkBehaviour component that properly syncs connection counts from server to all clients. This is necessary because `NetworkManager` cannot use `ClientRpc` or `SyncVar`.

## The Problem
Previously, the connection count wasn't syncing correctly:
- **Host** saw actual server connections (e.g., 2/4)
- **Clients** tried to count spawned NetworkPlayerData objects, but these don't exist in MainMenu (e.g., 1/4)
- This led to mismatched player counts in the lobby UI

## The Solution
Created a dedicated `NetworkConnectionTracker` NetworkBehaviour that:
1. Uses a `SyncVar` to sync connection count to all clients
2. Gets spawned automatically by the server
3. Fires events when connection count changes

## Setup Instructions

### Automatic Setup (Recommended)
The NetworkConnectionTracker will be created automatically at runtime if no prefab is assigned. No manual setup required!

### Manual Setup (Optional - for customization)
If you want to create a prefab:

1. Create an empty GameObject in your scene
2. Add `NetworkIdentity` component
3. Add `NetworkConnectionTracker` component
4. Create a prefab from this GameObject
5. Assign the prefab to `BarelyMovedNetworkManager` > "Connection Tracker Prefab" field

## How It Works

### Server Side
1. When server starts, `BarelyMovedNetworkManager.OnStartServer()` calls `SpawnConnectionTracker()`
2. When players connect/disconnect, `UpdateConnectionTracker()` is called
3. The tracker's `SyncVar` automatically replicates to all clients

### Client Side
1. Clients receive the spawned tracker object
2. The `SyncVar` hook fires when the value changes
3. `OnConnectionCountUpdated` event notifies listeners (like `LobbyPlayerUI`)
4. UI updates to show correct player count

## Code Changes Summary

### BarelyMovedNetworkManager
- **Removed**: `RpcUpdateConnectionCount` (ClientRpc methods not allowed in NetworkManager)
- **Removed**: `m_ClientConnectionCount` field
- **Removed**: `OnConnectionCountUpdated` event (moved to tracker)
- **Added**: `SpawnConnectionTracker()` - creates tracker on server start
- **Added**: `UpdateConnectionTracker()` - updates count on connect/disconnect
- **Updated**: `ConnectedPlayerCount` property now uses tracker for clients

### LobbyPlayerUI
- **Changed**: Subscribe to `NetworkConnectionTracker.OnConnectionCountUpdated` instead of `BarelyMovedNetworkManager.OnConnectionCountUpdated`

## Events

Subscribe to connection count updates:

```csharp
private void Start()
{
    NetworkConnectionTracker.OnConnectionCountUpdated += OnConnectionCountChanged;
}

private void OnConnectionCountChanged(int newCount)
{
    Debug.Log($"Connection count: {newCount}");
}

private void OnDestroy()
{
    NetworkConnectionTracker.OnConnectionCountUpdated -= OnConnectionCountChanged;
}
```

## Testing

### Expected Behavior
1. **In MainMenu (no spawned players)**:
   - Host creates lobby → Shows "Players: 1/4"
   - Client joins → Both show "Players: 2/4"
   - Another client joins → All show "Players: 3/4"

2. **Player Names**:
   - Host sees: "🏆 Host" and other players
   - Clients see: "✓ You" and other players
   - All counts match across all clients

3. **When Players Disconnect**:
   - Count decrements immediately on all clients
   - UI updates automatically via event

## Troubleshooting

### "Client shows 0/4"
- The tracker may not have spawned yet
- Check console for "Connection tracker spawned" message
- Ensure NetworkServer is active

### "Client shows wrong count"
- Check that LobbyPlayerUI is subscribed to `NetworkConnectionTracker.OnConnectionCountUpdated`
- Verify tracker instance exists: `NetworkConnectionTracker.Instance != null`

### "Mirror.Weaver error about ClientRpc"
- This error is fixed - ensure you're using the updated `BarelyMovedNetworkManager` without RPC methods

## Related Files
- `NetworkConnectionTracker.cs` - The tracker component
- `BarelyMovedNetworkManager.cs` - Spawns and updates the tracker
- `LobbyPlayerUI.cs` - Subscribes to tracker events

