"""Shards of Time, Round 6: a cleaned Meshy model that is never rigged (the Great Sphinx) as a static world model.

Run:  F:\blender\blender.exe -b --factory-startup --python Tools/Blender/static_export.py -- sphinx_kira
Reads SourceArt/Meshy/<name>/<name>.blend (meshy_cleanup.py: metres, on the floor, facing -Y); writes
Assets/Resources/World/<name>.fbx (no textures inside) and copies its texture to Assets/Resources/World/<name>_albedo.png.
"""
import bpy, os, shutil, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
for name in sys.argv[sys.argv.index('--') + 1:]:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend'))
    ob = bpy.data.objects[name]
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, object_types={'MESH'}, path_mode='STRIP',
                             embed_textures=False, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False)
    shutil.copy2(os.path.join(ROOT, 'SourceArt', 'Meshy', name, 'base_color.png'), os.path.join(OUT, name + '_albedo.png'))
    print('STATIC', name, len(ob.data.polygons), 'faces ->', os.path.join('Assets', 'Resources', 'World', name + '.fbx'))
