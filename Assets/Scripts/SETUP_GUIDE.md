# Barely Moved Co. - Setup Guide

## 📋 Overview
This is a co-op moving simulator built with **Unity 6**, **Mirror Networking**, and **Steamworks.NET**.

---

## 🔧 Required Packages

### Already Installed
✅ Unity Input System (1.14.2)  
✅ Cinemachine (3.1.4)  
✅ Steamworks.NET (in Assets folder)

### Need to Install - Mirror Networking

**Option 1: Via Asset Store**
1. Open Package Manager
2. Search for "Mirror Networking"
3. Download & Import

**Option 2: Via Git URL**
1. Window → Package Manager
2. Click '+' → Add package from git URL
3. Enter: `https://github.com/MirrorNetworking/Mirror.git?path=/Assets/Mirror`

**Option 3: Download Release**
1. Visit: https://github.com/MirrorNetworking/Mirror/releases
2. Download latest `.unitypackage`
3. Import into project

---

## 🎮 Scene Setup

### 1. Create Main Game Scene

**Network Manager Setup:**
1. Create empty GameObject: `NetworkManager`
2. Add component: `BarelyMovedNetworkManager`
3. Add component: `SteamLobbyManager`
4. Assign Player Prefab (see below)
5. Set Max Players: 4
6. Create spawn points and assign to array

**Steam Manager:**
1. Create empty GameObject: `SteamManager`
2. Add component: `SteamManager` (from Steamworks.NET)
3. Create `steam_appid.txt` in project root with ID `480` for testing

**Camera Setup:**
1. Create empty GameObject: `CameraManager`
2. Add `CinemachineVirtualCamera` as child
3. Add component: `CameraManager` script
4. Set camera distance and settings

**Job Management:**
1. Create empty GameObject: `JobManager`
2. Add component: `JobManager`
3. Create delivery zone (see below)

---

### 2. Create Player Prefab

**Hierarchy:**
```
PlayerPrefab
├── Model (your 3D model)
├── GroundCheck (empty transform at feet)
├── CameraTarget (empty transform at head height)
└── GrabOrigin (empty transform in front of player)
```

**Components on Root:**
- NetworkIdentity
- NetworkTransform
- CharacterController
- PlayerInput (with InputSystem_Actions asset assigned)
- NetworkPlayerController
- PlayerInputHandler
- PlayerGrabSystem

**Settings:**
- CharacterController: Set radius, height, center
- PlayerGrabSystem: Assign GrabOrigin, set grab range, create GrabbableLayer
- NetworkIdentity: Check "Local Player Authority"

**Save as Prefab** in `Assets/Prefabs/`

---

### 3. Create Delivery Zone

**Hierarchy:**
```
DeliveryZone
└── TriggerVolume (with BoxCollider)
```

**Components:**
- NetworkIdentity
- DeliveryZone script
- BoxCollider (isTrigger = true)

**Settings:**
- Set zone size
- Assign to JobManager

---

### 4. Create Grabbable Items

#### Single-Player Item (Box, Lamp, etc.)
```
Item_Box
├── Model
└── GrabPoints (child with grab point transforms)
```

**Components:**
- NetworkIdentity
- NetworkTransform
- Rigidbody
- Collider
- SinglePlayerItem script

**Settings:**
- Create ItemData ScriptableObject
- Assign to item
- Set grab points
- Layer: Set to "Grabbable"

#### Dual-Player Item (Couch, Fridge, etc.)
```
Item_Couch
├── Model
├── GrabPoint_Front
└── GrabPoint_Back
```

**Components:**
- NetworkIdentity
- NetworkTransform
- Rigidbody
- Collider
- DualPlayerItem script

**Settings:**
- Create ItemData ScriptableObject
- Assign front/back grab points
- Set mass higher
- Layer: Set to "Grabbable"

---

## 🎯 Layer Setup

