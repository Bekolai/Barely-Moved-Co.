# Network Architecture - Connection Tracking

## Component Hierarchy

```
BarelyMovedNetworkManager (NetworkManager)
    ├─ Manages server/client lifecycle
    ├─ Spawns NetworkConnectionTracker on server start
    └─ Updates tracker on player connect/disconnect

NetworkConnectionTracker (NetworkBehaviour)
    ├─ SyncVar: m_ConnectionCount
    ├─ Singleton Instance
    └─ Event: OnConnectionCountUpdated

LobbyPlayerUI (MonoBehaviour)
    ├─ Subscribes to NetworkConnectionTracker.OnConnectionCountUpdated
    └─ Updates UI when connection count changes
```

## Data Flow Diagram

### Server → Client Sync

```
┌──────────────────────────────────────────────────────────────┐
│                         SERVER                                │
│                                                               │
│  BarelyMovedNetworkManager                                    │
│    │                                                          │
│    ├─ OnStartServer()                                        │
│    │   └─> SpawnConnectionTracker()                          │
│    │       └─> NetworkServer.Spawn(tracker)  ─────┐          │
│    │                                              │          │
│    ├─ OnServerAddPlayer(conn)                    │          │
│    │   └─> UpdateConnectionTracker()             │          │
│    │       └─> tracker.UpdateConnectionCount(n)  │          │
│    │           └─> m_ConnectionCount = n ───────┼───┐       │
│    │                                             │   │       │
│    └─ OnServerDisconnect(conn)                  │   │       │
│        └─> UpdateConnectionTracker()            │   │       │
│            └─> tracker.UpdateConnectionCount(n) │   │       │
│                └─> m_ConnectionCount = n ───────┼───┤       │
│                                                  │   │       │
└──────────────────────────────────────────────────┼───┼───────┘
                                                   │   │
                        Mirror Network Replication │   │
                        (GameObject Spawn)         │   │ (SyncVar)
                                                   │   │
┌──────────────────────────────────────────────────┼───┼───────┐
│                        CLIENT                    │   │       │
│                                                  ▼   ▼       │
│  NetworkConnectionTracker                        │   │       │
│    │                                             │   │       │
│    ├─ Instance (singleton) ◄─────────────────────┘   │       │
│    │                                                  │       │
│    └─ OnConnectionCountChanged(old, new) ◄───────────┘       │
│        └─> OnConnectionCountUpdated?.Invoke(new)             │
│            └───┐                                             │
│                │                                             │
│  LobbyPlayerUI │                                             │
│    │           │                                             │
│    └─ OnConnectionCountUpdated(count) ◄──────────────────────┘
│        └─> UpdatePlayerList()                                │
│            └─> m_PlayerCountText.text = $"Players: {count}/4"│
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

## Sequence Diagram - Player Joins Lobby

```
Host                NetworkManager          Tracker             Client1             LobbyUI
 │                         │                   │                   │                   │
 │─StartHost()───────────>│                   │                   │                   │
 │                         │                   │                   │                   │
 │                         │─SpawnTracker()───>│                   │                   │
 │                         │                   │─[Spawned]────────>│                   │
 │                         │                   │                   │                   │
 │                         │─UpdateTracker(1)─>│                   │                   │
 │                         │                   │─[SyncVar=1]──────>│                   │
 │                         │                   │                   │─Event(1)─────────>│
 │                         │                   │                   │                   │
 │                         │                   │                   │<──"Players:1/4"───│
 │                         │                   │                   │                   │
 │<─────────────────────────────[Host sees 1/4]────────────────────────────────────────│
 │                         │                   │                   │                   │
 │                         │                   │                   │                   │
[Client1 Joins]            │                   │                   │                   │
 │<────StartClient()──────────────────────────────────────────────│                   │
 │                         │                   │                   │                   │
 │                         │◄─Connected────────────────────────────│                   │
 │                         │                   │                   │                   │
 │                         │─OnAddPlayer()───> │                   │                   │
 │                         │─UpdateTracker(2)─>│                   │                   │
 │                         │                   │─[SyncVar=2]──────>│                   │
 │                         │                   │                   │─Event(2)─────────>│
 │                         │                   │                   │                   │
 │                         │                   │                   │<──"Players:2/4"───│
 │                         │                   │                   │                   │
 │                         │                   │─[SyncVar=2]──────>│ (Host also gets)  │
 │                         │                   │                   │─Event(2)─────────>│
 │                         │                   │                   │                   │
 │                         │                   │                   │<──"Players:2/4"───│
 │                         │                   │                   │                   │
 │<─────────────────────────────[Both see 2/4]─────────────────────────────────────────│
```

## Why This Architecture?

### ❌ Why Not Use NetworkManager Directly?
```csharp
// This DOESN'T WORK:
public class BarelyMovedNetworkManager : NetworkManager
{
    [ClientRpc] // ❌ ERROR: ClientRpc must be in NetworkBehaviour
    private void RpcUpdateConnectionCount(int count) { }
}
```
**Problem**: `NetworkManager` is not a `NetworkBehaviour`, so it can't use:
- `[ClientRpc]` methods
- `[SyncVar]` fields
- `[Command]` methods

### ✅ Why Use a Separate NetworkBehaviour?
```csharp
// This WORKS:
public class NetworkConnectionTracker : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnConnectionCountChanged))] // ✅ Valid!
    private int m_ConnectionCount = 0;
}
```
**Solution**: Create a dedicated `NetworkBehaviour` that:
- Can use Mirror's networking attributes
- Gets spawned by NetworkManager
- Automatically syncs to all clients
- Fires events for reactive updates

## Benefits of This Pattern

1. **Separation of Concerns**
   - NetworkManager handles lifecycle
   - Tracker handles data sync
   - UI reacts to events

2. **Automatic Sync**
   - SyncVar handles all replication
   - No manual ClientRpc calls needed
   - Works for late joiners automatically

3. **Event-Driven Updates**
   - UI updates reactively
   - No polling required
   - Clean subscription pattern

4. **Scalable**
   - Easy to add more synced data
   - Multiple systems can subscribe
   - Follows Unity best practices

## Alternative Patterns (Not Used)

### ❌ Manual Message Sending
```csharp
// Could work but more complex:
NetworkClient.connection.Send(new ConnectionCountMessage { count = n });
```
**Drawback**: Requires custom message handlers, more boilerplate

### ❌ Polling Every Frame
```csharp
// Could work but inefficient:
void Update() {
    int count = NetworkServer.connections.Count;
    // No way to send to clients...
}
```
**Drawback**: Inefficient, still can't send to clients from NetworkManager

### ✅ NetworkBehaviour with SyncVar (Chosen)
**Advantages**:
- Built-in Mirror feature
- Automatic replication
- Efficient (only sends on change)
- Event hooks included
- Clean code

