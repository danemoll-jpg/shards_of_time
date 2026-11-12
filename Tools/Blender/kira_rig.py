"""Shards of Time, Round 2 item 5: Kira's four-legged rig and clips, by script (Meshy's rigging is humanoid only and Mixamo rigs
only two-legged humanoids).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/kira_rig.py -- kira kira_sabertooth

Reads SourceArt/Meshy/<name>/<name>.blend (Tools/Blender/meshy_cleanup.py: metres, paws at z 0, nose to -Y).
1. Landmarks from the mesh: the four paws (lowest vertices, split front / back, left / right), the body's middle line, the head
   (the front-top lump), the eyes (amber in the texture: they give the way her face looks) and the tail (behind the hips).
2. Skeleton: Root, Hips, Spine, Chest, Neck, Head, Tail1-5, and per leg Upper, Lower, Paw. **No ear bones** (the Kira lock: her
   folded ears are part of the head and can never rise).
0. (Round 5) The mesh is rebuilt closed (closed_mesh.py: voxel remesh, 15k triangles, Meshy's maps baked to
   SourceArt/Rigged/<name>/base_color.png + normal.png) after the landmarks are read from Meshy's mesh.
3. Skin: Blender's bone heat on a closed remeshed copy, copied back to the real mesh (a distance-based skin, limited by region,
   is the fallback). Ears go with the head.
4. Head forward: home Kira's concept had her head turned, Meshy kept it; her neck and head get a fixed counter-turn (at most
   45 deg) in every clip. Only models in COUNTER_TURN get it: the sabertooth (Fix 3.1) is modelled with a straight head, and a
   counter-turn twisted her face.
5. Clips (30 fps): idle, walk, run, jump, fall, land, swim, attack (claw swipe), hurt. In place (the game moves her).
6. LODs: <name>_LOD0 (full), _LOD1 (50 %), _LOD2 (20 %), skinned the same.
Writes SourceArt/Rigged/<name>/model.fbx (mesh + skeleton + every clip as a take), <name>_rig.blend, and
Data/screenshots/<shots>/<name>_<clip>.png (a frame of each clip) and face close-ups <name>_face_<clip>_<view>.png for idle, run
and attack from the front, side and 3/4. Optional argument shots=<folder under Data/screenshots> (default round2/kira_rig).
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector, Quaternion, Matrix
from mathutils.kdtree import KDTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round2', 'kira_rig')
COUNTER_TURN = {'kira', 'kira_mummy'}   # Round 6: mummy Kira's head measures 14 deg turned (home Kira 34)
THIN = {'kira': 1, 'kira_mummy': 1, 'kira_lamb': 1, 'kira_dragon': 1, 'kira_otter': 1}   # Round 10: otter Kira's whiskers   # Round 6: mummy Kira's whiskers too
# Round 5, item 9: the Ice Age creatures get the same four-legged rig. Their real heights (metres): each is rigged at Kira's size
# (0.5 m: every number here was tuned for her), then the armature is scaled back up for the export.
CREATURES = {'wolf': 1.25, 'snow_critter': .8, 'mammoth': 4.4, 'camel': 2.3, 'guardian': 1.7, 'goat': 1.05, 'sheep': .95, 'guard_dog': .85, 'rat': .35, 'horse': 1.6}   # Round 8: the dungeon rat, the wagon pony   # Round 7: Bethlehem's animals   # Round 6: Egypt's camel and temple guardian
TRIS = {'wolf': 12000, 'snow_critter': 8000, 'mammoth': 15000, 'camel': 15000, 'guardian': 12000, 'kira_mummy': 20000, 'goat': 12000, 'sheep': 8000, 'guard_dog': 12000, 'kira_dragon': 16000, 'rat': 8000, 'horse': 15000}   # Meshy budgets (Kira: 20000)   # Round 5: home Kira's whiskers (thin strands) come off in the closed rebuild   # models whose head Meshy built turned (see 4. above)
FPS = 30
# Round 6: mummy Kira's cream bandages pass the eye colour test; her amber eyes are much more saturated
EYE_RB = {'kira_mummy': .33}
TURN_MEASURED = {'kira_mummy': 14.0}   # mummy Kira's head, measured on Meshy's original texture (7 eye points); her amber recolour hides them
LEGS = ('FL', 'FR', 'BL', 'BR')
# Round 8: dragon Kira's bat wings are thin sheets the voxel rebuild would swallow: the faces in this box (at Kira's size: |x| above
# x, y between y, z above z; below `top` only faces standing up (|normal z| < .6), so the back's own skin stays) leave Meshy's mesh
# before the solid is made, come back as a welded, decimated sheet with a reversed twin (two-sided), share the baked maps, and
# follow a wing bone each (WingL / WingR on the Chest; no ear bones)
WINGS = {'kira_dragon': dict(x=.022, y=(-.125, .26), z=.28, top=.32, tris=3000)}
# Round 8: a stronger opening on the head only (box at Kira's size, voxels): dragon Kira's whisker roots lie along her cheeks and brows
THIN_HEAD = {'kira_dragon': (((-.3, -.6, .25), (.3, -.2, .6)), 2)}


def wing_face(rule, c, n):
    return abs(c.x) > rule['x'] and rule['y'][0] < c.y < rule['y'][1] and c.z > rule['z'] and (abs(n.z) < .6 or c.z > rule['top'])


def log(*a): print('KIRA', *a, flush=True)


# ---------------------------------------------------------------------------------------------------------------- landmarks
def landmarks(ob, name):
    me = ob.data; co = [ob.matrix_world @ v.co for v in me.vertices]
    allco = co
    if name in WINGS:   # Round 8: the wings are no part of the back, hips, head or tail
        mw = ob.matrix_world; wv = set()
        for p in me.polygons:
            if wing_face(WINGS[name], mw @ p.center, mw.to_3x3() @ p.normal): wv.update(p.vertices)
        co = [c for i, c in enumerate(co) if i not in wv]
    H = max(c.z for c in co)
    low = [c for c in co if c.z < .035]
    ymid = sum(c.y for c in low) / len(low)
    # four clusters of floor points (k-means from the corners), then front / back by y and left / right by x in each pair
    xs = [c.x for c in low]; ys = [c.y for c in low]
    cs = [Vector((x, y, 0)) for x in (min(xs), max(xs)) for y in (min(ys), max(ys))]
    for _ in range(20):
        groups = [[] for _ in cs]
        for c in low: groups[min(range(4), key=lambda i: (c.xy - cs[i].xy).length)].append(c)
        cs = [sum(g, Vector()) / len(g) if g else cs[i] for i, g in enumerate(groups)]
    cs = sorted(cs, key=lambda c: c.y)                    # front (low y) first
    paws = {}
    for pair, (a, b) in (('F', cs[:2]), ('B', cs[2:])):
        l, r = (a, b) if a.x > b.x else (b, a)            # left = +X (her left when she faces -Y)
        paws[pair + 'L'] = Vector((l.x, l.y, 0)); paws[pair + 'R'] = Vector((r.x, r.y, 0))
    yf = (paws['FL'].y + paws['FR'].y) / 2; yb = (paws['BL'].y + paws['BR'].y) / 2

    def slab(y, w=.02):   # the body's cross-section at y: vertices above the legs
        pts = [c for c in co if abs(c.y - y) < w and c.z > H * .25 and abs(c.x) < .12]
        return (min(p.z for p in pts), max(p.z for p in pts)) if pts else (H * .3, H * .6)
    lo_b, hi_b = slab(yb); lo_f, hi_f = slab(yf)
    hips = Vector((0, yb, (lo_b + hi_b) / 2)); chest = Vector((0, yf, (lo_f + hi_f) / 2))
    # head: the top part in front of the front legs
    top = [c for c in co if c.y < yf + .05 and c.z > H * .55]
    head_c = sum(top, Vector()) / len(top)
    head_top = max(c.z for c in top)
    # eyes: amber pixels on the head
    img = None
    for m in ob.data.materials:
        for n in (m.node_tree.nodes if m and m.use_nodes else []):
            if n.type == 'TEX_IMAGE' and n.image and img is None: img = n.image
    eyes = []
    if img:
        W, Hh = img.size; px = img.pixels[:]; uv = me.uv_layers.active.data
        seen = set()
        for l in me.loops:
            vi = l.vertex_index
            if vi in seen: continue
            u, w = uv[l.index].uv; x = min(W - 1, int(u % 1 * W)); y = min(Hh - 1, int(w % 1 * Hh)); i = (y * W + x) * 4
            r, g, b = px[i:i + 3]
            if r > .7 and g > .55 and r - b > EYE_RB.get(name, .15) and g - b > .1 and r - g < .2 and allco[vi].z > H * .62:   # pale gold, upper head (not the fangs)
                seen.add(vi); eyes.append(allco[vi])
    face = Vector((0, -1, 0)); turn = 0.0
    if len(eyes) >= 6:
        # two eyes: split along their longest horizontal spread
        e0 = sum(eyes, Vector()) / len(eyes)
        ax = max(((1, 0), (0, 1), (.7, .7), (.7, -.7)), key=lambda a: sum(abs((p - e0).x * a[0] + (p - e0).y * a[1]) for p in eyes))
        A = [p for p in eyes if (p - e0).x * ax[0] + (p - e0).y * ax[1] < 0]; B = [p for p in eyes if p not in A]
        ea = sum(A, Vector()) / len(A); eb = sum(B, Vector()) / len(B)
        across = (eb - ea); across.z = 0
        f = Vector((-across.y, across.x, 0)).normalized()
        if f.dot((e0 - head_c).xy.to_3d()) < 0: f = -f
        face = f
        turn = math.atan2(face.x, -face.y)   # 0 = looking along -Y; + = turned toward +X
    if len(eyes) < 6 and name in TURN_MEASURED: turn = math.radians(TURN_MEASURED[name])   # Round 6: measured before the eyes were recoloured
    # tail: behind the hips and above them
    tail = [c for c in co if c.y > yb and c.z > hi_b + .015]
    if not tail:   # Round 5: a hanging tail (the wolf's) is below the back: behind the hind legs instead
        tail = [c for c in co if c.y > yb + .05 and c.z > H * .12]
    if tail:
        base = min(tail, key=lambda c: c.z)
        tip = max(tail, key=lambda c: (c - base).length)
    else:
        base = Vector((0, yb + .05, hi_b)); tip = base + Vector((0, .1, .15))
    L = dict(name=name, H=H, paws=paws, yf=yf, yb=yb, hips=hips, chest=chest, head=head_c, head_top=head_top, face=face, turn=turn,
             tail=tail, tail_base=base, tail_tip=tip, hi_b=hi_b, hi_f=hi_f, eyes=len(eyes))
    log(f'{name}: height {H:.3f}, front paws y {yf:.3f}, back paws y {yb:.3f}, hips z {hips.z:.3f}, chest z {chest.z:.3f}, '
        f'head ({head_c.x:.3f},{head_c.y:.3f},{head_c.z:.3f}), {len(eyes)} eye points, head turned {math.degrees(turn):.0f} deg, '
        f'tail {len(tail)} points, tip z {tip.z:.3f}')
    return L, allco


# ---------------------------------------------------------------------------------------------------------------- wings (Round 8)
def split_wings(ob, rule):
    """(body copy without the wings: the solid's source, the wings as their own two-sided sheet). `ob` keeps everything (the bake's
    source)."""
    import mesh_fix

    def part(keep_wing, nm):
        c = ob.copy(); c.data = ob.data.copy(); c.name = nm; bpy.context.scene.collection.objects.link(c)
        bm = bmesh.new(); bm.from_mesh(c.data); mw = c.matrix_world
        bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=.0004)   # Meshy's triangle soup welded: one surface, wings grown out of the back
        if not keep_wing:   # loose whisker strands (their own pieces once welded) out of the solid's source
            seen = set(); pieces = []
            for f in bm.faces:
                if f in seen: continue
                st = [f]; seen.add(f); pc = []
                while st:
                    g = st.pop(); pc.append(g)
                    for e in g.edges:
                        for h in e.link_faces:
                            if h not in seen: seen.add(h); st.append(h)
                pieces.append(pc)
            pieces.sort(key=len, reverse=True)
            bmesh.ops.delete(bm, geom=[f for pc in pieces[1:] for f in pc], context='FACES')
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if wing_face(rule, mw @ f.calc_center_median(), mw.to_3x3() @ f.normal) != keep_wing], context='FACES')
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
        if not keep_wing:   # the wing roots leave holes in the back: capped, so the solid's flood fill stays outside
            caps = bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)['faces']; log(f'wings: {len(caps)} holes capped in the body')
            # each cap corner takes the UV of the rim face beside it (the fur round the wing root): the caps alone are a second bake
            # source, so the closed body under the wing roots (no Meshy skin there) isn't left black
            lay = bm.loops.layers.uv.active; capset = set(caps)
            for f in caps:
                for l in f.loops:
                    rim = next((k for k in l.vert.link_loops if k.face not in capset), None)
                    if rim is not None: l[lay].uv = rim[lay].uv.copy()
            bm.faces.index_update(); keep = {f.index for f in caps}
            b2 = bm.copy(); b2.faces.ensure_lookup_table()
            bmesh.ops.delete(b2, geom=[f for f in b2.faces if f.index not in keep], context='FACES')
            me2 = c.data.copy(); b2.to_mesh(me2); b2.free()
            cobj = bpy.data.objects.new(ob.name + '_caps', me2); cobj.matrix_world = c.matrix_world
            bpy.context.scene.collection.objects.link(cobj); extra_src.append(cobj)
        else:   # the wing: Meshy's soup has faces flipped at random (light / dark triangles); one way round, lightly smoothed
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
            inner = [v for v in bm.verts if not v.is_boundary]
            for _ in range(2): bmesh.ops.smooth_vert(bm, verts=inner, factor=.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bm.to_mesh(c.data); bm.free()
        return c
    extra_src = []
    body = part(False, ob.name + '_nowings'); wing = part(True, ob.name + '_wings')
    have = sum(len(p.vertices) - 2 for p in wing.data.polygons)
    if have > rule['tris']:
        bpy.ops.object.select_all(action='DESELECT'); wing.select_set(True); bpy.context.view_layer.objects.active = wing
        d = wing.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = rule['tris'] / have; d.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier='dec')
    for p in wing.data.polygons: p.use_smooth = True
    made, total = mesh_fix.double_sheets(wing, log=log)
    g = wing.vertex_groups.new(name='WingSheet'); g.add(list(range(len(wing.data.vertices))), 1.0, 'REPLACE')
    log(f'wings: {have} Meshy triangles -> {total} in the sheet, {made} with a reversed twin (two-sided); body solid from {len(body.data.polygons)} faces')
    return body, wing, extra_src


def wing_marks(ob, L):
    """after the rebuild: the wing sheet's vertices (its group, then the group goes), each side's root and tip for the bones"""
    g = ob.vertex_groups.get('WingSheet')
    if g is None: return
    idx = [v.index for v in ob.data.vertices if any(e.group == g.index for e in v.groups)]
    ob.vertex_groups.remove(g)
    mw = ob.matrix_world; L['wing_idx'] = set(idx); L['wings'] = {}
    for side, sgn in (('L', 1), ('R', -1)):
        pts = [mw @ ob.data.vertices[i].co for i in idx if ob.data.vertices[i].co.x * sgn > 0]
        zlo = min(p.z for p in pts)
        base = [p for p in pts if p.z < zlo + .02]; root = sum(base, Vector()) / len(base); root.x = sgn * max(.012, abs(root.x) * .7)
        far = sorted(pts, key=lambda p: (p - root).length)[-max(5, len(pts) // 20):]; tip = sum(far, Vector()) / len(far)
        L['wings'][side] = (root, tip)
        log(f'wing {side}: {len(pts)} vertices, root ({root.x:.3f},{root.y:.3f},{root.z:.3f}), tip ({tip.x:.3f},{tip.y:.3f},{tip.z:.3f})')


def wing_weights(ob, L):
    """wing vertices: the side's wing bone, blended into the Chest over the first 3 cm from the root; nothing else takes wing bones"""
    mw = ob.matrix_world; names = [g.name for g in ob.vertex_groups]
    for v in ob.data.vertices:
        if v.index in L['wing_idx']:
            p = mw @ v.co; side = 'L' if p.x > 0 else 'R'; root, tip = L['wings'][side]
            ax = (tip - root).normalized(); w = max(0., min(1., ((p - root).dot(ax) - .004) / .03))
            for n in names: ob.vertex_groups[n].remove([v.index])
            ob.vertex_groups['Wing' + side].add([v.index], w, 'REPLACE')
            if w < 1: ob.vertex_groups['Chest'].add([v.index], 1 - w, 'REPLACE')
        else:
            for n in ('WingL', 'WingR'):
                if n in ob.vertex_groups: ob.vertex_groups[n].remove([v.index])
    log(f'wings: {len(L["wing_idx"])} vertices follow WingL / WingR (blended into the Chest at the root)')


