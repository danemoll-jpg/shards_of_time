"""Shards of Time, Round 6 items 7 and 12: Ancient Egypt's ground, its pyramids and its stylized props, by script.

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/egypt_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height, as iceage_world.py):
  Assets/Resources/World/egypt_terrain.fbx   Terrain__Sand (dunes, the flat town and sites, the Nile's bed and banks), FarSand (the
                                             desert out to the far pyramids, no collider needed)
  Assets/Resources/World/egypt_pyramids.fbx  Pyramid1, Pyramid2 (the two great ones, far north, gold caps) and BuildPyramid (the one
                                             under construction: nine 4 m tiers up to an unfinished 12 m top at 36 m; mesh collider in game)
  Assets/Resources/World/egypt_props.fbx     one empty per prop, its parts <part>__<Material>: House1-4 (pastel), Dome, Palm, Obelisk,
                                             Stall (striped awning), Basket, Pot, Block, Cart, Reeds, Boat, Column, CatStatue, Crate, Plank,
                                             Pole, Sarcophagus, Brazier, WallSeg
  Data/screenshots/round6/world/egypt_top.png   a map from above
Materials are only names: the game colours each part by the word after "__" (World/WorldArt.cs).

The layout (the same numbers as World/Egypt.cs):
  the town, flat at 0: x -70..70, z -20..125 (the arrival plaza z 0..20, the main street x -6..6, the market z 50..86)
  the Nile: water x -150..-105 (top -0.6), its bed -5, a deep pool r 8 at (-128, 77) down to -17; the far bank x -175..-150, flat
  the quarry road: x 40..100, z 30..190, flat (the quicksand pit x 45..75, z 138..162 dips 0.4 m)
  the camel's route: (24, 128) -> (14, 150) -> (-6, 172) -> (-24, 186), a flat band 10 m wide
  the Sphinx's site: x -95..-20, z 188..242, flat; the pyramid under construction: x 30..110, z 185..275, flat
  dunes everywhere else (up to ~6 m); two great pyramids far north: (130, 430) base 170 h 105, (-70, 480) base 130 h 80
"""
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round6', 'world')
X0, X1, Z0, Z1, STEP = -200.0, 170.0, -80.0, 360.0, 2.5
COL = {'Sand': (.93, .78, .52), 'Sandstone': (.87, .7, .48), 'Gold': (.95, .72, .25), 'PastelPink': (.98, .68, .72), 'PastelTeal': (.45, .82, .8),
       'PastelLilac': (.75, .65, .9), 'PastelGold': (.98, .82, .45), 'PastelPeach': (.99, .78, .6), 'Turquoise': (.2, .72, .75),
       'AwningRed': (.85, .25, .25), 'AwningWhite': (.97, .95, .9), 'AwningBlue': (.2, .4, .8), 'Wicker': (.72, .55, .32), 'Palm': (.25, .55, .25),
       'Bark': (.42, .28, .18), 'Wood': (.45, .28, .16), 'Reed': (.55, .6, .3), 'Plaster': (.96, .92, .85), 'Dark': (.15, .12, .1),
       'Terracotta': (.78, .42, .28), 'Lapis': (.12, .25, .7), 'Stone': (.58, .55, .53)}


