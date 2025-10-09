# 🚀 Quick Start - Get Playing in 10 Minutes!

## Step 1: Install Mirror (2 min)
```
1. Open Unity
2. Window → Package Manager
3. Click '+' → Add package from git URL
4. Paste: https://github.com/MirrorNetworking/Mirror.git?path=/Assets/Mirror
5. Wait for import
```

## Step 2: Create Layers (1 min)
```
1. Inspector → Layers → Edit Layers
2. Add layer "Grabbable"
3. Add layer "Ground"
```

## Step 3: Create Test Scene (3 min)

### 3.1 - Network Setup
1. Create Empty: `NetworkManager`
   - Add: `BarelyMovedNetworkManager`
   - Add: `SteamLobbyManager`

2. Create Empty: `SteamManager`
   - Add: `SteamManager` (from Steamworks.NET)

### 3.2 - Camera
1. Create Empty: `CameraManager`
   - Add child: `CM vcam1` (Cinemachine Virtual Camera)
   - Add: `CameraManager` script to parent

### 3.3 - Ground
1. Create 3D Plane (scale 10,1,10)
2. Layer: Ground

### 3.4 - Player Prefab
1. Create Empty: `Player`
   - Add: Capsule (child, visual)
   - Add: Empty child `GroundCheck` (at feet)
   - Add: Empty child `CameraTarget` (at head)
   - Add: Empty child `GrabOrigin` (in front)

2. Add Components:
   - CharacterController
   - NetworkIdentity (✓ Local Player Authority)
   - NetworkTransform
   - PlayerInput (assign InputSystem_Actions)
   - NetworkPlayerController
   - PlayerInputHandler
   - PlayerGrabSystem

3. Configure PlayerGrabSystem:
   - Grab Origin: drag GrabOrigin
   - Grabbable Layer: select Grabbable

4. Configure NetworkPlayerController:
   - Ground Check: drag GroundCheck
   - Camera Target: drag CameraTarget
   - Ground Mask: select Ground

5. Save as Prefab → Delete from scene

### 3.5 - Network Manager Setup
1. Select NetworkManager
   - Player Prefab: drag Player prefab
   - Create 4 empty GameObjects as spawn points
   - Drag to spawn points array

## Step 4: Create Test Item (2 min)

1. Create Cube
2. Add Components:
   - Rigidbody
   - NetworkIdentity
   - NetworkTransform
   - SinglePlayerItem

3. Create ItemData:
   - Right-click → Create → Barely Moved → Item Data
   - Set base value: 100
   - Drag to SinglePlayerItem

4. Layer: Grabbable
5. Save as Prefab

6. Spawn 3-4 in scene

## Step 5: Create Delivery Zone (1 min)

1. Create Cube (scale 3,2,3)
2. Add Components:
   - BoxCollider (✓ Is Trigger)
   - NetworkIdentity
   - DeliveryZone

3. Material: make it semi-transparent green

## Step 6: Create Job Manager (1 min)

1. Create Empty: `JobManager`
2. Add: `JobManager` script
3. Delivery Zone: drag DeliveryZone
4. Required Items: drag test items from scene

## Step 7: TEST! (30 sec)

### Local Test:
1. Press Play
2. Player spawns
3. Press E near items to grab
4. Left click to throw
5. Drop in delivery zone

### Network Test:
1. Build game (File → Build)
2. Run build → creates lobby
3. Press Play in editor
4. Should auto-connect!

---

## 🎉 You're Done!

**Next Steps:**
- Read `SETUP_GUIDE.md` for detailed info
- Read `README.md` for architecture details
- Create proper 3D models
- Design more items
- Build awesome levels!

---

## ❓ Issues?

**Nothing compiles:**
→ Install Mirror (Step 1)

**Player falls through floor:**
→ Check layers (Step 2)
→ Add Ground layer to physics layers

**Can't grab items:**
→ Check Grabbable layer is set
→ Check GrabRange in PlayerGrabSystem

**Steam errors:**
→ Create `steam_appid.txt` in project root
→ Add text: `480`
→ Make sure Steam is running

---

**Need Help?** Check the console - all errors are logged with helpful messages!

