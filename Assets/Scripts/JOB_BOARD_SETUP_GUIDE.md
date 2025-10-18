# Job Board Setup Guide for Prep Scene

## Overview
This guide explains how to set up the interactive Job Board in the Prep Scene so players can select jobs.

---

## What Was Fixed

### ✅ Problem 1: Auto-Transition to Prep Scene
**Issue**: When clicking "Host Game", the lobby showed for a second then immediately transitioned to Prep Scene.

**Solution**: 
- Updated `MainMenuManager.HostGame()` to clear the `onlineScene` property
- This prevents Mirror from auto-loading a scene when the server starts
- Now the game stays in MainMenu/Lobby until host clicks "Start Game"

### ✅ Problem 2: Cursor Locked in Prep Scene
**Issue**: Players couldn't interact with the Job Board UI because cursor was locked.

**Solution**:
- `JobSelectionUI` now properly manages cursor state
- When job board opens: cursor unlocks and becomes visible
- When job board closes: cursor locks back for gameplay
- Player input is disabled when UI is open

### ✅ Problem 3: No Job Board Interaction
**Issue**: No way to open the job board in prep scene.

**Solution**:
- Created `JobBoardZone.cs` - an interactable zone for the job board
- Updated `PlayerInteractionSystem.cs` to detect and interact with job boards
- Works exactly like `LevelFinishZone` but for the prep scene

---

## Setup Instructions

### Step 1: Create Job Board Zone in Prep Scene

1. **Open PrepScene.unity**

2. **Create the Job Board GameObject**:
   - Create empty GameObject: `JobBoard`
   - Position it where you want the job board to be in your hub

3. **Add Box Collider** (Trigger Zone):
   - Add Component → Box Collider
   - ✓ Check "Is Trigger"
   - Set size (e.g., 3x3x3 for a medium zone)

4. **Add NetworkIdentity**:
   - Add Component → Network Identity
   - Server Only: ✗ (unchecked)

5. **Add JobBoardZone Component**:
   - Add Component → JobBoardZone
   - Configure in Inspector:
     - **Interaction Zone**: Assign self (drag JobBoard GameObject)
     - **Zone Size**: Set to match your collider (e.g., 3, 3, 3)
     - **Interaction Range**: 3-5 units
     - **Player Layer**: Select "Player" layer
     - **Zone Color**: Yellow/Orange (1, 0.8, 0, 0.3)
     - **Zone Renderer**: Optional - for visual feedback

6. **Add a Visual Mesh** (Optional but Recommended):
   - Add child GameObject: `JobBoardMesh`
   - Add Component → Mesh Filter → Set mesh (Cube, Plane, or custom model)
   - Add Component → Mesh Renderer
   - Assign a material (use yellow/orange to distinguish it)
   - This helps players see where the job board is

