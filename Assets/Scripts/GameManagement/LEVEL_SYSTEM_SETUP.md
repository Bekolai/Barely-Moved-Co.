# Level System Setup Guide

## Overview
This guide explains how to set up and use the new level completion system with optional timers, manual finish zones, and result screens.

## Components Created

### 1. Core Systems
- **JobManager** (Modified): Now supports optional timer and manual finish requirements
- **LevelResultsData**: Persists level completion data between scenes
- **SceneTransitionManager**: Handles scene transitions and calculates results
- **LevelFinishZone**: Interactable zone for manual level completion

### 2. UI Components
- **LevelFinishConfirmationUI**: Confirmation dialog for finishing levels
- **JobCompleteUI**: Results screen shown in prep scene
- **InteractionPromptUI**: Simple prompt for interactable objects

### 3. Player Systems
- **PlayerInteractionSystem**: Handles interactions with finish zones
- **PlayerInputHandler** (Modified): Added interact action support

---

## Setup Instructions

### Step 1: Add Interact Action to Input System

1. Open `Assets/InputSystem_Actions.inputactions`
2. Add a new action called "Interact"
3. Bind it to the **E** key (or your preferred key)
4. Example binding:
   - Action: `Interact`
   - Type: Button
   - Binding: Keyboard `E`

### Step 2: Configure JobManager in Scene

In your job/level scene (e.g., SampleScene):

1. Select the **JobManager** GameObject
2. Configure the following settings:
   - **Use Timer**: Toggle on/off for timed levels
   - **Job Time Limit**: Set time limit in seconds (if timer enabled)
   - **Require Manual Finish**: If true, players must interact with finish zone

### Step 3: Add LevelFinishZone to Scene

For levels that require manual completion:

1. Create a new GameObject: `LevelFinishZone`
2. Add the **LevelFinishZone** component
3. Add a **BoxCollider** component:
   - Set as **Trigger**
   - Size: 2x2x2 (or adjust as needed)
4. Configure the component:
   - **Zone Size**: Size of the interaction area
   - **Interaction Range**: How close players need to be
   - **Player Layer**: Set to your player layer mask
5. Optional: Add a visual mesh (like a glowing pad or car door)

### Step 4: Add PlayerInteractionSystem to Player

1. Open your **Player** prefab
2. Add the **PlayerInteractionSystem** component
3. Configure:
   - **Interaction Range**: Detection radius (e.g., 3)
   - **Interactable Layer**: Layer for interactable objects
   - **Interaction Origin**: Reference to camera or player transform

### Step 5: Setup Interaction Prompt UI

In your Canvas:

1. Create a new UI element: `InteractionPrompt`
2. Add **InteractionPromptUI** component
3. Add child **TextMeshPro** for the prompt text
4. Configure:
   - Position near bottom of screen
   - Default text: "Press E to Interact"

### Step 6: Setup Confirmation Dialog UI

In your Canvas:

1. Create a new Panel: `FinishConfirmationDialog`
2. Add **LevelFinishConfirmationUI** component
3. Add child elements:
   - **TextMeshPro** for message
   - **Button** for "Yes" (confirm)
   - **Button** for "No" (cancel)
4. Link components in inspector:
   - Assign Dialog Panel reference
   - Assign Message Text reference
   - Assign Confirm Button reference
   - Assign Cancel Button reference

### Step 7: Setup SceneTransitionManager

1. Create a new GameObject: `SceneTransitionManager`
2. Add the **SceneTransitionManager** component
3. Configure:
   - **Prep Scene Name**: Name of your prep/hub scene
   - **Job Scene Name**: Name of your job scene
   - **Transition Delay**: Delay before scene transition (e.g., 2 seconds)
   - **Payment Settings**:
     - Base Payment: Money earned for completing job
     - Time Bonus Per Second: Bonus for finishing early
     - Damage Deduction Per Item: Penalty for broken items

### Step 8: Create Prep Scene with Results UI

In your prep scene:

1. Create a Canvas if not exists
2. Create a new Panel: `JobCompletePanel`
3. Add **JobCompleteUI** component
4. Add child elements for displaying:
   - Title text
   - Money earned
   - Money deducted
   - Net profit
   - Performance stats (time, items delivered, items broken)
   - Continue button
5. Link all references in inspector

---

## Usage Examples

### Example 1: Timed Level (No Manual Finish)

Perfect for quick delivery jobs where you just need to deliver all items before time runs out.

**JobManager Settings:**
- Use Timer: ✓ Enabled
- Job Time Limit: 600 (10 minutes)
- Require Manual Finish: ✗ Disabled

**Result:** Level automatically completes when all items are delivered OR fails when timer runs out.

---

### Example 2: Manual Finish (No Timer)

Perfect for relaxed levels where players decide when they're done.

**JobManager Settings:**
- Use Timer: ✗ Disabled
- Require Manual Finish: ✓ Enabled

