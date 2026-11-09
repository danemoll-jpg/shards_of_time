"""Shards of Time, Round 4 items 4 and 8: the Ice Age valley's terrain, the distant volcanoes and the stylized props, by script.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/iceage_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height, so the game's coordinates read the same):
  Assets/Resources/World/iceage_terrain.fbx    the valley: Terrain__Snow (smooth) and Terrain__Rock (faceted cliffs), one
                                               mesh each (the game gives them mesh colliders)
  Assets/Resources/World/iceage_volcanoes.fbx  two volcanoes far to the north (Volcano__Rock, Volcano__Snow, Volcano__Lava)
  Assets/Resources/World/iceage_props.fbx      one empty per prop, its parts named <part>__<Material>: Rock1-3, Stone (standing
                                               stone), Pine, Bones, IceCrystal, Fiddle (the Violinist's bone fiddle)
  Data/screenshots/round4/world/terrain_top.png  a map of the valley from above
Materials are only names: the game colours each part by the word after "__" (World/WorldArt.cs).

The layout (the same numbers as World/IceAge.cs, which places the gameplay on top by raycasting onto this ground):
  the valley floor |x| < ~64 from the south wall (z -30) to the stone circle (z 490), cliffs either side;
  the pond (22, 55), r 13, 3.2 m deep; the gorge across the valley at z 240..264 (floor -10 at z 246..258) with a ramp
  out on the south side at x 44..54; the ice cave's footprint (x -62..-24, z 312..368) sunk to -4.5 (the cave's floor and
  the pit under its cracked ice are built by the game); the frozen well (18, 478) r 3; flat spots at the start and the
  stone circle; foothills rising north of z 520.
"""
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round4', 'world')
X0, X1, Z0, Z1, STEP = -170.0, 170.0, -70.0, 600.0, 2.5


