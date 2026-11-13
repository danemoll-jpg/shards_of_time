"""Shards of Time, Round 12 item 0a: look under skirts, dresses and long coats. Renders a rigged person on a few frames of the shared
clips (idle, walk, run, jump) from low front, low side and low back, with its game textures, so anything stretched between the legs
(hair or hem vertices weighted to the wrong bone, a sealed hem) shows. Also prints the vertices that move far from their rest place
when the legs swing (the worst offenders and which bones pull them).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/skirt_check.py -- [out=<folder under Data/screenshots>] <model>=<rigged fbx> ...
Writes <out>/<model>_<clip>_<view>.png (default Data/screenshots/round12/skirts).
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Data', 'screenshots', 'round12', 'skirts')
CLIPS = {'idle': ('Idle.fbx', .3), 'walk': ('Walking.fbx', .3), 'run': ('Fast Run.fbx', .55), 'jump': ('Jumping.fbx', .45)}


def log(*a): print('SKIRT', *a, flush=True)


def load_action(path):
    before = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [a for a in bpy.data.actions if a not in before]
    for o in list(bpy.context.selected_objects): bpy.data.objects.remove(o)
    return new[0] if new else None


def texture(model, meshes):
    if model.endswith('_orig'): model = model[:-5]; dirs = (os.path.join(ROOT, 'SourceArt', 'Meshy', model),)   # the original mesh: Meshy's map
    else: dirs = None
    for d in dirs or (os.path.join(ROOT, 'SourceArt', 'Rigged', model), os.path.join(ROOT, 'SourceArt', 'Meshy', model)):
        p = os.path.join(d, 'base_color.png')
        if os.path.exists(p):
            mat = bpy.data.materials.new(model); mat.use_nodes = True; nt = mat.node_tree
            tc = nt.nodes.new('ShaderNodeTexImage'); tc.image = bpy.data.images.load(p)
            nt.links.new(tc.outputs['Color'], nt.nodes['Principled BSDF'].inputs['Base Color'])
            for o in meshes:
                if o.data.uv_layers:
                    o.data.materials.clear(); o.data.materials.append(mat)
            return p
    return None


def setup(h):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 640, 800
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.85, .86, .8, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((110, 0, 0), 1.2)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.lens = 35
    return cam


def shot(cam, h, ang, path, centre):
    # low: the camera at shin height, 2.6 heights away, looking a little up at the hips
    a = math.radians(ang); r = h * 1.6
    cam.location = (centre.x + r * math.sin(a), centre.y - r * math.cos(a), h * .12)
    d = Vector((centre.x, centre.y, h * .42)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)


def check(model, fbx, out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if arm.animation_data: arm.animation_data_clear()
    tex = texture(model, meshes)
    dg = bpy.context.evaluated_depsgraph_get()
    zs = [(o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices]
    h = max(zs) - min(zs); lo = min(zs)
    centre = Vector((0, 0, 0))
    cam = setup(h)
    os.makedirs(out, exist_ok=True)
    log(f'{model}: height {h:.2f}, texture {os.path.relpath(tex, ROOT) if tex else "none"}')
    for tag, (clip, t) in CLIPS.items():
        p = os.path.join(ROOT, 'SourceArt', 'Mixamo', clip)
        if not os.path.exists(p): log(f'  no {clip}'); continue
        act = load_action(p)
        if not act: continue
        arm.animation_data_create(); arm.animation_data.action = act
        f0, f1 = act.frame_range; bpy.context.scene.frame_set(int(f0 + (f1 - f0) * t))
        dg = bpy.context.evaluated_depsgraph_get()
        # the lowest vertices that hang between the legs: anything under the knees that is between the feet in x and darker than skin
        worst = 0
        for o in meshes:
            ev = o.evaluated_get(dg); me = ev.to_mesh()
            xs = [(ev.matrix_world @ v.co) for v in me.vertices]
            ev.to_mesh_clear()
            # spikes: very long edges (stretched triangles)
            me2 = o.evaluated_get(dg).to_mesh()
            we = None
            for e in me2.edges:
                a_, b_ = me2.vertices[e.vertices[0]].co, me2.vertices[e.vertices[1]].co
                if (a_ - b_).length > worst: worst = (a_ - b_).length; we = (e.vertices[0], e.vertices[1], a_.copy(), b_.copy())
            o.evaluated_get(dg).to_mesh_clear()
        if we:
            def groups(i): return ', '.join(f'{o.vertex_groups[g.group].name} {g.weight:.2f}' for g in sorted(o.data.vertices[i].groups, key=lambda g: -g.weight)[:3])
            log(f'  {tag}: longest edge {worst:.3f} (height {h:.2f}) at {tuple(round(c, 2) for c in we[2])} [{groups(we[0])}] - {tuple(round(c, 2) for c in we[3])} [{groups(we[1])}]')
        for view, ang in (('front', 0), ('side', 90), ('back', 180), ('34', 35)):
            shot(cam, h, ang, os.path.join(out, f'{model}_{tag}_{view}.png'), centre)
        if tag == 'idle':   # close-ups of the legs from behind and the front, low
            for view, ang in (('legsback', 180), ('legsfront', 0)):
                a = math.radians(ang); cam.location = (h * .55 * math.sin(a), -h * .55 * math.cos(a), h * .2)
                cam.rotation_euler = (Vector((0, 0, h * .25)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
                bpy.context.scene.render.filepath = os.path.join(out, f'{model}_{view}.png'); bpy.ops.render.render(write_still=True)


args = sys.argv[sys.argv.index('--') + 1:]
out = OUT
for a in args:
    if a.startswith('out='): out = os.path.join(ROOT, 'Data', 'screenshots', a[4:])
for a in [x for x in args if not x.startswith('out=')]:
    m, f = a.split('=', 1); check(m, f, out)
