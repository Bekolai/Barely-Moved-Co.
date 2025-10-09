# Barely Moved Co. - Scripts Documentation

## 🚨 IMPORTANT: Install Mirror First!

**Current compiler errors are expected** - they'll disappear once Mirror is installed.

### Install Mirror Networking:
```
Window → Package Manager → '+' → Add package from git URL
Paste: https://github.com/MirrorNetworking/Mirror.git?path=/Assets/Mirror
```

---

## 📁 Folder Structure

```
Scripts/
├── Network/                    # Networking & Steam integration
│   ├── BarelyMovedNetworkManager.cs
│   └── SteamLobbyManager.cs
│
├── Player/                     # Player control & interaction
│   ├── NetworkPlayerController.cs
│   ├── PlayerInputHandler.cs
│   └── PlayerGrabSystem.cs
│
├── Items/                      # Grabbable objects
│   ├── ItemData.cs            (ScriptableObject)
│   ├── GrabbableItem.cs       (Base class)
│   ├── SinglePlayerItem.cs
│   └── DualPlayerItem.cs
│
├── Interactables/              # World objects
│   └── DeliveryZone.cs
│
├── GameManagement/             # Game flow & scoring
│   └── JobManager.cs
│
├── Camera/                     # Cinemachine integration
│   └── CameraManager.cs
│
├── UI/                         # User interface
│   └── GameHUD.cs
│
└── Steamworks.NET/             # Steam integration (pre-existing)
    └── SteamManager.cs
```

---

## 🎮 Core Systems

### Network Layer (Server Authoritative)

**BarelyMovedNetworkManager**
- Extends Mirror's NetworkManager
- Handles host/client connections
- Manages player spawning
- Integrates with Steam lobbies

**SteamLobbyManager**
- Creates Steam lobbies
- Handles friend invites
- Manages Steam P2P connections
- Automatic join on Steam invite

**Key Design:**
- Host = Server + Client (simulates all physics)
- Clients send input, receive state updates
- All items physics-authoritative on server

---

### Player Systems

**NetworkPlayerController**
- Third-person character controller
- Network-synced movement
- Jump & sprint mechanics
- Client predicts, server validates

**PlayerInputHandler**
- Unity New Input System wrapper
- Only processes local player input
- Exposes clean input properties
- Handles input consumption

**PlayerGrabSystem**
- Detects nearby grabbable items
- Sends grab/drop/throw commands to server
- Updates held item position
- Visual item highlighting

**Controls:**
- **Move:** WASD / Left Stick
- **Look:** Mouse / Right Stick
- **Grab/Drop:** E / Right Trigger
- **Throw:** Left Click / Left Trigger
- **Jump:** Space / A Button
- **Sprint:** Left Shift / Left Stick Click

---

### Item Systems

**ItemData (ScriptableObject)**
- Stores item properties
- Base value & min value
- Damage calculation settings
- Mass & physics properties
- Create via: Right-click → Create → Barely Moved → Item Data

**GrabbableItem (Abstract Base)**
- Network synced transform & state
- Damage tracking on collision
- Grab/release/throw logic
- Server-authoritative physics
- Rigidbody kinematic control

**SinglePlayerItem**
- Can be carried by one player
- Simpler physics
- Boxes, lamps, small furniture