**Create these layers:**
1. `Grabbable` - for all items
2. `Ground` - for floors/terrain

**Physics Settings:**
- Edit → Project Settings → Physics
- Ensure Grabbable can collide with Ground

---

## 📱 Steam Integration

### Development Testing
1. Create `steam_appid.txt` in project root
2. Add `480` (Spacewar test AppID)
3. Run Steam client in background

### Production
1. Get your Steam AppID from Steamworks
2. Replace `AppId_t.Invalid` in SteamManager.cs
3. Remove `steam_appid.txt`
4. Build and upload to Steam

---

## 🌐 Network Testing

### Local Testing (Single Machine)
1. Build the game
2. Run the build → Click "Host"
3. Run in Editor → Auto-connects to localhost

### Steam P2P Testing
1. Both players run Steam
2. Host clicks "Create Lobby"
3. Host clicks "Invite Friends"
4. Friend joins through Steam overlay

---

## 🎨 UI Setup

### Create Game HUD Canvas
1. Create UI Canvas
2. Add TextMeshPro elements for:
   - Timer
   - Item Count
   - Progress Bar
   - Connection Status
3. Add `GameHUD` component
4. Assign UI references

### Create Lobby UI
1. Create buttons: Host, Invite, Leave
2. Link to GameHUD methods

---

## ✅ Testing Checklist

**Single Player:**
- [ ] Player spawns correctly
- [ ] Camera follows player
- [ ] Movement works (WASD, controller)
- [ ] Can grab single-player items (E)
- [ ] Can throw items (Left Click)
- [ ] Items take damage on collision
- [ ] Delivery zone accepts items
- [ ] Timer counts down

**Multiplayer (2 Players):**
- [ ] Lobby creation works
- [ ] Steam invites work
- [ ] Both players see each other
- [ ] Items sync correctly
- [ ] Dual-player items require 2 people
- [ ] Movement is smooth (no jitter)
- [ ] Both see same score

---

## 🐛 Common Issues

### "Mirror NetworkManager not found"
→ Install Mirror package (see above)

### "SteamAPI_Init() failed"
→ Ensure Steam is running, check steam_appid.txt exists

### Items falling through floor
→ Check Ground layer, ensure continuous collision detection on Rigidbody

### Players can't grab items
→ Verify layers, check GrabRange, ensure item has NetworkIdentity

### Jittery movement over network
→ Check NetworkTransform settings, ensure host is authoritative

### Input not working
→ Ensure PlayerInput component has InputSystem_Actions assigned
→ Check input action map is set to "Player"

---

## 📚 Script Reference

### Network
- `BarelyMovedNetworkManager` - Main network manager
- `SteamLobbyManager` - Steam lobby integration

### Player
- `NetworkPlayerController` - Movement & physics
- `PlayerInputHandler` - Input processing
- `PlayerGrabSystem` - Item interaction

### Items
- `GrabbableItem` - Base item class
- `SinglePlayerItem` - 1-player items
- `DualPlayerItem` - 2-player items
- `ItemData` - ScriptableObject for item stats

### Game
- `JobManager` - Job/level management
- `DeliveryZone` - Delivery zone trigger
- `GameHUD` - UI controller

### Camera
- `CameraManager` - Cinemachine integration

---

## 🚀 Next Steps

1. **Create your first level:**
   - Add terrain/floor
   - Place items
   - Set up delivery zone
   - Test locally

2. **Design item variety:**
   - Create ItemData assets
   - Balance mass/fragility
   - Set appropriate values

3. **Implement progression:**
   - Job selection screen
   - Upgrade system
   - Cosmetics

4. **Polish:**
   - VFX for damage/delivery
   - SFX for all actions
   - Animations for characters
   - Better UI/UX

---

## 📞 Support

Check Unity console for detailed error messages.
All scripts have debug context menus - right-click components in inspector.

Good luck with Barely Moved Co.! 🚚📦