7. **Configure Layer**:
   - Set JobBoard layer to "Interactable" (or create this layer if it doesn't exist)

### Step 2: Update Player Prefab

1. **Open your Player Prefab**

2. **Find PlayerInteractionSystem Component**:
   - Should already be on the player
   - If not, add it: Add Component → Player Interaction System

3. **Configure PlayerInteractionSystem**:
   - **Interaction Range**: 3-5 units (should match job board range)
   - **Interactable Layer**: Select "Interactable" layer
   - **Interaction Origin**: Assign player's camera or center transform

4. **Verify PlayerInputHandler**:
   - Make sure player has `PlayerInputHandler` component
   - It should handle the interact input (E key by default)

### Step 3: Create Job Board UI in Prep Scene

1. **Create Canvas** (if not exists):
   - Create → UI → Canvas
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920x1080

2. **Create Job Board Panel**:
   - Right-click Canvas → UI → Panel
   - Rename to `JobBoardPanel`
   - Set as large as you want (e.g., 800x600)
   - Add a semi-transparent background

3. **Add UI Elements to Panel**:
   ```
   JobBoardPanel
   ├── TitleText (TextMeshProUGUI) - "Available Jobs"
   ├── JobDetailsPanel
   │   ├── JobTitleText (TextMeshProUGUI)
   │   ├── JobDescriptionText (TextMeshProUGUI)
   │   ├── JobRewardText (TextMeshProUGUI)
   │   └── JobDifficultyText (TextMeshProUGUI)
   ├── JobButtonsPanel (Horizontal Layout Group)
   │   ├── JobButton1 (Button) - "Apartment Move"
   │   ├── JobButton2 (Button) - "Office Relocation"
   │   └── JobButton3 (Button) - "Mansion Move"
   └── BottomButtons
       ├── StartJobButton (Button) - "Start Job"
       └── CloseButton (Button) - "Close"
   ```

4. **Create JobSelectionUI GameObject**:
   - Create empty GameObject under Canvas: `JobSelectionUI`
   - Add Component → Job Selection UI
   - Assign all UI references in Inspector:
     - Job Board Panel → JobBoardPanel
     - Job Title Text → JobTitleText
     - Job Description Text → JobDescriptionText
     - Job Reward Text → JobRewardText
     - Job Difficulty Text → JobDifficultyText
     - Job Buttons → Array of 3 buttons
     - Start Job Button → StartJobButton
     - Close Button → CloseButton

5. **Set JobBoardPanel to Inactive**:
   - Select JobBoardPanel
   - Uncheck the checkbox at top of Inspector
   - This hides it by default

### Step 4: Configure NetworkManager (One-Time Setup)

1. **Find NetworkManager in MainMenu Scene**:
   - Should be the `BarelyMovedNetworkManager` GameObject

2. **Configure Settings**:
   - **Offline Scene**: "MainMenu" or leave empty
   - **Online Scene**: "" (IMPORTANT: Leave empty or set to "MainMenu")
   - **Auto Create Player**: ✓ (checked)
   - **Spawn Players In Main Menu**: ✗ (unchecked)

3. **Important**: The online scene will be controlled by `GameStateManager`, not NetworkManager

---

## How It Works

### Scene Flow:
```
1. Player hosts game in MainMenu
   → Stays in MainMenu (no auto-transition)
   
2. Host clicks "Start Game"
   → GameStateManager.TransitionToPrep()
   → All players load PrepScene
   → Players spawn at spawn points
   
3. Player walks to Job Board Zone
   → Interaction prompt appears: "Press E to View Jobs"
   
4. Player presses E
   → JobSelectionUI opens
   → Cursor unlocks, player input disabled
   → UI shows available jobs
   
5. Player selects job, clicks "Start Job" (host only)
   → JobSelectionUI closes
   → GameStateManager.TransitionToLevel()
   → All players load LevelScene
```

### Interaction System:
- `PlayerInteractionSystem` constantly checks for nearby interactables
- Detects `JobBoardZone` and `LevelFinishZone`
- Shows appropriate interaction prompt based on what's nearby
- Handles input when player presses E

### Networking:
- `JobBoardZone` uses NetworkBehaviour for multiplayer
- Interaction detection happens on server
- UI opening is called via ClientRpc (only for the interacting player)
- All scene transitions are server-authoritative

---

## Testing Checklist

### Single Player Test:
- [ ] Start game, click "Host Game"
- [ ] Should stay in lobby (not auto-transition)
- [ ] Click "Start Game"
- [ ] Should load PrepScene with player spawned
- [ ] Walk to Job Board Zone
- [ ] Interaction prompt should appear
- [ ] Press E
- [ ] Job board UI should open with cursor visible
- [ ] Select a job
- [ ] Click "Start Job"
- [ ] Should transition to level scene

### Multiplayer Test:
- [ ] Host creates game, stays in lobby
- [ ] Client joins
- [ ] Both see each other in lobby
- [ ] Host starts game → both load prep scene
- [ ] Both can walk to job board
- [ ] Both can view jobs
- [ ] Only host can start jobs
- [ ] Both transition to level when host starts job

---

## Troubleshooting

### "Lobby immediately transitions to Prep Scene"
**Fix**: 
- Open `NetworkManager` in MainMenu scene
- Set "Online Scene" to empty string or "MainMenu"
- The code now handles this automatically via `MainMenuManager.HostGame()`

### "Can't interact with job board"
**Checks**:
- JobBoard GameObject has `JobBoardZone` component
- JobBoard has Box Collider with "Is Trigger" checked
- JobBoard layer is set to "Interactable"
- Player has `PlayerInteractionSystem` component
- PlayerInteractionSystem's "Interactable Layer" includes your JobBoard layer
- Interaction Range on both player and job board are reasonable (3-5 units)

### "Job board UI doesn't open"
**Checks**:
- JobSelectionUI component exists in the scene
- All UI references are assigned in JobSelectionUI Inspector
- JobBoardPanel is initially inactive (unchecked)
- Canvas has EventSystem

### "Cursor still locked when job board opens"
**Checks**:
- JobSelectionUI's `ShowJobBoard()` method unlocks cursor
- Check Console for any errors preventing UI from opening
- Verify no other script is locking the cursor

### "Press E prompt doesn't show"
**Checks**:
- InteractionPromptUI exists in scene
- PlayerInteractionSystem finds it in Awake
- Interaction range overlaps with player position
- Console doesn't show "InteractionPromptUI not found"

### "Only host can see/start jobs"
**Expected Behavior**: 
- All players can VIEW jobs (open UI, see details)
- Only HOST can START jobs (the button should be disabled for clients)
- This is intentional for host-authoritative game flow

---

## Layer Setup

### Required Layers:
1. **Player** - for player GameObjects
2. **Interactable** - for job boards, finish zones, etc.

### To Create Layers:
1. Edit → Project Settings → Tags and Layers
2. Add "Player" to User Layer 6 (or any free slot)
3. Add "Interactable" to User Layer 7 (or any free slot)

### Layer Mask Configuration:
- PlayerInteractionSystem → Interactable Layer → Select "Interactable"
- JobBoardZone → Player Layer → Select "Player"
- LevelFinishZone → Player Layer → Select "Player"

---

## Quick Setup Summary

**In PrepScene:**
1. Create JobBoard GameObject with BoxCollider (trigger), NetworkIdentity, JobBoardZone component
2. Set JobBoard layer to "Interactable"
3. Create JobBoardPanel UI with JobSelectionUI component
4. Assign all UI references

**On Player Prefab:**
1. Verify PlayerInteractionSystem component exists
2. Set Interactable Layer to include "Interactable"
3. Set Interaction Range (3-5 units)

**In NetworkManager (MainMenu):**
1. Set Online Scene to "" (empty)
2. This is now handled automatically by MainMenuManager

**Done!**

---

## Additional Notes

- The job board uses the same interaction system as the level finish zone
- You can create multiple job boards in the same scene if needed
- The cursor management is handled automatically by JobSelectionUI
- Player input is disabled while UI is open to prevent movement
- The system is fully networked - all players see the same job board state

---

## File Locations

New files created:
- `Assets/Scripts/Interactables/JobBoardZone.cs`
- `Assets/Scripts/JOB_BOARD_SETUP_GUIDE.md` (this file)

Modified files:
- `Assets/Scripts/Player/PlayerInteractionSystem.cs`
- `Assets/Scripts/GameManagement/MainMenuManager.cs`
- `Assets/Scripts/GameManagement/PrepSceneManager.cs`
- `Assets/Scripts/Network/BarelyMovedNetworkManager.cs`

---

**Status**: ✅ Ready for setup and testing

**Next Steps**: Follow the setup instructions above to create the job board in your PrepScene!

