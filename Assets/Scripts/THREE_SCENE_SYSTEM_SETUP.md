# Three Scene System Setup Guide

## Overview
This guide explains the new 3-scene architecture for Barely Moved Co., including Main Menu, Prep Scene, and Level Scene.

## Architecture

### Scene Flow
```
Main Menu → Prep Scene (Hub) → Level Scene (Gameplay) → Prep Scene (Results) → ...
```

### Scene Descriptions

#### 1. Main Menu Scene (`MainMenu.unity`)
- **Purpose**: Player lobby, network setup, and game settings
- **Features**:
  - Host/Join multiplayer lobby
  - Game settings
  - Player list with ready status
  - Start game button (host only)
- **Key Components**:
  - `MainMenuManager`: Manages menu flow and networking
  - `MainMenuUI`: UI controller for menu panels
  - `LobbyPlayerUI`: Shows connected players
  - `BarelyMovedNetworkManager`: Must be present (DontDestroyOnLoad)
  - `GameStateManager`: Tracks overall game state (DontDestroyOnLoad)

#### 2. Prep Scene (`PrepScene.unity`)
- **Purpose**: Hub world where players prepare for jobs
- **Features**:
  - Job selection board
  - Upgrade shop (TODO)
  - Character customization (TODO)
  - Social hub for players to interact
- **Key Components**:
  - `PrepSceneManager`: Manages hub scene
  - `JobSelectionUI`: UI for selecting available jobs
  - Player spawn points (multiple positions for co-op)

#### 3. Level Scene (`SampleScene.unity` - can be renamed to `LevelScene.unity`)
- **Purpose**: Actual gameplay - moving furniture and completing jobs
- **Features**:
  - Level prefab spawning
  - Job objectives
  - Timer and scoring
  - Level completion/return to prep
- **Key Components**:
  - `JobManager`: Manages current job state
  - `DeliveryZone`: Where items are delivered
  - `LevelFinishZone`: Interactive zone to complete level
  - Player spawn points

---

## Setup Instructions

### Step 1: Main Menu Scene Setup

1. **Create/Open MainMenu.unity**
   - Create empty GameObject: `MainMenuManager`
   - Add component: `MainMenuManager`