def sstep(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


def n2(x, z, s, o=0.0):
    return noise.noise(Vector((x / s + o, z / s - o, .37 * o)))


def height(x, z):
    h = .55 * n2(x, z, 45) + .25 * n2(x, z, 17, 3.1)
    # side cliffs: the valley's half-width wobbles; past it the ground rises steeply into rock
    half = 64 + 6 * math.sin(z / 41.0) + 4 * n2(0, z, 30, 7.7)
    d = abs(x) - half
    if d > 0: h += sstep(d / 14.0) * 30 + d * .35 + 4 * abs(n2(x, z, 9, 1.3)) * sstep(d / 6.0)
    # the south wall behind the start, the foothills to the north
    if z < -30: h += (-30 - z) * 1.6 + 3 * abs(n2(x, z, 8, 2.2))
    if z > 520: h += sstep((z - 520) / 80.0) * 28 + (z - 520) * .12
    # flat spots: the start, the stone circle, the mammoth ridge's loading rock, the vents' foot
    for cx, cz, r in ((0, 12, 22), (0, 490, 24), (40, 165, 30), (45, 420, 22)):
        k = sstep(1 - math.hypot(x - cx, z - cz) / r)
        h = h * (1 - k) + .05 * k
    # the pond: a smooth bowl
    r = math.hypot(x - 22, z - 55)
    if r < 18: h = h * sstep((r - 13) / 5.0) if r > 13 else 0
    if r < 13: h -= 3.2 * (1 - (r / 13.0) ** 2)
    # the gorge across the valley: steep walls 240..246 and 258..264, floor -10 between
    dz = abs(z - 252)
    if dz < 12: h = h * sstep((dz - 6) / 6.0) - 10 * sstep((12 - dz) / 6.0)
    # the ramp out of the gorge on the south side (x 44..54): from the floor at z 246 up to the bank at z 226
    if 226 <= z <= 247 and 42 <= x <= 56:
        side = sstep(min(x - 42, 56 - x) / 2.0)
        ramp = -10 + (246 - z) * .5
        h = min(h, h * (1 - side) + ramp * side) if z < 246 else h * (1 - side) + -10 * side
    # the ice cave's footprint (the game builds the floor at 0 and the pit under its cracked ice)
    if -64 <= x <= -22 and 310 <= z <= 370:
        k = sstep(min(x + 64, -22 - x, z - 310, 370 - z) / 2.0)
        h = h * (1 - k) - 4.5 * k
    # the frozen well near the stone circle
    r = math.hypot(x - 18, z - 478)
    if r < 4.5: h = h * sstep((r - 3) / 1.5) - 3.5 * (1 - sstep((r - 3) / 1.5))
    return h


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def export(path, objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, apply_scale_options='FBX_SCALE_ALL',
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    print('WORLD wrote', os.path.relpath(path, ROOT), sum(len(o.data.polygons) for o in objs if o.type == 'MESH'), 'faces')


def mesh_obj(name, bm, smooth=False, parent=None):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    if parent: ob.parent = parent
    return ob


# ---------------------------------------------------------------------------------------------------------------- terrain
def terrain():
    clear()
    nx = int((X1 - X0) / STEP) + 1; nz = int((Z1 - Z0) / STEP) + 1
    H = [[height(X0 + i * STEP, Z0 + j * STEP) for i in range(nx)] for j in range(nz)]
    parts = {'Snow': bmesh.new(), 'Rock': bmesh.new()}
    verts = {k: {} for k in parts}

    def v(k, i, j):
        d = verts[k]
        if (i, j) not in d: d[(i, j)] = parts[k].verts.new((X0 + i * STEP, Z0 + j * STEP, H[j][i]))
        return d[(i, j)]

    rnd = random.Random(4)
    for j in range(nz - 1):
        for i in range(nx - 1):
            for tri in (((i, j), (i + 1, j), (i + 1, j + 1)), ((i, j), (i + 1, j + 1), (i, j + 1))):
                p = [Vector((X0 + a * STEP, Z0 + b * STEP, H[b][a])) for a, b in tri]
                n = (p[1] - p[0]).cross(p[2] - p[0]).normalized()
                cx = sum(q.x for q in p) / 3; cz = sum(q.y for q in p) / 3
                slope = math.degrees(math.acos(max(-1, min(1, n.z))))
                # rock on steep faces (snow clings a little further up on the gentle tops); the gorge walls are rock
                rock = slope > 36 + 6 * n2(cx, cz, 11, 5.5) or (abs(cz - 252) < 11 and slope > 24)
                k = 'Rock' if rock else 'Snow'
                parts[k].faces.new([v(k, a, b) for a, b in tri])
    obs = [mesh_obj('Terrain__Snow', parts['Snow'], smooth=True), mesh_obj('Terrain__Rock', parts['Rock'], smooth=False)]
    export(os.path.join(OUT, 'iceage_terrain.fbx'), obs)
    # a map from above (for the layout)
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'; sc.render.resolution_x = 680; sc.render.resolution_y = 1340
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'OBJECT'
    obs[0].color = (.92, .95, 1, 1); obs[1].color = (.5, .4, .32, 1)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = Z1 - Z0; cam.location = (0, (Z0 + Z1) / 2, 300); cam.data.clip_end = 1000
    os.makedirs(SHOTS, exist_ok=True); sc.render.filepath = os.path.join(SHOTS, 'terrain_top.png'); bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------------------------------------------------------- volcanoes
def cone(name, cx, cy, r, h, crater, seed, parent):
    bm = bmesh.new(); rings, segs = 22, 64; rnd = random.Random(seed)
    grid = []
    for a in range(rings + 1):
        t = a / rings; row = []
        for s in range(segs):
            ang = s / segs * math.tau
            rr = r * (1 - t) + crater * t
            wob = 1 + .07 * noise.noise(Vector((math.cos(ang) * 2 + seed, math.sin(ang) * 2, t * 3)))
            z = h * (1 - (1 - t) ** 1.7) if t < 1 else h
            row.append(bm.verts.new((cx + math.cos(ang) * rr * wob, cy + math.sin(ang) * rr * wob, z)))
        grid.append(row)
    bot = bm.verts.new((cx, cy, h - crater * .6))
    snow = bmesh.new()
    for a in range(rings):
        for s in range(segs):
            bm.faces.new((grid[a][s], grid[a][(s + 1) % segs], grid[a + 1][(s + 1) % segs], grid[a + 1][s]))
    for s in range(segs): bm.faces.new((grid[rings][(s + 1) % segs], grid[rings][s], bot))
    return mesh_obj(name, bm, smooth=False, parent=parent)


def volcanoes():
    clear()
    root = bpy.data.objects.new('Volcanoes', None); bpy.context.scene.collection.objects.link(root)
    obs = [root]
    for i, (cx, cy, r, h) in enumerate(((-130, 830, 300, 230), (120, 890, 340, 270))):
        ob = cone(f'Volcano{i}__Basalt', cx, cy, r, h, r * .1, i * 13 + 1, root)
        # snow on the upper slopes: a slightly bigger shell over the band between 45% and 85% of the height (faceted)
        snow = cone(f'Volcano{i}Snow__Snow', cx, cy, r * .62, h * .78, r * .24, i * 13 + 1, root)
        snow.location.z = h * .22 + .5
        # lava: a glowing ring in the crater and four streaks down the sides
        bm = bmesh.new(); segs = 32; cr = r * .1
        top = [bm.verts.new((cx + math.cos(a / segs * math.tau) * cr * 1.05, cy + math.sin(a / segs * math.tau) * cr * 1.05, h + 1)) for a in range(segs)]
        inner = [bm.verts.new((cx + math.cos(a / segs * math.tau) * cr * .7, cy + math.sin(a / segs * math.tau) * cr * .7, h - cr * .3)) for a in range(segs)]
        for a in range(segs): bm.faces.new((top[a], top[(a + 1) % segs], inner[(a + 1) % segs], inner[a]))
        rnd = random.Random(i)
        for k in range(4):
            ang = (k + rnd.random() * .5) / 4 * math.tau + (math.pi * .5 if i else 0)
            prev = None
            for st in range(12):
                t = st / 11; rr = cr + (r * .55 - cr) * t
                tc = (r - rr) / (r - cr); z = h * (1 - (1 - tc) ** 1.7) + 1.5   # on the cone's surface (as cone() builds it)
                wob = math.sin(st * .9 + k) * .06
                a2 = ang + wob; w = 6 * (1 - t * .6)
                p = Vector((cx + math.cos(a2) * rr, cy + math.sin(a2) * rr, z)); side = Vector((-math.sin(a2), math.cos(a2), 0)) * w / 2
                cur = (bm.verts.new(p - side), bm.verts.new(p + side))
                if prev: bm.faces.new((prev[0], prev[1], cur[1], cur[0]))
                prev = cur
        obs += [ob, snow, mesh_obj(f'Volcano{i}Lava__Lava', bm, parent=root)]
    export(os.path.join(OUT, 'iceage_volcanoes.fbx'), obs)


# ---------------------------------------------------------------------------------------------------------------- props
def lumpy(name, size, seed, flatten=.3, subdiv=2, parent=None, stretch=(1, 1, 1)):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1)
    rnd = random.Random(seed)
    for vv in bm.verts:
        d = 1 + .22 * noise.noise(vv.co * 1.7 + Vector((seed, seed * .5, 0)))
        vv.co = Vector((vv.co.x * stretch[0], vv.co.y * stretch[1], vv.co.z * stretch[2])) * d * size
    lo = min(vv.co.z for vv in bm.verts)
    for vv in bm.verts:
        if vv.co.z < lo + size * flatten: vv.co.z = lo + size * flatten
        vv.co.z -= lo + size * flatten
    return mesh_obj(name, bm, smooth=False, parent=parent)


def empty(name, parent=None):
    e = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(e)
    if parent: e.parent = parent
    return e


def cyl(name, r0, r1, h, segs, parent, z0=0.0, smooth=False, cap=True):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=segs, radius1=r0, radius2=r1, depth=h)
    for vv in bm.verts: vv.co.z += h / 2 + z0
    return mesh_obj(name, bm, smooth=smooth, parent=parent)


