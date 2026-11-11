"""Shards of Time, Round 8: put a cape (split off by Tools/Blender/split_cape.py) back on the Mixamo-rigged body.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/attach_cape.py -- <model> [rig=<rigged fbx>] [out=<fbx>] [shots=<tag>]

<model>: e.g. violinist_medieval. rig: Dan's Mixamo download of <model>_nocape.fbx (default SourceArt/Mixamo/<model>/T-Pose*.fbx).
1. The cape from SourceArt/Meshy/<model>/<model>_split.blend is placed on the rigged body (the split's body and the rigged body
   are matched by their bounds, so Mixamo's units / offset don't matter).
2. Rigid skin, no cloth: every cape vertex follows the spine chain by its height (Hips -> Spine -> Spine1 -> Spine2 -> Neck), the
   parts over the shoulders partly the shoulder (clavicle) bones. No arm or leg bones, so the cape never stretches with a limb;
   it swings with the body as one piece.
3. The cape's inside gets back faces (mesh_fix.double_sheets) so it isn't see-through from the front; then it is joined to the body
   (same Meshy texture, one material).
Writes SourceArt/Mixamo/<model>/rigged.fbx (default; Tools/import_characters.py takes it before T-Pose.fbx) and renders
Data/screenshots/round8/cape/<tag>_{rest_front,rest_back,walk,run,cheer,punch}.png (the clips: back 3/4 views).
"""
import bpy, math, os, sys
from mathutils import Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mesh_fix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round8', 'cape')
CHAIN = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck']
CLIPS = {'walk': ('Walking.fbx', .3, 150), 'run': ('Fast Run.fbx', .3, 150), 'run_side': ('Fast Run.fbx', .55, 90), 'cheer': ('Cheering (1).fbx', .5, 210),
         'punch': ('Punching.fbx', .45, 210)}


def log(*a): print('ATTACH', *a, flush=True)


def bone(arm, short):
    return next(b for b in arm.data.bones if b.name.split(':')[-1] == short)


def world_bounds(vs):
    return (Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))),
            Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs))))


def evaluated_world(ob):
    dg = bpy.context.evaluated_depsgraph_get(); e = ob.evaluated_get(dg)
    return [e.matrix_world @ v.co for v in e.data.vertices]


def attach(model, rig, out, tag):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=rig)
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    body = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
    if arm.animation_data: arm.animation_data_clear()
    arm.data.pose_position = 'REST'; bpy.context.view_layer.update()   # the mesh is stored in its A-pose: work there
    # the split body and cape (cleaned Meshy space, metres)
    with bpy.data.libraries.load(os.path.join(ROOT, 'SourceArt', 'Meshy', model, model + '_split.blend')) as (src, dst):
        dst.objects = ['body', 'cape']
    ref, cape = dst.objects
    for o in (ref, cape): bpy.context.scene.collection.objects.link(o)
    lo_r, hi_r = world_bounds(evaluated_world(body)); lo_s, hi_s = world_bounds([ref.matrix_world @ v.co for v in ref.data.vertices])
    s = (hi_r.z - lo_r.z) / (hi_s.z - lo_s.z)
    c_r, c_s = (lo_r + hi_r) / 2, (lo_s + hi_s) / 2
    log(f'rig body {hi_r.z - lo_r.z:.3f} tall, {hi_r.x - lo_r.x:.3f} wide, {hi_r.y - lo_r.y:.3f} deep; split body x{s:.4f} -> '
        f'{(hi_s.x - lo_s.x) * s:.3f} wide, {(hi_s.y - lo_s.y) * s:.3f} deep (widths should match if it is the same body)')
    W = [Vector((c_r.x + (p.x - c_s.x) * s, c_r.y + (p.y - c_s.y) * s, lo_r.z + (p.z - lo_s.z) * s))
         for p in (cape.matrix_world @ v.co for v in cape.data.vertices)]
    bpy.data.objects.remove(ref)
    # skin weights by height along the spine chain (rest pose, world space)
    Mw = arm.matrix_world
    zb = [(Mw @ bone(arm, b).head_local).z for b in CHAIN]
    sh = {side: Mw @ bone(arm, side + 'Shoulder').head_local for side in ('Left', 'Right')}
    span = abs(sh['Left'].x - sh['Right'].x)
    for b in CHAIN + ['LeftShoulder', 'RightShoulder']: cape.vertex_groups.new(name=bone(arm, b).name)
    G = {g.name.split(':')[-1]: g for g in cape.vertex_groups}
    for v, p in zip(cape.data.vertices, W):
        w = {}
        if p.z <= zb[0]: w[CHAIN[0]] = 1
        elif p.z >= zb[-1]: w[CHAIN[-1]] = 1
        else:
            for i in range(len(zb) - 1):
                if zb[i] <= p.z <= zb[i + 1]:
                    t = (p.z - zb[i]) / max(1e-6, zb[i + 1] - zb[i]); w[CHAIN[i]] = 1 - t; w[CHAIN[i + 1]] = t; break
        # over the shoulders: part on the clavicle of that side (out from the neck, above the chest)
        k = max(0., min(1., (abs(p.x) - span * .25) / (span * .5))) * max(0., min(1., (p.z - zb[2]) / max(1e-6, zb[3] - zb[2])))
        if k > 0:
            for b in w: w[b] *= 1 - .6 * k
            w[('Left' if (p.x > 0) == (sh['Left'].x > 0) else 'Right') + 'Shoulder'] = .6 * k
        for b, x in w.items():
            if x > 1e-4: G[b].add([v.index], x, 'REPLACE')
    # into the body's place under the armature (as close_person.py does), then its inside made visible
    M = body.matrix_world.copy()
    for v, p in zip(cape.data.vertices, W): v.co = M.inverted() @ p
    cape.parent = arm; cape.matrix_parent_inverse = body.matrix_parent_inverse.copy(); cape.matrix_basis = body.matrix_basis.copy()
    n0 = len(cape.data.polygons)
    mesh_fix.double_sheets(cape, log=log)
    log('bones (rest, z): ' + ', '.join(f'{b.name.split(":")[-1]} {(Mw @ b.head_local).z:.2f}' for b in arm.data.bones
                                         if b.name.split(':')[-1] in CHAIN + ['Head', 'LeftShoulder', 'LeftArm']))
    log(f'cape: {len(cape.data.vertices)} vertices, {n0} -> {len(cape.data.polygons)} faces (inside faces added); '
        f'spine bones at z {", ".join(f"{z:.2f}" for z in zb)}')
    cape.modifiers.new('Armature', 'ARMATURE').object = arm
    render(arm, body, tag)   # the cape still in its own texture here (the same Meshy texture as the body when the rig is its own)
    cape.modifiers.clear()
    cape.data.materials.clear()   # one material for the joined mesh: Unity gives submeshes > 0 the plain "hard parts" material
    for m in body.data.materials: cape.data.materials.append(m)
    bpy.ops.object.select_all(action='DESELECT'); cape.select_set(True); body.select_set(True)
    bpy.context.view_layer.objects.active = body; bpy.ops.object.join()
    arm.data.pose_position = 'POSE'; bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); body.select_set(True); bpy.context.view_layer.objects.active = arm
    os.makedirs(os.path.dirname(out), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             path_mode='COPY', embed_textures=True, bake_anim=False, apply_scale_options='FBX_SCALE_ALL')
    log(f'{model}: {sum(len(p.vertices) - 2 for p in body.data.polygons)} triangles with the cape -> {os.path.relpath(out, ROOT)}')


