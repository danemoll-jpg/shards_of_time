"""Shards of Time, Round 8: the Medieval Kingdom's ground and its stylized props, by script (as bethlehem_world.py).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/medieval_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height):
  Assets/Resources/World/medieval_terrain.fbx  Terrain__Grass (the castle island, the moat cut in, the roads, the tournament field, the
                                               village, rolling hills), FarHills (dark hills round the edge, no collider needed)
  Assets/Resources/World/medieval_props.fbx    one empty per prop at the origin, its parts <part>__<Material> (parts of the same name
                                               are merged into one mesh, so a prop is a handful of renderers): Castle (the whole shell),
                                               Pedestal, PurpleShard, HayWagon, WagonWreck, Torch, Brazier, Bench, FlowerPot, Barrel,
                                               Crate, Boulder, Bramble, Banner, BannerPole, Chandelier, FeastTable, Portcullis,
                                               SmallPortcullis, Drawbridge, Sword, Shield, Bow, Arrow, Key, CellBars, Chains, HayBale,
                                               House1-3, MarketStall, Smithy, Stands, Tent, TentBlue, ListBarrier, HighStriker, Mallet,
                                               Log, TreeOak, TreeClump
  Data/screenshots/round8/world/medieval_top.png, medieval_castle_se.png, medieval_castle_sw.png, medieval_hall.png,
                                medieval_courtyard.png, medieval_moat.png, medieval_props.png, medieval_props_field.png, medieval_props_small.png
Materials are only names: the game colours each part by the word after "__" (World/WorldArt.cs Palette).
A prop's front faces Blender -Y (= Unity -z, south, at yaw 0), as Bethlehem's barn and houses.

The layout (the same numbers as World/Medieval.cs):
  flat at 0: the castle island x -36..36, z -6..90 and a band round the moat x -60..60, z -30..114; the road south from the drawbridge
    x -5..5, z -30..-55; the tournament field x 40..130, z -125..-40; the village x -140..-30, z -135..-40; roads (0,-55) -> (-40,-80)
    and (0,-55) -> (40,-80), 8 m wide
  the moat: the ring between x -48..48, z -18..102 and x -36..36, z -6..90, floor -5 (water at -1.2 is the game's); a deep pit in its
    NW corner x -48..-36, z 90..102, floor -16. The terrain is 2 m fine round the castle with extra lines on the moat's edges.
  the Castle prop (at (0,0,0), yaw 0): curtain walls 3 m thick, 10 m tall; gatehouse; four round towers r 5; the hollow keep with the
    great hall, its gallery and fireplace; the slender spired tower on the keep's NE corner; the arcade along the east wall; the
    cobbled courtyard; the steps to the keep.
"""
import bpy, bmesh, math, os, random
from mathutils import Vector, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round8', 'world')
X0, X1, Z0, Z1 = -210.0, 210.0, -175.0, 175.0
COL = {'Grass': (.42, .45, .22), 'Dirt': (.55, .4, .25), 'Wood': (.45, .28, .16), 'Plank': (.55, .36, .2), 'Hay': (.93, .78, .38),
       'Plaster': (.96, .92, .85), 'Dark': (.15, .12, .1), 'WarmLight': (1, .82, .45), 'Gold': (.95, .72, .25), 'Stone': (.58, .55, .53),
       'Sandstone': (.87, .7, .48), 'Bark': (.42, .28, .18), 'Rope': (.75, .62, .4), 'Iron': (.25, .25, .27), 'Ember': (1, .45, .12),
       'Terracotta': (.78, .42, .28), 'Cream': (.95, .89, .78), 'Red': (.82, .1, .14), 'Green': (.1, .42, .18), 'Wicker': (.72, .55, .32),
       # Round 8: the Medieval Kingdom (also in WorldArt.cs Palette)
       'CastleStone': (.80, .66, .48), 'Cobble': (.6, .53, .46), 'Flagstone': (.86, .77, .62), 'Slate': (.3, .34, .44),
       'Timber': (.28, .18, .12), 'BannerRed': (.78, .1, .14), 'BannerBlue': (.15, .28, .72), 'BannerPurple': (.45, .2, .7),
       'Canvas': (.95, .9, .78), 'FlowerPink': (.98, .45, .65), 'FlowerYellow': (1, .82, .2), 'FlowerViolet': (.55, .35, .9),
       'Leaf': (.22, .48, .2), 'Steel': (.78, .8, .84), 'Bramble': (.26, .24, .14), 'PurpleShard': (.7, .4, 1)}

ROADS = [((0, -30), (0, -55), 5), ((0, -55), (-40, -80), 4), ((0, -55), (40, -80), 4)]
TOWERS = [(-31.5, -1.5), (31.5, -1.5), (-31.5, 61.5), (31.5, 61.5)]


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


def inside(x, z, x0, x1, z0, z1):
    """> 0 inside the rectangle (the distance to its nearest side), < 0 outside"""
    return min(x - x0, x1 - x, z - z0, z1 - z)


def moat(x, z):
    """the moat's depth (0 on the island and outside); the sides fall over 0.3 m so the channel's edges sit on the numbers"""
    d_out = inside(x, z, -48, 48, -18, 102)
    if d_out <= 0: return None
    d_in = -inside(x, z, -36, 36, -6, 90)
    f = max(0.0, min(1.0, min(d_out, d_in) / .3))
    p = max(0.0, min(1.0, inside(x, z, -48, -36, 90, 102) / .3))   # the deep pit in the NW corner
    return -5 * f - 11 * p


def height(x, z):
    m = moat(x, z)
    if m is not None: return m
    hills = 5.0 * (n2(x, z, 55, 2.1) + .7) + 1.6 * (n2(x, z, 22, 5.3) + .4)
    hills = max(0.0, hills)
    flat = max(box_mask(x, z, -60, 60, -30, 114, 26), box_mask(x, z, 40, 130, -125, -40, 22), box_mask(x, z, -140, -30, -135, -40, 22))
    for a, b, w in ROADS: flat = max(flat, 1 - sstep((seg_d(x, z, a, b) - w) / 20))
    return hills * (1 - flat)


def max_slope():
    worst, at = 0.0, None
    for i in range(int((X1 - X0) / 4)):
        for j in range(int((Z1 - Z0) / 4)):
            x, z = X0 + i * 4, Z0 + j * 4
            if inside(x, z, -49, 49, -19, 103) > 0: continue   # the moat's walls (stone in game)
            g = math.degrees(math.atan(math.hypot(height(x + 1, z) - height(x - 1, z), height(x, z + 1) - height(x, z - 1)) / 2))
            if g > worst: worst, at = g, (x, z)
    return worst, at


def axis_lines(lo, hi, fine_lo, fine_hi, extra):
    pts = set()
    v = lo
    while v <= hi + 1e-6:
        if not (fine_lo < v < fine_hi): pts.add(round(v, 3))
        v += 4
    v = fine_lo
    while v <= fine_hi + 1e-6: pts.add(round(v, 3)); v += 2
    for e in extra: pts.add(round(e, 3))
    return sorted(pts)


def mat(name):
    m = bpy.data.materials.get(name)
    if not m:
        m = bpy.data.materials.new(name); m.diffuse_color = (*COL.get(name, (1, 0, 1)), 1)
    return m


def grid_mesh(name, xs, zs, hf, smooth=True):
    nx = len(xs); nz = len(zs)
    bm = bmesh.new(); vs = []
    for z in zs:
        for x in xs: vs.append(bm.verts.new((x, z, hf(x, z))))
    for j in range(nz - 1):
        for i in range(nx - 1):
            a = j * nx + i
            bm.faces.new((vs[a], vs[a + 1], vs[a + nx + 1], vs[a + nx]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
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


def blk(name, x0, x1, y0, y1, z0, z1, parent):
    """a box by its bounds"""
    return cube(name, ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (x1 - x0, y1 - y0, z1 - z0), parent)


def cone(name, c, r0, r1, h, parent, n=12, smooth=True):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=n, radius1=r0, radius2=r1, depth=h)
    for v in bm.verts: v.co.z += h / 2
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = smooth
    return ob


def cyl_x(name, c, r, length, parent, n=12, smooth=True):
    """a cylinder lying along X, centred on c"""
    ob = cone(name, c, r, r, length, parent, n, smooth); ob.rotation_euler = (0, math.radians(90), 0); ob.location = (c[0] - length / 2, c[1], c[2])
    return ob


def cyl_y(name, c, r, length, parent, n=12, smooth=True):
    """a cylinder lying along Y, centred on c"""
    ob = cone(name, c, r, r, length, parent, n, smooth); ob.rotation_euler = (math.radians(-90), 0, 0); ob.location = (c[0], c[1] - length / 2, c[2])
    return ob


def disc_y(name, c, r, thick, parent, n=16):
    """a flat disc standing upright facing -Y (a window's round top, a wheel seen from the side is cyl_x)"""
    return cyl_y(name, c, r, thick, parent, n, smooth=False)


def sphere(name, c, r, parent, scale=(1, 1, 1), half=False, seg=(16, 10)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg[0], v_segments=seg[1], radius=r)
    if half: bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-4], context='VERTS')
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    ob = part(name, bm, parent); ob.location = c
    for p in ob.data.polygons: p.use_smooth = True
    return ob