2. **Setup NetworkManager** (Required - only in Main Menu)
   - Create GameObject: `NetworkManager`
   - Add component: `BarelyMovedNetworkManager`
   - Configure:
     - Player Prefab: Assign your player prefab
     - Max Players: 4
     - Don't Destroy On Load: ✓
     - Offline Scene: (leave empty or MainMenu)
     - Online Scene: PrepScene
     - Auto Create Player: ✓ (but won't spawn in MainMenu)
     - Spawn Players In Main Menu: ✗ (unchecked)

3. **Setup GameStateManager** (Required - only in Main Menu)
   - Create GameObject: `GameStateManager`
   - Add component: `GameStateManager`
   - Add component: `NetworkIdentity`
   - Configure:
     - Main Menu Scene Name: "MainMenu"
     - Prep Scene Name: "PrepScene"
     - Level Scene Name: "SampleScene"

4. **Setup SceneTransitionManager** (Optional - can be in any scene)
   - Create GameObject: `SceneTransitionManager`
   - Add component: `SceneTransitionManager`
   - Configure scene names to match

5. **Create Main Menu UI**
   - Create Canvas
   - Create UI panels:
     - **Main Menu Panel**:
       - Host Button → Links to `MainMenuManager.HostGame()`
       - Join Button → Opens join panel
       - Settings Button → Opens settings
       - Quit Button → Links to `MainMenuManager.QuitGame()`
     
     - **Lobby Panel**:
       - Lobby Title Text (shows "Hosting" or "In Lobby")
       - Player Count Text
       - Player List Container (for `LobbyPlayerUI`)
       - Start Game Button → Links to `MainMenuManager.StartGame()` (host only)
       - Leave Lobby Button → Links to `MainMenuManager.LeaveLobby()`
     
     - **Join Panel**:
       - Server Address Input Field
       - Confirm Button
       - Cancel Button
     
     - **Settings Panel**:
       - Volume sliders, graphics options, etc.
       - Close Button

   - Add `MainMenuUI` component to Canvas
   - Assign all UI references in inspector

6. **Setup Lobby Player List**
   - Create empty GameObject under Player List Container: `LobbyPlayerList`
   - Add component: `LobbyPlayerUI`
   - Configure:
     - Player List Container: Assign the container
     - Player Count Text: Assign text element
     - Max Players: 4

---

### Step 2: Prep Scene Setup

1. **Create/Setup PrepScene.unity**
   - Create GameObject: `PrepSceneManager`
   - Add component: `PrepSceneManager`
   - Add component: `NetworkIdentity`

2. **Create Player Spawn Points**
   - Create empty GameObjects at desired spawn locations
   - Name them: `PlayerSpawn_1`, `PlayerSpawn_2`, etc.
   - Assign to `PrepSceneManager.m_PlayerSpawnPoints` array
   - Gizmos will show spawn locations in editor

3. **Create Job Selection UI**
   - Create Canvas (if not exists)
   - Create Panel: `JobBoardPanel`
   - Add UI elements:
     - Job Title Text
     - Job Description Text
     - Job Reward Text
     - Job Difficulty Text
     - Job selection buttons (3 buttons for different jobs)
     - Start Job Button → Links to `JobSelectionUI.OnStartJobClicked()`
     - Close Button → Links to `JobSelectionUI.OnCloseClicked()`
   
   - Create GameObject: `JobSelectionUI`
   - Add component: `JobSelectionUI`
   - Assign all UI references

4. **Create Interactable Objects** (Optional)
   - Job Board: Interactive object that opens `JobSelectionUI`
   - Upgrade Shop: Opens upgrade UI (TODO)
   - Customization Station: Opens customization UI (TODO)

---

### Step 3: Level Scene Setup

1. **Verify Existing Components**
   - `JobManager` should already exist
   - `DeliveryZone` should already exist
   - Update `JobManager`:
     - Use Timer: ✓ or ✗ based on preference
     - Require Manual Finish: ✓ (recommended for 3-scene flow)

2. **Verify LevelFinishZone** (if using manual finish)
   - Should already exist in scene
   - Component: `LevelFinishZone`
   - Verifies it triggers job completion properly

3. **Create Player Spawn Points**
   - Create spawn points for level (separate from prep scene)
   - Assign to `BarelyMovedNetworkManager.m_PlayerSpawnPoints` (or scene-specific spawn system)

---

### Step 4: Build Settings

1. **Add All Scenes to Build Settings** (File → Build Settings)
   - Order matters for scene indices:
     1. `MainMenu` (index 0)
     2. `PrepScene` (index 1)
     3. `SampleScene` or `LevelScene` (index 2)

2. **Set MainMenu as Default Scene**
   - Drag MainMenu to top of list

---

## Scene Transition Flow

### Main Menu → Prep Scene
1. Host creates lobby via `MainMenuManager.HostGame()`
2. Clients join via `MainMenuManager.JoinGame(address)`
3. Host clicks "Start Game"
4. `MainMenuManager.StartGame()` → `GameStateManager.TransitionToPrep()`
5. NetworkManager loads PrepScene for all clients
6. Players spawn in PrepScene
7. GameState changes to `PrepHub`

### Prep Scene → Level Scene
1. Players interact with job board
2. `JobSelectionUI` shows available jobs
3. Host selects job and clicks "Start Job"
4. `PrepSceneManager.StartJob()` → `GameStateManager.TransitionToLevel()`
5. NetworkManager loads LevelScene for all clients
6. Players spawn in level
7. `JobManager.StartJob()` called automatically
8. GameState changes to `InLevel`

### Level Scene → Prep Scene
1. Job completes (either manual finish or all items delivered)
2. `JobManager.ManualCompleteJob()` or automatic completion
3. `JobManager.OnJobCompleted` event fires
4. `SceneTransitionManager` listens and calculates results
5. `LevelResultsData` stores job results
6. After delay: `GameStateManager.TransitionBackToPrep()`
7. NetworkManager loads PrepScene
8. Players spawn back in prep scene
9. GameState changes to `PrepHub`

---

## Key Classes Reference

### GameStateManager
- **Location**: `Assets/Scripts/GameManagement/GameStateManager.cs`
- **Purpose**: Tracks overall game state across all scenes
- **States**: `MainMenu`, `PrepHub`, `InLevel`, `Loading`
- **Key Methods**:
  - `TransitionToPrep()`: Main Menu → Prep Scene
  - `TransitionToLevel()`: Prep Scene → Level Scene
  - `TransitionBackToPrep()`: Level Scene → Prep Scene

### MainMenuManager
- **Location**: `Assets/Scripts/GameManagement/MainMenuManager.cs`
- **Purpose**: Manages main menu scene and lobby
- **Key Methods**:
  - `HostGame()`: Start hosting a multiplayer game
  - `JoinGame(address)`: Join existing game
  - `StartGame()`: Transition to prep scene (host only)
  - `LeaveLobby()`: Disconnect and return to main menu

### PrepSceneManager
- **Location**: `Assets/Scripts/GameManagement/PrepSceneManager.cs`
- **Purpose**: Manages prep/hub scene
- **Key Methods**:
  - `StartJob()`: Begin selected job (transition to level)
  - `GetSpawnPoint()`: Get spawn point for player

### BarelyMovedNetworkManager
- **Location**: `Assets/Scripts/Network/BarelyMovedNetworkManager.cs`
- **Purpose**: Custom NetworkManager with scene-aware player spawning
- **Key Features**:
  - Doesn't spawn players in MainMenu
  - Uses scene-specific spawn points
  - Handles scene transitions
  - Notifies GameStateManager on scene load

### SceneTransitionManager
- **Location**: `Assets/Scripts/GameManagement/SceneTransitionManager.cs`
- **Purpose**: Handles job completion and result calculation
- **Key Methods**:
  - `LoadMainMenu()`: Load main menu scene
  - `LoadPrepScene()`: Load prep scene
  - `LoadLevelScene()`: Load level scene

---

## Testing Workflow

### Solo Testing
1. Start in MainMenu
2. Click "Host Game"
3. Click "Start Game"
4. Should load PrepScene with player spawned
5. Open job board, select job, start
6. Should load LevelScene
7. Complete job or use finish zone
8. Should return to PrepScene

### Multiplayer Testing
1. **Host**: Start game, click "Host Game"
2. **Client**: Start game, enter host's IP, click "Join"
3. Both players see each other in lobby
4. Host clicks "Start Game"
5. Both transition to PrepScene
6. Host selects and starts job
7. Both transition to LevelScene
8. Complete job
9. Both return to PrepScene

---

## Common Issues & Solutions

### Players Not Spawning in Prep Scene
- Check `PrepSceneManager` has spawn points assigned
- Verify `BarelyMovedNetworkManager.m_SpawnPlayersInMainMenu` is false
- Check player prefab is assigned in NetworkManager

### Scene Not Transitioning
- Verify all scenes are in Build Settings
- Check `GameStateManager` is in scene (DontDestroyOnLoad)
- Ensure host/server is calling transition methods
- Check NetworkManager's online scene setting

### Job Not Starting in Level
- Verify `JobManager` exists in level scene
- Check `GameStateManager.OnSceneLoaded()` calls `JobManager.StartJob()`
- Ensure `JobManager` has required items and delivery zone assigned

### UI Not Showing/Working
- Check Canvas is set to Screen Space - Overlay
- Verify event system exists in scene
- Check button onClick events are assigned
- Ensure UI references are assigned in inspector

---

## Future Enhancements

### Planned Features
1. **Upgrade Shop UI**: Allow players to purchase equipment upgrades
2. **Character Customization**: Cosmetic customization station
3. **Multiple Levels**: Different level prefabs/scenes for variety
4. **Persistent Progression**: Save player money, upgrades, unlocks
5. **Lobby Chat**: Text chat in main menu lobby
6. **Ready System**: Players mark ready before starting
7. **Loading Screen**: Visual feedback during scene transitions
8. **Results Screen**: Detailed breakdown after job completion

---

## Notes

- **NetworkManager Must Persist**: Only create NetworkManager in MainMenu, it will persist through all scenes
- **GameStateManager Must Persist**: Same as NetworkManager, only create once
- **Scene Order Matters**: Ensure scenes are in correct build order
- **Host Authority**: Only host/server can trigger scene transitions
- **Player Spawning**: Handled differently per scene (MainMenu = no spawn, Prep/Level = spawn)

---

## Quick Reference Commands

### Force Scene Transition (Debug)
```csharp
// From any scene, call:
GameStateManager.Instance.TransitionToPrep();
GameStateManager.Instance.TransitionToLevel();
GameStateManager.Instance.TransitionBackToPrep();
```

### Manual Testing
- Use context menus on managers (right-click in inspector)
- `GameStateManager`: "Debug State Info"
- `JobManager`: "Start Job", "Debug Job Info"
- `BarelyMovedNetworkManager`: "Debug Connection Info"

---

Last Updated: October 2025

