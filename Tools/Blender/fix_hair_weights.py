"""Shards of Time, Round 2: give long hair its own skin weights (Mixamo weights Abi's back hair to her arms, so it stretches
out with them).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/fix_hair_weights.py -- <model>=<rigged fbx> ...

1. Find the hair: hair-coloured vertices (brown in the Meshy base-colour texture) above the neck, plus hair-coloured ones Mixamo
   tied to an arm that are more than 8.5 cm from every arm bone (hair beside the arm); grown through mesh edges over
   hair-coloured vertices. Dark spots on the arms are close to the arm bones, so they stay out.
2. Re-weight it: every hair vertex loses all its weights and gets Head / Neck / Spine2 / Spine1 by its height in the bind pose
   (the mesh is stored in its A-pose), blended between neighbouring bones.
3. Soften the seam: hair vertices next to non-hair ones get a mix of the two (two rings, never the arm bones), so nothing tears.
3b. The body under the hair (the sweater's back panel), between the shoulder joints and far from the arm bones, also comes
   off the arm bones.
4. Cut the faces that join hair to an arm (Meshy fuses hair and sleeve; those strips would stretch). They sit behind the hair.
Writes SourceArt/Mixamo/<model>/rigged_hairfix.fbx (rigged, With Skin) and prints the counts.
"""
import bpy, os, sys
from collections import deque

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CHAIN = ('Spine1', 'Spine2', 'Neck', 'Head')     # bottom to top


ARM = ('Shoulder', 'Arm', 'ForeArm', 'Hand')
R = .085                                          # metres: further than this from an arm bone is not arm


