# Co-op Physics Carrying System - Development Plan

## Overview
Hybrid physics-based carrying system where objects maintain physics simulation but are constrained when held by players. System supports both single-player and two-player cooperative carrying with collision-based damage.

---

## Phase 1: Core Architecture Setup

### Step 1.1: Create Base Object System
**Objective**: Set up the foundation for carriable objects

**Tasks for Cursor**:
- Create `CarriableObject.cs` script with properties:
  - `requiresTwoPlayers` (bool)
  - `currentValue` (float) - item's worth/health
  - `maxValue` (float)
  - `damageOnCollision` (float)
  - `minVelocityForDamage` (float) - threshold for collision damage
  - `carryPoints` (Transform[]) - positions where players attach (1 or 2)
  - `weight` (float)
- Add `Rigidbody` configuration (use interpolation, set appropriate mass)
- Implement collision detection with `OnCollisionEnter` to reduce value based on impact force
- Add visual feedback for damage (color change, particle effects)

**Testing**: Drop objects from various heights, verify collision damage works

---

### Step 1.2: Player Interaction System
**Objective**: Enable players to pick up and drop objects

**Tasks for Cursor**:
- Create `PlayerCarryController.cs`:
  - Detection sphere/raycast for nearby carriable objects
  - Input handling for pickup/drop (E key or gamepad button)
  - Reference to currently held object
  - `isCarrying` state
  - Player ID (for multi-carry coordination)
- Implement pickup logic:
  - For single-carry: immediately parent to player
  - For two-carry: mark as "waiting for second player"
- Implement drop logic:
  - Detach object, restore physics
  - Apply small forward force based on player velocity
- Add UI prompt when near carriable objects

**Testing**: Pick up and drop single-carry objects, verify physics restoration

---

## Phase 2: Hybrid Physics System

### Step 2.1: Constrained Physics While Carrying
**Objective**: Maintain physics simulation but constrain movement to player(s)

**Tasks for Cursor**:
- Create `CarryPhysicsController.cs`:
  - For single-carry:
    - Use `Rigidbody.MovePosition` to follow player offset
    - Keep `isKinematic = false`
    - Reduce drag while carried (make it feel lighter)
    - Lock rotation or use `Rigidbody.MoveRotation` for controlled tilt
  - For two-carry:
    - Calculate midpoint between two players
    - Position object at midpoint with offset
    - Rotate object to face between players
    - Allow slight sway/lag based on player movement sync
- Implement smooth interpolation (configurable speed)
- Add "struggle" physics when only one player carries two-player object:
  - Increased drag
  - Downward force (gravity effect)
  - Movement penalty for the player

**Testing**: Carry objects through tight spaces, verify collisions still register

---

### Step 2.2: Collision Handling While Carried
**Objective**: Objects can still hit walls/obstacles and take damage while being carried

**Tasks for Cursor**:
- Modify `CarriableObject.cs` collision handling:
  - Enable collision detection even when carried
  - Calculate damage based on:
    - Relative velocity to collision
    - Object weight
    - Surface hardness (use tags/layers)
  - Apply impulse back to player(s) when hitting walls
  - Optionally force drop if damage exceeds threshold
- Add collision feedback:
  - Screen shake for carriers
  - Sound effects (thud, crack based on damage)
  - Visual damage indicators

**Testing**: Run into walls while carrying, verify damage applies correctly

---

## Phase 3: Two-Player Cooperative System

### Step 3.1: Multi-Carry Coordination
**Objective**: Enable two players to carry large objects together

**Tasks for Cursor**:
- Create `TwoPlayerCarryManager.cs`:
  - Track which players are attempting to carry object
  - Assign players to front/back carry points
  - State machine: `Idle → WaitingForSecondPlayer → Carrying → Dropped`
- Update `PlayerCarryController.cs`:
  - Detect when approaching object already held by one player
  - Show different UI prompt ("Press E to help carry")
  - Sync pickup/drop between players
- Implement carry point assignment:
  - First player gets closest carry point
  - Second player gets remaining point
  - Visual indicators on carry points (glowing spheres)

**Testing**: Have two players pick up and carry a sofa together

---

### Step 3.2: Synchronized Movement
**Objective**: Object moves smoothly based on both players' positions

**Tasks for Cursor**:
- Enhance `CarryPhysicsController.cs`:
  - Calculate weighted average position if players are different distances
  - Handle player desync (one player stops, other continues):
    - Rotate object toward moving player
    - Add tension/resistance
    - Slow down leading player slightly
  - Implement "stretch limit" - if players too far apart:
    - Warning visual (object flashing)
    - Force drop if exceeded
  - Add rotation based on player formation (vertical stairs, tight turns)

**Testing**: Have players carry through doorways, around corners, up stairs

---

## Phase 4: Polish & Game Feel

