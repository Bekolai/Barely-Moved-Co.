# Runtime Mesh Fracturing System

## Overview
This system provides **true runtime mesh fracturing** for destructible objects. When items break, they fracture into real mesh pieces with physics, rather than using particle effects.

## Components

### 1. MeshFracturer (Static Utility)
**Location:** `Assets/Scripts/FX/MeshFracturer.cs`

Core algorithm that fractures meshes at runtime using plane-based slicing.

**Features:**
- Splits meshes using randomized cutting planes
- Creates proper UVs for new geometry
- Generates cap faces to seal cuts
- Biases fracture lines toward impact point for realism
- Produces 6-20 fragments per object

**How it works:**
1. Starts with the original mesh
2. Applies multiple random plane cuts
3. Each cut splits fragments into positive/negative sides
4. Generates cap geometry to seal the cuts
5. Returns list of fractured mesh pieces

### 2. FracturedPiece Component
**Location:** `Assets/Scripts/FX/FracturedPiece.cs`

Component attached to each fractured piece.

**Features:**
- Full physics simulation (Rigidbody + MeshCollider)
- Auto-lifetime management (destroys after 5 seconds by default)
- Fade-out effect near end of life
- Explosive force application
- Mass calculated from fragment volume

**Properties:**
- `m_Lifetime` - How long the piece exists (default: 5s)
- `m_FadeStartTime` - When to start fading (default: 4s)

### 3. GrabbableItem Integration
**Location:** `Assets/Scripts/Items/GrabbableItem.cs`

Modified to use mesh fracturing instead of particle VFX.

**New Inspector Settings:**
- **Fracture Count** (6-20): Number of pieces to break into
- **Fracture Explosion Force**: How much force pushes pieces apart
- **Fracture Explosion Radius**: Radius of explosive force
- **Fragment Lifetime**: How long pieces stay before cleanup
- **Server Destroy Delay**: Delay before network cleanup

## Usage

### For Existing Items
The system automatically works with all `GrabbableItem` objects. Just adjust the fracture settings in the Inspector:

1. Select any item with `GrabbableItem` component
2. Look for "Mesh Fracture Settings" in Inspector
3. Adjust:
   - **Fracture Count**: More pieces = more dramatic break
   - **Explosion Force**: Higher = pieces fly further
   - **Explosion Radius**: Larger = more even distribution
   - **Fragment Lifetime**: Longer = pieces stay visible longer

### Performance Considerations
- Each fracture creates 6-20 new GameObjects with physics
- Fragments auto-cleanup after lifetime expires
- Recommended max simultaneous fractures: 5-10 items
- Fracturing happens on all clients (networked via RPC)

### Networking
- Server triggers fracture via `RpcFractureMesh`
- Each client generates fragments locally
- Fragments are **client-side only** (not networked)
- Original item is destroyed on server after delay

## Technical Details

### Mesh Slicing Algorithm
Uses recursive plane-based slicing:
1. Generate random cutting plane near impact point
2. For each existing fragment:
   - Classify vertices as positive/negative side
   - Split triangles that cross the plane
   - Generate cap geometry to seal the cut
3. Repeat until desired fragment count reached

### Cap Generation
- Projects cut edge vertices onto cutting plane
- Sorts vertices around centroid
- Creates fan triangulation
- Generates front and back faces (reversed winding)

### Physics Setup
- Convex MeshCollider for each fragment
- Mass based on fragment volume
- Initial velocity from object + random offset
- Angular velocity for realistic tumbling
- Damping for natural settling

## Customization

### Modifying Fracture Behavior
Edit `MeshFracturer.cs`:
- `c_MinFragmentVolume` - Minimum fragment size
- `GenerateCuttingPlane()` - Change cut patterns
- Plane generation bias toward impact point

### Custom Fragment Materials
Fragments inherit material from original object.
For transparency/fading, ensure material shader supports alpha.

### Different Fracture Patterns
Current: Random planes biased toward impact
Alternatives:
- Radial fracture from impact point
- Grid-based fracture
- Voronoi diagram (more complex)

## Troubleshooting

**Fragments don't appear:**
- Check that object has MeshFilter and MeshRenderer
- Verify mesh is readable (import settings)
- Check console for fracture warnings

**Too few/many pieces:**
- Adjust "Fracture Count" in Inspector
- Some meshes may produce fewer fragments

**Performance issues:**
- Reduce Fragment Lifetime
- Lower Fracture Count
- Limit simultaneous fractures

**Fragments fly too far:**
- Reduce Fracture Explosion Force
- Adjust Fracture Explosion Radius

**Networking issues:**
- Fracturing is client-side visual only
- Original item destruction is networked
- Clients may see slightly different fragments (this is normal)

## Future Enhancements
- Pre-fractured mesh caching
- Custom fracture patterns per item type
- Sound effects on fracture
- Particle dust on break
- Mesh complexity reduction for performance
- LOD support for fragments

