"""Shards of Time, Round 7 items 2, 4 and 11: Bethlehem's ground and its stylized props, by script (as egypt_world.py).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/bethlehem_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height):
  Assets/Resources/World/bethlehem_terrain.fbx  Terrain__Grass (the farmyard, the hills, the village, the Wise Men's road), FarHills
                                                (the dark hills round the edge, no collider needed)
  Assets/Resources/World/bethlehem_props.fbx    one empty per prop, its parts <part>__<Material>: Barn (rustic boards, X-braced doors,
                                                a round window in the gable, a thatched roof with the crash hole), Manger, HayBale,
                                                HayStack, Barrel, Fence (one 4 m length), GiftRed / GiftGreen / GiftBlue (boxes with
                                                bows), Lantern, Harp, House1-3 (flat mud-brick roofs), Inn, Stall (closed for the night),
                                                Well, Olive, Campfire, Rock, Cart, Trough, Signpost
  Data/screenshots/round7/world/bethlehem_top.png, bethlehem_props.png
Materials are only names: the game colours each part by the word after "__" (World/WorldArt.cs).

The layout (the same numbers as World/Bethlehem.cs):
  the farmyard, flat at 0: x -35..35, z -10..70; the barn x -6..6, z 26..42 (door south), its roof hole over (-2.5, 33)
  the hills (east): x 40..195, z -70..230, rolling up to ~11 m; flat pockets for the shepherds' camps (70, 40), (128, 112), (150, 0),
    the well (108, 72), the olive grove x 95..145, z 150..200
  the village (west): flat, x -195..-55, z -30..150
  the Wise Men's road: (175, 205) -> (100, 185) -> (35, 195) -> (-40, 182) -> (-120, 200) -> (-180, 190), a flat band 10 m wide
  dark hills all round the edge (FarHills)
"""
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round7', 'world')
X0, X1, Z0, Z1, STEP = -210.0, 210.0, -90.0, 260.0, 2.5
COL = {'Grass': (.42, .45, .22), 'Dirt': (.55, .4, .25), 'Wood': (.45, .28, .16), 'Plank': (.55, .36, .2), 'Thatch': (.78, .6, .3),
       'Hay': (.93, .78, .38), 'Mudbrick': (.78, .62, .45), 'Plaster': (.92, .86, .74), 'Dark': (.12, .1, .09), 'WarmLight': (1, .82, .45),
       'GiftRed': (.85, .12, .15), 'GiftGreen': (.12, .6, .3), 'GiftBlue': (.2, .35, .85), 'Gold': (.95, .72, .25), 'Stone': (.55, .53, .5),
       'Olive': (.3, .4, .2), 'Bark': (.42, .28, .18), 'Rope': (.75, .62, .4), 'Iron': (.25, .25, .27), 'Ember': (1, .45, .12),
       'AwningRed': (.75, .25, .22), 'Terracotta': (.78, .42, .28), 'Water': (.2, .35, .5)}

ROAD = [(175, 205), (100, 185), (35, 195), (-40, 182), (-120, 200), (-180, 190)]
CAMPS = [(70, 40), (128, 112), (150, 0)]


def sstep(t):
    t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)


def n2(x, z, s, o=0.0):
    return noise.noise(Vector((x / s + o, z / s - o, .37 * o)))


def box_mask(x, z, x0, x1, z0, z1, soft):
    dx = max(x0 - x, 0, x - x1); dz = max(z0 - z, 0, z - z1)
    return 1 - sstep(math.hypot(dx, dz) / soft)


def seg_d(x, z, a, b):
    ax, az = a; bx, bz = b; vx, vz = bx - ax, bz - az
    t = max(0, min(1, ((x - ax) * vx + (z - az) * vz) / (vx * vx + vz * vz)))
    return math.hypot(x - ax - vx * t, z - az - vz * t)