### Step 4.1: Animation & Visual Feedback
**Objective**: Make carrying feel tactile and responsive

**Tasks for Cursor**:
- Implement carry animations:
  - Player IK (Inverse Kinematics) for hands on object
  - Lean/strain animation based on object weight
  - Different animations for one-handed vs two-handed carry
- Add object visual effects:
  - Slight bobbing/swaying while carried
  - Damage states (cracks, dents as value decreases)
  - Sparks/particles on hard collisions
- Create audio system:
  - Footstep variation when carrying (heavier sounds)
  - Grunting/effort sounds based on weight
  - Material-specific collision sounds

**Testing**: Carry various objects, verify animations feel natural

---

### Step 4.2: Gameplay Balancing
**Objective**: Fine-tune systems for fun cooperative gameplay

**Tasks for Cursor**:
- Create `ObjectStatsScriptableObject.cs`:
  - Store prefab variations with different weights/values
  - Balance damage thresholds
  - Configure movement speed penalties
- Implement difficulty modifiers:
  - `PlayerCarryStats.cs` - strength stat affects speed penalty
  - Environment hazards (slippery floors, narrow passages)
  - Time pressure mechanics (value decreases over time)
- Add special mechanics:
  - Fragile items (high damage multiplier)
  - Heavy items (require specific player stats)
  - Stackable small items (carry multiple small boxes)

**Testing**: Playtest with target gameplay loop, adjust values

---

## Phase 5: Network Synchronization (If Multiplayer Online)

### Step 5.1: Network Setup
**Objective**: Sync carrying system across network (if using Netcode/Mirror/Photon)

**Tasks for Cursor**:
- Add network components to `CarriableObject`:
  - Network transform for position sync
  - Network variables for `currentValue`, `isBeingCarried`
  - RPC for damage events
- Update `PlayerCarryController`:
  - Client-authoritative or server-authoritative model (decide)
  - RPCs for pickup/drop actions
  - Sync carry state across clients
- Handle network edge cases:
  - Player disconnect while carrying
  - Ownership transfer for objects
  - Lag compensation for collision damage

**Testing**: Test with 100-200ms latency, verify smooth experience

---

## Implementation Order Summary

1. **Week 1**: Phase 1 - Basic single-player carrying with collision damage
2. **Week 2**: Phase 2 - Hybrid physics system refinement
3. **Week 3**: Phase 3 - Two-player cooperation mechanics
4. **Week 4**: Phase 4 - Polish, animations, audio
5. **Week 5** (if needed): Phase 5 - Network implementation

---

## Key Technical Decisions

### Physics Approach
**Recommended**: Keep `Rigidbody.isKinematic = false` but use:
- `Rigidbody.MovePosition()` for smooth following
- Reduced `drag` and `angularDrag` when carried
- ConfigurableJoint as alternative for spring-like carrying feel

### Collision Detection
- Use **Continuous Dynamic** collision detection for fast-moving objects
- Layer-based collision matrix (carried objects vs environment)
- Collision callbacks remain active during carry

### Reference Games Study
- **Skyrim**: Objects have physics but "stick" to hand position with spring force
- **REPO**: Exaggerated physics, objects flail but stay within bounds
- **Moving Out**: Tight constraints, physics only for collisions

---

## Common Pitfalls to Avoid

1. **Jittery Movement**: Use FixedUpdate for physics, smooth interpolation
2. **Object Escape**: Add boundary checks if object gets too far from carrier
3. **Collision Spam**: Use cooldown on damage calculation
4. **Network Desync**: Always verify critical state on server
5. **Performance**: Pool particles, limit collision checks with layers

---

## Testing Checklist

- [ ] Single player can pick up small objects
- [ ] Single player cannot pick up large objects alone
- [ ] Two players can cooperatively carry large objects
- [ ] Objects take damage from wall collisions while carried
- [ ] Objects drop smoothly with proper physics
- [ ] Players can navigate stairs/doorways while carrying
- [ ] Network sync works (if applicable)
- [ ] Animations play correctly
- [ ] Audio triggers appropriately
- [ ] No physics explosions or objects escaping

---

## Prompt Templates for Cursor AI

### For Each Phase:
```
"Create a {script_name} for Unity that handles {specific_functionality}. 
The script should integrate with existing CarriableObject system. 
Use Unity's physics system with Rigidbody.MovePosition for smooth movement. 
Include public serialized fields for designer tweaking. 
Add XML documentation comments for all public methods."
```

### For Debugging:
```
"Debug the {specific_issue} in my carrying system. The problem occurs when 
{describe_scenario}. I'm using Unity {version} with {physics_settings}. 
The current behavior is {what_happens} but I expect {desired_behavior}."
```

### For Optimization:
```
"Optimize the {script_name} for performance. Current FPS drops occur when 
{scenario}. Profile and suggest improvements focusing on {area}."
```