**Setup Required:**
- Place **LevelFinishZone** in scene (e.g., at driver's door)
- Players deliver items at their own pace
- When ready, interact with finish zone to complete

**Result:** Level only completes when player interacts with finish zone and confirms.

---

### Example 3: Timed with Manual Finish

Perfect for realistic moving jobs where you have a deadline but control when to leave.

**JobManager Settings:**
- Use Timer: ✓ Enabled
- Job Time Limit: 900 (15 minutes)
- Require Manual Finish: ✓ Enabled

**Setup Required:**
- Place **LevelFinishZone** in scene
- Players must deliver items AND interact with finish zone before time runs out

**Result:** Players can finish early (earning time bonus) or risk waiting too long and failing.

---

## Layer Setup

Make sure you have the following layers configured:

1. **Player** layer: For player detection in finish zone
2. **Interactable** layer: For finish zone and other interactables
3. **CarriedItem** layer (optional): For items being carried

Configure in `Edit > Project Settings > Tags and Layers`

---

## Scene Flow

### Job Scene → Prep Scene

1. Job starts (JobManager.StartJob)
2. Players deliver items to DeliveryZone
3. **Option A (Automatic):** All items delivered → Job completes
4. **Option B (Manual):** Player interacts with finish zone → Confirmation dialog → Job completes
5. **Option C (Timer):** Time runs out → Job fails
6. JobManager fires OnJobCompleted event
7. SceneTransitionManager calculates results
8. Results stored in LevelResultsData (persists across scenes)
9. Scene transitions to prep scene
10. JobCompleteUI displays results in prep scene
11. Player can review stats and continue to next job

---

## Testing

### In-Editor Testing

1. **Test Timer Toggle:**
   - Play scene
   - Check if timer shows/hides based on UseTimer setting

2. **Test Finish Zone:**
   - Play scene
   - Walk to finish zone
   - Press E to interact
   - Confirm dialog should appear
   - Click Yes to complete level

3. **Test Scene Transition:**
   - Use context menu on SceneTransitionManager: "Test Transition to Prep"
   - Verify results appear in prep scene

---

## Customization

### Modify Payment Calculation

Edit `SceneTransitionManager.CalculateAndStoreResults()`:

```csharp
// Example: Add bonus for perfect delivery
float perfectBonus = 0f;
if (itemsBroken == 0)
{
    perfectBonus = 500f;
}

resultsData.SetResults(
    basePayment + perfectBonus,
    timeBonus,
    damageDeductions,
    // ... other parameters
);
```

### Custom Finish Zone Visuals

1. Add a mesh renderer to LevelFinishZone GameObject
2. Add particle effects for visual feedback
3. Add audio source for interaction sounds

### Multiple Finish Zones

You can have multiple finish zones in a scene:
- Each checks if job is active
- First player to interact completes the job for everyone
- Good for cooperative gameplay

---

## Troubleshooting

### "Interact action not found" Warning

**Solution:** Add "Interact" action to InputSystem_Actions.inputactions (see Step 1)

### Finish Zone Not Working

**Check:**
1. LevelFinishZone has BoxCollider with "Is Trigger" enabled
2. Player layer is in the Player Layer mask
3. PlayerInteractionSystem is on player prefab
4. Interactable layer is set correctly

### Results Not Showing in Prep Scene

**Check:**
1. LevelResultsData persists (DontDestroyOnLoad)
2. JobCompleteUI is in prep scene
3. Scene transition is completing properly
4. SceneTransitionManager is calculating results

### Timer Not Updating

**Check:**
1. JobManager.UseTimer is enabled
2. JobManager has NetworkIdentity
3. Server is active (timer only updates on server)

---

## Network Considerations

All components are **Mirror-networked**:
- JobManager runs on server (authoritative)
- LevelFinishZone validates interactions on server
- Confirmation dialogs show on local clients
- Scene transitions are server-controlled

For single-player testing, just host a game in editor.

---

## Next Steps

1. Create your prep scene
2. Add shop/upgrade systems
3. Add job selection UI
4. Implement persistent economy (save/load money)
5. Add multiple job scenes with varying difficulty

---

## API Reference

### JobManager
```csharp
// Properties
bool UseTimer { get; }
bool RequiresManualFinish { get; }
float TimeRemaining { get; }
bool JobActive { get; }

// Methods
[Server] void StartJob()
[Server] void ManualCompleteJob()

// Events
event JobEventDelegate OnJobCompleted;
event JobEventDelegate OnJobFailed;
```

### LevelFinishZone
```csharp
// Methods
void TryInteract(uint playerNetId)
[Command] void CmdConfirmFinish()

// Events
event FinishZoneEventDelegate OnPlayerEnteredRange;
event FinishZoneEventDelegate OnPlayerExitedRange;
```

### SceneTransitionManager
```csharp
// Methods
void LoadPrepScene()
void LoadJobScene()
void LoadScene(string sceneName)
```

### LevelResultsData
```csharp
// Properties
float NetProfit { get; }
int ItemsDelivered { get; }
int ItemsBroken { get; }

// Methods
void SetResults(...)
void ClearResults()
```

---

## Support

For issues or questions, check:
1. This documentation
2. Component tooltips in inspector
3. Debug logs in console
4. Scene setup examples