def height(x, z):
    hills = 4.8 * (n2(x, z, 45, 2.1) + .7) + 1.4 * (n2(x, z, 18, 5.3) + .4)
    hills = max(0.0, hills)
    east = sstep((x - 30) / 25)                      # the hills rise east of the farmyard
    rim = .6 * sstep((abs(z - 90) - 150) / 40) + .5 * sstep((-x - 200) / 20)   # a little rise at the map's edges
    h = hills * max(east, rim, .25 * sstep((z - 80) / 40))
    flat = max(box_mask(x, z, -35, 35, -10, 70, 14), box_mask(x, z, -195, -55, -30, 150, 10))
    for cx, cz in CAMPS: flat = max(flat, 1 - sstep((math.hypot(x - cx, z - cz) - 7) / 22))   # Round 7 check: a gentle rim (7 m made slopes too steep to walk)
    flat = max(flat, 1 - sstep((math.hypot(x - 108, z - 72) - 6) / 18), box_mask(x, z, 95, 145, 150, 200, 8) * .7)
    # flat pockets: the rocky outcrop, the two rock pillars (the pounce), the shepherds' cave
    flat = max(flat, box_mask(x, z, 82, 98, 102, 120, 20), box_mask(x, z, 148, 186, 118, 150, 20), box_mask(x, z, 176, 200, 48, 72, 20))
    road = 1 - sstep((min(seg_d(x, z, a, b) for a, b in zip(ROAD, ROAD[1:])) - 5) / 22)
    # the farm track from the barn's yard north to the road, and west to the village
    road = max(road, 1 - sstep((seg_d(x, z, (0, 70), (20, 190)) - 3) / 18), 1 - sstep((seg_d(x, z, (-35, 30), (-55, 40)) - 4) / 18))
    flat = max(flat, road)
    h = h * (1 - flat)
    dw = math.hypot(x - 108, z - 72)   # the well's shaft: 13-14 m down (the deep dive, later)
    if dw < 4: h = min(h, -14 * (1 - sstep((dw - 2.6) / 1.4)))
    return h


def max_slope():
    worst, at = 0.0, None
    for i in range(int((X1 - X0) / 5)):
        for j in range(int((Z1 - Z0) / 5)):
            x, z = X0 + i * 5, Z0 + j * 5
            if math.hypot(x - 108, z - 72) < 6: continue
            g = math.degrees(math.atan(math.hypot(height(x + 1, z) - height(x - 1, z), height(x, z + 1) - height(x, z - 1)) / 2))
            if g > worst: worst, at = g, (x, z)
    return worst, at


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
    ob.data.materials.append(mat(name.split('__')[1] if '__' in name else 'Grass'))
    return ob


def export(path, objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
        for c in o.children_recursive: c.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, apply_scale_options='FBX_SCALE_ALL',
                             add_leaf_bones=False, path_mode='STRIP', mesh_smooth_type='FACE')


def part(name, bm, parent):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.parent = parent
    ob.data.materials.append(mat(name.split('__')[1]))
    return ob


def empty(name):
    e = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(e); return e


def cube(name, c, s, parent, rot=0.0, tilt=(0.0, 0.0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
    ob = part(name, bm, parent); ob.location = c; ob.rotation_euler = (tilt[0], tilt[1], rot)
    return ob


def cone(name, c, r0, r1, h, parent, n=12, smooth=True):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=n, radius1=r0, radius2=r1, depth=h)
    for v in bm.verts: v.co.z += h / 2
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = smooth
    return ob


def cyl_x(name, c, r, length, parent, n=12):
    """a cylinder lying along X (a hay bale, a barrel on its side, a log)"""
    ob = cone(name, c, r, r, length, parent, n); ob.rotation_euler = (0, math.radians(90), 0); ob.location = (c[0] - length / 2, c[1], c[2])
    return ob


def sphere(name, c, r, parent, scale=(1, 1, 1), half=False):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=r)
    if half: bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-4], context='VERTS')
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = True
    return ob


def ring(name, c, r, thick, parent, n=20, axis='Y'):
    """a ring of small boxes (axis Y: standing upright, facing -Y; axis Z: lying flat)"""
    e = []
    for k in range(n):
        a = k / n * math.tau
        p = (c[0] + math.cos(a) * r, c[1], c[2] + math.sin(a) * r) if axis == 'Y' else (c[0] + math.cos(a) * r, c[1] + math.sin(a) * r, c[2])
        b = cube(name, p, (thick, thick, 2 * math.pi * r / n * 1.15), parent)
        b.rotation_euler = (0, -a + math.pi / 2, 0) if axis == 'Y' else (math.pi / 2, 0, a)
        e.append(b)
    return e