# ---------------------------------------------------------------------------------------------------------------- skeleton
def tail_chain(L, n=5):
    base, tail = L['tail_base'], L['tail']
    if len(tail) < 20: return [base + (L['tail_tip'] - base) * (i / n) for i in range(n + 1)]
    # walk out along the tail: points binned by distance from the base, each bin's centre
    d = [(p - base).length for p in tail]; dmax = max(d)
    pts = [base]
    for i in range(1, n + 1):
        lo, hi = dmax * (i - .5) / n, dmax * (i + .5) / n if i < n else dmax + 1
        b = [p for p, x in zip(tail, d) if lo <= x < hi]
        pts.append(sum(b, Vector()) / len(b) if b else pts[-1] + (L['tail_tip'] - pts[-1]) / (n + 1 - i))
    return pts


def build(L):
    arm_data = bpy.data.armatures.new('KiraRig'); arm = bpy.data.objects.new('KiraRig', arm_data)
    bpy.context.scene.collection.objects.link(arm); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT'); eb = arm_data.edit_bones

    def bone(name, h, t, parent=None, connect=False):
        b = eb.new(name); b.head = h; b.tail = t
        if parent: b.parent = eb[parent]; b.use_connect = connect
        return b
    H, hips, chest = L['H'], L['hips'], L['chest']
    bone('Root', Vector((0, 0, 0)), Vector((0, 0, H * .2)))
    mid = (hips + chest) / 2; mid.z = max(hips.z, chest.z) + .01
    bone('Hips', hips, hips + (mid - hips) * .5, 'Root')
    bone('Spine', hips + (mid - hips) * .5, mid, 'Hips', True)
    bone('Chest', mid, chest, 'Spine', True)
    head = L['head']; neck_base = chest + Vector((0, -.02, (L['hi_f'] - chest.z) * .6))
    head_base = Vector((head.x * .5, (neck_base.y + head.y) / 2, (neck_base.z + head.z) / 2))
    bone('Neck', neck_base, head_base, 'Chest')
    bone('Head', head_base, Vector((head.x, head.y, L['head_top'])), 'Neck', True)
    tc = tail_chain(L)
    for i in range(5):
        bone(f'Tail{i + 1}', tc[i], tc[i + 1] if (tc[i + 1] - tc[i]).length > .005 else tc[i] + Vector((0, .02, .02)),
             'Hips' if i == 0 else f'Tail{i}', i > 0)
    for k in LEGS:
        p = L['paws'][k]; front = k[0] == 'F'
        top = Vector((p.x * .8, p.y + (.01 if front else -.01), (chest.z if front else hips.z) - .01))
        knee = Vector((p.x, p.y + (.015 if front else -.03), top.z * .5))   # elbow slightly back, knee forward
        if not front: knee.y = p.y - .03
        ankle = Vector((p.x, p.y + (0 if front else .02), .035))
        toe = Vector((p.x, p.y - .04, .015))
        par = 'Chest' if front else 'Hips'
        bone(f'{k}_Upper', top, knee, par)
        bone(f'{k}_Lower', knee, ankle, f'{k}_Upper', True)
        bone(f'{k}_Paw', ankle, toe, f'{k}_Lower', True)
    for side, (root, tip) in L.get('wings', {}).items():   # Round 8: dragon Kira's wings
        bone('Wing' + side, root, tip, 'Chest')
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm


# ---------------------------------------------------------------------------------------------------------------- skin
def seg_dist(p, a, b):
    ab = b - a; t = max(0., min(1., (p - a).dot(ab) / max(1e-9, ab.dot(ab))))
    return (p - (a + ab * t)).length


def skin(ob, arm, L, co):
    bones = {b.name: (arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local) for b in arm.data.bones if b.name != 'Root'}
    for n in bones: ob.vertex_groups.new(name=n)
    H = L['H']; tail_set = set(id(p) for p in [])
    tail_pts = KDTree(max(1, len(L['tail'])))
    for i, p in enumerate(L['tail']): tail_pts.insert(p, i)
    tail_pts.balance()
    head_r = max(.06, (L['head_top'] - L['head'].z) * 1.5)
    for vi, p in enumerate(co):
        cand = []
        is_tail = L['tail'] and tail_pts.find(p)[2] < 1e-6
        near_head = (p - L['head']).length < head_r and p.y < L['yf'] + .02 and p.z > L['chest'].z
        for n, (a, b) in bones.items():
            leg = n[:2] in LEGS and '_' in n
            if is_tail and not n.startswith('Tail') and n != 'Hips': continue
            if not is_tail and n.startswith('Tail') and n != 'Tail1': continue
            if leg:
                k = n[:2]; front = k[0] == 'F'; left = k[1] == 'L'
                joint = (L['chest'].z if front else L['hips'].z)
                if p.z > joint + .02 or (p.x > 0) != left or (p.y < (L['yf'] + L['yb']) / 2) != front: continue
                # only inside a column round the leg (top joint to paw), so the belly and the other legs stay on the body
                ta, tb = bones[f'{k}_Upper'][0], L['paws'][k]
                hz = Vector((p.x, p.y, 0)); la = Vector((ta.x, ta.y, 0)); lb = Vector((tb.x, tb.y, 0))
                if seg_dist(hz, la, lb) > H * .11 and p.z > H * .12: continue
            if near_head and n not in ('Head', 'Neck'): continue
            if n == 'Head' and not near_head and p.z < L['chest'].z: continue
            cand.append((seg_dist(p, a, b), n))
        cand.sort(); cand = cand[:3]
        ws = [(n, 1 / (d ** 4 + 1e-10)) for d, n in cand]; s = sum(w for _, w in ws)
        for n, w in ws:
            if w / s > .02: ob.vertex_groups[n].add([vi], w / s, 'REPLACE')
    heat(ob, arm, L)
    mod = ob.modifiers.new('Armature', 'ARMATURE'); mod.object = arm
    ob.parent = arm


