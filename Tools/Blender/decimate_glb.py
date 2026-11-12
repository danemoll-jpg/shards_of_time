"""Shards of Time, Round 11: a heavy image-to-3D model (Tripo's website: ~1.9M triangles) decimated for Mixamo / the game.

Run:  F:\blender\blender.exe -b --factory-startup --python Tools/Blender/decimate_glb.py -- <in.glb> <out.glb> <triangles>
Imports the glTF, puts a Decimate (collapse) modifier on every mesh so the whole model ends near <triangles>, applies it, writes the glTF
with its textures. The source file is never changed.
"""
import bpy, sys
src, dst, target = sys.argv[sys.argv.index('--') + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
ratio = min(1.0, int(target) / max(1, tris))
for o in meshes:
    bpy.context.view_layer.objects.active = o
    m = o.modifiers.new('Decimate', 'DECIMATE'); m.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=m.name)
after = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB')
print(f'DECIMATE {src}: {tris} -> {after} triangles (ratio {ratio:.4f})', flush=True)