def sstep(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


def n2(x, z, s, o=0.0):
    return noise.noise(Vector((x / s + o, z / s - o, .37 * o)))


def box_mask(x, z, x0, x1, z0, z1, soft):
    """1 inside the box, 0 more than `soft` metres outside it"""
    dx = max(x0 - x, 0, x - x1); dz = max(z0 - z, 0, z - z1)
    return 1 - sstep(math.hypot(dx, dz) / soft)


ROUTE = [(24, 128), (14, 150), (-6, 172), (-24, 186)]


def seg_d(x, z, a, b):
    ax, az = a; bx, bz = b; vx, vz = bx - ax, bz - az
    t = max(0, min(1, ((x - ax) * vx + (z - az) * vz) / (vx * vx + vz * vz)))
    return math.hypot(x - ax - vx * t, z - az - vz * t)


def height(x, z):
    dune = 2.6 * (n2(x, z, 38, 1.3) + 1) + 1.4 * (n2(x, z, 15, 4.1) + .6)
    dune = max(0.0, dune)
    flat = max(box_mask(x, z, -70, 70, -20, 125, 14), box_mask(x, z, -95, -20, 188, 242, 12), box_mask(x, z, 30, 110, 185, 275, 12),
               box_mask(x, z, 40, 100, 30, 190, 10), box_mask(x, z, -175, -95, -80, 360, 6))
    route = 1 - sstep((min(seg_d(x, z, a, b) for a, b in zip(ROUTE, ROUTE[1:])) - 5) / 8)
    flat = max(flat, route)
    h = dune * (1 - flat)
    # the Nile: bed at -5 between x -150 and -105, banks sloping up over 10 m each side
    if -165 < x < -95:
        bank = min(sstep((x + 160) / 10), sstep((-95 - x) / 10))   # 0 at the far edges, 1 in the channel
        h = h * (1 - bank) - 5.0 * bank
        dp = math.hypot(x + 128, z - 77)
        if dp < 12: h = min(h, -5 - 12 * (1 - sstep((dp - 6) / 6)))
    if 45 < x < 75 and 138 < z < 162: h -= .4   # the quicksand pit
    return h


def mat(name):
    m = bpy.data.materials.get(name)
    if not m:
        m = bpy.data.materials.new(name); m.diffuse_color = (*COL.get(name, (1, 0, 1)), 1)
    return m


def grid_mesh(name, x0, x1, z0, z1, step, hf):
    nx = int((x1 - x0) / step) + 1; nz = int((z1 - z0) / step) + 1
    bm = bmesh.new(); vs = []
    for j in range(nz):
        for i in range(nx):
            x = x0 + i * step; z = z0 + j * step
            vs.append(bm.verts.new((x, z, hf(x, z))))
    for j in range(nz - 1):
        for i in range(nx - 1):
            a = j * nx + i
            bm.faces.new((vs[a], vs[a + 1], vs[a + nx + 1], vs[a + nx]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(mat(name.split('__')[1] if '__' in name else 'Sand'))
    return ob


def export(path, objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
        for c in o.children_recursive: c.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, apply_scale_options='FBX_SCALE_ALL',
                             add_leaf_bones=False, path_mode='STRIP', mesh_smooth_type='FACE')


# ---------------------------------------------------------------------------------------------------------------- parts
def part(name, bm, parent):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.parent = parent
    ob.data.materials.append(mat(name.split('__')[1]))
    return ob


def empty(name):
    e = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(e); return e


def cube(name, c, s, parent, rot=0.0):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
    ob = part(name, bm, parent); ob.location = c; ob.rotation_euler = (0, 0, rot)
    return ob


def cone(name, c, r0, r1, h, parent, n=12, smooth=True):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=n, radius1=r0, radius2=r1, depth=h)
    for v in bm.verts: v.co.z += h / 2
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = smooth
    return ob


def sphere(name, c, r, parent, scale=(1, 1, 1), half=False):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=r)
    if half: bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-4], context='VERTS')
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = True
    return ob


def pyramid_mesh(name, cx, cz, base, top, h, tiers=0):
    bm = bmesh.new()
    if tiers:
        for i in range(tiers):
            half = base / 2 - (base - top) / 2 * i / (tiers - 1) if tiers > 1 else base / 2
            th = h / tiers
            r = bmesh.ops.create_cube(bm, size=1)
            for v in r['verts']: v.co = Vector((cx + v.co.x * half * 2, cz + v.co.y * half * 2, (v.co.z + .5) * th + i * th))
    else:
        b = base / 2; t = top / 2
        vs = [bm.verts.new((cx + sx * b, cz + sz * b, 0)) for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        ts = [bm.verts.new((cx + sx * t, cz + sz * t, h)) for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1))] if t > 0 else [bm.verts.new((cx, cz, h))]
        for i in range(4):
            j = (i + 1) % 4
            if t > 0: bm.faces.new((vs[i], vs[j], ts[j], ts[i]))
            else: bm.faces.new((vs[i], vs[j], ts[0]))
        if t > 0: bm.faces.new(ts)
        bm.faces.new(list(reversed(vs)))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(mat(name.split('__')[1]))
    return ob