def poly(name, verts, faces, parent, smooth=False):
    bm = bmesh.new(); vs = [bm.verts.new(v) for v in verts]
    for f in faces: bm.faces.new([vs[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = part(name, bm, parent)
    for p in ob.data.polygons: p.use_smooth = smooth
    return ob


def ring(name, c, r, thick, parent, n=20, axis='Y'):
    """a ring of small boxes (axis Y: upright, facing -Y; axis X: upright, facing X (a wheel); axis Z: lying flat)"""
    e = []
    for k in range(n):
        a = k / n * math.tau; seg = 2 * math.pi * r / n * 1.15
        if axis == 'Y':
            b = cube(name, (c[0] + math.cos(a) * r, c[1], c[2] + math.sin(a) * r), (thick, thick, seg), parent); b.rotation_euler = (0, -a + math.pi / 2, 0)
        elif axis == 'X':
            b = cube(name, (c[0], c[1] + math.cos(a) * r, c[2] + math.sin(a) * r), (thick, seg, thick), parent); b.rotation_euler = (a + math.pi / 2, 0, 0)
        else:
            b = cube(name, (c[0] + math.cos(a) * r, c[1] + math.sin(a) * r, c[2]), (thick, thick, seg), parent); b.rotation_euler = (math.pi / 2, 0, a)
        e.append(b)
    return e


def arch_fill(name, axis, a, b, spring, rise, top, t0, t1, parent, n=12):
    """the stone over an arched opening: from the arch's curve (elliptical, springing at `spring`, crown at spring + rise) up to `top`;
    the opening spans a..b along the wall (axis 'X': a wall running along X, t = its y range; axis 'Y': along Y, t = its x range)"""
    c, h = (a + b) / 2, (b - a) / 2
    pts = []
    for i in range(n + 1):
        s = a + (b - a) * i / n; u = (s - c) / h
        pts.append((s, spring + rise * math.sqrt(max(0.0, 1 - u * u))))
    def P(s, t, z): return (s, t, z) if axis == 'X' else (t, s, z)
    verts = []
    for s, z in pts: verts += [P(s, t0, z), P(s, t1, z), P(s, t0, top), P(s, t1, top)]
    faces = []
    for i in range(n):
        A, B = 4 * i, 4 * (i + 1)
        faces += [(A, B, B + 2, A + 2), (A + 1, A + 3, B + 3, B + 1), (A, A + 1, B + 1, B), (A + 2, B + 2, B + 3, A + 3)]
    faces += [(0, 2, 3, 1), (4 * n, 4 * n + 1, 4 * n + 3, 4 * n + 2)]
    ob = poly(name, verts, faces, parent)
    return ob, pts


def voussoirs(name, axis, a, b, spring, rise, face, out, parent, n=9, size=.8):
    """a ring of chunky stones round an arch, standing proud of the wall face (face = the wall's face coordinate, out = +-1)"""
    c, h = (a + b) / 2, (b - a) / 2
    for i in range(n):
        ang = math.pi * (i + .5) / n
        s = c - math.cos(ang) * (h + size / 2); z = spring + math.sin(ang) * (rise + size / 2)
        tang = math.atan2(math.cos(ang) * (rise + size / 2), math.sin(ang) * (h + size / 2))
        if axis == 'X':
            cube(name, (s, face + out * .1, z), (size * .85, .3, size), parent, 0, (0, -tang))
        else:
            cube(name, (face + out * .1, s, z), (.3, size * .85, size), parent, 0, (tang, 0))


def window(e, facing, p, w, h, glow=True):
    """a tall arched window on a wall face: p = the face point at the window's foot centre; facing '+X' '-X' '+Y' '-Y'"""
    sx = {'+X': 1, '-X': -1}.get(facing, 0); sy = {'+Y': 1, '-Y': -1}.get(facing, 0)
    pm = 'Window__WarmLight' if glow else 'Window__Dark'
    if sy:
        cube('Sill__Sandstone', (p[0], p[1] + sy * .1, p[2] - .1), (w + .5, .3, .2), e)
        cube(pm, (p[0], p[1] + sy * .03, p[2] + (h - w / 2) / 2), (w, .1, h - w / 2), e)
        disc_y(pm, (p[0], p[1] + sy * .03, p[2] + h - w / 2), w / 2, .1, e, 10)
        for s in (-1, 1): cube('Jamb__Sandstone', (p[0] + s * (w / 2 + .15), p[1] + sy * .08, p[2] + (h - w / 2) / 2), (.3, .2, h - w / 2), e)
    else:
        cube('Sill__Sandstone', (p[0] + sx * .1, p[1], p[2] - .1), (.3, w + .5, .2), e)
        cube(pm, (p[0] + sx * .03, p[1], p[2] + (h - w / 2) / 2), (.1, w, h - w / 2), e)
        cyl_x(pm, (p[0] + sx * .03, p[1], p[2] + h - w / 2), w / 2, .1, e, 10, smooth=False)
        for s in (-1, 1): cube('Jamb__Sandstone', (p[0] + sx * .08, p[1] + s * (w / 2 + .15), p[2] + (h - w / 2) / 2), (.2, .3, h - w / 2), e)


# ---------------------------------------------------------------------------------------------------------------- the castle
def merlon_run(e, axis, t0, t1, s0, s1, z, skip=lambda x, y: False, name='Merlon__CastleStone'):
    """merlons 1 m wide, 1.2 m tall, 1 m gaps along a wall's edge: axis 'X' runs along x (t = its y band), 'Y' along y"""
    n = int((s1 - s0 + 1) // 2)
    first = (s0 + s1) / 2 - (n - 1)
    for k in range(n):
        s = first + 2 * k
        x, y = (s, (t0 + t1) / 2) if axis == 'X' else ((t0 + t1) / 2, s)
        if skip(x, y): continue
        size = (1.0, t1 - t0, 1.2) if axis == 'X' else (t1 - t0, 1.0, 1.2)
        cube(name, (x, y, z + .6), size, e)


def near_tower(x, y, pad=.6):
    return any(math.hypot(x - tx, y - ty) < 5 + pad for tx, ty in TOWERS)


def round_tower(e, cx, cy, roof):
    cone('Tower__CastleStone', (cx, cy, 0), 5.0, 5.0, 16.0, e, 28)
    cone('Plinth__Stone', (cx, cy, 0), 5.3, 5.15, 1.0, e, 28)
    cone('Corbel__CastleStone', (cx, cy, 14.6), 5.0, 5.45, .6, e, 28)
    cone('Band__CastleStone', (cx, cy, 15.2), 5.45, 5.45, .8, e, 28)
    # arrow slits facing out of the castle
    out = math.atan2(cy - 30, cx)
    for da in (-.6, 0, .6):
        a = out + da
        for z in (4.5, 10.5):
            cube('Slit__Dark', (cx + math.cos(a) * 5.0, cy + math.sin(a) * 5.0, z), (.12, .32, 1.3), e, a)
    if roof:
        cone('Roof__Slate', (cx, cy, 16.0), 5.9, .15, 9.0, e, 24)
        cone('Eave__Slate', (cx, cy, 15.85), 5.95, 5.9, .3, e, 24)
        cone('Finial__Gold', (cx, cy, 24.9), .14, .02, 1.1, e, 6)
        sphere('Knob__Gold', (cx, cy, 25.1), .22, e, seg=(8, 6))
    else:
        cone('Top__Flagstone', (cx, cy, 15.95), 4.6, 4.6, .1, e, 24)
        n = 14
        for k in range(n):
            a = k / n * math.tau
            cube('Merlon__CastleStone', (cx + math.cos(a) * 5.05, cy + math.sin(a) * 5.05, 16.6), (.75, 1.05, 1.2), e, a)


def castle(e):
    S = 'Wall__CastleStone'
    # curtain walls (3 m thick, 10 m tall, the walkway on top at 10), merlons on the outer edge only
    blk(S, -33, -30, -3, 63, 0, 10, e); blk(S, 30, 33, -3, 63, 0, 10, e)
    blk(S, -33, -4, -3, 0, 0, 10, e); blk(S, 4, 33, -3, 0, 0, 10, e)
    arch_fill('Arch__CastleStone', 'X', -4, 4, 5.0, 2.0, 10, -3, 0, e)
    voussoirs('Voussoir__Sandstone', 'X', -4, 4, 5.0, 2.0, -3, -1, e)
    voussoirs('Voussoir__Sandstone', 'X', -4, 4, 5.0, 2.0, 0, 1, e)
    blk(S, -33, -18, 60, 63, 0, 10, e); blk(S, 18, 33, 60, 63, 0, 10, e)
    for x0, x1 in ((-33.25, -30), (30, 33.25)): blk('Plinth__Stone', x0, x1, -3, 63, 0, .8, e)
    blk('Plinth__Stone', -33, -8, -3.25, 0, 0, .8, e); blk('Plinth__Stone', 8, 33, -3.25, 0, 0, .8, e)
    # a stone string course under the walkway, outside
    blk('Course__Sandstone', -33.2, -33, -3, 63, 9.3, 9.6, e); blk('Course__Sandstone', 33, 33.2, -3, 63, 9.3, 9.6, e)
    blk('Course__Sandstone', -33, -8, -3.2, -3, 9.3, 9.6, e); blk('Course__Sandstone', 8, 33, -3.2, -3, 9.3, 9.6, e)
    merlon_run(e, 'Y', -33, -32.3, -3, 63, 10, near_tower)
    merlon_run(e, 'Y', 32.3, 33, -3, 63, 10, near_tower)
    merlon_run(e, 'X', -3, -2.3, -33, -8, 10, near_tower); merlon_run(e, 'X', -3, -2.3, 8, 33, 10, near_tower)
    merlon_run(e, 'X', 62.3, 63, -33, -18, 10, near_tower); merlon_run(e, 'X', 62.3, 63, 18, 33, 10, near_tower)
    # the gatehouse: two square towers flanking the gate, 14 m, crenellated tops
    for sx in (-1, 1):
        x0, x1 = (-8, -4) if sx < 0 else (4, 8)
        blk('Gatehouse__CastleStone', x0, x1, -6, 2, 0, 14, e)
        blk('Plinth__Stone', x0 - .2 * (sx < 0), x1 + .2 * (sx > 0), -6.25, 2, 0, .8, e)
        blk('Band__CastleStone', x0 - .25, x1 + .25, -6.25, 2.25, 12.8, 13.6, e)
        merlon_run(e, 'X', -6.25, -5.6, x0 - .25, x1 + .25, 14, name='Merlon__CastleStone')
        merlon_run(e, 'X', 1.6, 2.25, x0 - .25, x1 + .25, 14)
        merlon_run(e, 'Y', x0 - .25, x0 + .4, -4.5, .5, 14); merlon_run(e, 'Y', x1 - .4, x1 + .25, -4.5, .5, 14)
        for z in (4, 9):
            cube('Slit__Dark', ((x0 + x1) / 2, -6.02, z), (.32, .12, 1.3), e)
        # a banner bracket on each tower's front
        cube('Bracket__Iron', ((x0 + x1) / 2, -6.2, 11.8), (1.8, .25, .15), e)
    # the gate passage's floor of flagstones under the arch
    blk('Gateway__Flagstone', -4, 4, -6, 0, 0, .03, e)
    for tx, ty in TOWERS: round_tower(e, tx, ty, roof=ty < 30)
    keep(e)
    # the arcade along the inside of the east wall: 1 x 1 piers at x 24..25 every 4 m (z 8..52), arches between, the deck 6..6.6
    zs = list(range(8, 53, 4))
    for z in zs: blk('Pier__CastleStone', 24, 25, z - .5, z + .5, 0, 6, e)
    for z0, z1 in zip(zs, zs[1:]):
        arch_fill('ArcadeArch__CastleStone', 'Y', z0 + .5, z1 - .5, 4.2, 1.5, 6.0, 24, 25, e, 10)
    blk('Deck__CastleStone', 24, 30, 8, 52, 6, 6.6, e)
    blk('DeckEdge__Sandstone', 23.85, 24.15, 7.6, 52.4, 6.0, 6.75, e)
    # the courtyard: cobbles, a lighter flagstone path from the gate to the keep, a flagstone circle round the stone at (0, 34)
    blk('Floor__Dirt', -30, 30, 0, 63, 0, .02, e)
    blk('Path__Flagstone', -2.2, 2.2, 0, 59, 0, .03, e)
    blk('Path__Flagstone', -30, -4, 30.6, 32.4, 0, .03, e); blk('Path__Flagstone', 4, 24, 30.6, 32.4, 0, .03, e)
    cone('Circle__Flagstone', (0, 34, 0), 4.0, 4.0, .045, e, 32)
    for k in range(28):
        a = k / 28 * math.tau
        cube('Kerb__Stone', (math.cos(a) * 4.0, 34 + math.sin(a) * 4.0, .03), (.35, .95, .06), e, a)
    # the cobbles: rows of chunky stones (one mesh, tops and sides only) with dark joints between them
    rnd = random.Random(3); bm = bmesh.new()
    y = .1
    while y < 58.6:
        d = rnd.uniform(.75, 1.0); x = -29.9 + rnd.uniform(0, .6)
        while x < 23.6:
            w = rnd.uniform(.8, 1.35); cx, cy = x + w / 2, y + d / 2
            if not (abs(cx) < 2.8 or math.hypot(cx, cy - 34) < 4.6 or 30.1 < cy < 32.9 or near_tower(cx, cy, .2) or x + w > 23.8):
                h = rnd.uniform(.045, .075); i = .07
                lo = [bm.verts.new(v) for v in ((x, y, 0), (x + w, y, 0), (x + w, y + d, 0), (x, y + d, 0))]
                hi = [bm.verts.new(v) for v in ((x + i, y + i, h), (x + w - i, y + i, h), (x + w - i, y + d - i, h), (x + i, y + d - i, h))]
                bm.faces.new(hi)
                for k in range(4): bm.faces.new((lo[k], lo[(k + 1) % 4], hi[(k + 1) % 4], hi[k]))
            x += w + .09
        y += d + .09
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    part('Cobbles__Cobble', bm, e)
    # steps in front of the keep door (looks only)
    for k, (y0, h) in enumerate(((59, .15), (60.33, .3), (61.67, .45))):
        blk('Step__Flagstone', -5, 5, y0, 63, 0, h, e)
    # ivy patches on the walls
    for (x, y, z, sx, sy) in ((-29.9, 20, 3, .3, 2.2), (29.9, 40, 2.5, .3, 2.6), (-12, -.05 + .1, 3.5, 2.5, .3), (-29.9, 48, 6, .3, 1.6),
                              (16, 62.9, 3, 2.0, .3), (-33.1, 30, 4, .3, 2.4), (33.1, 14, 3, .3, 2.0)):
        for k in range(7):
            u = rnd.uniform(-1, 1); v = rnd.uniform(-1.6, 1.6)
            sphere('Ivy__Leaf', (x + u * (sx > 1) * sx * .6, y + u * (sy > 1) * sy * .6, z + v), rnd.uniform(.45, .8), e,
                   (.35 if sx < 1 else 1.0, .35 if sy < 1 else 1.0, 1.0), seg=(8, 6))
        sphere('Ivy__Leaf', (x, y, z - 2.4), .5, e, (.35 if sx < 1 else .8, .35 if sy < 1 else .8, 1.4), seg=(8, 6))
    # stone blocks standing proud of the faces (the film's chunky masonry)
    rnd = random.Random(17)
    KW = [(x, 8.9, 12.6) for x in (-11, -6.5, 6.5, 11)] + [(x, 15.9, 19.0) for x in (-12, -4, 4, 12)]
    SIDE = [(y, 8.9, 12.6) for y in (69, 75, 81)] + [(y, 15.9, 19.0) for y in (69, 75, 81)]
    NW = [(x, 8.9, 12.6) for x in (-12, -4, 4, 12)] + [(x, 15.9, 19.0) for x in (-12, -4, 4, 12)]
    gate = [(0, -.2, 7.6, 5.0)]
    for axis, face, out, a0, a1, z0, z1, holes in (
            ('Y', -33, -1, -3, 63, 1, 9.2, []), ('Y', 33, 1, -3, 63, 1, 9.2, []), ('Y', -30, 1, 0, 60, .3, 9.6, []),
            ('X', -3, -1, -33, 33, 1, 9.2, gate), ('X', 0, 1, -30, 30, .3, 9.6, gate), ('X', 63, 1, -33, -18, 1, 9.2, []), ('X', 63, 1, 18, 33, 1, 9.2, []),
            ('X', 60, -1, -30, -18, .3, 9.6, []), ('X', 60, -1, 18, 30, .3, 9.6, []),
            ('X', 63, -1, -18, 18, 1, 20.4, [(x, z0_, z1_, 1.3) for x, z0_, z1_ in KW] + [(0, -.2, 7.7, 4.2)]),
            ('X', 87, 1, -18, 18, 1, 20.4, [(x, z0_, z1_, 1.3) for x, z0_, z1_ in NW]),
            ('Y', -18, -1, 63, 87, 1, 20.4, [(y, z0_, z1_, 1.3) for y, z0_, z1_ in SIDE]), ('Y', 18, 1, 63, 87, 1, 20.4, [(y, z0_, z1_, 1.3) for y, z0_, z1_ in SIDE])):
        n = int((a1 - a0) * (z1 - z0) / 8)
        for k in range(n):
            s_ = rnd.uniform(a0 + .8, a1 - .8); z = rnd.uniform(z0, z1)
            if any(abs(s_ - hs) < hw and hz0 - .4 < z < hz1 + .4 for hs, hz0, hz1, hw in holes): continue
            if axis == 'Y' and near_tower(face, s_, .3): continue
            if axis == 'X' and face in (-3, 0, 63, 60) and near_tower(s_, face, .3): continue
            if face == -3 and abs(s_) < 8.4: continue   # the gatehouse
            w, h = rnd.uniform(.9, 1.7), rnd.uniform(.5, .8)
            c = 'Block__Sandstone' if k % 3 else 'Block__Flagstone'
            if axis == 'Y': cube(c, (face + out * .04, s_, z), (.16, w, h), e)
            else: cube(c, (s_, face + out * .04, z), (w, .16, h), e)


def keep(e):
    """x -18..18, z 63..87, 22 m, walls 3 m (the great hall inside: x -15..15, z 66..84, floor 0, ceiling slab 14..15); the door in the
    south face x -3..3, y 0..7 (arched); the roof walk at 22; the spired tower on the NE corner (centre (14, 83), r 3) from the roof up to
    34, its spire to 42"""
    K = 'Keep__CastleStone'
    blk(K, -18, -3, 63, 66, 0, 22, e); blk(K, 3, 18, 63, 66, 0, 22, e)
    arch_fill('KeepArch__CastleStone', 'X', -3, 3, 4.6, 2.4, 22, 63, 66, e)
    voussoirs('Voussoir__Sandstone', 'X', -3, 3, 4.6, 2.4, 63, -1, e, 9)
    blk(K, -18, 18, 84, 87, 0, 22, e)
    blk(K, -18, -15, 66, 84, 0, 22, e); blk(K, 15, 18, 66, 84, 0, 22, e)
    blk('Ceiling__Timber', -15, 15, 66, 84, 14, 15, e)
    for x in range(-12, 13, 4): blk('Beam__Timber', x - .25, x + .25, 66, 84, 13.6, 14, e)
    blk('Roof__Flagstone', -15, 15, 66, 84, 21, 22, e)
    blk('Plinth__Stone', -18.25, 18.25, 62.75, 87.25, 0, .8, e)
    blk('Corbel__CastleStone', -18.4, 18.4, 62.6, 87.4, 20.6, 22.0, e)
    merlon_run(e, 'X', 62.6, 63.3, -18.4, 18.4, 22); merlon_run(e, 'X', 86.7, 87.4, -18.4, 18.4, 22)
    merlon_run(e, 'Y', -18.4, -17.7, 64.6, 85.4, 22); merlon_run(e, 'Y', 17.7, 18.4, 64.6, 85.4, 22)
    # the door leaves, open against the inside of the south wall
    for s in (-1, 1):
        blk('Door__Plank', s * 3.05 if s > 0 else -6.05, s * 6.05 if s > 0 else -3.05, 66.0, 66.15, 0, 6.2, e)
        for z in (1.2, 4.8): blk('Strap__Iron', s * 3.05 if s > 0 else -6.05, s * 6.05 if s > 0 else -3.05, 66.15, 66.2, z, z + .15, e)
    # tall arched windows (glowing) outside, and their twins inside the hall
    for x in (-11, -6.5, 6.5, 11):
        window(e, '-Y', (x, 63, 9.0), 1.3, 3.6)
    for x in (-12, -4, 4, 12):
        window(e, '-Y', (x, 63, 16.0), 1.1, 3.0)
        window(e, '+Y', (x, 87, 9.0), 1.3, 3.6); window(e, '+Y', (x, 87, 16.0), 1.1, 3.0)
    for y in (69, 75, 81):
        window(e, '-X', (-18, y, 9.0), 1.3, 3.6); window(e, '+X', (18, y, 9.0), 1.3, 3.6)
        window(e, '-X', (-18, y, 16.0), 1.1, 3.0); window(e, '+X', (18, y, 16.0), 1.1, 3.0)
    for x in (-11, -6.5, 6.5, 11): window(e, '+Y', (x, 66, 8.0), 1.3, 3.6)
    for y in (69, 75, 81): window(e, '+X', (-15, y, 8.0), 1.3, 3.6)
    window(e, '-X', (15, 69, 8.0), 1.3, 3.6); window(e, '-X', (15, 81, 8.0), 1.3, 3.6)
    # the hall: a flagstone floor, the fireplace on the east wall (x 15 face, z 75), the gallery (north z 80..84, west x -15..-11, top at 6)
    blk('HallFloor__Flagstone', -15, 15, 66, 84, 0, .03, e)
    blk('Hearth__Stone', 13.2, 15, 72, 78, 0, .25, e)
    blk('Jamb__CastleStone', 13.6, 15, 72, 73, 0, 3.2, e); blk('Jamb__CastleStone', 13.6, 15, 77, 78, 0, 3.2, e)
    blk('Mantel__Sandstone', 13.2, 15, 71.6, 78.4, 3.2, 3.8, e)
    blk('Chimney__CastleStone', 14.0, 15, 72.4, 77.6, 3.8, 14, e)
    blk('Firebox__Dark', 14.7, 15, 73, 77, .25, 3.2, e)
    blk('Firebox__Dark', 13.6, 15, 73, 77, 2.9, 3.2, e)
    for k in range(3): cyl_y('Log__Bark', (14.25, 75, .42 + k * .05), .16, 2.6 - k * .4, e, 8).rotation_euler = (math.radians(-90), 0, math.radians(8 * (k - 1)))
    sphere('Fire__Ember', (14.2, 75, .45), .9, e, (.6, 1.6, .6), seg=(10, 6))
    for (x0, x1, y0, y1) in ((-15, 15, 80, 84), (-15, -11, 66, 80)): blk('Gallery__Plank', x0, x1, y0, y1, 5.6, 6.0, e)
    for x in range(-14, 15, 3): blk('Corbel__Timber', x - .2, x + .2, 82.6, 84, 5.0, 5.6, e)
    for y in range(68, 80, 3): blk('Corbel__Timber', -15, -13.6, y - .2, y + .2, 5.0, 5.6, e)
    for k in range(18):   # the railing on the open edges
        x = -11 + k * 26 / 17
        blk('Baluster__Wood', x - .06, x + .06, 79.9, 80.05, 6.0, 7.0, e)
    for k in range(10):
        y = 66.2 + k * 13.8 / 9
        blk('Baluster__Wood', -11.05, -10.9, y - .06, y + .06, 6.0, 7.0, e)
    blk('Rail__Wood', -11.1, 15, 79.85, 80.1, 6.95, 7.1, e); blk('Rail__Wood', -11.1, -10.85, 66, 80.1, 6.95, 7.1, e)
    blk('Rail__Wood', -11.1, 15, 79.9, 80.05, 6.45, 6.55, e); blk('Rail__Wood', -11.05, -10.9, 66, 80.0, 6.45, 6.55, e)
    # the slender spired tower (the film's landmark) on the keep's NE corner, rising from the roof walk
    cone('Spire__CastleStone', (14, 83, 21.0), 3.0, 3.0, 13.0, e, 20)
    cone('SpireBand__CastleStone', (14, 83, 32.8), 3.0, 3.4, 1.2, e, 20)
    cone('SpireRoof__Slate', (14, 83, 34.0), 3.7, .1, 8.0, e, 20)
    cone('Finial__Gold', (14, 83, 41.9), .12, .02, 1.2, e, 6)
    sphere('Knob__Gold', (14, 83, 42.1), .2, e, seg=(8, 6))
    for a in (0, 90, 180, 270):
        r = math.radians(a)
        cube('Window__WarmLight', (14 + math.cos(r) * 3.0, 83 + math.sin(r) * 3.0, 29.2), (.12, .7, 1.6), e, r)
        cube('Window__WarmLight', (14 + math.cos(r + .78) * 3.0, 83 + math.sin(r + .78) * 3.0, 25.0), (.12, .55, 1.2), e, r + .78)


# ---------------------------------------------------------------------------------------------------------------- small props
def wheel(e, c, r, thick=.14, spokes=8):
    """a big cart wheel standing in the YZ plane (its axle along X)"""
    ring('Wheel__Wood', c, r, thick, e, 16, 'X')
    for k in range(spokes):
        a = k / spokes * math.pi
        cube('Spoke__Wood', c, (.07, .07, 2 * r), e, 0, (a, 0))
    cyl_x('Hub__Iron', c, .13, .25, e, 10)


def hay_wagon(e, wreck=False):
    """bed 2 x 4 m (x -1..1, y -2..2), top of the bed at 1.15, four 0.75 m wheels, the shafts out front (-Y), heaped hay"""
    if not wreck:
        blk('Bed__Plank', -1, 1, -2, 2, 1.0, 1.15, e)
        for s in (-1, 1): blk('Side__Plank', s * .96 - .04, s * .96 + .04, -2, 2, 1.15, 1.6, e)
        blk('Side__Plank', -1, 1, 1.94, 2.02, 1.15, 1.6, e); blk('Side__Plank', -1, 1, -2.02, -1.94, 1.15, 1.45, e)
        for s in (-1, 1):
            for y in (-1.9, -.6, .6, 1.9): blk('Stake__Wood', s * 1.02 - .05, s * 1.02 + .05, y - .05, y + .05, .9, 1.75, e)
        blk('Axle__Iron', -1.15, 1.15, -1.35, -1.25, .7, .8, e); blk('Axle__Iron', -1.15, 1.15, 1.25, 1.35, .7, .8, e)
        for sx in (-1, 1):
            for sy in (-1, 1): wheel(e, (sx * 1.18, sy * 1.3, .75), .75)
        for s in (-1, 1):
            cube('Shaft__Wood', (s * .55, -3.4, .95), (.1, 3.0, .1), e, 0, (math.radians(-6), 0))
        blk('Yoke__Wood', -.6, .6, -4.85, -4.75, .78, .88, e)
        sphere('Load__Hay', (0, .05, 1.55), 1.0, e, (1.15, 2.15, .95))
        for (x, y, z, r) in ((-.5, -1.2, 2.1, .6), (.5, .9, 2.2, .65), (0, 0, 2.35, .7), (-.4, 1.5, 1.9, .5), (.6, -.4, 2.0, .55)):
            sphere('Load__Hay', (x, y, z), r, e, (1, 1.2, .7), seg=(10, 7))
        for k in range(14):
            a = k / 14 * math.tau
            cone('Straw__Hay', (math.cos(a) * 1.05, math.sin(a) * 2.0, 1.5), .04, .01, .5, e, 4).rotation_euler = (math.sin(a) * -.9, math.cos(a) * .9, 0)
    else:
        # the same cart smashed and burnt: the bed snapped and tilted, a wheel off and lying flat, charred boards, hay scattered
        cube('Bed__Plank', (0, .8, .7), (2, 2.4, .15), e, 0, (math.radians(-12), math.radians(6)))
        cube('Bed__Dark', (-.1, -1.1, .3), (1.9, 1.6, .15), e, math.radians(8), (math.radians(14), math.radians(-10)))
        cube('Side__Plank', (.96, .9, 1.0), (.08, 2.2, .45), e, 0, (math.radians(-12), math.radians(20)))
        cube('Side__Dark', (-1.1, .2, .5), (.08, 2.6, .4), e, math.radians(5), (0, math.radians(-70)))
        for sy in (-1, 1): wheel(e, (1.18, sy * 1.3, .75), .75)
        wheel(e, (-1.15, 1.3, .75), .75)
        ob = cone('Wheel__Dark', (-2.6, -1.0, 0), .78, .78, .14, e, 16)   # the loose wheel lying flat
        cone('Hub__Iron', (-2.6, -1.0, .14), .13, .13, .12, e, 10)
        rnd = random.Random(5)
        for k in range(9):
            cube('Board__Dark' if k % 2 else 'Board__Plank', (rnd.uniform(-2.8, 2.8), rnd.uniform(-3.5, 3), .06), (rnd.uniform(.9, 1.8), .18, .07), e, rnd.random() * 3,
                 (0, rnd.uniform(-.15, .15)))
        cube('Shaft__Wood', (.55, -3.0, .35), (.1, 2.6, .1), e, math.radians(15), (math.radians(8), 0))
        cube('Shaft__Dark', (-.9, -3.1, .06), (.1, 1.6, .1), e, math.radians(-40))
        for k in range(10):
            sphere('Hay__Hay', (rnd.uniform(-3, 3), rnd.uniform(-3.5, 3.5), 0), rnd.uniform(.3, .7), e, (1.3, 1, .4), half=True, seg=(8, 6))
        for k in range(4):
            sphere('Ash__Dark', (rnd.uniform(-1.2, 1.2), rnd.uniform(-1.5, 1.5), 0), rnd.uniform(.5, .9), e, (1.4, 1.1, .2), half=True, seg=(8, 5))
        sphere('Embers__Ember', (-.2, -.6, .05), .5, e, (1.4, 1, .25), seg=(8, 5))


def props():
    obs = []
    def P(name):
        e = empty(name); obs.append(e); return e
    castle(P('Castle'))
    e = P('Pedestal')   # the round stone drum the purple shard is stuck in (film: shard-in-stone): 1.1 m tall, 1.4 m wide
    cone('Drum__CastleStone', (0, 0, 0), .72, .68, .95, e, 20)
    cone('Base__CastleStone', (0, 0, 0), .8, .76, .16, e, 20)
    cone('Rim__CastleStone', (0, 0, .92), .74, .8, .1, e, 20); cone('Top__CastleStone', (0, 0, 1.02), .8, .76, .08, e, 20)
    blk('Slot__Dark', -.2, .2, -.07, .07, 1.06, 1.105, e)
    for (a, z) in ((.4, .5), (2.1, .3), (3.7, .7), (5.0, .2)):
        cube('Chip__Stone', (math.cos(a) * .7, math.sin(a) * .7, z), (.22, .18, .14), e, a)
    for a in (.9, 3.3):
        sphere('Moss__Leaf', (math.cos(a) * .72, math.sin(a) * .72, .12), .16, e, (.6, 1.2, .5), seg=(8, 5)).rotation_euler = (0, 0, a)
    e = P('PurpleShard')   # a tall purple crystal, 0.8 m, origin at its base
    cone('Crystal__PurpleShard', (0, 0, 0), .15, .19, .5, e, 6, smooth=False)
    cone('Crystal__PurpleShard', (0, 0, .5), .19, .0, .3, e, 6, smooth=False)
    for ob in e.children: ob.rotation_euler = (math.radians(4), math.radians(-3), .3)
    hay_wagon(P('HayWagon')); hay_wagon(P('WagonWreck'), True)
    e = P('Torch')   # on a wall: the wall is behind it (+Y), the torch leans out (-Y); origin at the bracket's plate on the wall
    blk('Plate__Iron', -.12, .12, -.04, 0, -.25, .25, e)
    cube('Arm__Iron', (0, -.17, 0), (.05, .32, .05), e)
    ring('Cup__Iron', (0, -.33, .08), .07, .03, e, 8, 'Z')
    cube('Stick__Wood', (0, -.33, .2), (.07, .07, .65), e, 0, (math.radians(-12), 0))
    cone('Wrap__Rope', (0, -.39, .46), .06, .07, .14, e, 8)
    cone('Flame__Ember', (0, -.4, .58), .11, .0, .38, e, 8)
    e = P('Brazier')   # an iron bowl on three legs, 1.1 m; Coals__Ember is the part the game lights
    for k in range(3):
        a = k / 3 * math.tau
        ob = cube('Leg__Iron', (math.cos(a) * .32, math.sin(a) * .32, .42), (.06, .06, .9), e)
        ob.rotation_euler = (math.sin(a) * math.radians(14), -math.cos(a) * math.radians(14), 0)
    cone('Bowl__Iron', (0, 0, .78), .22, .5, .3, e, 14)
    cone('Lip__Iron', (0, 0, 1.06), .52, .52, .05, e, 14)
    cone('Coals__Ember', (0, 0, 1.0), .45, .38, .08, e, 12)
    e = P('Bench')   # 2 m along X, seat 0.45 m, the back at +Y (it faces -Y)
    blk('Seat__Plank', -1, 1, -.22, .22, .38, .48, e)
    for s in (-1, 1):
        blk('Leg__Wood', s * .82 - .07, s * .82 + .07, -.2, .2, 0, .38, e)
        blk('Post__Wood', s * .82 - .06, s * .82 + .06, .14, .26, .48, .95, e)
        blk('Arm__Wood', s * .95 - .05, s * .95 + .05, -.2, .25, .62, .7, e)
    blk('Back__Plank', -1, 1, .17, .25, .62, .9, e); blk('Stretcher__Wood', -.82, .82, -.04, .04, .12, .2, e)
    e = P('FlowerPot')   # a terracotta pot (0.5 m) with a bush of pink, yellow and violet flowers, about 1 m high
    cone('Pot__Terracotta', (0, 0, 0), .28, .36, .45, e, 14); cone('Rim__Terracotta', (0, 0, .43), .4, .4, .08, e, 14)
    cone('Soil__Dirt', (0, 0, .47), .34, .34, .03, e, 12)
    sphere('Bush__Leaf', (0, 0, .62), .38, e, (1.1, 1.1, .7), seg=(10, 7))
    rnd = random.Random(9)
    for k in range(18):
        a = rnd.random() * math.tau; r = rnd.uniform(.12, .4); z = .62 + math.sqrt(max(0, .16 - r * r * .6)) * .9
        sphere('Flower__' + ('FlowerPink', 'FlowerYellow', 'FlowerViolet', 'Red')[k % 4], (math.cos(a) * r, math.sin(a) * r, z), .075, e, (1, 1, .6), seg=(6, 4))
    e = P('Barrel')
    cone('Staves__Plank', (0, 0, 0), .38, .42, .5, e, 14); cone('Staves__Plank', (0, 0, .5), .42, .38, .5, e, 14)
    for z in (.12, .5, .88): cone('Hoop__Iron', (0, 0, z - .03), .43, .43, .06, e, 14)
    cone('Lid__Wood', (0, 0, .99), .36, .36, .03, e, 14)
    e = P('Crate')   # 0.9 m
    blk('Box__Plank', -.43, .43, -.43, .43, .02, .88, e)
    for sx in (-1, 1):
        for sy in (-1, 1): blk('Edge__Wood', sx * .45 - .05, sx * .45 + .05, sy * .45 - .05, sy * .45 + .05, 0, .9, e)
    for sx in (-1, 1):
        for z in (.05, .85): blk('Edge__Wood', -.45, .45, sx * .45 - .05, sx * .45 + .05, z - .05, z + .05, e); blk('Edge__Wood', sx * .45 - .05, sx * .45 + .05, -.45, .45, z - .05, z + .05, e)
    for s in (-1, 1): cube('Brace__Wood', (0, s * .45, .45), (1.1, .06, .1), e, 0, (0, math.radians(s * 42)))
    e = P('Boulder')   # a big round stone ~3 m
    rnd = random.Random(21)
    for (x, y, z, s) in ((0, 0, 1.3, (1.5, 1.4, 1.35)), (.7, .4, .7, (1.0, .9, .8)), (-.8, -.3, .6, (.9, .9, .75)), (.2, -.7, .5, (.8, .7, .6))):
        sphere('Rock__Stone', (x, y, z), 1, e, s, seg=(12, 8)).rotation_euler = (0, 0, rnd.random() * 3)
    sphere('Moss__Leaf', (-.3, .2, 2.3), .7, e, (1.3, 1.1, .35), seg=(10, 6))
    e = P('Bramble')   # a thorny bush ~2 m across, 1.4 m tall
    rnd = random.Random(13)
    for (x, y, z, r) in ((0, 0, .6, .8), (.6, .3, .45, .6), (-.6, -.2, .5, .65), (.1, -.5, .4, .55), (-.2, .6, .5, .55), (.2, .1, 1.0, .5)):
        sphere('Bush__Bramble', (x, y, z), r, e, (1, 1, .85), seg=(10, 7))
    for k in range(40):
        a = rnd.random() * math.tau; el = rnd.uniform(-.2, 1.2); r = .85
        p = (math.cos(a) * math.cos(el) * r, math.sin(a) * math.cos(el) * r, .55 + math.sin(el) * r * .9)
        th = cone('Thorn__Bramble', p, .05, .0, .3, e, 4, smooth=False)
        th.rotation_euler = (-math.sin(a) * (math.pi / 2 - el), math.cos(a) * (math.pi / 2 - el), 0)
    for k in range(8):
        a = rnd.random() * math.tau
        sphere('Berry__BannerPurple', (math.cos(a) * .8, math.sin(a) * .8, rnd.uniform(.4, 1.0)), .06, e, seg=(6, 4))
    e = P('Banner')   # hangs from its rod (origin at the rod's middle), 1.2 x 4 m, its face to -Y: red with a gold crown and lion-ish shield
    cyl_x('Rod__Wood', (0, 0, 0), .05, 1.6, e, 8)
    for s in (-1, 1): sphere('Knob__Gold', (s * .82, 0, 0), .08, e, seg=(8, 6))
    blk('Cloth__BannerRed', -.6, .6, -.04, .04, -3.5, -.05, e)
    for s in (-1, 1):   # a swallowtail foot
        poly('Tail__BannerRed', [(s * .6, -.04, -3.5), (0, -.04, -3.5), (s * .6, -.04, -4.0), (s * .6, .04, -3.5), (0, .04, -3.5), (s * .6, .04, -4.0)],
             [(0, 1, 2), (3, 5, 4), (0, 2, 5, 3), (1, 4, 5, 2), (0, 3, 4, 1)], e)
    for s in (-1, 1): blk('Trim__Gold', s * .6 - .04, s * .6 + .04, -.06, .06, -3.5, -.05, e)
    blk('Trim__Gold', -.62, .62, -.06, .06, -.35, -.25, e)
    blk('Motif__Gold', -.3, .3, -.07, -.04, -1.1, -.95, e)
    for x in (-.25, 0, .25): cone('Motif__Gold', (x, -.055, -.95), .07, .0, .3 if x else .4, e, 4, smooth=False)
    cube('Motif__Gold', (0, -.055, -2.0), (.55, .03, .55), e, 0, (0, math.radians(45)))
    cube('Motif__BannerRed', (0, -.075, -2.0), (.32, .03, .32), e, 0, (0, math.radians(45)))
    e = P('BannerPole')   # a 6 m pole with a pennant flying to +X
    cone('Pole__Wood', (0, 0, 0), .09, .07, 6.0, e, 8)
    sphere('Knob__Gold', (0, 0, 6.05), .14, e, seg=(8, 6))
    poly('Pennant__BannerBlue', [(0, -.03, 5.85), (0, -.03, 5.0), (2.0, -.03, 5.45), (0, .03, 5.85), (0, .03, 5.0), (2.0, .03, 5.45)],
         [(0, 1, 2), (3, 5, 4), (0, 2, 5, 3), (1, 4, 5, 2), (0, 3, 4, 1)], e)
    e = P('Chandelier')   # origin at its hook (it hangs down 3 m): an iron ring 2.4 m across with 8 candles
    ring('Ring__Iron', (0, 0, -3.0), 1.2, .09, e, 20, 'Z')
    for a in (0, 90): cube('Cross__Iron', (0, 0, -3.0), (2.4, .07, .07), e, math.radians(a))
    for k in range(4):
        a = k / 4 * math.tau + math.pi / 4
        x, y = math.cos(a) * 1.15, math.sin(a) * 1.15
        L = math.sqrt(x * x + y * y + 9)
        ob = cube('Chain__Iron', (x / 2, y / 2, -1.5), (.03, .03, L), e)
        ob.rotation_euler = Vector((x, y, -3.0)).normalized().to_track_quat('Z', 'Y').to_euler()
    cyl_y('Hook__Iron', (0, 0, -.05), .08, .03, e, 8)
    for k in range(8):
        a = k / 8 * math.tau
        x, y = math.cos(a) * 1.2, math.sin(a) * 1.2
        cone('Cup__Iron', (x, y, -2.98), .08, .1, .06, e, 8)
        cone('Candle__Cream', (x, y, -2.92), .05, .05, .26, e, 8)
        cone('Flame__Ember', (x, y, -2.66), .045, .0, .14, e, 6)
    e = P('FeastTable')   # 12 m along X, 1.3 m wide, top at 0.84; benches both sides (y +-1.15)
    blk('Top__Wood', -6, 6, -.65, .65, .78, .84, e)
    for x in (-5.5, -2, 2, 5.5): blk('Trestle__Timber', x - .08, x + .08, -.5, .5, 0, .78, e); blk('Foot__Timber', x - .1, x + .1, -.55, .55, 0, .1, e)
    blk('Runner__BannerRed', -5.8, 5.8, -.22, .22, .84, .85, e)
    for s in (-1, 1):
        blk('Bench__Plank', -5.8, 5.8, s * 1.15 - .2, s * 1.15 + .2, .4, .47, e)
        for x in (-5.2, 0, 5.2): blk('BenchLeg__Wood', x - .08, x + .08, s * 1.15 - .16, s * 1.15 + .16, 0, .4, e)
        for k in range(10):
            x = -5.1 + k * 1.13
            cone('Plate__Cream', (x, s * .42, .84), .16, .16, .02, e, 10)
            cone('Goblet__Gold', (x + .32, s * .38, .84), .03, .05, .1, e, 6); cone('Goblet__Gold', (x + .32, s * .38, .94), .05, .07, .09, e, 6)
    for x in (-2.6, 2.6):
        cone('Platter__Iron', (x, 0, .84), .42, .42, .03, e, 12)
        sphere('Roast__Terracotta', (x, 0, .97), .25, e, (1.4, 1, .75), seg=(10, 7))
        for s in (-1, 1): cyl_x('Bone__Cream', (x + s * .42, 0, 1.0), .035, .2, e, 6)
    for x in (-4.5, -.3, 4.0):
        sphere('Bread__Wicker', (x, .1, .9), .14, e, (1.6, .8, .55), seg=(8, 6))
        cone('Bowl__Plank', (x + .9, 0, .84), .12, .2, .1, e, 10)
        for k in range(4): sphere('Fruit__' + ('Red', 'FlowerYellow', 'Green', 'Red')[k], (x + .9 + (k % 2 - .5) * .14, (k // 2 - .5) * .14, .97), .07, e, seg=(6, 4))
    for x in (-1.3, 1.3): cone('Candle__Cream', (x, 0, .84), .05, .05, .3, e, 8); cone('Flame__Ember', (x, 0, 1.14), .045, .0, .14, e, 6)
    for name, w, h in (('Portcullis', 8.0, 7.0), ('SmallPortcullis', 4.0, 3.5)):
        e = P(name)   # an iron grid w x h (x -w/2..w/2, z 0..h), spikes at its foot
        n = int(round(w / .5))
        for k in range(n + 1):
            x = -w / 2 + .1 + k * (w - .2) / n
            blk('Bar__Iron', x - .07, x + .07, -.07, .07, .3, h, e)
            cone('Spike__Iron', (x, 0, 0), .0, .09, .3, e, 4, smooth=False)
        for k in range(int(h // 1.6) + 1):
            z = .9 + k * (h - 1.0) / max(1, int(h // 1.6))
            blk('Rail__Iron', -w / 2, w / 2, -.1, .1, z - .07, z + .07, e)
    e = P('Drawbridge')   # a plank deck 8 x 15 m: the hinge at the origin, the deck running out to -Y, its flat walkable top at z 0
    for k in range(30):
        blk('Deck__Plank' if k % 3 else 'Deck__Wood', -4, 4, -15 + k * .5 + .02, -15 + (k + 1) * .5 - .02, -.35, 0, e)
    for x in (-3.4, 0, 3.4): blk('Strap__Iron', x - .12, x + .12, -15, 0, -.02, .03, e)
    for x in (-3.9, 3.9): blk('Beam__Timber', x - .15, x + .15, -15, 0, -.7, -.05, e)
    blk('Edge__Iron', -4.05, 4.05, -15.1, -14.9, -.4, .05, e)
    for s in (-1, 1):
        ring('Ring__Iron', (s * 3.7, -14.6, .25), .18, .05, e, 8, 'X')
        # the chains from the deck's far end up to the gate (anchors at 9 m over the hinge); a separate part the game may hide
        a = Vector((s * 3.7, -14.6, .3)); b = Vector((s * 3.7, 0, 9.0)); d = b - a
        for k in range(28):
            p = a + d * ((k + .5) / 28)
            ob = cube('Chain__Iron', p, (.05, .14 if k % 2 else .05, d.length / 28 * 1.1), e)
            ob.rotation_euler = d.normalized().to_track_quat('Z', 'Y').to_euler()
    e = P('Sword')   # origin at the pommel's foot, the blade up (+Z): 1.15 m
    sphere('Pommel__Gold', (0, 0, .04), .045, e, seg=(8, 6))
    cone('Grip__Dark', (0, 0, .07), .025, .025, .18, e, 8)
    blk('Guard__Gold', -.16, .16, -.025, .025, .25, .29, e)
    blk('Blade__Steel', -.035, .035, -.008, .008, .29, 1.0, e)
    poly('Tip__Steel', [(-.035, -.008, 1.0), (.035, -.008, 1.0), (0, -.008, 1.12), (-.035, .008, 1.0), (.035, .008, 1.0), (0, .008, 1.12)],
         [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (0, 2, 5, 3)], e)
    blk('Fuller__Iron', -.008, .008, -.01, .01, .32, .85, e)
    e = P('Shield')   # a heater shield 0.7 x 0.95 m, its face to -Y: red with a gold rim and emblem; origin at its middle
    out = [(-.35, .45), (.35, .45)]
    for k in range(1, 9):
        t = k / 8; out.append((.35 * math.cos(t * math.pi / 2) ** .8, .1 - .6 * t ** 1.15))
    out = out + [(-x, z) for x, z in reversed(out[2:-1])]
    vs = [(x, -.04, z) for x, z in out] + [(x, .04, z) for x, z in out]; n = len(out)
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    poly('Face__BannerRed', vs, faces, e)
    for i in range(n):
        (x0, z0), (x1, z1) = out[i], out[(i + 1) % n]
        cube('Rim__Gold', ((x0 + x1) / 2, -.03, (z0 + z1) / 2), (math.hypot(x1 - x0, z1 - z0) + .04, .1, .05), e, 0, (0, -math.atan2(z1 - z0, x1 - x0)))
    for s in (-1, 1): cube('Emblem__Gold', (s * .1, -.06, .02), (.07, .03, .42), e, 0, (0, math.radians(s * 35)))
    sphere('Emblem__Gold', (0, -.05, .22), .08, e, (1, .4, 1), seg=(8, 6))
    for x in (-.12, 0, .12): cone('Emblem__Gold', (x, -.06, .29), .035, .0, .12, e, 4)
    blk('Strap__Wood', -.25, .25, .04, .07, -.02, .06, e)
    e = P('Bow')   # a longbow 1.75 m, standing up (Z), bowed to +X, the string at x 0; origin at the grip
    pts = [(.22 * (1 - (z / .87) ** 2), z) for z in [-.87 + k * 1.74 / 12 for k in range(13)]]
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        cube('Limb__Wood', ((x0 + x1) / 2, 0, (z0 + z1) / 2), (.04, .035, math.hypot(x1 - x0, z1 - z0) + .02), e, 0, (0, math.atan2(x1 - x0, z1 - z0)))
    cube('Grip__Rope', (.22, 0, 0), (.055, .05, .16), e)
    blk('String__Cream', -.005, .005, -.005, .005, -.87, .87, e)
    e = P('Arrow')   # 0.85 m, pointing -Y; origin at its middle
    cyl_y('Shaft__Wood', (0, 0, 0), .012, .8, e, 6)
    cone('Head__Steel', (0, -.4, 0), .035, .0, .1, e, 4, smooth=False).rotation_euler = (math.radians(90), 0, 0)
    for a in (0, 120, 240):
        r = math.radians(a)
        cube('Fletch__BannerRed', (math.cos(r) * .025, .33, math.sin(r) * .025), (.04, .12, .005), e, 0, (r, 0)).rotation_euler = (0, -r, 0)
    e = P('Key')   # a big iron key 0.4 m, standing (the bow on top); origin at its tip
    ring('Bow__Iron', (0, 0, .33), .055, .025, e, 10, 'Y')
    blk('Shank__Iron', -.014, .014, -.014, .014, 0, .28, e)
    blk('Bit__Iron', 0, .07, -.012, .012, .02, .05, e); blk('Bit__Iron', 0, .05, -.012, .012, .07, .1, e)
    cone('Collar__Iron', (0, 0, .26), .025, .025, .03, e, 8)
    e = P('CellBars')   # a cell door of iron bars, 2 x 2.8 m (x -1..1, z 0..2.8), its face to -Y
    for x in (-1, 1): blk('Frame__Iron', x - .07, x + .07, -.07, .07, 0, 2.8, e)
    for z in (0, 2.8): blk('Frame__Iron', -1, 1, -.07, .07, z - .07, z + .07, e)
    for k in range(7):
        x = -.78 + k * .26; blk('Bar__Iron', x - .03, x + .03, -.03, .03, 0, 2.8, e)
    for z in (.9, 1.9): blk('Band__Iron', -1, 1, -.05, .05, z - .05, z + .05, e)
    blk('Lock__Dark', .62, .9, -.12, .06, 1.1, 1.4, e)
    for z in (.4, 2.4): cyl_y('Hinge__Iron', (-1.05, 0, z), .05, .2, e, 6).rotation_euler = (0, 0, 0)
    e = P('Chains')   # wall shackles: the wall behind (+Y), two chains hanging from a plate, iron cuffs; origin at the plate on the wall
    blk('Plate__Iron', -.35, .35, -.04, 0, -.12, .12, e)
    for s in (-1, 1):
        for k in range(8):
            z = -.05 - k * .085; cube('Link__Iron', (s * .22 + s * k * .01, -.06, z), (.05 if k % 2 else .015, .015 if k % 2 else .05, .1), e)
        ring('Cuff__Iron', (s * .3, -.07, -.8), .07, .03, e, 8, 'Z')
    e = P('HayBale')   # a square bale 1.2 x .7 x .6, twine round it
    cube('Bale__Hay', (0, 0, .3), (1.2, .7, .6), e)
    for sx in (-.3, .3): cube('Twine__Rope', (sx, 0, .3), (.04, .72, .62), e)
    for name, ground, upper, roofm, ridge, chim in (('House1', 'CastleStone', 'Plaster', 'Slate', 'X', True),
                                                     ('House2', 'Plaster', 'Cream', 'Terracotta', 'Y', False),
                                                     ('House3', 'Cream', 'Plaster', 'Slate', 'X', True)):
        house(P(name), ground, upper, roofm, ridge, chim, name)
    e = P('MarketStall')   # footprint 3 x 1.5 (x -1.5..1.5, y -.75..0.75), the counter at 1 m in front (-Y), a striped awning
    for sx in (-1.45, 1.45):
        for sy in (-.7, .7): blk('Post__Wood', sx - .06, sx + .06, sy - .06, sy + .06, 0, 2.6 if sy > 0 else 2.2, e)
    blk('Counter__Plank', -1.5, 1.5, -.75, -.25, 0, 1.0, e); blk('Top__Wood', -1.55, 1.55, -.8, -.2, 1.0, 1.06, e)
    ang = math.atan2(.6, 2.0)
    for k in range(6):
        x = -1.6 + (k + .5) * 3.2 / 6
        cube('Awning__' + ('BannerRed' if k % 2 == 0 else 'Canvas'), (x, -.05, 2.45), (3.2 / 6 + .01, 2.1, .05), e, 0, (ang, 0))
        cone('Scallop__' + ('BannerRed' if k % 2 == 0 else 'Canvas'), (x, -1.06, 2.03), .22, .22, .04, e, 8).rotation_euler = (math.radians(90 - 17), 0, 0)
    for k in range(8):
        sphere('Apple__' + ('Red' if k < 5 else 'Green'), (-1.2 + k * .14, -.5, 1.12), .06, e, seg=(6, 4))
    for k in range(3): sphere('Cabbage__Leaf', (.3 + k * .3, -.5, 1.15), .12, e, seg=(8, 5))
    for k in range(2): sphere('Bread__Wicker', (-.5 + k * .3, -.45, 1.12), .1, e, (1.5, .8, .6), seg=(8, 5))
    cone('Basket__Wicker', (1.1, -.5, 1.06), .14, .18, .14, e, 10)
    blk('Shelf__Plank', -1.4, 1.4, .3, .7, .9, .96, e)
    for k in range(4): cone('Jug__Terracotta', (-1 + k * .65, .5, .96), .1, .07, .28, e, 8)
    e = P('Smithy')   # an open shed 5 x 4 (x -2.5..2.5, y -2..2), open to the front (-Y): stone back wall, a forge, an anvil
    blk('Back__CastleStone', -2.5, 2.5, 1.6, 2.0, 0, 2.8, e)
    for x in (-2.4, 2.4): blk('Post__Timber', x - .1, x + .1, -1.9, -1.7, 0, 2.9, e)
    blk('Lintel__Timber', -2.5, 2.5, -1.92, -1.68, 2.75, 2.95, e)
    cube('Roof__Slate', (0, -.05, 3.15), (5.6, 4.6, .18), e, 0, (math.radians(-6), 0))
    blk('Forge__Stone', -2.3, -.9, .4, 1.6, 0, 1.0, e); blk('Coals__Ember', -2.15, -1.05, .55, 1.45, 1.0, 1.08, e)
    cone('Hood__Iron', (-1.6, 1.0, 1.9), .8, .25, .9, e, 4); blk('Flue__Iron', -1.75, -1.45, .85, 1.15, 2.8, 3.6, e)
    cone('Stump__Bark', (.6, -.3, 0), .32, .3, .55, e, 10)
    blk('Anvil__Iron', .3, .9, -.45, -.15, .55, .7, e); blk('Anvil__Iron', .2, 1.05, -.48, -.12, .7, .85, e)
    cone('Horn__Iron', (1.05, -.3, .77), .08, .0, .3, e, 6).rotation_euler = (0, math.radians(90), 0)
    cone('Tub__Plank', (1.8, .6, 0), .4, .4, .55, e, 12); cone('Water__Water', (1.8, .6, .5), .35, .35, .02, e, 12)
    cube('Hammer__Wood', (.6, -.25, .9), (.04, .4, .04), e); blk('HammerHead__Iron', .55, .65, -.5, -.4, .87, .93, e)
    for k in range(3): cube('Tool__Iron', (-.6 + k * .4, 1.55, 1.6), (.05, .04, .7), e)
    e = P('Stands')   # tiered wooden bleachers 20 m along X: tiers at 1, 2, 3 m, each 1.5 m deep (y 0..4.5), the front edge at y 0
    for k in range(3):
        y0 = k * 1.5
        blk('Tier__Plank', -10, 10, y0, 4.5, 0, k + 1.0, e)
        blk('Seat__Wood', -10, 10, y0, y0 + .35, k + 1.0, k + 1.06, e)
    for k in range(10):
        blk('Bunting__' + ('BannerRed' if k % 2 == 0 else 'BannerBlue'), -10 + k * 2, -8 + k * 2, -.03, 0, .1, .9, e)
        blk('Bunting__' + ('FlowerYellow' if k % 2 == 0 else 'Canvas'), -10 + k * 2, -8 + k * 2, 1.47, 1.5, 1.1, 1.9, e)
    for x in (-10, 10): blk('Rail__Wood', x - .06, x + .06, 0, 4.5, 3.0, 4.0, e)
    blk('Rail__Wood', -10, 10, 4.4, 4.5, 3.9, 4.0, e)
    for k in range(5):
        x = -10 + k * 5
        blk('Pole__Wood', x - .07, x + .07, 4.4, 4.54, 3.0, 7.0, e)
        poly('Pennant__' + ('BannerRed' if k % 2 else 'BannerBlue'), [(x, 4.45, 6.9), (x, 4.45, 6.2), (x + 1.4, 4.45, 6.55), (x, 4.5, 6.9), (x, 4.5, 6.2), (x + 1.4, 4.5, 6.55)],
             [(0, 1, 2), (3, 5, 4), (0, 2, 5, 3), (1, 4, 5, 2), (0, 3, 4, 1)], e)
    for name, colour in (('Tent', 'BannerRed'), ('TentBlue', 'BannerBlue')):
        tent(P(name), colour)
    e = P('ListBarrier')   # the jousting tilt: one 10 m length along X, 1.3 m tall, striped boards
    for k in range(5):
        x = -5 + k * 2.5; blk('Post__Wood', x - .08, x + .08, -.08, .08, 0, 1.3, e)
    for k in range(10):
        blk('Board__' + ('BannerRed' if k % 2 == 0 else 'Canvas'), -5 + k, -4 + k, -.05, .05, .55, 1.15, e)
    blk('Cap__Wood', -5.05, 5.05, -.1, .1, 1.15, 1.3, e)
    e = P('HighStriker')   # the test of strength: a 6 m post, a painted scale on its front (-Y), the bell (Gold) on top, the puck,
    blk('Base__Plank', -.8, .8, -.7, .7, 0, .3, e)   # a base with the lever pad in front
    blk('Post__Wood', -.2, .2, -.2, .2, .3, 6.3, e)
    for k, c in enumerate(('Leaf', 'Leaf', 'FlowerYellow', 'FlowerYellow', 'Terracotta', 'BannerRed', 'BannerRed')):
        blk('Scale__' + c, -.16, .16, -.23, -.2, .6 + k * .78, .6 + (k + 1) * .78 - .06, e)
    for s in (-1, 1): blk('Track__Iron', s * .12 - .02, s * .12 + .02, -.27, -.23, .4, 6.0, e)
    blk('Puck__BannerRed', -.15, .15, -.36, -.24, .4, .6, e)
    blk('Top__Wood', -.32, .32, -.32, .32, 6.3, 6.45, e)
    sphere('Bell__Gold', (0, -.3, 6.15), .28, e, seg=(12, 8), half=True)
    cube('Lever__Wood', (0, -.85, .42), (.25, 1.1, .12), e, 0, (math.radians(10), 0))
    blk('Pad__BannerRed', -.3, .3, -1.6, -1.2, .48, .62, e)
    blk('Pivot__Iron', -.2, .2, -.55, -.45, .3, .45, e)
    e = P('Mallet')   # a big wooden mallet 1.4 m, origin at the handle's end, the head up (+Z), along X
    cone('Handle__Wood', (0, 0, 0), .04, .045, 1.2, e, 8)
    cyl_x('Head__Plank', (0, 0, 1.3), .2, .55, e, 12)
    for x in (-.24, .24): cyl_x('Band__Iron', (x, 0, 1.3), .21, .05, e, 12)
    e = P('Log')   # a heavy log 3 m along X, r 0.35
    cyl_x('Bark__Bark', (0, 0, .35), .35, 3.0, e, 10)
    for s in (-1, 1): cyl_x('Cut__Plank', (s * 1.51, 0, .35), .3, .02, e, 10)
    cube('Stub__Bark', (.6, 0, .7), (.12, .12, .25), e, 0, (.3, 0))
    tree(P('TreeOak'), 0, 0, 1.0, 1)
    e = P('TreeClump')   # four oaks and some bushes, ~14 m across
    for (x, y, s, seed) in ((0, 0, 1.15, 2), (4.5, 2.0, .9, 3), (-3.8, 2.5, .95, 4), (1.0, -4.2, .8, 5)):
        tree(e, x, y, s, seed)
    for (x, y) in ((3.5, -2.5), (-2.5, -2.8), (-5.5, -.5), (5.5, 4.5)):
        sphere('Bush__Leaf', (x, y, .3), .9, e, (1.2, 1, .7), seg=(10, 6))
    return obs


def tree(e, x, y, s, seed):
    """a stylized round oak about 7 m tall (scale s)"""
    rnd = random.Random(seed)
    cone('Trunk__Bark', (x, y, 0), .5 * s, .32 * s, 3.4 * s, e, 8)
    for a in (0, 2.1, 4.2):
        cube('Root__Bark', (x + math.cos(a) * .45 * s, y + math.sin(a) * .45 * s, .15 * s), (.6 * s, .25 * s, .3 * s), e, a)
    cube('Branch__Bark', (x + .5 * s, y, 3.4 * s), (.18 * s, .18 * s, 1.4 * s), e, 0, (0, .7))
    for (dx, dy, dz, r) in ((0, 0, 5.0, 2.3), (1.4, .5, 4.4, 1.6), (-1.3, -.4, 4.5, 1.7), (.3, -1.3, 4.7, 1.5), (-.4, 1.3, 5.6, 1.5)):
        sphere('Leaves__Leaf', (x + dx * s, y + dy * s, dz * s), r * s * rnd.uniform(.9, 1.1), e, (1, 1, .85), seg=(12, 8))


def house(e, ground, upper, roofm, ridge, chim, name):
    """two storeys, footprint 6 x 6 (x -3..3, y -3..3), walls to 6 m (the ground floor 0..3, the timber-framed upper 3..6), a steep roof to
    ~10 m with a 0.5 m overhang; the door and the shop window in front (-Y)"""
    blk('Ground__' + ground, -3, 3, -3, 3, 0, 3, e)
    blk('Upper__' + upper, -3, 3, -3, 3, 3, 6, e)
    T = 'Frame__Timber'
    for z in (2.95, 5.85):
        for s in (-1, 1):
            blk(T, -3.06, 3.06, s * 3 - .06, s * 3 + .06, z, z + .18, e); blk(T, s * 3 - .06, s * 3 + .06, -3.06, 3.06, z, z + .18, e)
    for k in range(5):
        t = -3 + k * 1.5
        for s in (-1, 1):
            blk(T, t - .08, t + .08, s * 3 - .06, s * 3 + .06, 3.1, 5.9, e); blk(T, s * 3 - .06, s * 3 + .06, t - .08, t + .08, 3.1, 5.9, e)
    for s in (-1, 1):   # diagonal braces on the front and back
        for d in (-1, 1):
            cube(T, (d * 2.25, s * 3.0, 4.5), (.12, .14, 3.2), e, 0, (0, math.radians(d * 26)))
    # the door, a lit shop window and shutters in front; small windows upstairs and on the sides
    blk('Door__Plank', -.6, .6, -3.08, -2.95, 0, 2.2, e); blk('DoorFrame__Timber', -.75, .75, -3.1, -2.95, 2.2, 2.4, e)
    blk('Window__WarmLight', 1.0, 2.2, -3.06, -2.95, 1.1, 2.1, e)
    for x in (.75, 2.45): blk('Shutter__' + ('BannerBlue' if name == 'House1' else 'Green' if name == 'House2' else 'BannerRed'), x - .25, x + .25, -3.1, -3.0, 1.05, 2.15, e)
    blk('Window__WarmLight' if name != 'House3' else 'Window__Dark', -2.0, -1.2, -3.06, -2.95, 1.2, 2.0, e)
    for x in (-1.5, 1.5):
        blk('Window__WarmLight', x - .4, x + .4, -3.12, -3.0, 4.0, 5.0, e); blk('Window__Dark', x - .4, x + .4, 3.0, 3.12, 4.0, 5.0, e)
    for y in (-1, 1): blk('Window__Dark', 3.0, 3.12, y - .35, y + .35, 4.1, 4.9, e); blk('Window__WarmLight', -3.12, -3.0, y - .35, y + .35, 4.1, 4.9, e)
    blk('Box__Wood', 1.4, 2.6, -3.35, -3.05, 3.9, 4.05, e)   # a flower box under an upstairs window
    for k in range(5): sphere('Flower__' + ('FlowerPink', 'FlowerYellow', 'FlowerViolet', 'FlowerPink', 'FlowerYellow')[k], (1.5 + k * .25, -3.2, 4.12), .1, e, seg=(6, 4))
    # the roof: two slabs on a ridge, gable triangles at the ends, a ridge cap
    z0, rise, over, th = 6.0, 4.0, .5, .22
    half = 3.0; a = math.atan2(rise, half); L = math.hypot(half, rise) + over
    for s in (-1, 1):
        cx = s * (L / 2 * math.cos(a) - .05) ; cz = z0 + rise - L / 2 * math.sin(a) + th / 2 / math.cos(a)
        if ridge == 'X':
            cube('Roof__' + roofm, (0, cx, cz), (6 + 2 * over, L, th), e, 0, (-s * a, 0))
        else:
            cube('Roof__' + roofm, (cx, 0, cz), (L, 6 + 2 * over, th), e, 0, (0, s * a))
    for s in (-1, 1):
        if ridge == 'X':
            poly('Gable__' + upper, [(s * 3, -3, z0), (s * 3, 3, z0), (s * 3, 0, z0 + rise), (s * 2.9, -3, z0), (s * 2.9, 3, z0), (s * 2.9, 0, z0 + rise)],
                 [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
            blk(T, s * 3 - .06, s * 3 + .06, -.08, .08, z0, z0 + rise - .2, e)
        else:
            poly('Gable__' + upper, [(-3, s * 3, z0), (3, s * 3, z0), (0, s * 3, z0 + rise), (-3, s * 2.9, z0), (3, s * 2.9, z0), (0, s * 2.9, z0 + rise)],
                 [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
            blk(T, -.08, .08, s * 3 - .06, s * 3 + .06, z0, z0 + rise - .2, e)
            blk('Window__WarmLight', -.4, .4, s * 3 - .1 if s > 0 else s * 3 - .02, s * 3 + .02 if s < 0 else s * 3 + .1, z0 + .6, z0 + 1.6, e)
    if ridge == 'X': blk('Ridge__' + roofm, -3.5, 3.5, -.2, .2, z0 + rise + .05, z0 + rise + .3, e)
    else: blk('Ridge__' + roofm, -.2, .2, -3.5, 3.5, z0 + rise + .05, z0 + rise + .3, e)
    if chim:
        blk('Chimney__CastleStone', 1.6, 2.3, .8, 1.5, 6.5, 10.8, e); blk('ChimneyCap__Stone', 1.5, 2.4, .7, 1.6, 10.8, 11.0, e)


def tent(e, colour):
    """a striped pavilion, 5 m across (r 2.5), walls to 2.4, the roof to 4.8, a scalloped valance, a pole and pennant; the door flap at -Y"""
    n = 16; r = 2.5; H = 2.4; top = 4.8
    for k in range(n):
        a0 = k / n * math.tau; a1 = (k + 1) / n * math.tau; c = colour if k % 2 == 0 else 'Canvas'
        p0 = (math.cos(a0) * r, math.sin(a0) * r); p1 = (math.cos(a1) * r, math.sin(a1) * r)
        q0 = (p0[0] * .97, p0[1] * .97); q1 = (p1[0] * .97, p1[1] * .97)
        poly('Wall__' + c, [(*p0, 0), (*p1, 0), (*p1, H), (*p0, H), (*q0, 0), (*q1, 0), (*q1, H), (*q0, H)],
             [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (3, 2, 6, 7), (0, 3, 7, 4), (1, 5, 6, 2)], e)
        R = r + .35; o0 = (math.cos(a0) * R, math.sin(a0) * R); o1 = (math.cos(a1) * R, math.sin(a1) * R)
        poly('Roof__' + c, [(*o0, H - .1), (*o1, H - .1), (0, 0, top), (o0[0], o0[1], H - .2), (o1[0], o1[1], H - .2), (0, 0, top - .1)],
             [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
        am = (a0 + a1) / 2
        poly('Valance__' + (colour if k % 2 else 'Canvas'), [(*o0, H - .1), (*o1, H - .1), (math.cos(am) * R, math.sin(am) * R, H - .55),
                                                               (o0[0] * .99, o0[1] * .99, H - .1), (o1[0] * .99, o1[1] * .99, H - .1), (math.cos(am) * R * .99, math.sin(am) * R * .99, H - .55)],
             [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
    poly('Door__Dark', [(-.6, -r - .02, 0), (.6, -r - .02, 0), (0, -r - .02, 2.0), (-.6, -r - .06, 0), (.6, -r - .06, 0), (0, -r - .06, 2.0)],
         [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
    cone('Pole__Wood', (0, 0, top - .2), .05, .04, 1.4, e, 6)
    sphere('Knob__Gold', (0, 0, top + 1.2), .1, e, seg=(8, 6))
    poly('Pennant__' + colour, [(0, -.02, top + 1.1), (0, -.02, top + .6), (1.2, -.02, top + .85), (0, .02, top + 1.1), (0, .02, top + .6), (1.2, .02, top + .85)],
         [(0, 1, 2), (3, 5, 4), (0, 2, 5, 3), (1, 4, 5, 2), (0, 3, 4, 1)], e)
    for k in range(4):
        a = k / 4 * math.tau + math.pi / 4
        cube('Guy__Rope', (math.cos(a) * 3.1, math.sin(a) * 3.1, 1.1), (.02, .02, 2.6), e, 0).rotation_euler = (math.sin(a) * .55, -math.cos(a) * .55, 0)
        cube('Peg__Wood', (math.cos(a) * 3.6, math.sin(a) * 3.6, .1), (.06, .06, .25), e)


# ---------------------------------------------------------------------------------------------------------------- tidy, render
def merge_parts(e):
    """join a prop's parts that share a name (Blender's .001 suffixes ignored) into one mesh, so the game gets a few renderers per prop"""
    groups = {}
    for c in list(e.children): groups.setdefault(c.name.split('.')[0], []).append(c)
    for key, obs in groups.items():
        if len(obs) == 1: continue
        bm = bmesh.new()
        for o in obs:
            me = o.data.copy(); me.transform(o.matrix_basis); bm.from_mesh(me); bpy.data.meshes.remove(me)
        for o in obs:
            me = o.data; bpy.data.objects.remove(o)
            if me.users == 0: bpy.data.meshes.remove(me)
        new = bpy.data.meshes.new(key); bm.to_mesh(new); bm.free()
        ob = bpy.data.objects.new(key, new); bpy.context.scene.collection.objects.link(ob); ob.parent = e
        ob.data.materials.append(mat(key.split('__')[1]))


def tris(obs):
    t = 0
    for o in obs:
        if o.type == 'MESH': t += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return t


def look(cam, at):
    d = Vector(at) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def shot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, name); bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    xs = axis_lines(X0, X1, -60, 60, [-48, -47.7, -36.3, -36, 36, 36.3, 47.7, 48])
    zs = axis_lines(Z0, Z1, -30, 114, [-18, -17.7, -6.3, -6, 89.7, 90, 90.3, 101.7, 102])
    t = grid_mesh('Terrain__Grass', xs, zs, height)
    for p in t.data.polygons:   # the moat's sides and floor flat-shaded (no smeared light down its steep edges)
        if any(t.data.vertices[v].co.z < -.01 for v in p.vertices): p.use_smooth = False
    def far_h(x, z):
        d = max(abs(x) - 210, abs(z) - 175, 0)
        return (6 + 10 * (n2(x, z, 70, 3.3) + .7)) * sstep(d / 60) - 1.0 * (1 - sstep(d / 10))
    fxs = [-700 + 20 * i for i in range(71)]; fzs = [-650 + 20 * i for i in range(66)]
    far = grid_mesh('FarHills__Grass', fxs, fzs, far_h)
    export(os.path.join(OUT, 'medieval_terrain.fbx'), [t, far])
    terrain_tris = tris([t])
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'; sc.display.shading.light = 'STUDIO'
    sc.display.shading.show_backface_culling = True
    sc.render.resolution_x = 1000; sc.render.resolution_y = 840
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 430; cam.location = (0, 0, 400); cam.data.clip_end = 2000
    obs = props()
    castle_e = obs[0]
    for o in obs[1:]: o.hide_render = True; [setattr(c, 'hide_render', True) for c in o.children]
    for o in obs: merge_parts(o)
    for o in obs[1:]:
        for c in o.children: c.hide_render = True
    shot(sc, 'medieval_top.png')
    # the castle in 3/4 views (with the terrain, so the moat's fit shows), and the great hall from inside
    far.hide_render = True
    cam.data.type = 'PERSP'; cam.data.lens = 24; sc.render.resolution_x = 1400; sc.render.resolution_y = 900
    cam.location = (62, -48, 40); look(cam, (0, 38, 4)); shot(sc, 'medieval_castle_se.png')
    cam.location = (-70, -30, 28); look(cam, (0, 45, 8)); shot(sc, 'medieval_castle_sw.png')
    cam.location = (0, 20, 3.0); look(cam, (0, 63, 9)); shot(sc, 'medieval_courtyard.png')
    cam.location = (-12, 68, 3.2); look(cam, (10, 80, 5)); cam.data.lens = 18; shot(sc, 'medieval_hall.png')
    # a look along the moat's west side to its deep NW pit
    cam.data.lens = 24; cam.location = (-58, 40, 14); look(cam, (-42, 92, -6)); shot(sc, 'medieval_moat.png')
    # the other props on sheets (ortho, from the front-left, above): the village and wagons, the field and the hall's things, small ones
    t.hide_render = True
    for c in castle_e.children: c.hide_render = True
    cam.data.type = 'ORTHO'
    sheets = (('medieval_props.png', 16, 4, ('House1', 'House2', 'House3', 'MarketStall', 'Smithy', 'HayWagon', 'WagonWreck', 'Tent', 'TentBlue',
                                              'TreeOak', 'TreeClump', 'Boulder')),
              ('medieval_props_field.png', 16, 4, ('Stands', 'ListBarrier', 'HighStriker', 'Mallet', 'Log', 'Drawbridge', 'Portcullis',
                                                    'SmallPortcullis', 'FeastTable', 'Chandelier', 'Banner', 'BannerPole')),
              ('medieval_props_small.png', 2.2, 6, ('Pedestal', 'PurpleShard', 'Torch', 'Brazier', 'Bench', 'FlowerPot', 'Barrel', 'Crate',
                                                     'Bramble', 'Sword', 'Shield', 'Bow', 'Arrow', 'Key', 'CellBars', 'Chains', 'HayBale')))
    byname = {o.name: o for o in obs}
    for fname, sp, cols, names in sheets:
        group = [byname[n] for n in names]
        for o in obs[1:]:
            for c in o.children: c.hide_render = o not in group
        for i, o in enumerate(group):
            lift = {'Chandelier': 3.4, 'Banner': 4.1, 'Torch': .3, 'Chains': .9}.get(o.name, 0)
            o.location = ((i % cols) * sp, -(i // cols) * sp, lift)
        bpy.context.view_layer.update()
        rows = (len(group) + cols - 1) // cols
        mid = Vector(((cols - 1) * sp / 2, -(rows - 1) * sp / 2, sp * .15))
        cam.rotation_euler = (math.radians(62), 0, math.radians(-25)); cam.data.ortho_scale = max(cols, rows) * sp * 1.15
        cam.location = mid + cam.rotation_euler.to_matrix() @ Vector((0, 0, 200)); cam.data.clip_end = 1000
        shot(sc, fname)
    bpy.data.objects.remove(cam); bpy.data.objects.remove(far); bpy.data.objects.remove(t)
    for o in obs:
        o.location = (0, 0, 0)
        for c in o.children: c.hide_render = False
    export(os.path.join(OUT, 'medieval_props.fbx'), obs)
    print('MEDIEVAL terrain: %d x %d lines, %d tris' % (len(xs), len(zs), terrain_tris))
    for o in obs:
        print('MEDIEVAL prop %-16s %6d tris %3d parts' % (o.name, tris(o.children), len(o.children)))
    print('MEDIEVAL written:', len(obs), 'props, %d tris in all; steepest ground %.0f deg at %s' % ((sum(tris(o.children) for o in obs),) + max_slope()))


main()