def heat(ob, arm, L):
    """Blender's bone-heat skinning on a closed remeshed copy (Meshy's mesh has gaps that make bone heat fail), copied back
    onto the real mesh. Leg / tail / head weights from skin() stay as the fallback where heat leaves a vertex empty."""
    arm.data.bones['Root'].use_deform = False
    proxy = ob.copy(); proxy.data = ob.data.copy(); proxy.vertex_groups.clear(); proxy.modifiers.clear()
    bpy.context.scene.collection.objects.link(proxy)
    if L.get('wing_idx'):   # Round 8: the wing sheets (twins on top of each other) make bone heat fail; they get their own weights
        bm = bmesh.new(); bm.from_mesh(proxy.data); bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in L['wing_idx']], context='VERTS'); bm.to_mesh(proxy.data); bm.free()
    r = proxy.modifiers.new('remesh', 'REMESH'); r.mode = 'VOXEL'; r.voxel_size = L['H'] * .012
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = proxy; proxy.select_set(True)
    bpy.ops.object.modifier_apply(modifier='remesh')
    proxy.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    weighted = sum(1 for v in proxy.data.vertices if v.groups)
    log(f'bone heat: {weighted} of {len(proxy.data.vertices)} remeshed vertices weighted')
    if weighted < len(proxy.data.vertices) * .9:
        log('bone heat failed: keeping the distance weights'); bpy.data.objects.remove(proxy); return
    saved = {g.name: g.index for g in ob.vertex_groups}
    for g in proxy.vertex_groups:
        if g.name not in saved: ob.vertex_groups.new(name=g.name)
    dt = ob.modifiers.new('dt', 'DATA_TRANSFER'); dt.object = proxy; dt.use_vert_data = True
    dt.data_types_verts = {'VGROUP_WEIGHTS'}; dt.vert_mapping = 'POLYINTERP_NEAREST'
    dt.layers_vgroup_select_src = 'ALL'; dt.layers_vgroup_select_dst = 'NAME'; dt.mix_mode = 'REPLACE'; dt.mix_factor = 1
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.datalayout_transfer(modifier='dt')
    bpy.ops.object.modifier_apply(modifier='dt')
    bpy.data.objects.remove(proxy)


