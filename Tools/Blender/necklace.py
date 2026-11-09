"""Shards of Time, Round 2 item 8: Dan's necklace as its own small hard-surface model (a cord and the shard; the game makes the
shard glow and pulse with the necklace logic).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/necklace.py

Sized for Dan (1.8 m): the cord is a loop round the base of the neck (origin = the neck's centre at the collar, Blender -Y = his
front) that drops in a soft V to the shard on his chest. Three objects: Cord (leather), Bail (the little ring) and Shard (a
six-sided crystal, its own object so the game can glow / pulse it on its own).
Writes Assets/Resources/Characters/necklace.fbx and Data/screenshots/round2/necklace.png.
"""
import bpy, bmesh, math, os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
NECK_R = .075          # the loop's radius round the neck (m)
DROP = .17             # how far the shard hangs below the collar, at the front
CORD_R = .0025         # cord thickness (radius)
SHARD_H, SHARD_R = .05, .013


def mat(name, rgb, emit=0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value = (*rgb, 1)
    if emit:
        b.inputs['Emission'].default_value = (*rgb, 1)
        if 'Emission Strength' in b.inputs: b.inputs['Emission Strength'].default_value = emit
    return m


def cord():
    # a closed loop: round the neck at the back and sides, sagging into a V at the front (y = -NECK_R side)
    pts = []
    n = 64
    for i in range(n):
        a = 2 * math.pi * i / n                     # a = 0 at the front (-Y)
        x = NECK_R * 1.15 * math.sin(a); y = -NECK_R * math.cos(a)
        front = max(0., math.cos(a))                # 1 at the front, 0 at the sides and back
        z = -DROP * front ** 2.2 + .012 * (1 - front)
        y -= .03 * front ** 2                       # lies forward on the chest
        pts.append((x, y, z))
    cu = bpy.data.curves.new('Cord', 'CURVE'); cu.dimensions = '3D'
    sp = cu.splines.new('POLY'); sp.points.add(len(pts) - 1)
    for p, c in zip(sp.points, pts): p.co = (*c, 1)
    sp.use_cyclic_u = True
    cu.bevel_depth = CORD_R; cu.bevel_resolution = 2
    ob = bpy.data.objects.new('Cord', cu); bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.convert(target='MESH'); ob.select_set(False)
    ob.data.materials.append(mat('NecklaceCord', (.23, .14, .08)))
    return ob, Vector(pts[0])


def bail(at):
    bpy.ops.mesh.primitive_torus_add(major_radius=.005, minor_radius=.0013, major_segments=16, minor_segments=6,
                                     location=at + Vector((0, 0, -.004)), rotation=(0, math.radians(90), 0))
    ob = bpy.context.active_object; ob.name = 'Bail'
    ob.data.materials.append(mat('NecklaceBail', (.75, .62, .35))); return ob


def shard(top):
    # a six-sided crystal: a short point on top, a long point below, slightly flattened front to back
    bm = bmesh.new()
    ring = [bm.verts.new((SHARD_R * math.cos(2 * math.pi * i / 6), SHARD_R * .7 * math.sin(2 * math.pi * i / 6), -SHARD_H * .3)) for i in range(6)]
    t = bm.verts.new((0, 0, 0)); b = bm.verts.new((0, 0, -SHARD_H))
    for i in range(6):
        j = (i + 1) % 6
        bm.faces.new((t, ring[j], ring[i])); bm.faces.new((b, ring[i], ring[j]))
    me = bpy.data.meshes.new('Shard'); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new('Shard', me); bpy.context.scene.collection.objects.link(ob)
    ob.location = top + Vector((0, 0, -.009))
    ob.data.materials.append(mat('NecklaceShard', (.45, .9, 1.0), emit=2)); return ob


def render(objs):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x = sc.render.resolution_y = 768; sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = 3; l.rotation_euler = (math.radians(50), 0, math.radians(30))
    sc.collection.objects.link(l)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = .32
    a = math.radians(25); cam.location = (math.sin(a), -math.cos(a), -.1 + .2); cam.rotation_euler = (math.radians(79), 0, a)
    os.makedirs(os.path.join(ROOT, 'Data', 'screenshots', 'round2'), exist_ok=True)
    sc.render.filepath = os.path.join(ROOT, 'Data', 'screenshots', 'round2', 'necklace.png'); bpy.ops.render.render(write_still=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
c, front = cord()
b = bail(front)
s = shard(front + Vector((0, 0, -.004)))
out = os.path.join(ROOT, 'Assets', 'Resources', 'Characters'); os.makedirs(out, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=os.path.join(out, 'necklace.fbx'), use_selection=True, object_types={'MESH'},
                         apply_scale_options='FBX_SCALE_ALL', bake_anim=False)
tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in (c, b, s)}
print(f'NECKLACE: {tris} triangles; loop radius {NECK_R} m, shard {SHARD_H * 100:.0f} cm, {DROP * 100:.0f} cm below the collar '
      f'-> Assets/Resources/Characters/necklace.fbx')
render((c, b, s))
