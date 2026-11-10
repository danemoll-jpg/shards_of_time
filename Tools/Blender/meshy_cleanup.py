"""Shards of Time, Round 2 items 2-4 (Hostile Horizon's meshy_cleanup.py, ported): clean up a Meshy model and render it.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/meshy_cleanup.py -- <name> [<name> ...]

Reads SourceArt/Meshy/<name>/model.glb (from Tools/meshy_generate.py). Joins the meshes, scales the model to its height in
metres, puts the origin on the floor between the feet, centres it, keeps it facing front (Blender -Y = Unity +Z once imported).
Writes:
  SourceArt/Meshy/<name>/<name>.blend                  the clean model (the start for rigging / LODs)
  SourceArt/Mixamo/<name>/upload/<name>.fbx            people only: the file Dan uploads to Mixamo (textures embedded)
  Data/screenshots/round2/models/<name>_{front,side,back,34}.png
People get their LODs in Round 3 from Mixamo's rigged file (so the LODs keep the skin weights); Kira gets hers after her rig.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
# heights in metres (Docs/MODELS-NEEDED.md); Kira's is her head height in game (Round 1: 0.5 m)
HEIGHT = {'dan_home': 1.8, 'dan_iceage': 1.8, 'abi_home': 1.65, 'abi_iceage': 1.65, 'kira': 0.5, 'kira_sabertooth': 0.5,
          # Round 5: the Violinist (Mixamo) and the Ice Age creatures (sizes of their Round 4 placeholders)
          'violinist_iceage': 1.85, 'wolf': 1.25, 'snow_critter': 0.8, 'mammoth': 4.4}
PEOPLE = {'dan_home', 'dan_iceage', 'abi_home', 'abi_iceage', 'violinist_iceage'}
ROUND5 = {'violinist_iceage', 'wolf', 'snow_critter', 'mammoth'}
YAW = {}   # degrees about Z per model, nose to -Y (Round 2's sabertooth needed 90; Fix 3.1's Meshy model already faces front)
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round2', 'models')


def bounds(ob):
    vs = [ob.matrix_world @ v.co for v in ob.data.vertices]
    return (Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))),
            Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs))))


def clean(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    d = os.path.join(ROOT, 'SourceArt', 'Meshy', name)
    bpy.ops.import_scene.gltf(filepath=os.path.join(d, 'model.glb'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in meshes:   # the glTF importer adds an empty root: keep each mesh where it is, drop the rest
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    for o in [o for o in bpy.context.scene.objects if o.type != 'MESH']:
        bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name; ob.data.name = name
    if os.path.exists(os.path.join(d, 'base_color_meshy.png')):   # Fix 3.1: fur recoloured by Tools/match_fur.py
        for m in ob.data.materials:
            for l in (m.node_tree.links if m and m.use_nodes else []):
                if l.to_socket.name == 'Base Color' and l.from_node.type == 'TEX_IMAGE':
                    l.from_node.image = bpy.data.images.load(os.path.join(d, 'base_color.png'))
    ob.rotation_mode = 'XYZ'; ob.rotation_euler = (0, 0, math.radians(YAW.get(name, 0)))
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lo, hi = bounds(ob)
    s = HEIGHT[name] / (hi.z - lo.z)
    ob.location = (-(lo.x + hi.x) / 2 * s, -(lo.y + hi.y) / 2 * s, -lo.z * s)
    ob.scale = (s, s, s)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lo, hi = bounds(ob)
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(d, name + '.blend'))
    if name in PEOPLE:
        up = os.path.join(ROOT, 'SourceArt', 'Mixamo', name, 'upload'); os.makedirs(up, exist_ok=True)
        bpy.ops.export_scene.fbx(filepath=os.path.join(up, name + '.fbx'), use_selection=False, object_types={'MESH'},
                                 path_mode='COPY', embed_textures=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False)
    render(name)
    print(f'CLEANUP {name}: {tris} triangles, height {hi.z - lo.z:.3f} m, width {hi.x - lo.x:.2f} m, depth {hi.y - lo.y:.2f} m, '
          f'feet at z {lo.z:.3f}, {len(ob.data.materials)} material(s), scale {s:.3f}')


def render(name):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = (768, 1024) if name in PEOPLE else (1024, 1024)
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    h = HEIGHT[name]
    ob = bpy.data.objects[name]; lo, hi = bounds(ob)
    size = max(h, hi.x - lo.x, hi.y - lo.y)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = size * 1.15
    shots = os.path.join(ROOT, 'Data', 'screenshots', 'round5', 'models') if name in ROUND5 else SHOTS
    os.makedirs(shots, exist_ok=True)
    for tag, ang in (('front', 0), ('side', 90), ('back', 180), ('34', 35)):
        a = math.radians(ang); r = 10
        cam.location = (r * math.sin(a), -r * math.cos(a), h / 2)
        cam.rotation_euler = (math.radians(90), 0, a)
        sc.render.filepath = os.path.join(shots, f'{name}_{tag}.png')
        bpy.ops.render.render(write_still=True)


names = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else list(HEIGHT)
for n in names:
    if os.path.exists(os.path.join(ROOT, 'SourceArt', 'Meshy', n, 'model.glb')):
        clean(n)
    else:
        print(f'CLEANUP {n}: no model.glb yet')