# ---------------------------------------------------------------------------------------------------------------- clips
def rest_rot(arm, n):
    return arm.data.bones[n].matrix_local.to_quaternion()


def wrot(arm, n, axis, deg):
    """A rotation about a world axis (rest frame) as the bone's local pose rotation."""
    r = rest_rot(arm, n)
    return r.inverted() @ Quaternion(Vector(axis), math.radians(deg)) @ r


def key(arm, frame, pose, hips_z=0.0, hips_y=0.0):
    """pose: {bone: [(axis, deg), ...]} world-axis rotations. Every bone gets a key (so clips don't leak into each other)."""
    for pb in arm.pose.bones:
        q = Quaternion()
        for axis, deg in pose.get(pb.name, []): q = wrot(arm, pb.name, axis, deg) @ q
        pb.rotation_mode = 'QUATERNION'; pb.rotation_quaternion = q
        pb.keyframe_insert('rotation_quaternion', frame=frame)
        pb.location = (0, 0, 0)
        if pb.name == 'Hips':
            # a world up / forward offset in the bone's local frame
            pb.location = rest_rot(arm, 'Hips').inverted() @ Vector((0, hips_y, hips_z))
        pb.keyframe_insert('location', frame=frame)


X, Z = (1, 0, 0), (0, 0, 1)   # X: pitch (legs swing fore / aft), Z: yaw


