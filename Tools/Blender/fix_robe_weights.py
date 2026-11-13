"""Shards of Time, Round 12 item 0a: long robes and coats off the arm bones (the low-angle check found the Bethlehem Violinist's robe
and the crowned Wise Man's robe weighted to their forearms: walking swung great sheets of cloth out with the arms).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/fix_robe_weights.py -- <model>=<rigged fbx> ...

Every vertex below the shoulders that Mixamo tied to an arm bone but that is more than R from every arm bone (cloth hanging beside
the arm, not the arm) loses its arm weights; if little is left it gets the nearest two of the hips / spine / thigh / shin bones,
blended by distance. Writes SourceArt/Mixamo/<model>/rigged.fbx (Tools/import_characters.py takes it after rigged_closed /
rigged_hairfix) and prints the counts.
"""
import bpy, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ARM = ('Shoulder', 'Arm', 'ForeArm', 'Hand')
BODY = ('Hips', 'Spine', 'Spine1', 'LeftUpLeg', 'RightUpLeg', 'LeftLeg', 'RightLeg')
R = .12


def log(*a): print('ROBE', *a, flush=True)


def seg_dist(p, a, b):
    ab = b - a; t = max(0., min(1., (p - a).dot(ab) / max(1e-9, ab.dot(ab))))
    return (p - (a + ab * t)).length


def is_arm(name):
    n = name.split(':')[-1]
    return any(n.startswith(side + a) for side in ('Left', 'Right') for a in ARM)


def fix(model, fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    ob = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    pre = arm.data.bones[0].name.split(':')[0] + ':' if ':' in arm.data.bones[0].name else ''
    mw, aw = ob.matrix_world, arm.matrix_world
    bones = {b.name: (aw @ b.head_local, aw @ b.tail_local) for b in arm.data.bones}
    segs = [bones[n] for n in bones if is_arm(n)]
    body = {pre + n: bones[pre + n] for n in BODY if pre + n in bones}
    unit = (bones[pre + 'Head'][0] - bones[pre + 'Hips'][0]).length / .75   # the file's metre (Mixamo files are often in cm)
    shoulder = min(bones[pre + s + 'Shoulder'][0].z for s in ('Left', 'Right')) - .06 * unit
    groups = {g.name: g for g in ob.vertex_groups}
    for n in body:
        if n not in groups: groups[n] = ob.vertex_groups.new(name=n)
    moved = 0
    for v in ob.data.vertices:
        armw = [(g.group, g.weight) for g in v.groups if is_arm(ob.vertex_groups[g.group].name) and g.weight > 0]
        if not armw: continue
        p = mw @ v.co
        if p.z > shoulder or min(seg_dist(p, a, b) for a, b in segs) < R * unit: continue
        left = sum(g.weight for g in v.groups) - sum(w for _, w in armw)
        for gi, _ in armw: ob.vertex_groups[gi].remove([v.index])
        if left < .3:
            near = sorted((seg_dist(p, a, b), n) for n, (a, b) in body.items())[:2]
            w0 = 1 / max(1e-4, near[0][0]); w1 = 1 / max(1e-4, near[1][0])
            groups[near[0][1]].add([v.index], w0 / (w0 + w1), 'ADD'); groups[near[1][1]].add([v.index], w1 / (w0 + w1), 'ADD')
        moved += 1
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    out = os.path.join(ROOT, 'SourceArt', 'Mixamo', model, 'rigged.fbx')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); ob.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             path_mode='COPY', embed_textures=True, bake_anim=False, apply_scale_options='FBX_SCALE_ALL')
    log(f'{model}: {moved} robe vertices off the arm bones (further than {R} m from them, below the shoulders) -> {os.path.relpath(out, ROOT)}')


for a in sys.argv[sys.argv.index('--') + 1:]:
    m, f = a.split('=', 1); fix(m, f)
