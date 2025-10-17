# Mesh Fracturing Setup Guide

## Quick Start

The mesh fracturing system is **already integrated** into all `GrabbableItem` objects. When items break, they'll automatically fracture into pieces.

## Important: Enable Mesh Read/Write

For fracturing to work, **all item meshes MUST have Read/Write enabled**:

### Steps:
1. Select your item mesh in Project window (e.g., `Cube.fbx`)
2. In Inspector, check **Read/Write Enabled** 
3. Click **Apply**
4. Repeat for all item meshes

### Common Meshes to Update:
- Box/Cube meshes
- Vase/Bottle meshes  
- Furniture meshes
- Any custom item models

**If you forget this step:** You'll see error in console: 
`"Mesh 'X' is not readable! Enable Read/Write in import settings."`

## Adjusting Fracture Settings

Select any item with `GrabbableItem` component:

### Fracture Count (6-20)
- **Lower (6-8)**: Fewer, larger pieces - better performance
- **Medium (10-12)**: Balanced - recommended
- **Higher (15-20)**: Many small pieces - more dramatic but slower

### Fracture Explosion Force (0-10)
- **Low (1-3)**: Pieces stay close together
- **Medium (3-5)**: Pieces spread naturally
- **High (7-10)**: Pieces explode outward dramatically

### Fracture Explosion Radius (0-5)
- Controls how far the explosive force reaches
- **Larger = more uniform distribution**
- **Smaller = concentrated near impact**

### Fragment Lifetime (1-10 seconds)
- How long pieces exist before cleanup
- **Shorter**: Better performance, cleaner scene
- **Longer**: More visible debris

## Testing

1. Start Play mode
2. Grab an item (E key)
3. Throw it at wall/floor (Right Mouse Button)
4. Watch it break into pieces!

## Troubleshooting

### "No mesh found for fracturing"
- Item needs a `MeshFilter` component in its hierarchy
- Check that Visual Root has a mesh

### "Mesh is not readable"
- Enable Read/Write in mesh import settings (see above)

### Pieces don't fall properly
- Check that fragments have correct layer
- Verify collision matrix allows fragment layer to collide with ground

### Too many pieces / Performance issues
- Reduce Fracture Count to 6-8
- Reduce Fragment Lifetime to 2-3 seconds
- Limit number of items breaking simultaneously

### Pieces disappear immediately
- Check Fragment Lifetime isn't 0
- Verify FracturedPiece component is being added

### Fracture looks weird/unrealistic
- Try adjusting Explosion Force and Radius
- Impact point affects fracture pattern
- Different shapes fracture differently

## Performance Considerations

Each fractured item creates:
- 6-20 new GameObjects
- Each with Rigidbody + MeshCollider
- Active for ~5 seconds

**Recommended limits:**
- Max 5-10 items breaking at once
- Use 6-10 pieces for small objects
- Use 12-20 pieces only for large/important items

## Technical Notes

- Fracturing happens **client-side only** (not networked)
- Each client generates slightly different fragments (normal)
- Original item destroyed on server after 0.15s
- Fragments auto-cleanup after lifetime
- Uses plane-based slicing algorithm

## Legacy VFX System

Old particle-based ShatterVFX system still exists but is no longer used by GrabbableItem. It can be removed if not needed elsewhere:
- `ShatterVFX.cs`
- `ShatterVFXSpawner.cs`
- ShatterVFX GameObject in scene

## Need Help?

Check the detailed documentation: `MESH_FRACTURE_README.md`