def base_pose(L):
    if L['name'] not in COUNTER_TURN: return {}
    t = -max(-45, min(45, math.degrees(L['turn'])))   # turn the face back toward -Y (at most 45 deg, so the face never tears): neck 40 %, head 60 %
    return {'Neck': [(Z, t * .4)], 'Head': [(Z, t * .6)]}


def merge(*ps):
    out = {}
    for p in ps:
        for k, v in p.items(): out.setdefault(k, []).extend(v)
    return out


def leg(k, swing, bend):
    """swing: + = paw forward. bend: knee / elbow flex in degrees."""
    front = k[0] == 'F'
    return {f'{k}_Upper': [(X, swing)], f'{k}_Lower': [(X, bend if front else -bend)], f'{k}_Paw': [(X, -bend * .5 if front else bend * .5)]}


def tail_wave(ph, amp, lift=0):
    return {f'Tail{i + 1}': [(Z, amp * math.sin(ph - i * .7)), (X, lift / 5)] for i in range(5)}


def clips(arm, L):
    base = base_pose(L); acts = {}
    sc = bpy.context.scene; sc.render.fps = FPS

    has_wings = 'WingL' in arm.data.bones
    Y = (0, 1, 0)
    # Round 8: the wings' spread (deg, + = out from the back) per clip: a gentle breath, tucked swimming, spread for jumps and glides
    spread = {'idle': lambda t: 3 * s(t), 'walk': lambda t: 3 * s(t, .25), 'run': lambda t: 6 + 8 * s(t), 'jump': lambda t: 35 * min(1, t / .5),
              'fall': lambda t: 40 + 15 * s(t), 'land': lambda t: 20 * math.sin(math.pi * t), 'swim': lambda t: -6,
              'attack': lambda t: 30 * math.sin(math.pi * t), 'hurt': lambda t: 22 * math.sin(math.pi * t), 'stunned': lambda t: 12 + 4 * s(t)}

    def make(name, frames, fn, loop=True):
        act = bpy.data.actions.new(name); act.use_fake_user = True
        arm.animation_data_create(); arm.animation_data.action = act
        for f in range(frames + 1):
            t = f / frames
            pose, hz, hy = fn(t)
            if has_wings:
                a = spread.get(name, lambda t: 0)(t); pose = merge(pose, {'WingL': [(Y, a)], 'WingR': [(Y, -a)]})
            key(arm, f, merge(base, pose), hz, hy)
        acts[name] = (act, frames)
    s = lambda t, ph=0: math.sin(2 * math.pi * (t + ph))
    H = L['H']

    def idle(t):
        return merge(tail_wave(2 * math.pi * t, 10, 4), {'Chest': [(X, 1.5 * s(t))], 'Head': [(X, 2 * s(t, .25))]}), .003 * s(t), 0
    make('idle', 60, idle)

    def walk(t):
        p = {}
        for k, ph in (('BL', 0), ('FL', .25), ('BR', .5), ('FR', .75)):
            p = merge(p, leg(k, 22 * s(t, ph), 35 * max(0, s(t, ph + .25))))
        return merge(p, tail_wave(2 * math.pi * t, 8, 6), {'Spine': [(Z, 3 * s(t))], 'Head': [(X, 2 * s(t, .5) * 2)]}), .008 * abs(s(t)), 0
    make('walk', 32, walk)

    def run(t):
        p = {}
        for k, ph in (('FL', 0), ('FR', .08), ('BL', .5), ('BR', .58)):
            p = merge(p, leg(k, 38 * s(t, ph), 55 * max(0, s(t, ph + .25))))
        flex = 12 * s(t, .1)
        return merge(p, tail_wave(2 * math.pi * t, 5, -10), {'Spine': [(X, flex)], 'Chest': [(X, -flex * .6)], 'Head': [(X, -flex * .4)]}), \
            .02 * s(t, .25) + .01, 0
    make('run', 18, run)

    def jump(t):   # crouch, then stretch out
        c = math.sin(math.pi * min(1, t / .4)) if t < .4 else 0
        e = 0 if t < .4 else min(1, (t - .4) / .3)
        p = {}
        for k in LEGS:
            front = k[0] == 'F'
            p = merge(p, leg(k, (-10 * c) + (35 if front else -45) * e, 50 * c - 10 * e))
        return merge(p, tail_wave(0, 0, 20 * e), {'Spine': [(X, 8 * c - 10 * e)], 'Head': [(X, -10 * e)]}), -.06 * H * c, 0
    make('jump', 15, jump, False)

    def fall(t):
        p = {}
        for k in LEGS:
            front = k[0] == 'F'
            p = merge(p, leg(k, (30 if front else -20) + 4 * s(t, .3 if front else 0), 15 + 5 * s(t)))
        return merge(p, tail_wave(2 * math.pi * t, 15, 30), {'Head': [(X, 8)]}), 0, 0
    make('fall', 20, fall)

    def land(t):
        c = math.sin(math.pi * t)
        p = {}
        for k in LEGS: p = merge(p, leg(k, -6 * c, 55 * c))
        return merge(p, tail_wave(0, 0, -10 * c), {'Spine': [(X, 6 * c)], 'Head': [(X, 6 * c)]}), -.1 * H * c, 0
    make('land', 10, land, False)

    if L['name'] == 'kira_otter':   # Round 10: otter Kira's belly-slide (on her tummy, front paws tucked back, hind legs out behind)
        def slide(t):
            p = {}
            for k in LEGS:
                front = k[0] == 'F'
                p = merge(p, leg(k, (-55 if front else -70) + 3 * s(t, .2 if front else 0), 10))
            return merge(p, tail_wave(2 * math.pi * t, 6, 8), {'Spine': [(X, -4)], 'Neck': [(X, -14)], 'Head': [(X, -8 + 2 * s(t))]}), -.32 * H, 0
        make('slide', 24, slide)

    def swim(t):
        p = {}
        for k, ph in (('FL', 0), ('FR', .5), ('BL', .5), ('BR', 0)):
            p = merge(p, leg(k, 35 * s(t, ph) + (10 if k[0] == 'F' else -25), 30 * max(0, s(t, ph + .25))))
        return merge(p, tail_wave(2 * math.pi * t, 12, -15), {'Chest': [(X, -12)], 'Neck': [(X, -18)], 'Head': [(X, -10)]}), 0, 0
    make('swim', 24, swim)

    def attack(t):   # right front paw up, then a swipe across
        up = math.sin(math.pi * min(1, t / .45)) if t < .45 else max(0, 1 - (t - .45) / .55)
        sw = 0 if t < .3 else math.sin(math.pi * min(1, (t - .3) / .45))
        p = merge(leg('FR', 80 * up, 40 * up), {'FR_Upper': [(Z, 45 * sw)]}, leg('FL', -8 * up, 10 * up), leg('BL', -10 * up, 25 * up),
                  leg('BR', -10 * up, 25 * up))
        return merge(p, tail_wave(0, 0, 25 * up), {'Chest': [(X, -14 * up), (Z, -10 * sw)], 'Head': [(Z, 8 * sw)]}), .02 * up, .01 * up
    make('attack', 12, attack, False)

    def hurt(t):
        c = math.sin(math.pi * t)
        p = {}
        for k in LEGS: p = merge(p, leg(k, -12 * c if k[0] == 'F' else 8 * c, 25 * c))
        return merge(p, tail_wave(0, 0, 40 * c), {'Head': [(X, -25 * c)], 'Neck': [(X, -10 * c)], 'Chest': [(X, -8 * c)]}), -.03 * c, .03 * c
    make('hurt', 12, hurt, False)

    if L['name'] in CREATURES:   # Round 5: stunned (the ground pound): slumped, legs splayed, head wobbling
        def stunned(t):
            p = {}
            bend = 12 if L['name'] == 'snow_critter' else 45   # the critter's stubby legs stretch its fur if bent far
            for k in LEGS: p = merge(p, leg(k, 6 if k[0] == 'F' else -6, bend))
            return merge(p, tail_wave(0, 0, -25), {'Head': [(Z, 18 * s(t)), (X, 14)], 'Neck': [(Z, 8 * s(t, .2)), (X, 10)], 'Chest': [(Z, 4 * s(t))]}), -.08 * H, 0
        make('stunned', 40, stunned)
    return acts