def props():
    obs = []
    # pastel houses: a box with a parapet, a dark door, windows; 6 x 6 x 4.6 m (the game scales them)
    for i, colour in enumerate(('PastelPink', 'PastelTeal', 'PastelLilac', 'PastelPeach'), 1):
        e = empty(f'House{i}'); obs.append(e)
        cube(f'Walls__{colour}', (0, 0, 2.3), (6, 6, 4.6), e)
        cube('Parapet__Plaster', (0, 0, 4.75), (6.3, 6.3, .3), e)
        cube('Door__Dark', (0, -3.01, 1.1), (1.2, .05, 2.2), e)
        cube('Lintel__PastelGold', (0, -3.05, 2.35), (1.6, .1, .3), e)
        for sx in (-1.8, 1.8): cube('Window__Dark', (sx, -3.01, 3.0), (.7, .05, .8), e)
        for sx in (-1.8, 1.8): cube('Window__Dark', (3.01, sx, 3.0), (.05, .7, .8), e)
    e = empty('Dome'); obs.append(e)
    cone('Drum__Plaster', (0, 0, 0), 2.2, 2.2, .6, e, 20); sphere('Cap__Turquoise', (0, 0, .6), 2.2, e, (1, 1, 1.05), half=True)
    sphere('Tip__Gold', (0, 0, 2.95), .25, e)
    e = empty('Palm'); obs.append(e)
    for k in range(7):   # a curved trunk of rings
        cone('Trunk__Bark', (k * .12, 0, k * 1.0), .32 - k * .02, .3 - k * .02, 1.05, e, 8)
    for k in range(8):
        a = k / 8 * math.tau
        f = cube('Frond__Palm', (.84 + math.cos(a) * 1.4, math.sin(a) * 1.4, 6.8), (3.0, .7, .08), e, a)
        f.rotation_euler = (0, math.radians(18), a)
    sphere('Dates__Terracotta', (.84, 0, 6.6), .35, e)
    e = empty('Obelisk'); obs.append(e)
    cone('Shaft__Sandstone', (0, 0, 0), 1.0, .65, 8, e, 4, smooth=False); obs[-1].children[-1].rotation_euler = (0, 0, math.radians(45))
    cone('Pyramidion__Gold', (0, 0, 8), .65, 0, 1.0, e, 4, smooth=False); obs[-1].children[-1].rotation_euler = (0, 0, math.radians(45))
    e = empty('Stall'); obs.append(e)
    for sx in (-1.4, 1.4):
        for sz in (-1, 1): cube('Post__Wood', (sx, sz, 1.2), (.12, .12, 2.4), e)
    cube('Counter__Wood', (0, -.6, .5), (2.9, 1.0, 1.0), e)
    for k in range(7):   # the striped awning, sloping
        c = 'AwningRed' if k % 2 == 0 else 'AwningWhite'
        a = cube(f'Awning__{c}', (-1.5 + .43 * k + .215, 0, 2.5), (.43, 2.6, .06), e); a.rotation_euler = (math.radians(-10), 0, 0)
    e = empty('Stall2'); obs.append(e)
    for sx in (-1.4, 1.4):
        for sz in (-1, 1): cube('Post__Wood', (sx, sz, 1.2), (.12, .12, 2.4), e)
    cube('Counter__Wood', (0, -.6, .5), (2.9, 1.0, 1.0), e)
    for k in range(7):
        c = 'AwningBlue' if k % 2 == 0 else 'AwningWhite'
        a = cube(f'Awning__{c}', (-1.5 + .43 * k + .215, 0, 2.5), (.43, 2.6, .06), e); a.rotation_euler = (math.radians(-10), 0, 0)
    e = empty('Basket'); obs.append(e)
    cone('Weave__Wicker', (0, 0, 0), .35, .45, .5, e, 12); sphere('Fruit__Terracotta', (0, 0, .5), .3, e, (1, 1, .5))
    e = empty('Pot'); obs.append(e)
    sphere('Belly__Terracotta', (0, 0, .45), .4, e, (1, 1, 1.15)); cone('Neck__Terracotta', (0, 0, .85), .16, .2, .3, e, 10)
    e = empty('Block'); obs.append(e)
    cube('Stone__Sandstone', (0, 0, .75), (2.4, 1.5, 1.5), e)
    e = empty('Cart'); obs.append(e)
    cube('Bed__Wood', (0, 0, .7), (2.6, 1.8, .25), e)
    for sx in (-1, 1):
        w = cone('Wheel__Wood', (sx * 1.0, 0, .55), .55, .55, .15, e, 12); w.rotation_euler = (0, math.radians(90), 0); w.location = (sx * 1.05 - .075, 0, .55)
    cube('Handle__Wood', (0, -1.6, .8), (.1, 1.6, .1), e)
    e = empty('Reeds'); obs.append(e)
    rnd = random.Random(3)
    for k in range(14):
        a = rnd.random() * math.tau; r = rnd.random() * .8
        cone('Reed__Reed', (math.cos(a) * r, math.sin(a) * r, 0), .05, .01, 1.6 + rnd.random() * 1.2, e, 5)
        if k % 3 == 0: sphere('Head__Bark', (math.cos(a) * r, math.sin(a) * r, 2.3), .07, e, (1, 1, 3))
    e = empty('Boat'); obs.append(e)
    hull = sphere('Hull__Reed', (0, 0, .35), 1, e, (1.3, 4.2, .45))
    for sy in (-1, 1): cone('Prow__Reed', (0, sy * 3.9, .3), .35, .12, 1.4, e, 8).rotation_euler = (math.radians(-sy * 35), 0, 0)
    cube('Deck__Wood', (0, 0, .62), (1.8, 5.6, .12), e)
    e = empty('Column'); obs.append(e)
    cone('Shaft__Sandstone', (0, 0, 0), .55, .5, 5.5, e, 12); cone('Lotus__PastelTeal', (0, 0, 5.5), .5, .85, .8, e, 12)
    cube('Abacus__Sandstone', (0, 0, 6.45), (1.6, 1.6, .3), e)
    e = empty('CatStatue'); obs.append(e)   # a seated cat (the temple's door guards)
    cube('Base__Sandstone', (0, 0, .4), (1.4, 2.0, .8), e)
    sphere('Body__Dark', (0, .2, 1.45), .55, e, (.9, 1.1, 1.3)); sphere('Head__Dark', (0, -.25, 2.45), .38, e)
    for sx in (-1, 1): cone('Ear__Dark', (sx * .22, -.25, 2.7), .14, 0, .35, e, 4)
    for sx in (-1, 1): sphere('Eye__Gold', (sx * .14, -.58, 2.5), .07, e)
    cube('Collar__Gold', (0, -.15, 2.05), (.7, .5, .12), e)
    e = empty('Crate'); obs.append(e)
    cube('Box__Wood', (0, 0, .5), (1, 1, 1), e)
    e = empty('Plank'); obs.append(e)
    cube('Board__Wood', (0, 0, -.1), (1, 1, .2), e)   # scaled in game: 1 x 1 x 0.2, top at 0
    e = empty('Pole'); obs.append(e)
    cone('Pole__Bark', (0, 0, 0), .12, .1, 1, e, 6)   # scaled in game: 1 m tall
    e = empty('Sarcophagus'); obs.append(e)
    cube('Base__Lapis', (0, 0, .5), (1.4, 2.6, 1.0), e); sphere('Lid__Gold', (0, 0, 1.0), .7, e, (1, 1.8, .35)); sphere('Face__Gold', (0, -.95, 1.25), .32, e)
    e = empty('Brazier'); obs.append(e)
    cone('Stand__Gold', (0, 0, 0), .2, .12, 1.1, e, 8); cone('Bowl__Gold', (0, 0, 1.1), .2, .45, .3, e, 10)
    e = empty('WallSeg'); obs.append(e)
    cube('Wall__Sandstone', (0, 0, 1.2), (4, .8, 2.4), e); cube('Cap__Plaster', (0, 0, 2.45), (4.1, .9, .15), e)
    return obs


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    t = grid_mesh('Terrain__Sand', X0, X1, Z0, Z1, STEP, height)
    far = grid_mesh('FarSand__Sand', -700, 700, 340, 900, 20, lambda x, z: max(0, 4 * (n2(x, z, 90, 2.2) + .6)) * sstep((z - 360) / 60) - 1.5 * (1 - sstep((z - 360) / 30)))
    export(os.path.join(OUT, 'egypt_terrain.fbx'), [t, far])
    p1 = pyramid_mesh('Pyramid1__Sandstone', 130, 430, 170, 0, 105); cap1 = pyramid_mesh('Cap1__Gold', 130, 430, 17, 0, 10.5); cap1.location.z = 94.5
    p2 = pyramid_mesh('Pyramid2__Sandstone', -70, 480, 130, 0, 80); cap2 = pyramid_mesh('Cap2__Gold', -70, 480, 13, 0, 8); cap2.location.z = 72
    bp = pyramid_mesh('BuildPyramid__Sandstone', 70, 235, 56, 12, 36, tiers=9)
    export(os.path.join(OUT, 'egypt_pyramids.fbx'), [p1, cap1, p2, cap2, bp])
    # the map: from above
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'; sc.display.shading.light = 'STUDIO'
    sc.render.resolution_x = 900; sc.render.resolution_y = 1000
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 560; cam.location = (-15, 220, 400)
    sc.render.filepath = os.path.join(SHOTS, 'egypt_top.png'); bpy.ops.render.render(write_still=True)
    for o in list(bpy.context.scene.objects): bpy.data.objects.remove(o)
    obs = props()
    for i, o in enumerate(obs): o.location = ((i % 6) * 9, (i // 6) * 9, 0)
    bpy.context.view_layer.update()
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'PERSP'; cam.location = (22, -38, 30); cam.rotation_euler = (math.radians(58), 0, 0); cam.data.lens = 30
    sc.render.resolution_x = 1400; sc.render.resolution_y = 900
    sc.render.filepath = os.path.join(SHOTS, 'egypt_props.png'); bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
    for o in obs: o.location = (0, 0, 0)
    export(os.path.join(OUT, 'egypt_props.fbx'), obs)
    print('EGYPT written:', len(obs), 'props')


main()
