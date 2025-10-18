# Level System - Quick Reference Card

## 🎯 What You Asked For

✅ **Two Ways to End Levels:**
1. **Timer** - Optional, configurable per level
2. **Manual Finish Zone** - Interact with zone (like driver's door) with confirmation

✅ **Job Complete UI** - Shows in prep scene after level:
- Money earned
- Money deducted (broken items)
- Net profit
- Performance stats

---

## ⚙️ Quick Setup Checklist

### 1. Add Interact Input (REQUIRED)
Open `InputSystem_Actions.inputactions` and add:
- Action: **Interact**
- Binding: **E key**

### 2. Configure Level (JobManager)
- ☐ Use Timer: On/Off
- ☐ Time Limit: Seconds (if timer on)
- ☐ Require Manual Finish: On/Off

### 3. Add Finish Zone (If Manual Finish)
- ☐ Create GameObject with LevelFinishZone component
- ☐ Add BoxCollider (trigger enabled)
- ☐ Position at "driver's door" or exit point

### 4. Add to Player Prefab
- ☐ Add PlayerInteractionSystem component

### 5. Create UI in Scene
- ☐ Interaction prompt (shows "Press E")
- ☐ Confirmation dialog (Yes/No buttons)

### 6. Create Prep Scene UI
- ☐ JobCompleteUI panel with money/stats display

### 7. Add Scene Manager
- ☐ SceneTransitionManager GameObject in scene
- ☐ Configure scene names and payment settings

---

## 🎮 Level Types

### Type 1: Classic Timed
```
Use Timer: ✓
Require Manual Finish: ✗
```
Complete all deliveries before time runs out.

### Type 2: Casual Manual
```
Use Timer: ✗
Require Manual Finish: ✓
```
Take your time, finish when ready.

### Type 3: Realistic Timed + Manual
```
Use Timer: ✓
Require Manual Finish: ✓
```
Deliver items AND drive away before deadline.

---

## 📊 Flow Diagram

```
START JOB
    ↓
Deliver Items to DeliveryZone (back of truck)
    ↓
[If Manual Finish Required]
    ↓
Go to LevelFinishZone (driver's door)
    ↓
Press E to Interact
    ↓
Confirm "Ready to go?"
    ↓
JOB COMPLETE
    ↓
Calculate Money (base + bonus - deductions)
    ↓
Store Results in LevelResultsData
    ↓
Transition to Prep Scene (2 sec delay)
    ↓
Show JobCompleteUI with results
    ↓
Continue to next job
```

---

## 💰 Payment System

### Money Earned
- **Base Payment**: Fixed amount for completing job
- **Time Bonus**: Remaining time × bonus per second

### Money Deducted
- **Broken Items**: Number of broken items × penalty per item
- **Failed Delivery**: Items not delivered

### Net Profit
`Net Profit = (Base + Time Bonus) - Deductions`

---

## 🔧 Component Locations

| Component | Path |
|-----------|------|
| JobManager (modified) | `Assets/Scripts/GameManagement/JobManager.cs` |
| LevelResultsData | `Assets/Scripts/GameManagement/LevelResultsData.cs` |
| SceneTransitionManager | `Assets/Scripts/GameManagement/SceneTransitionManager.cs` |
| LevelFinishZone | `Assets/Scripts/Interactables/LevelFinishZone.cs` |
| PlayerInteractionSystem | `Assets/Scripts/Player/PlayerInteractionSystem.cs` |
| JobCompleteUI | `Assets/Scripts/UI/JobCompleteUI.cs` |
| LevelFinishConfirmationUI | `Assets/Scripts/UI/LevelFinishConfirmationUI.cs` |
| InteractionPromptUI | `Assets/Scripts/UI/InteractionPromptUI.cs` |

---

## 🐛 Common Issues

### "Interact action not found"
→ Add "Interact" action to input actions asset

### Finish zone not working
→ Check BoxCollider is trigger, Player layer is correct

### No results showing
→ Make sure SceneTransitionManager exists and scene names are correct

### Timer not working
→ Enable "Use Timer" in JobManager

---

## 🎨 Visual Concept

**Delivery Zone (Back of Truck)**
- Where you drop off items
- Shows item count and value
- Existing DeliveryZone component

**Finish Zone (Driver's Door)**
- Where you interact to leave
- Shows "Press E to finish job"
- New LevelFinishZone component

**Confirmation Dialog**
- "Are you ready to finish and head back?"
- Yes / No buttons
- Pauses game while showing

**Results Screen (Prep Scene)**
- Big $ number (net profit)
- Breakdown of earnings/deductions
- Performance stats
- Continue button

---

## 📝 Notes for Your Game

- **Delivery Zone = Back of car** (existing system)
- **Finish Zone = Driver's side** (new system)
- **Think of it like:**
  1. Load items into truck (delivery zone)
  2. Get in driver's seat (finish zone)
  3. Drive away (scene transition)
  4. Count money at office (prep scene)

---

## 🚀 Next Steps

1. Add the "Interact" input action
2. Test a level with manual finish
3. Create your prep scene with results UI
4. Design your shop/upgrade system
5. Create multiple job scenes

---

## 💡 Tips

- Use **timer + manual finish** for realistic pressure
- Use **no timer + manual finish** for relaxed gameplay
- Use **timer + auto finish** for classic arcade style
- Adjust payment values to balance your economy
- Add visual feedback to finish zone (glowing, particles)
- Consider adding SFX for confirmation and scene transition