# ---------------------------------------------------------------------------------------------------------------- LODs, export
def lods(ob):
    out = [ob]; ob.name = ob.name + '_LOD0'
    for i, r in ((1, .5), (2, .2)):
        c = ob.copy(); c.data = ob.data.copy(); c.name = ob.name[:-5] + f'_LOD{i}'
        bpy.context.scene.collection.objects.link(c)
        m = c.modifiers.new('dec', 'DECIMATE'); m.ratio = r
        bpy.context.view_layer.objects.active = c
        bpy.ops.object.modifier_move_to_index(modifier='dec', index=0); bpy.ops.object.modifier_apply(modifier='dec')
        out.append(c)
    return out


def render(arm, L, name, acts):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x = sc.render.resolution_y = 640; sc.view_settings.view_transform = 'Standard'
    w = sc.world or bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = .85
    lod0 = next((o for o in bpy.data.objects if o.name.endswith('_LOD0')), None)
    if lod0:   # Round 8: long bodies (dragon Kira's tail, the rat's tail) in frame
        ws = [lod0.matrix_world @ v.co for v in lod0.data.vertices]; ys = [w.y for w in ws]
        cam.data.ortho_scale = max(.85, (max(ys) - min(ys)) * 1.15)
    a = math.radians(55); cam.location = (2 * math.sin(a), -2 * math.cos(a), .22 + .5); cam.rotation_euler = (math.radians(75), 0, a)
    if lod0 and cam.data.ortho_scale > .85:   # Round 8: aimed at the middle of a long body
        cy = (max(ys) + min(ys)) / 2; cam.location = (2 * math.sin(a), cy - 2 * math.cos(a), .22 + .5)
    for o in bpy.data.objects:
        if o.name.endswith(('_LOD1', '_LOD2')): o.hide_render = True
    os.makedirs(SHOTS, exist_ok=True)
    for n, (act, frames) in acts.items():
        arm.animation_data.action = act
        sc.frame_set(int(frames * (.25 if n in ('walk', 'run', 'swim', 'idle', 'fall') else .5)))
        sc.render.filepath = os.path.join(SHOTS, f'{name}_{n}.png'); bpy.ops.render.render(write_still=True)
    face_shots(arm, L, name, acts, cam)
    arm.animation_data.action = None
    for pb in arm.pose.bones: pb.rotation_quaternion = Quaternion(); pb.location = (0, 0, 0)
    sc.frame_set(0)
    sc.render.filepath = os.path.join(SHOTS, f'{name}_rest.png'); bpy.ops.render.render(write_still=True)