# ---------------------------------------------------------------------------------------------------------------- props
def barn(e):
    """12 x 16 m (x -6..6, y -8..8; the door in the south gable at y -8), walls 4.5 m, ridge 8 m. The roof's west slope has the crash
    hole (a gap of missing thatch over about (-2.5, -1), the barn's own coordinates), broken boards round its edge."""
    W, L, H, R = 6.0, 8.0, 4.5, 8.0
    # walls of vertical boards (a darker post at each corner, a few boards out of line), the door gap 5 m wide in the south wall
    rnd = random.Random(7)
    for side in (-1, 1):   # east and west long walls
        for k in range(16):
            y = -L + .5 + k
            cube('Board__Plank' if k % 3 else 'Board__Wood', (side * W, y, H / 2), (.15, 1.0, H + rnd.uniform(-.1, .1)), e)
    for k in range(12):   # the north wall
        x = -W + .5 + k
        cube('Board__Plank' if k % 3 else 'Board__Wood', (x, L, H / 2), (1.0, .15, H), e)
    for k in range(12):   # the south wall, either side of the door
        x = -W + .5 + k
        if abs(x) < 2.5: continue
        cube('Board__Plank' if k % 3 else 'Board__Wood', (x, -L, H / 2), (1.0, .15, H), e)
    cube('Lintel__Wood', (0, -L - .05, H - .25), (5.4, .3, .5), e)
    for sx in (-1, 1):
        for sy in (-1, 1): cube('Post__Wood', (sx * W, sy * L, H / 2 + .1), (.35, .35, H + .2), e)
    # the gables: boards up to the ridge (south one with the round window, north one closed)
    for sy in (-1, 1):
        for k in range(12):
            x = -W + .5 + k; top = H + (R - H) * (1 - abs(x) / W)
            if sy == -1 and abs(x) < 1.0 and True: pass
            cube('Gable__Plank', (x, sy * L, (H + top) / 2), (1.0, .15, top - H), e)
    ring('Window__Wood', (0, -L - .12, 6.3), .85, .18, e, 16)
    cone('Window__WarmLight', (0, -L - .1, 6.3), .78, .78, .05, e, 20).rotation_euler = (math.radians(90), 0, 0)
    # a tie beam and the X braces over the door
    cube('Beam__Wood', (0, -L - .1, H + .1), (12.4, .25, .3), e)
    for sx in (-1, 1): cube('Brace__Wood', (sx * 1.3, -L - .12, H + 1.0), (2.8, .18, .18), e, 0, (0, math.radians(sx * 32)))
    # the two doors, open wide against the south wall (X-braced)
    for sx in (-1, 1):
        cx = sx * 4.35
        for k in range(4): cube('Door__Plank', (cx - .9 + k * .6 + .3 - .3, -L - .45, 1.9), (.58, .1, 3.8), e)
        cube('Door__Wood', (cx, -L - .52, 1.9), (2.4, .08, .2), e)
        for d in (-1, 1): cube('Door__Wood', (cx, -L - .52, 1.9), (2.9, .08, .16), e, 0, (0, math.radians(d * 55)))
        cube('Door__Wood', (cx, -L - .52, .3), (2.4, .08, .2), e); cube('Door__Wood', (cx, -L - .52, 3.5), (2.4, .08, .2), e)
    # the roof: thatch slabs on two slopes, each slope made of 8 x 4 tiles so the hole is a few tiles missing
    slope = math.atan2(R - H, W); run = math.hypot(W + .8, (R - H) * (W + .8) / W)
    for side in (-1, 1):
        for i in range(8):            # along y
            for j in range(4):        # down the slope
                y = -L - .6 + (i + .5) * (2 * L + 1.2) / 8
                d = (j + .5) / 4 * run   # distance down the slope from the ridge
                x = side * d * math.cos(slope); z = R + .15 - d * math.sin(slope)
                hole = side == -1 and i in (3, 4) and j in (1, 2)
                if hole: continue
                cube('Thatch__Thatch', (x, y, z), (run / 4 + .05, (2 * L + 1.2) / 8 + .05, .35), e, 0, (0, side * slope))
        cube('Ridge__Thatch', (0, 0, R + .25), (.7, 2 * L + 1.4, .4), e)
    # broken boards and straw sticking up round the hole
    for k, (dx, dy, a) in enumerate(((-1.0, -1.6, 20), (-3.6, -1.8, -35), (-1.4, 1.8, 50), (-3.3, 1.5, -10), (-2.4, -2.0, 80))):
        cube('Broken__Plank', (dx, dy, 6.4 - abs(dx) * .4), (.18, 1.1, .08), e, math.radians(a), (math.radians(25), 0))
    for k in range(10):
        a = k / 10 * math.tau
        cone('Straw__Hay', (-2.4 + math.cos(a) * 1.6, -.1 + math.sin(a) * 1.9, 6.2), .05, .01, .6, e, 4).rotation_euler = (math.cos(a) * .6, math.sin(a) * .6, 0)
    # the floor: packed dirt and loose hay
    cube('Floor__Dirt', (0, 0, .03), (12, 16, .06), e)