def is_hair(c):
    r, g, b = c[0], c[1], c[2]
    return max(r, g, b) < .45 and r >= g >= b and r - b < .2   # brown / near black incl. highlights: not skin, green or red cloth


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
    pre = arm.data.bones[0].name.split(':')[0] + ':'
    img = bpy.data.images.load(os.path.join(ROOT, 'SourceArt', 'Meshy', model, 'base_color.png'))
    W, H = img.size; px = img.pixels[:]
    me = ob.data; uv = me.uv_layers.active.data; mw = ob.matrix_world; aw = arm.matrix_world
    n = len(me.vertices)
    col = [False] * n
    for loop in me.loops:                       # a vertex is hair-coloured if any of its corners is
        if col[loop.vertex_index]: continue
        u, w = uv[loop.index].uv
        x = min(W - 1, max(0, int(u % 1 * W))); y = min(H - 1, max(0, int(w % 1 * H)))
        i = (y * W + x) * 4
        col[loop.vertex_index] = is_hair(px[i:i + 3])
    nb = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices; nb[a].append(b); nb[b].append(a)
    z = [(mw @ v.co).z for v in me.vertices]
    bz = {c: (aw @ arm.data.bones[pre + c].head_local).z for c in CHAIN}
    # 1. hair: grown from the head over hair colour, plus any hair-coloured vertex that Mixamo tied to an arm but that
    #    isn't near an arm bone (hair hanging beside the arm)
    segs = []
    for side in ('Left', 'Right'):
        hs = [aw @ arm.data.bones[pre + side + b].head_local for b in ARM]
        hs.append(aw @ arm.data.bones[pre + side + 'Hand'].tail_local)
        segs += list(zip(hs, hs[1:]))
    hair = [False] * n; q = deque(i for i in range(n) if col[i] and z[i] > bz['Neck'])
    far = 0
    for i in range(n):
        if col[i] and not hair[i] and any(is_arm(ob.vertex_groups[g.group].name) and g.weight > .05 for g in me.vertices[i].groups):
            p = mw @ me.vertices[i].co
            if min(seg_dist(p, a, b) for a, b in segs) > R: hair[i] = True; q.append(i); far += 1
    for i in q: hair[i] = True
    while q:
        i = q.popleft()
        for j in nb[i]:
            if col[j] and not hair[j]: hair[j] = True; q.append(j)
    # 2. new weights by height
    vg = {c: ob.vertex_groups.get(pre + c) or ob.vertex_groups.new(name=pre + c) for c in CHAIN}
    old = {}
    for i in range(n):
        if not hair[i]: continue
        v = me.vertices[i]
        old[i] = [(ob.vertex_groups[g.group], g.weight) for g in v.groups]
        for g, _ in old[i]: g.remove([i])
        zz = z[i]
        if zz >= bz['Head']: w = {'Head': 1}
        elif zz <= bz['Spine1']: w = {'Spine1': 1}
        else:
            for lo, hi in zip(CHAIN, CHAIN[1:]):
                if bz[lo] <= zz <= bz[hi]:
                    t = (zz - bz[lo]) / max(1e-6, bz[hi] - bz[lo]); w = {lo: 1 - t, hi: t}; break
        for c, x in w.items():
            if x > 0: vg[c].add([i], x, 'REPLACE')
    # 3. soften the seam: rings 1 and 2 from non-hair keep 2/3 and 1/3 of their old weights, never the arm bones'
    ring = {}
    edge = [i for i in old if any(not hair[j] for j in nb[i])]
    for i in edge: ring[i] = 1
    for i in edge:
        for j in nb[i]:
            if hair[j] and j not in ring: ring[j] = 2
    for i, r in ring.items():
        keep = 2 / 3 if r == 1 else 1 / 3
        v = me.vertices[i]
        for g in v.groups: g.weight *= (1 - keep)
        rest = [(g, w) for g, w in old[i] if not is_arm(g.name)]; tot = sum(w for _, w in rest)
        if tot <= 0: continue
        for g, w in rest: g.add([i], w / tot * keep, 'ADD')
    # 3b. the body under the hair: a non-hair vertex between the shoulder joints that Mixamo tied to an arm but that is more
    #     than R from every arm bone moves its arm weight to the spine bone at its height (the sweater's back panel)
    inner = min(abs((aw @ arm.data.bones[pre + s_ + 'Arm'].head_local).x) for s_ in ('Left', 'Right'))
    body = 0
    for v in me.vertices:
        if hair[v.index]: continue
        p = mw @ v.co
        if abs(p.x) >= inner: continue
        aw_ = [(g, g.weight) for g in v.groups if is_arm(ob.vertex_groups[g.group].name) and g.weight > 0]
        if not aw_ or min(seg_dist(p, a_, b_) for a_, b_ in segs) <= R: continue
        take = sum(w for _, w in aw_)
        for g, _ in aw_: g.weight = 0
        c = min(CHAIN[:3], key=lambda c: abs(bz[c] - p.z))
        vg[c].add([v.index], take, 'ADD'); body += 1
    print(f'HAIR {model}: {body} body vertices under the hair taken off the arm bones')
    # 4. cut the strips that join hair to sleeve (Meshy fuses them): a face with a hair corner and an arm corner
    armw = [0.] * n
    for v in me.vertices:
        if not hair[v.index]: armw[v.index] = sum(g.weight for g in v.groups if is_arm(ob.vertex_groups[g.group].name))
    cut = [f.index for f in me.polygons if any(hair[i] for i in f.vertices) and any(armw[i] > .5 for i in f.vertices)]
    if cut:
        import bmesh
        bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in cut], context='FACES_ONLY'); bm.to_mesh(me); bm.free()
    print(f'HAIR {model}: cut {len(cut)} faces joining hair to the arms')
    out = os.path.join(ROOT, 'SourceArt', 'Mixamo', model, 'rigged_hairfix.fbx')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             path_mode='COPY', embed_textures=True, bake_anim=False, apply_scale_options='FBX_SCALE_ALL')
    print(f'HAIR {model}: {sum(col)} hair-coloured vertices, {len(old)} in the hair ({far} of them beside the arms), '
          f'{len(ring)} softened at the seam -> {os.path.relpath(out, ROOT)}')


for a in sys.argv[sys.argv.index('--') + 1:]:
    m, f = a.split('=', 1); fix(m, f)