def face_shots(arm, L, name, acts, cam):
    """Close-ups of the head (Fix 3.1): front, side, 3/4 in idle, run and attack, the camera aimed at the posed Head bone."""
    sc = bpy.context.scene; scale = cam.data.ortho_scale; cam.data.ortho_scale = .3
    for n in ('idle', 'run', 'attack'):
        act, frames = acts[n]; arm.animation_data.action = act
        sc.frame_set(int(frames * (.5 if n == 'attack' else .25))); bpy.context.view_layer.update()
        pb = arm.pose.bones['Head']; c = arm.matrix_world @ pb.head   # the base of the head: eyes to chin in frame
        for view, ang in (('front', 0), ('side', 90), ('34', 40)):
            a = math.radians(ang); d = Vector((math.sin(a), -math.cos(a), .15)).normalized()
            cam.location = c + d * 2
            cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = os.path.join(SHOTS, f'{name}_face_{n}_{view}.png'); bpy.ops.render.render(write_still=True)
    cam.data.ortho_scale = scale


def run(name):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend'))
    ob = bpy.data.objects[name]
    grow = CREATURES.get(name, .5) / .5
    if grow != 1:   # rig a creature at Kira's size; scaled back up for the export
        ob.scale = (1 / grow,) * 3
        bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    L, co = landmarks(ob, name)
    # Round 5, item 1b: the mesh is rebuilt as one closed surface (Tools/Blender/closed_mesh.py) with Meshy's maps baked onto it,
    # so no camera sees a back face; the landmarks above come from Meshy's mesh (its texture finds the eyes)
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__))); import closed_mesh, hard_parts
    out = os.path.join(ROOT, 'SourceArt', 'Rigged', name)
    # Round 6, item 3: tusks / fangs / claws out of the fur first (clean pieces, a plain material), joined back after the rebuild
    hard, hinfo = hard_parts.split(ob, name, L, log=log)
    if os.path.exists(os.path.join(out, 'hard.txt')) and not hard: os.remove(os.path.join(out, 'hard.txt'))
    body = wing = None; caps = []
    if name in WINGS: body, wing, caps = split_wings(ob, WINGS[name])
    ob = closed_mesh.rebuild(ob, L['H'] * .005, TRIS.get(name, 20000), out, thin=THIN.get(name, 0), solid_src=body, extra=wing,
                             bake_also=caps, reach=.1 if name in WINGS else 0.0, thin_box=(*THIN_HEAD[name][0], THIN_HEAD[name][1]) if name in THIN_HEAD else None, log=log)
    for o in [body] + caps:
        if o is not None: bpy.data.objects.remove(o)
    wing_marks(ob, L)   # 20k: fewer and the thin parts fold through themselves
    first = None
    if hard: first = hard_parts.join(ob, hard_parts.close(hard, hinfo, L, out, name, log=log))
    co = [ob.matrix_world @ v.co for v in ob.data.vertices]
    L['tail'] = [c for i, c in enumerate(co) if c.y > L['yb'] and c.z > L['hi_b'] + .015 and i not in L.get('wing_idx', ())]
    arm = build(L)
    skin(ob, arm, L, co)
    if L.get('wings'): wing_weights(ob, L)
    if first is not None: hard_parts.rigid(ob, first, hinfo, L, log=log)
    acts = clips(arm, L)
    # Round 4, item 1: fur tufts and whiskers are single sheets; the ones seen from behind get a reversed twin (Tools/Blender/mesh_fix.py)
    # (Round 4's reversed twins, mesh_fix.double_sheets, are not needed on a closed mesh: Round 5)
    parts = lods(ob)
    out = os.path.join(ROOT, 'SourceArt', 'Rigged', name); os.makedirs(out, exist_ok=True)
    if grow != 1:
        render(arm, L, name, acts)   # framed for Kira's size
        arm.scale = (grow,) * 3      # the creature's real size (the mesh and LODs ride the armature)
        bpy.context.view_layer.update()
    arm.animation_data.action = acts['idle'][0]
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, name + '_rig.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for o in [arm] + parts: o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, 'model.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             add_leaf_bones=False, path_mode='STRIP', embed_textures=False, apply_scale_options='FBX_SCALE_ALL',
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0)
    tris = [sum(len(p.vertices) - 2 for p in o.data.polygons) for o in parts]
    log(f'{name}: {len(arm.data.bones)} bones (no ear bones), LOD triangles {tris}, clips {list(acts)}, scale x{grow:.2f} -> SourceArt/Rigged/{name}/model.fbx')
    if grow == 1: render(arm, L, name, acts)


for n in sys.argv[sys.argv.index('--') + 1:]:
    if n.startswith('shots='): SHOTS = os.path.join(ROOT, 'Data', 'screenshots', *n[6:].split('/')); continue
    run(n)