def props():
    clear()
    obs = []
    for i, (s, st) in enumerate(((1.0, (1.2, 1, .8)), (1.0, (1, 1.3, 1)), (1.0, (1.5, 1.1, .6)))):
        e = empty(f'Rock{i + 1}'); obs += [e, lumpy(f'Rock{i + 1}Body__Rock', s, i * 7 + 2, parent=e, stretch=st)]
        cap = lumpy(f'Rock{i + 1}Cap__Snow', s * .8, i * 7 + 2, flatten=.62, parent=e, stretch=(st[0] * 1.05, st[1] * 1.05, st[2] * .9))
        cap.location.z = s * st[2] * .55; obs.append(cap)
    # a standing stone: a tall, slightly leaning slab, 1 m wide, 3.2 m tall
    e = empty('Stone'); obs.append(e)
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for vv in bm.verts:
        vv.co = Vector((vv.co.x * 1.0, vv.co.y * .55, (vv.co.z + .5) * 3.2))
        if vv.co.z > 2: vv.co.x *= .8; vv.co.y *= .85
        vv.co += Vector((.06, .04, 0)) * noise.noise(vv.co * 2.1)
    obs.append(mesh_obj('StoneBody__Stone', bm, parent=e))
    # a pine: trunk + three tiers of needles, each with a snow cap
    e = empty('Pine'); obs.append(e)
    obs.append(cyl('PineTrunk__Bark', .28, .18, 2.2, 8, e))
    for k, (r, h, z) in enumerate(((2.2, 3.0, 1.4), (1.7, 2.6, 3.0), (1.1, 2.2, 4.5))):
        obs.append(cyl(f'PineTier{k}__Pine', r, 0, h, 9, e, z0=z))
        obs.append(cyl(f'PineSnow{k}__Snow', r * .55, 0, h * .5, 9, e, z0=z + h * .5 + .04))
    # mammoth bones: a curved tusk and three ribs sticking out of the snow
    e = empty('Bones'); obs.append(e)
    for k in range(4):
        bm = bmesh.new(); segs = 10; rings = 8; prev = None
        R, w = (2.4, .22) if k == 0 else (1.5, .12)
        for a in range(rings + 1):
            t = a / rings * (math.pi * (.6 if k == 0 else .9)); c = Vector((math.cos(t) * R - R, k * .9 if k else 0, math.sin(t) * R))
            if k == 0: c = Vector((c.x, -1.5, c.z * .8))
            ring = [bm.verts.new(c + Vector((0, math.cos(s / segs * math.tau), math.sin(s / segs * math.tau))) * w * (1 - a / rings * .6)) for s in range(segs)]
            if prev:
                for s in range(segs): bm.faces.new((prev[s], prev[(s + 1) % segs], ring[(s + 1) % segs], ring[s]))
            prev = ring
        obs.append(mesh_obj(f'Bone{k}__Bone', bm, smooth=True, parent=e))
    # an ice crystal cluster
    e = empty('IceCrystal'); obs.append(e)
    rnd = random.Random(9)
    for k in range(5):
        bm = bmesh.new()
        h = rnd.uniform(1.2, 2.6); r = rnd.uniform(.25, .45)
        bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=r, radius2=r * .7, depth=h)
        tip = bm.verts.new((0, 0, h / 2 + r * 1.2))
        top = [vv for vv in bm.verts if abs(vv.co.z - h / 2) < 1e-4]
        for f in [f for f in bm.faces if all(abs(vv.co.z - h / 2) < 1e-4 for vv in f.verts)]: bm.faces.remove(f)
        top.sort(key=lambda vv: math.atan2(vv.co.y, vv.co.x))
        for a in range(len(top)): bm.faces.new((top[a], top[(a + 1) % len(top)], tip))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        tilt = Vector((rnd.uniform(-.5, .5), rnd.uniform(-.5, .5), 1)).normalized()
        rot = Vector((0, 0, 1)).rotation_difference(tilt).to_matrix()
        for vv in bm.verts: vv.co = rot @ (vv.co + Vector((0, 0, h / 2))) + Vector((rnd.uniform(-.5, .5), rnd.uniform(-.5, .5), -.2))
        obs.append(mesh_obj(f'Crystal{k}__Ice', bm, parent=e))
    # the Violinist's bone fiddle (held under the chin; ~0.6 m) and its bow
    e = empty('Fiddle'); obs.append(e)
    body = lumpy('FiddleBody__Bone', .14, 21, flatten=0, subdiv=2, parent=e, stretch=(1, 2.2, .7)); obs.append(body)
    obs.append(cyl('FiddleNeck__Bone', .03, .025, .32, 6, e, z0=0)); obs[-1].rotation_euler = (math.radians(-90), 0, 0); obs[-1].location = (0, .28, .02)
    obs.append(cyl('FiddleBow__Bark', .012, .012, .7, 5, e)); obs[-1].rotation_euler = (0, math.radians(90), math.radians(20)); obs[-1].location = (-.35, .05, .12)
    export(os.path.join(OUT, 'iceage_props.fbx'), obs)


terrain()
volcanoes()
props()