**DualPlayerItem**
- Requires TWO players to carry
- Front & back grab points
- Movement averages player positions
- Sofas, fridges, large furniture
- Can be grabbed by one player (but won't move properly)

---

### Game Management

**JobManager**
- Tracks current job state
- Timer countdown
- Item delivery tracking
- Score calculation
- Job completion/failure detection

**DeliveryZone**
- Trigger zone for item delivery
- Tracks delivered items
- Calculates total value
- Server validates deliveries

---

### Camera System

**CameraManager**
- Singleton manager for Cinemachine
- Sets follow target for local player
- Configurable distance & framing
- Smooth third-person follow

---

### UI System

**GameHUD**
- Timer display
- Item count & progress
- Score display
- Connection status
- Lobby controls (Host, Invite, Leave)

---

## 🔌 Network Architecture

### Client-Server Model

```
HOST (Server + Client)
├── Simulates ALL physics
├── Validates all inputs
├── Authority over all NetworkObjects
└── Sends state updates to clients

CLIENTS
├── Send input to server
├── Receive state updates
├── Predict local player movement
└── Interpolate remote players/items
```

### Data Flow: Grabbing an Item

```
1. Client: Detects nearby item (local raycast)
2. Client: Player presses E
3. Client: Sends CmdGrabItem() to server
4. Server: Validates request
5. Server: Calls item.TryGrab()
6. Server: Updates item SyncVars
7. Server: Sends RpcOnItemGrabbed() to all clients
8. All Clients: Update local item reference
```

### Data Flow: Moving Held Item

```
Every frame while holding:
1. Client: Calculates hold position
2. Client: Sends CmdUpdateHeldItemPosition()
3. Server: Updates item transform
4. NetworkTransform: Syncs to all clients
5. Clients: Interpolate item position
```

---

## 🎯 Best Practices

### Network Commands

**[Command]** - Client → Server
```csharp
[Command]
private void CmdDoSomething(int _value)
{
    // Runs on server only
    // Called from client
}
```

**[ClientRpc]** - Server → All Clients
```csharp
[ClientRpc]
private void RpcNotifyClients(string _message)
{
    // Runs on all clients
    // Called from server
}
```

**[Server]** - Server Only
```csharp
[Server]
public void ServerOnlyMethod()
{
    // Only runs on server
    // Error if called from client
}
```

### SyncVars
```csharp
[SyncVar] private float m_Health;
// Automatically synced to clients
// Only server should modify
```

---

## 🐛 Debugging

All scripts have editor-only debug methods:

**Context Menus** (Right-click component in Inspector):
- NetworkManager: "Debug Connection Info"
- SteamLobbyManager: "Debug Lobby Info"  
- JobManager: "Start Job", "Debug Job Info"
- CameraManager: "Debug Camera Info"

**Gizmos:**
- PlayerGrabSystem: Shows grab range
- GrabbableItem: Shows grab points
- DeliveryZone: Shows trigger zone
- DualPlayerItem: Shows front/back points

**Console Logs:**
All major systems log with prefixes:
- `[BarelyMovedNetworkManager]`
- `[SteamLobbyManager]`
- `[GrabbableItem]`
- etc.

---

## 🔧 Extending the System

### Adding New Item Types

1. Create new class inheriting from `GrabbableItem`
2. Override grab/release logic as needed
3. Add custom behavior in Update/FixedUpdate
4. Create ItemData ScriptableObject
5. Set up prefab with NetworkIdentity

### Adding New Interactables

1. Create script with NetworkBehaviour
2. Add trigger/collision detection
3. Send commands to server for validation
4. Use ClientRpc for visual feedback

### Custom Input Actions

1. Open InputSystem_Actions asset
2. Add new action to "Player" map
3. Add binding (keyboard + gamepad)
4. Access in PlayerInputHandler
5. Process in relevant system

---

## 📊 Performance Considerations

**Network Bandwidth:**
- Items only sync when moving
- SyncVars update only on change
- Transform sync uses compression
- ~100 kbps per player (typical)

**Physics:**
- Server simulates all item physics
- Max ~50-100 physics items recommended
- Use object pooling for many items
- Disable far items with distance checks

**Optimization Tips:**
- Reduce NetworkTransform sync rate for far items
- Use LODs on item models
- Batch static colliders
- Limit particle effects

---

## ✨ Future Enhancements

Ideas for expansion:

**Gameplay:**
- [ ] Job board/level selection
- [ ] Upgrade shop (better gloves, cart, etc.)
- [ ] Obstacles (dogs, broken floors, rain)
- [ ] Cosmetic character customization
- [ ] Leaderboards

**Technical:**
- [ ] Prediction for better client feel
- [ ] Lag compensation
- [ ] Reconnection handling
- [ ] Save/load progress
- [ ] Replay system

**Content:**
- [ ] More item types (fragile glass, heavy safe)
- [ ] Different job locations
- [ ] Weather/time of day
- [ ] Procedural houses
- [ ] Boss levels

---

## 📖 Further Reading

**Mirror Networking:**
- Docs: https://mirror-networking.gitbook.io/
- GitHub: https://github.com/MirrorNetworking/Mirror

**Steamworks.NET:**
- Docs: https://steamworks.github.io/
- GitHub: https://github.com/rlabrecque/Steamworks.NET

**Unity Input System:**
- Docs: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/

**Cinemachine:**
- Docs: https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/

---

Made with ❤️ for Barely Moved Co.