def load_action(path):
    before = set(bpy.data.actions); objs = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    act = [a for a in bpy.data.actions if a not in before][0]
    for o in [o for o in bpy.context.scene.objects if o not in objs]: bpy.data.objects.remove(o)
    return act


def render(arm, body, tag):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 640, 800
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    vs = evaluated_world(body); lo, hi = world_bounds(vs); h = hi.z - lo.z; cz = (lo.z + hi.z) / 2
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = h * 1.35
    os.makedirs(SHOTS, exist_ok=True)

    def shot(ang, name):
        a = math.radians(ang); d = h * 6
        cam.location = (d * math.sin(a), -d * math.cos(a), cz); cam.rotation_euler = (math.radians(90), 0, a)
        cam.data.clip_end = d * 3
        sc.render.filepath = os.path.join(SHOTS, f'{tag}_{name}.png'); bpy.ops.render.render(write_still=True)
    shot(0, 'rest_front'); shot(180, 'rest_back')
    arm.data.pose_position = 'POSE'
    for name, (clip, t, ang) in CLIPS.items():
        p = os.path.join(ROOT, 'SourceArt', 'Mixamo', clip)
        if not os.path.exists(p): continue
        act = load_action(p)
        arm.animation_data_create(); arm.animation_data.action = act
        if hasattr(arm.animation_data, 'action_slot') and act.slots: arm.animation_data.action_slot = act.slots[0]
        f0, f1 = act.frame_range; sc.frame_set(int(f0 + (f1 - f0) * t))
        shot(ang, name)
    arm.animation_data.action = None; arm.data.pose_position = 'REST'; sc.frame_set(0)


args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
model = args[0] if args and '=' not in args[0] else 'violinist_medieval'
opt = dict(a.split('=', 1) for a in args if '=' in a)
mdir = os.path.join(ROOT, 'SourceArt', 'Mixamo', model)
rig = opt.get('rig') or next((os.path.join(mdir, f) for f in sorted(os.listdir(mdir)) if f.startswith('T-Pose') and f.endswith('.fbx')), None)
if not rig: raise SystemExit(f'ATTACH {model}: no rigged file yet ({mdir}/T-Pose.fbx)')
attach(model, os.path.abspath(rig), os.path.abspath(opt.get('out') or os.path.join(mdir, 'rigged.fbx')), opt.get('shots', model))