def props():
    obs = []
    e = empty('Barn'); obs.append(e); barn(e)
    e = empty('Manger'); obs.append(e)   # a wooden trough on X legs, full of hay
    for sx in (-1, 1):
        cube('Side__Wood', (0, sx * .38, .75), (1.8, .08, .5), e, 0, (math.radians(sx * 18), 0))
        for sy in (-1, 1): cube('Leg__Wood', (sx * .75, sy * .2, .35), (.1, .1, .8), e, 0, (math.radians(sy * 20), 0))
    for sx in (-1, 1): cube('End__Wood', (sx * .9, 0, .75), (.08, .9, .55), e)
    cube('Bottom__Wood', (0, 0, .52), (1.8, .5, .06), e)
    sphere('Hay__Hay', (0, 0, .9), .5, e, (1.7, .7, .3))
    e = empty('HayBale'); obs.append(e)   # a square bale 1.2 x .7 x .6, twine round it
    cube('Bale__Hay', (0, 0, .3), (1.2, .7, .6), e)
    for sx in (-.3, .3): cube('Twine__Rope', (sx, 0, .3), (.04, .72, .62), e)
    e = empty('HayStack'); obs.append(e)   # a round stack, 3 m across, 2.2 m high
    sphere('Stack__Hay', (0, 0, 0), 1.5, e, (1, 1, 1.45), half=True)
    e = empty('Barrel'); obs.append(e)
    cone('Staves__Plank', (0, 0, 0), .38, .42, .5, e, 14); cone('Staves__Plank', (0, 0, .5), .42, .38, .5, e, 14)
    for z in (.12, .5, .88): cone('Hoop__Iron', (0, 0, z - .03), .43, .43, .06, e, 14)
    e = empty('Fence'); obs.append(e)   # one 4 m length: two posts, three rails
    for sx in (-2, 2): cone('Post__Wood', (sx, 0, 0), .1, .09, 1.4, e, 6)
    for z in (.4, .8, 1.2): cube('Rail__Plank', (0, 0, z), (4.1, .08, .14), e, 0, (math.radians(2 * (z - .8)), 0))
    for name, col in (('GiftRed', 'GiftRed'), ('GiftGreen', 'GiftGreen'), ('GiftBlue', 'GiftBlue')):
        e = empty(name); obs.append(e)   # the Wise Men's modern presents: a box, a ribbon both ways, a bow on top (0.4 m)
        cube(f'Box__{col}', (0, 0, .2), (.4, .4, .4), e)
        cube('Ribbon__Gold', (0, 0, .2), (.42, .07, .42), e); cube('Ribbon__Gold', (0, 0, .2), (.07, .42, .42), e)
        for a in (-35, 35): sphere('Bow__Gold', (math.sin(math.radians(a)) * .1, 0, .44), .09, e, (1.4, .5, .8)).rotation_euler = (0, math.radians(a), 0)
        sphere('Knot__Gold', (0, 0, .43), .045, e)
    e = empty('Lantern'); obs.append(e)   # the watchman's lantern (held at its ring, 0 = the ring)
    cube('Frame__Iron', (0, 0, -.32), (.2, .2, .02), e); cube('Frame__Iron', (0, 0, -.06), (.2, .2, .02), e)
    for sx in (-1, 1):
        for sy in (-1, 1): cube('Frame__Iron', (sx * .09, sy * .09, -.19), (.02, .02, .26), e)
    cube('Glass__WarmLight', (0, 0, -.19), (.15, .15, .22), e); cone('Cap__Iron', (0, 0, -.05), .13, .03, .07, e, 4)
    ring('Ring__Iron', (0, 0, .02), .04, .015, e, 8)
    e = empty('Harp'); obs.append(e)   # a small shepherd's harp (0.55 m): a curved neck, a pillar, a sound box, seven strings
    cube('Box__Wood', (0, 0, .2), (.09, .12, .42), e, 0, (math.radians(-12), 0))
    cube('Pillar__Wood', (0, .32, .26), (.05, .05, .46), e, 0, (math.radians(8), 0))
    for k in range(5):
        a = k / 4
        cube('Neck__Wood', (0, .02 + a * .3, .44 + math.sin(a * math.pi) * .06), (.05, .09, .05), e)
    for k in range(7):
        y = .06 + k * .038
        top = .46 + math.sin((y - .02) / .3 * math.pi) * .05; bot = .1 + y * .18
        cube('String__Gold', (0, y, (top + bot) / 2), (.006, .006, top - bot), e)
    cube('Scroll__Gold', (0, .34, .5), (.06, .06, .06), e)
    # village houses: mud brick, flat roofs with a low parapet, a dark door, small windows, a few with an outside stair hint
    for i, colour in enumerate(('Mudbrick', 'Plaster', 'Mudbrick'), 1):
        e = empty(f'House{i}'); obs.append(e)
        cube(f'Walls__{colour}', (0, 0, 2.1), (6, 6, 4.2), e)
        for sx, sy in ((-1, 0), (1, 0), (0, -1), (0, 1)): cube('Parapet__Plaster', (sx * 2.95, sy * 2.95, 4.35), (6.1 if sx == 0 else .2, 6.1 if sy == 0 else .2, .3), e)
        cube('Door__Dark', (0, -3.01, 1.0), (1.1, .05, 2.0), e)
        cube('Window__WarmLight' if i != 2 else 'Window__Dark', (1.8, -3.01, 2.8), (.6, .05, .6), e)
        cube('Window__Dark', (3.01, 1.2, 2.8), (.05, .6, .6), e)
        for k in range(5): cone('Beam__Wood', (-2.4 + k * 1.2, -3.0, 3.9), .07, .07, .4, e, 6).rotation_euler = (math.radians(90), 0, 0)
    e = empty('Inn'); obs.append(e)   # two storeys (7.5 m), 10 x 8, a balcony, a lit door, the sign bracket over it
    cube('Walls__Plaster', (0, 0, 3.75), (10, 8, 7.5), e)
    for sx, sy in ((-1, 0), (1, 0), (0, -1), (0, 1)): cube('Parapet__Mudbrick', (sx * 4.95, sy * 3.95, 7.65), (10.1 if sx == 0 else .2, 8.1 if sy == 0 else .2, .3), e)
    cube('Door__WarmLight', (0, -4.01, 1.2), (1.5, .05, 2.4), e)
    for sx in (-3.2, -1.6, 1.6, 3.2): cube('Window__WarmLight', (sx, -4.01, 5.2), (.7, .05, .8), e)
    for sx in (-3.2, 3.2): cube('Window__WarmLight', (sx, -4.01, 2.0), (.7, .05, .8), e)
    cube('Balcony__Wood', (0, -4.5, 3.6), (6, 1.0, .15), e); cube('Rail__Wood', (0, -4.95, 4.1), (6, .08, .08), e)
    cube('Bracket__Wood', (1.6, -4.6, 3.0), (.08, 1.2, .08), e)
    e = empty('Stall'); obs.append(e)   # closed for the night: a counter, a rolled-up awning, shutters
    for sx in (-1.4, 1.4):
        for sz in (-1, 1): cube('Post__Wood', (sx, sz, 1.2), (.12, .12, 2.4), e)
    cube('Counter__Wood', (0, -.6, .5), (2.9, 1.0, 1.0), e); cube('Shutter__Plank', (0, -1.12, 1.4), (2.9, .06, .8), e)
    cyl_x('Awning__AwningRed', (0, -1.0, 2.35), .18, 3.0, e)
    e = empty('Well'); obs.append(e)   # a round stone wall (r 1.3, 0.9 m high), two posts, a beam, a bucket
    for k in range(14):
        a = k / 14 * math.tau
        cube('Wall__Stone', (math.cos(a) * 1.3, math.sin(a) * 1.3, .45), (.5, .62, .9), e, a)
    cone('Water__Water', (0, 0, -.6), 1.1, 1.1, .05, e, 16)
    for sx in (-1, 1): cube('Post__Wood', (sx * 1.35, 0, 1.3), (.15, .15, 1.8), e)
    cube('Beam__Wood', (0, 0, 2.15), (2.9, .15, .15), e); cone('Bucket__Plank', (.3, 0, 1.2), .18, .2, .3, e, 10)
    cube('Rope__Rope', (.3, 0, 1.8), (.03, .03, .7), e)
    e = empty('Olive'); obs.append(e)   # a gnarled olive tree, 4.5 m
    for k in range(4): cone('Trunk__Bark', (k * .18 * (-1) ** k, k * .1, k * .8), .35 - k * .05, .3 - k * .05, .85, e, 7)
    for (x, y, z, r) in ((0, 0, 3.6, 1.6), (1.1, .4, 3.2, 1.1), (-1.0, -.3, 3.3, 1.2), (.2, -1, 3.9, 1.0), (-.3, .9, 4.0, 1.0)):
        sphere('Leaves__Olive', (x, y, z), r, e, (1, 1, .7))
    e = empty('Campfire'); obs.append(e)   # a ring of stones, crossed logs, glowing embers (the flames are the game's)
    for k in range(9):
        a = k / 9 * math.tau
        sphere('Stone__Stone', (math.cos(a) * .75, math.sin(a) * .75, .08), .2, e, (1.2, 1, .7))
    for k in range(4):
        a = k / 4 * math.pi
        l = cyl_x('Log__Bark', (0, 0, .15), .09, 1.1, e, 8); l.rotation_euler = (0, math.radians(90 - 12), a); l.location = (0, 0, .2)
    sphere('Embers__Ember', (0, 0, .08), .35, e, (1, 1, .35))
    e = empty('Rock'); obs.append(e)   # an outcrop 4 x 3 x 2.6 m (scaled in game)
    rnd = random.Random(11)
    for (x, y, z, s) in ((0, 0, .9, (2.2, 1.6, 1.4)), (1.2, .5, .6, (1.4, 1.2, 1.0)), (-1.1, -.3, .5, (1.3, 1.1, .9)), (.3, .2, 1.8, (1.2, 1.0, .8))):
        r = sphere('Rock__Stone', (x, y, z), 1, e, s); r.rotation_euler = (0, 0, rnd.random() * 3)
    e = empty('Cart'); obs.append(e)
    cube('Bed__Plank', (0, 0, .7), (2.6, 1.6, .2), e)
    for sy in (-1, 1): cube('Side__Plank', (0, sy * .8, .95), (2.6, .08, .4), e)
    for sy in (-1, 1):
        w = cone('Wheel__Wood', (0, sy * .95, .55), .55, .55, .12, e, 12); w.rotation_euler = (math.radians(90), 0, 0)
    cube('Shaft__Wood', (-2.0, 0, .6), (1.6, .1, .1), e)
    sphere('Load__Hay', (0, 0, 1.0), .9, e, (1.3, .8, .5))
    e = empty('Trough'); obs.append(e)
    cube('Trough__Plank', (0, 0, .3), (2.0, .6, .5), e); cube('Water__Water', (0, 0, .5), (1.85, .45, .05), e)
    e = empty('Signpost'); obs.append(e)   # the inn's sign board on two chains (its words are the game's)
    cube('Board__Plank', (0, 0, -.45), (1.4, .06, .55), e)
    for sx in (-.5, .5): cube('Chain__Iron', (sx, 0, -.08), (.02, .02, .3), e)
    return obs


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    t = grid_mesh('Terrain__Grass', X0, X1, Z0, Z1, STEP, height)
    def far_h(x, z):
        d = max(abs(x) - 210, z - 260, -90 - z, 0)
        return (6 + 10 * (n2(x, z, 70, 3.3) + .7)) * sstep(d / 60) - 1.0 * (1 - sstep(d / 10))
    far = grid_mesh('FarHills__Grass', -700, 700, -600, 800, 20, far_h)
    export(os.path.join(OUT, 'bethlehem_terrain.fbx'), [t, far])
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'; sc.display.shading.light = 'STUDIO'
    sc.render.resolution_x = 1000; sc.render.resolution_y = 860
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 440; cam.location = (0, 85, 400)
    sc.render.filepath = os.path.join(SHOTS, 'bethlehem_top.png'); bpy.ops.render.render(write_still=True)
    for o in list(bpy.context.scene.objects): bpy.data.objects.remove(o)
    obs = props()
    for i, o in enumerate(obs): o.location = ((i % 6) * 14, (i // 6) * 14, 0)
    bpy.context.view_layer.update()
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'PERSP'; cam.location = (35, -50, 40); cam.rotation_euler = (math.radians(58), 0, 0); cam.data.lens = 28
    sc.render.resolution_x = 1400; sc.render.resolution_y = 900
    sc.render.filepath = os.path.join(SHOTS, 'bethlehem_props.png'); bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
    for o in obs: o.location = (0, 0, 0)
    export(os.path.join(OUT, 'bethlehem_props.fbx'), obs)
    print('BETHLEHEM written:', len(obs), 'props; steepest ground %.0f deg at %s' % max_slope())


main()
