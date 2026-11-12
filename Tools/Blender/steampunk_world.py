"""Shards of Time, Round 11: Victorian Steampunk's ground and its stylized props, by script (as pirate_world.py; medieval_world.py's helpers).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/steampunk_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height; everything flat at 0):
  Assets/Resources/World/steampunk_terrain.fbx  FactoryFloor__FactoryFloor (x -36..36, z 0..96), Yard__Flagstone (x -30..30, z 96..112),
                                                Cobbles__Cobble (x -100..100, z 112..300), Rail__Iron + Sleeper__Wood (decorative tram
                                                tracks: a loop round the square x +-22, z 178..222, and the avenue x +-6, z 112..178)
  Assets/Resources/World/steampunk_props.fbx    one empty per prop, parts <part>__<Material> (see PROPS below for each one's origin)
  Data/screenshots/round11/world/steampunk_*.png
A prop's front faces Blender -Y (Unity -z). Props whose origin is a world spot (FactoryHall at the world origin; WallPatch / WallBreak at
(0, 0, 96); ConveyorBelt / BeltGuard at (0, 0, 8); BeltGate at (0, 3, 80); PressFrame / PressStub at (0, 0, 84)) are built in that frame.
"""
import bpy, bmesh, math, os, random, sys
from mathutils import Vector, noise, Quaternion

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, 'medieval_world.py'), encoding='utf-8').read()
exec(_src.split('# ---------------------------------------------------------------------------------------------------------------- the castle')[0])

SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round11', 'world')
COL.update({'Brass': (.86, .66, .3), 'Black': (.08, .07, .07),
            # Round 11 (also in WorldArt.cs Palette)
            'Brick': (.62, .25, .18), 'BrickDark': (.45, .18, .14), 'CreamStone': (.93, .86, .7), 'FactoryFloor': (.36, .34, .36),
            'IronGrate': (.3, .31, .34), 'IronMesh': (.28, .29, .32), 'Copper': (.78, .45, .28), 'BeltRubber': (.16, .16, .18),
            'BlueRoller': (.2, .45, .85), 'NightGlass': (.25, .35, .7), 'WarmGlass': (1, .8, .45), 'TealGlow': (.2, .95, .85),
            'TealShard': (.2, 1, .9), 'Teal': (.15, .6, .6), 'ClockFace': (.97, .94, .85), 'ClockGreen': (.12, .32, .25), 'Glass': (.55, .7, .8),
            'Oil': (.05, .05, .07), 'Rubble': (.5, .3, .25), 'RedAlarm': (1, .15, .1)})
random.seed(11)


# ---------------------------------------------------------------------------------------------------------------- more helpers
def bar(name, a, b, s, parent):
    """a square bar (s x s, or (w, h)) from point a to point b"""
    a = Vector(a); b = Vector(b); d = b - a
    w, h = (s, s) if not isinstance(s, (tuple, list)) else s
    ob = cube(name, (a + b) / 2, (w, h, d.length), parent)
    ob.rotation_mode = 'QUATERNION'; ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    return ob


def rod(name, a, b, r, parent, n=8, smooth=True):
    """a round rod from point a to point b"""
    a = Vector(a); b = Vector(b); d = b - a
    ob = cone(name, (0, 0, 0), r, r, d.length, parent, n, smooth)
    ob.location = a; ob.rotation_mode = 'QUATERNION'; ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    return ob


def tube(name, pts, r, parent, n=8, joint=None):
    """a bent rod through the points, a ball at every bend"""
    for a, b in zip(pts, pts[1:]): rod(name, a, b, r, parent, n)
    for p in pts[1:-1]: sphere(joint or name, p, r, parent, seg=(n, max(4, n // 2)))


def prism(name, outline, z0, z1, parent):
    """an outline (list of (x, y)) extruded from z0 to z1"""
    bm = bmesh.new()
    vs = [bm.verts.new((x, y, z0)) for x, y in outline]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in r['geom']:
        if isinstance(v, bmesh.types.BMVert): v.co.z = z1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return part(name, bm, parent)


def slab(name, x0, x1, y0, y1, zf, thick, parent):
    """a slab whose top follows zf(x) (linear inside the piece): roof pieces"""
    v = [(x0, y0, zf(x0)), (x1, y0, zf(x1)), (x1, y1, zf(x1)), (x0, y1, zf(x0))]
    verts = v + [(x, y, z - thick) for x, y, z in v]
    return poly(name, verts, [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], parent)


def annulus_y(name, c, ro, ri, t, parent, n=32):
    """a flat ring standing upright facing -Y, y from c.y - t/2 to c.y + t/2"""
    bm = bmesh.new(); rings = []
    for r in (ro, ri):
        for y in (c[1] - t / 2, c[1] + t / 2):
            rings.append([bm.verts.new((c[0] + math.cos(k / n * math.tau) * r, y, c[2] + math.sin(k / n * math.tau) * r)) for k in range(n)])
    of, ob_, inf, inb = rings
    for k in range(n):
        j = (k + 1) % n
        bm.faces.new((of[k], of[j], inf[j], inf[k])); bm.faces.new((ob_[k], inb[k], inb[j], ob_[j]))
        bm.faces.new((of[k], ob_[k], ob_[j], of[j])); bm.faces.new((inf[k], inf[j], inb[j], inb[k]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return part(name, bm, parent)


def gear_y(name, c, r, thick, parent, teeth=16, tooth=None, tname=None, spokes=0, rim=None, hub=None, n=24):
    """a gear standing upright facing -Y, centred on c: a disc (or a rim with spokes when spokes > 0) and teeth round it"""
    tw, th = tooth or (r * .16, r * 2 * math.pi / teeth * .5)
    if spokes:
        annulus_y(name, c, r, r * (rim or .78), thick, parent, n)
        for k in range(spokes):
            a = k / spokes * math.tau + .3
            cube(name, (c[0] + math.cos(a) * r * .45, c[1], c[2] + math.sin(a) * r * .45), (r * .82, thick * .8, r * .16), parent, 0, (0, -a))
        cyl_y(hub or name, c, r * .2, thick * 1.3, parent, 12)
    else:
        cyl_y(name, c, r, thick, parent, n, smooth=False)
        cyl_y(hub or name, c, r * .22, thick * 1.25, parent, 12)
    for k in range(teeth):
        a = (k + .5) / teeth * math.tau
        cube(tname or name, (c[0] + math.cos(a) * (r + tw / 2 - .01), c[1], c[2] + math.sin(a) * (r + tw / 2 - .01)), (tw + .02, thick, th), parent, 0, (0, -a))


def gear_x(name, c, r, thick, parent, teeth=12):
    """a small gear facing X (on a side face)"""
    cyl_x(name, c, r, thick, parent, 16, smooth=False)
    tw, th = r * .2, r * 2 * math.pi / teeth * .5
    for k in range(teeth):
        a = (k + .5) / teeth * math.tau
        b = cube(name, (c[0], c[1] + math.cos(a) * (r + tw / 2), c[2] + math.sin(a) * (r + tw / 2)), (thick, tw + .02, th), parent); b.rotation_euler = (a, 0, 0)


def gear_z(name, c, r, thick, parent, teeth=16, tooth=None):
    """a gear lying flat, its bottom at c.z"""
    cone(name, c, r, r, thick, parent, 24, False)
    tw, th = tooth or (r * .14, r * 2 * math.pi / teeth * .5)
    for k in range(teeth):
        a = (k + .5) / teeth * math.tau
        cube(name, (c[0] + math.cos(a) * (r + tw / 2 - .01), c[1] + math.sin(a) * (r + tw / 2 - .01), c[2] + thick / 2), (tw + .02, th, thick), parent, a)


def rail_run(e, a, b, base, name='CatRail__Iron', h=1.1, post=.06, step=1.5, gaps=()):
    """a thin railing from a to b (x, y) at height base: posts and two bars; gaps = (t0, t1) fractions-free spans in metres from a"""
    a = Vector((a[0], a[1], 0)); b = Vector((b[0], b[1], 0)); d = b - a; L = d.length; u = d / L
    def open_(s): return any(g0 - 1e-3 <= s <= g1 + 1e-3 for g0, g1 in gaps)
    # the bars, split round the gaps
    cuts = [0.0]
    for g0, g1 in sorted(gaps): cuts += [g0, g1]
    cuts.append(L)
    for i in range(0, len(cuts), 2):
        s0, s1 = cuts[i], cuts[i + 1]
        if s1 - s0 < .05: continue
        n = max(1, int(math.ceil((s1 - s0) / step)))
        for k in range(n + 1):
            p = a + u * (s0 + (s1 - s0) * k / n)
            cube(name, (p.x, p.y, base + h / 2), (post, post, h), e)
        for z in (base + h * .5, base + h - .025):
            bar(name, (a + u * s0).to_tuple()[:2] + (z,), (a + u * s1).to_tuple()[:2] + (z,), .05, e)


def stair(e, name, x0, x1, ybot, ytop, ztop, rise=.35):
    """solid steps along Y from ybot (floor) to ytop (top at ztop), each `rise` high, solid down to 0"""
    n = int(round(ztop / rise)); run = (ytop - ybot) / n
    for i in range(n):
        ya, yb = ybot + run * i, ybot + run * (i + 1)
        blk(name, x0, x1, min(ya, yb), max(ya, yb), 0, rise * (i + 1), e)


def settle(e):
    """lift / drop the prop's parts so its lowest point is at 0"""
    bpy.context.view_layer.update()
    lo = min((o.matrix_world @ v.co).z for o in e.children_recursive if o.type == 'MESH' for v in o.data.vertices)
    for c in e.children: c.location.z -= lo


def lamp_hanging(e, x, y, ztop, zlamp):
    rod('LampRod__Iron', (x, y, ztop), (x, y, zlamp + .5), .03, e, 5)
    cone('Shade__Iron', (x, y, zlamp + .25), .55, .12, .3, e, 10)
    sphere('Bulb__WarmGlass', (x, y, zlamp + .22), .2, e, seg=(8, 5))


# ---------------------------------------------------------------------------------------------------------------- ground
def bm_box(bm, x0, x1, y0, y1, z0, z1):
    r = bmesh.ops.create_cube(bm, size=1)
    for v in r['verts']: v.co = Vector(((x0 + x1) / 2 + v.co.x * (x1 - x0), (y0 + y1) / 2 + v.co.y * (y1 - y0), (z0 + z1) / 2 + v.co.z * (z1 - z0)))


def flat(name, x0, x1, z0, z1, cell=4.0):
    nx = max(1, int(round((x1 - x0) / cell))); nz = max(1, int(round((z1 - z0) / cell)))
    xs = [x0 + (x1 - x0) * i / nx for i in range(nx + 1)]; zs = [z0 + (z1 - z0) * j / nz for j in range(nz + 1)]
    return grid_mesh(name, xs, zs, lambda x, z: 0.0, smooth=False)


def tracks():
    rail = bmesh.new(); slp = bmesh.new()
    def straight(ax, fixed, a, b):          # a straight track along X (ax 'X', at y = fixed) or along Y (at x = fixed)
        for o in (-.6, .6):
            if ax == 'X': bm_box(rail, a, b, fixed + o - .035, fixed + o + .035, 0, .06)
            else: bm_box(rail, fixed + o - .035, fixed + o + .035, a, b, 0, .06)
        n = int((b - a) / 1.2)
        for k in range(n + 1):
            t = a + (b - a) * k / n
            if ax == 'X': bm_box(slp, t - .12, t + .12, fixed - .85, fixed + .85, 0, .03)
            else: bm_box(slp, fixed - .85, fixed + .85, t - .12, t + .12, 0, .03)
    # the loop round the square: centre lines x = +-22 (z 178..222), z = 178 / 222 (x -22..22); the rails run to the outer corners
    straight('Y', -22, 177.4, 222.6); straight('Y', 22, 177.4, 222.6)
    straight('X', 178, -21.4, 21.4); straight('X', 222, -21.4, 21.4)
    # the avenue
    straight('Y', -6, 112, 177.4); straight('Y', 6, 112, 177.4)
    out = []
    for nm, bm in (('Rail__Iron', rail), ('Sleeper__Wood', slp)):
        me = bpy.data.meshes.new(nm); bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new(nm, me); bpy.context.scene.collection.objects.link(ob); ob.data.materials.append(mat(nm.split('__')[1]))
        out.append(ob)
    return out


def ground():
    return [flat('FactoryFloor__FactoryFloor', -36, 36, 0, 96), flat('Yard__Flagstone', -30, 30, 96, 112),
            flat('Cobbles__Cobble', -100, 100, 112, 300)] + tracks()


# ---------------------------------------------------------------------------------------------------------------- the factory
def zr(x): return 20.0 - 2.0 * abs(x) / 36.8    # the roof's top: 18 at the eaves (x +-36.8), 20 at the ridge


def hall_window(e, x_face, out, y):
    """a tall arched window (3 m x 7 m, sill at 9) on the long wall's face x_face, `out` = the face's outward sign"""
    x = x_face + out * .04
    cube('Window__NightGlass', (x, y, 9 + 2.75), (.08, 3, 5.5), e)
    cyl_x('Window__NightGlass', (x, y, 14.5), 1.5, .08, e, 14, smooth=False)
    xf = x_face + out * .09
    for dy in (-1.5, -.5, .5, 1.5):
        cube('Mullion__Iron', (xf, y + dy, 9 + 3.0), (.08, .1, 6.0), e)
    for z in (9.0, 11.0, 13.0, 14.5):
        cube('Mullion__Iron', (xf, y, z), (.08, 3.1, .1), e)
    cube('Sill__CreamStone', (x_face + out * .12, y, 8.9), (.25, 3.6, .2), e)


def factory_hall(e):
    B = 'Wall__Brick'
    # walls (0.8 thick outside the interior box), the north wall round its hole x -5..5, y 0..9
    blk(B, -36.8, -36, -.8, 96.8, 0, 18, e); blk(B, 36, 36.8, -.8, 96.8, 0, 18, e)
    blk(B, -36, 36, -.8, 0, 0, 18, e)
    blk(B, -36, -5, 96, 96.8, 0, 18, e); blk(B, 5, 36, 96, 96.8, 0, 18, e); blk(B, -5, 5, 96, 96.8, 9, 18, e)
    for y0, y1 in ((-.8, 0), (96, 96.8)):   # gables up to the ridge
        poly(B, [(-36.8, y0, 18), (36.8, y0, 18), (0, y0, 20), (-36.8, y1, 18), (36.8, y1, 18), (0, y1, 20)],
             [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)], e)
    # the hole's brick arch edge (cosmetic, inside and outside) and pilasters outside the north wall
    for y in (95.9, 96.9):
        blk('Lintel__CreamStone', -5.4, 5.4, y - .1, y + .1, 9, 9.6, e)
    for x in range(-32, 33, 8):
        if abs(x) < 7: continue
        blk('Pilaster__BrickDark', x - .5, x + .5, 96.8, 97.2, 0, 18, e)
    blk('Plinth__BrickDark', -37.1, 37.1, -1.1, -.8, 0, 1.0, e); blk('Plinth__BrickDark', -37.1, -36.8, -1.1, 97.1, 0, 1.0, e); blk('Plinth__BrickDark', 36.8, 37.1, -1.1, 97.1, 0, 1.0, e)
    blk('Plinth__BrickDark', -37.1, -5, 96.8, 97.1, 0, 1.0, e); blk('Plinth__BrickDark', 5, 37.1, 96.8, 97.1, 0, 1.0, e)
    for x0, x1 in ((-37.0, 37.0),):
        blk('Cornice__CreamStone', x0, x1, -1.0, -.8, 17.4, 18.0, e); blk('Cornice__CreamStone', x0, x1, 96.8, 97.0, 17.4, 18.0, e)
    blk('Cornice__CreamStone', -37.0, -36.8, -1.0, 97.0, 17.4, 18.0, e); blk('Cornice__CreamStone', 36.8, 37.0, -1.0, 97.0, 17.4, 18.0, e)
    # tall arched windows on the long walls, every 12 m (both faces)
    for y in range(6, 96, 12):
        for xf, out in ((-36, 1), (-36.8, -1), (36, -1), (36.8, 1)):
            hall_window(e, xf, out, y)
    # a round window high in the south wall (both faces)
    for yf, s in ((0, 1), (-.8, -1)):
        disc_y('Window__NightGlass', (0, yf + s * .04, 12.5), 3.0, .08, e, 24)
        ring('Mullion__Iron', (0, yf + s * .1, 12.5), 3.0, .14, e, 24, 'Y')
        for k in range(6):
            a = k / 6 * math.pi
            cube('Mullion__Iron', (0, yf + s * .1, 12.5), (6.0, .1, .1), e, 0, (0, a))
    # iron pillars x = +-24 every 16 m up to the trusses, with brass collars
    for y in (16, 32, 48, 64, 80):
        for x in (-24, 24):
            blk('Pillar__Iron', x - .4, x + .4, y - .4, y + .4, 0, 16, e)
            blk('PillarBase__Iron', x - .55, x + .55, y - .55, y + .55, 0, .5, e)
            blk('Collar__Brass', x - .48, x + .48, y - .48, y + .48, 15.2, 15.6, e)
    # roof trusses across at 16 every 16 m: a bottom chord, top chords under the roof, verticals and diagonals
    for y in (16, 32, 48, 64, 80):
        blk('Truss__Iron', -36, 36, y - .15, y + .15, 16, 16.35, e)
        def top(x): return zr(x) - .3 - .15
        bar('Truss__Iron', (-36, y, top(-36)), (0, y, top(0)), (.25, .3), e); bar('Truss__Iron', (0, y, top(0)), (36, y, top(36)), (.25, .3), e)
        xs = [-36 + 6 * i for i in range(13)]
        for i, x in enumerate(xs):
            bar('Truss__Iron', (x, y, 16.3), (x, y, top(x)), .18, e)
            if i < len(xs) - 1:
                xb = xs[i + 1]
                if x < 0: bar('Truss__Iron', (x, y, 16.3), (xb, y, top(xb)), .14, e)
                else: bar('Truss__Iron', (x, y, top(x)), (xb, y, 16.3), .14, e)
        for x in (-12, 0, 12): lamp_hanging(e, x, y, 16.0, 12.0)
    # the pitched roof (slate, 18..20) with skylights in bands |x| 8..16
    holes = [(6, 14), (22, 30), (38, 46), (54, 62), (70, 78), (86, 94)]
    for s in (-1, 1):
        def X(a, b): return (min(s * a, s * b), max(s * a, s * b))
        x0, x1 = X(16, 37.3); slab('Roof__Slate', x0, x1, -1.3, 97.3, zr, .3, e)
        x0, x1 = X(0, 8); slab('Roof__Slate', x0, x1, -1.3, 97.3, zr, .3, e)
        x0, x1 = X(8, 16); ys = [-1.3]
        for h0, h1 in holes: ys += [h0, h1]
        ys.append(97.3)
        for i in range(0, len(ys), 2): slab('Roof__Slate', x0, x1, ys[i], ys[i + 1], zr, .3, e)
        for h0, h1 in holes:
            slab('Skylight__NightGlass', x0, x1, h0, h1, lambda x: zr(x) - .12, .05, e)
            for xx in (8, 10.67, 13.33, 16):
                xm = s * xx; bar('SkyFrame__Iron', (xm, h0, zr(xm) - .05), (xm, h1, zr(xm) - .05), .1, e)
            for yy in (h0, (h0 + h1) / 2, h1):
                bar('SkyFrame__Iron', (s * 8, yy, zr(8) - .05), (s * 16, yy, zr(16) - .05), .1, e)
    cube('Ridge__Iron', (0, 48, 20.05), (.4, 98.6, .2), e)
    # pipe runs along the long walls at 5 and 12, and along the north wall at 12; brackets every 6 m
    for x in (-35.55, 35.55):
        for z in (5, 12):
            cyl_y('Pipe__Copper', (x, 48, z), .22, 96, e, 10)
            for y in range(3, 96, 6):
                blk('PipeClamp__Iron', x - .3, x + .3, y - .08, y + .08, z - .28, z + .28, e)
                blk('PipeClamp__Iron', min(x, x * 36 / 35.55), max(x, x * 36 / 35.55), y - .05, y + .05, z - .05, z + .05, e)
            for y in range(12, 96, 24): cyl_y('Flange__Brass', (x, y, z), .3, .12, e, 10)
    for x0, x1 in ((-36, -5.8), (5.8, 36)):
        cyl_x('Pipe__Copper', ((x0 + x1) / 2, 95.55, 12), .22, x1 - x0, e, 10)
    for x in (-5.8, 5.8):
        rod('Pipe__Copper', (x, 95.55, 12), (x, 95.55, 9.8), .22, e, 10); sphere('Pipe__Copper', (x, 95.55, 12), .22, e, seg=(10, 6))
    cyl_x('Pipe__Copper', (0, 95.55, 9.8), .22, 11.6, e, 10)
    # big decorative gears on the walls
    for c, r, f in (((-16, 95.85, 12.5), 2.4, 'N'), ((16, 95.85, 12.0), 2.0, 'N'), ((-12, .15, 12.0), 2.2, 'S'), ((12, .15, 12.8), 1.6, 'S'),
                    ((-35.85, 24, 13.5), 1.8, 'W'), ((35.85, 60, 13.5), 1.8, 'E'), ((-35.85, 66, 4.0), 1.5, 'W'), ((35.85, 36, 4.0), 1.5, 'E')):
        if f in 'NS':
            gear_y('WallGear__Brass', c, r, .3, e, teeth=int(r * 8), spokes=5)
        else:
            g = empty('g'); g.parent = e; g.location = c; g.rotation_euler = (0, 0, math.pi / 2)
            gear_y('WallGear__Brass', (0, 0, 0), r, .3, g, teeth=int(r * 8), spokes=5)
    # the catwalk ring at 7: west, east, south; its railings on the inner edge, its brackets under it
    blk('Catwalk__IronGrate', -36, -33.5, 0, 96, 6.8, 7.0, e)
    blk('Catwalk__IronGrate', 33.5, 36, 0, 96, 6.8, 7.0, e)
    blk('Catwalk__IronGrate', -33.5, 33.5, 0, 2.5, 6.8, 7.0, e)
    rail_run(e, (-33.47, 2.5), (-33.47, 96), 7.0, gaps=[(83.6 - 2.5, 86.0 - 2.5)])
    rail_run(e, (33.47, 2.5), (33.47, 96), 7.0, gaps=[(10.0 - 2.5, 12.4 - 2.5)])
    rail_run(e, (-33.5, 2.53), (33.5, 2.53), 7.0)
    for y in range(2, 96, 4):
        bar('Bracket__Iron', (-36, y, 5.4), (-33.7, y, 6.8), .12, e); bar('Bracket__Iron', (36, y, 5.4), (33.7, y, 6.8), .12, e)
    for x in range(-30, 31, 4): bar('Bracket__Iron', (x, 0, 5.4), (x, 2.3, 6.8), .12, e)
    # two solid stairs up to the catwalk (0.35 high steps): east from z 22 up to z 10, west from z 74 up to z 86
    stair(e, 'Step__IronGrate', 30.5, 33.5, 22, 10, 7.0)
    stair(e, 'Step__IronGrate', -33.5, -30.5, 74, 86, 7.0)
    # their thin side rails (the open side) and the end over the drop
    for (x, y0, y1, ye) in ((30.53, 22, 10, 10.03), (-30.53, 74, 86, 85.97)):
        n = 8
        for k in range(n + 1):
            t = k / n; y = y0 + (y1 - y0) * t; z = 7.0 * t
            cube('CatRail__Iron', (x, y, z + .55 + .175), (.06, .06, 1.1 + .35), e)
        for dz in (.55, 1.08):
            bar('CatRail__Iron', (x, y0, dz + .35), (x, y1, 7.0 + dz), .05, e)
        rail_run(e, (30.5 if x > 0 else -33.5, ye), (33.5 if x > 0 else -30.5, ye), 7.0)


def wall_patch(e):
    blk('Patch__Brick', -5, 5, 0, .8, 0, 9, e)
    for y, s in ((-.04, -1), (.84, 1)):
        cube('Window__WarmGlass', (0, y, 5.5), (2.6, .08, 2.6), e)
        for dx in (-1.3, 0, 1.3): cube('Mullion__Iron', (dx, y + s * .05, 5.5), (.1, .08, 2.7), e)
        for dz in (4.2, 5.5, 6.8): cube('Mullion__Iron', (0, y + s * .05, dz), (2.7, .08, .1), e)
        cube('Sill__CreamStone', (0, y + s * .08, 4.1), (3.2, .25, .2), e)
    # newer, lighter bricks: a few proud courses so it reads as a patch
    for z in (1.5, 3.0, 7.6):
        blk('PatchCourse__BrickDark', -5, 5, -.06, 0, z, z + .12, e); blk('PatchCourse__BrickDark', -5, 5, .8, .86, z, z + .12, e)


def wall_break(e):
    def chunk(x, y, z, s, nm='Rubble__Brick'):
        cube(nm, (x, y, z), (s * random.uniform(.8, 1.3), s * random.uniform(.8, 1.2), s * random.uniform(.6, 1)), e,
             random.uniform(-.5, .5), (random.uniform(-.4, .4), random.uniform(-.4, .4)))
    for z in [i * .55 for i in range(17)]:
        for s in (-1, 1):
            x = s * random.uniform(4.75, 5.25)
            chunk(x, random.uniform(-.1, .9), z + .25, random.uniform(.45, .8))
    for k in range(18):
        x = -5.3 + k * 10.6 / 17
        chunk(x, random.uniform(-.1, .9), random.uniform(8.85, 9.25), random.uniform(.45, .8))
    # rubble just outside (world z 97..101): low lumps, max 0.5 high
    for k in range(22):
        x = random.uniform(-6, 6); y = random.uniform(1.2, 5); s = random.uniform(.2, .45)
        cube('Rubble__Rubble', (x, y, s * .45), (s * 1.4, s, s * .9), e, random.uniform(0, 3))


def conveyor(e):
    """origin = the world spot (0, 0, 8); the belt runs local y 0..80 (world z 8..88), its top at 3.0"""
    blk('BeltTop__BeltRubber', -2, 2, 0, 80, 2.85, 3.0, e)
    for y in range(80):
        cyl_x('Roller__BlueRoller', (0, y + .5, 2.78), .18, 4.9, e, 10)
    for s in (-1, 1):
        x0, x1 = (2.0, 2.4) if s > 0 else (-2.4, -2.0)
        blk('BeltFrame__Iron', x0, x1, 0, 80, .4, 3.0, e)
        for y in range(0, 81, 4):
            yy = min(max(y, .2), 79.8)
            blk('BeltLeg__Iron', x0 - .05, x1 + .05, yy - .2, yy + .2, 0, .45, e)
            cube('Rivet__Brass', (s * 2.42, yy, 1.7), (.04, .12, .12), e)
        # brass rails on the frames up to 4.1, open at y 64..68
        for ya, yb in ((0, 64), (68, 80)):
            n = int((yb - ya) / 2)
            for k in range(n + 1):
                cube('BeltRail__Brass', (s * 2.2, ya + (yb - ya) * k / n, 3.55), (.07, .07, 1.1), e)
            for z in (3.55, 4.06): bar('BeltRail__Brass', (s * 2.2, ya, z), (s * 2.2, yb, z), .06, e)
        # stairs down off the belt near its end (both sides): from the edge (x +-2.4, top 3.0) out to x +-7.5, 0.35 high steps
        n = 9; run = 5.1 / n
        for k in range(n):
            xa, xb = 2.4 + run * k, 2.4 + run * (k + 1)
            blk('BeltStair__IronGrate', min(s * xa, s * xb), max(s * xa, s * xb), 64.2, 67.8, 0, 3.0 - .35 * k, e)
    blk('BeltFrame__Iron', -2.4, 2.4, 79.8, 80, 0, 2.85, e)        # the end plate (no crawl space under the belt)
    blk('BeltFrame__Iron', -2.0, 2.0, 0, .2, 0, 2.85, e)
    blk('Chute__Iron', -2.4, 2.4, -.6, 0, 0, 6, e)                 # the backstop at the start
    blk('ChuteLip__Brass', -2.5, 2.5, -.7, .05, 5.9, 6.15, e)
    for x in (-1.6, 0, 1.6): cube('Rivet__Brass', (x, .02, 4.5), (.14, .06, .14), e)


def belt_guard(e):
    """origin (0, 0, 8): mesh screens on the rails x +-2.3, local y 0..80, 3.0..6.4"""
    for s in (-1, 1):
        x = s * 2.3
        blk('GuardPane__IronMesh', x - .04, x + .04, 0, 80, 3.0, 6.4, e)
        for y in range(0, 81, 4):
            cube('Guard__IronMesh', (s * 2.36, min(max(y, .05), 79.95), 4.7), (.1, .1, 3.4), e)
        for z in (3.05, 6.35): cube('Guard__IronMesh', (s * 2.36, 40, z), (.1, 80, .1), e)
        for y in range(0, 80, 4):
            for (za, zb) in ((3.05, 6.35), (6.35, 3.05)):
                bar('Guard__IronMesh', (s * 2.38, y, za), (s * 2.38, y + 4, zb), .05, e)


def belt_gate(e):
    """origin (0, 3, 80): bars across x -2..2 from the belt top up 3.4"""
    for x in [-2 + .4 * i for i in range(11)]:
        cube('GuardGate__Iron', (x, 0, 1.7), (.07, .07, 3.4), e)
    for z in (.05, 1.7, 3.35): cube('GuardGate__Iron', (0, 0, z), (4.1, .1, .1), e)
    for x in (-1.2, 0, 1.2): sphere('GateKnob__Brass', (x, 0, 3.45), .08, e, seg=(8, 5))


def stamper_frame(e):
    """origin on the belt's top centre: posts at x +-2.6 from -3 (the floor) to 3.6, a beam 3.6..4.2, a brass piston hanging from it"""
    for s in (-1, 1):
        blk('StamperFrame__Iron', s * 2.6 - .2, s * 2.6 + .2, -.3, .3, -3, 3.6, e)
        blk('StamperFrame__Iron', s * 2.6 - .3, s * 2.6 + .3, -.4, .4, -3, -2.7, e)
        cube('Rivet__Brass', (s * 2.6, -.31, 2.0), (.12, .04, .12), e); cube('Rivet__Brass', (s * 2.6, -.31, 0.5), (.12, .04, .12), e)
    blk('StamperFrame__Iron', -2.9, 2.9, -.35, .35, 3.6, 4.2, e)
    cone('Piston__Brass', (0, 0, 3.1), .42, .42, .5, e, 14)
    cone('PistonCap__Copper', (0, 0, 4.2), .3, .2, .4, e, 12)
    for x in (-1.5, 1.5): cube('Rivet__Brass', (x, -.36, 3.9), (.12, .04, .12), e)


def stamper_head(e):
    blk('StamperHead__Iron', -.9, .9, -.7, .7, 0, .6, e)
    blk('Band__Brass', -.92, .92, -.72, .72, .22, .36, e)
    rod('Rod__Brass', (0, 0, .6), (0, 0, 1.6), .15, e, 10)


def steam_vent(e):
    blk('Vent__Iron', -.6, .6, -.6, .6, 0, .04, e)
    for k in range(5): blk('Vent__Iron', -.55 + k * .25 + .03, -.55 + k * .25 + .1, -.55, .55, .04, .08, e)
    for s in (-1, 1): blk('Vent__Iron', -.6, .6, s * .57 - .03, s * .57 + .03, .04, .08, e)
    tube('VentPipe__Copper', [(.75, .4, 0), (.75, .4, .45), (.75, .75, .45)], .1, e)
    cone('Flange__Brass', (.75, .4, .02), .15, .15, .06, e, 10)


def control_panel(e):
    blk('Panel__Iron', -.8, .8, -.3, .3, 0, 1.0, e)
    poly('Console__Brass', [(-.8, -.3, 1.0), (.8, -.3, 1.0), (.8, .3, 1.4), (-.8, .3, 1.4), (-.8, .3, 1.0), (.8, .3, 1.0)],
         [(0, 1, 2, 3), (4, 5, 1, 0), (3, 2, 5, 4), (0, 3, 4), (1, 5, 2)], e)
    blk('Plinth__Brass', -.82, .82, -.32, .32, 0, .08, e)
    ang = math.atan2(.4, .6)
    for i, x in enumerate((-.5, 0, .5)):
        c = Vector((x, -.03, 1.22)) + Vector((0, -math.sin(ang), math.cos(ang))) * .02
        d = cyl_y('Dial__ClockFace', c, .14, .03, e, 14, smooth=False); d.rotation_euler = (math.radians(-90) + ang, 0, 0)
        cube('Needle__Iron', (x + .03, c.y - .02, c.z + .03), (.02, .01, .11), e, 0, (ang, -.6 + i * .5))
    for i, x in enumerate((-.55, -.3, -.05, .2, .45)):
        cone('Button__RedAlarm' if i % 2 == 0 else 'Button__Brass', (x, -.31, .78), .05, .05, .04, e, 8).rotation_euler = (math.radians(90), 0, 0)
    for x in (.62,):
        rod('Lever__Brass', (x, -.32, .55), (x, -.55, .85), .03, e, 6); sphere('Lever__RedAlarm', (x, -.55, .85), .06, e, seg=(8, 5))
    blk('Plate__Brass', -.4, .4, -.31, -.3, .2, .5, e)


def crate(e):
    cube('Crate__Wood', (0, 0, .5), (1, 1, 1), e)
    for x in (-.48, .48):
        for y in (-.48, .48): blk('Band__Iron', x - .04, x + .04, y - .04, y + .04, 0, 1.0, e)
    for z in (.04, .96):
        for s in (-1, 1):
            blk('Band__Iron', -.52, .52, s * .5 - .02, s * .5 + .02, z - .04, z + .04, e)
            blk('Band__Iron', s * .5 - .02, s * .5 + .02, -.52, .52, z - .04, z + .04, e)


def barrel(e):
    cone('Barrel__Wood', (0, 0, 0), .37, .425, .55, e, 14); cone('Barrel__Wood', (0, 0, .55), .425, .37, .55, e, 14)
    for z in (.1, .42, .64, .96): cone('Hoop__Iron', (0, 0, z), .41, .41, .06, e, 14)


def boiler(e):
    cyl_y('Boiler__Copper', (0, 0, 2.0), 1.6, 5.0, e, 20)
    for s in (-1, 1):
        sp = sphere('Boiler__Copper', (0, s * 2.5, 2.0), 1.6, e, (1, .35, 1), seg=(20, 10))
        cyl_y('Rivets__Brass', (0, s * 2.45, 2.0), 1.63, .12, e, 20)
        blk('Cradle__Iron', -1.4, 1.4, s * 1.8 - .25, s * 1.8 + .25, 0, 1.0, e)
    cyl_y('Rivets__Brass', (0, 0, 2.0), 1.63, .12, e, 20)
    rod('Chimney__Iron', (0, -1.6, 3.4), (0, -1.6, 6.0), .35, e, 12)
    cone('ChimneyCap__Brass', (0, -1.6, 6.0), .45, .5, .25, e, 12)
    sphere('Dome__Brass', (0, 1.0, 3.55), .45, e, half=True, seg=(12, 6))
    disc_y('Gauge__ClockFace', (0, -3.05, 2.6), .3, .06, e, 14)
    ring('GaugeRim__Brass', (0, -3.08, 2.6), .3, .06, e, 12, 'Y')
    tube('Pipe__Copper', [(1.1, -1.5, 3.2), (1.9, -1.5, 3.2), (1.9, -1.5, 0.1)], .14, e)


def bolt_bucket(e):
    W, D, H, t = 2.6, 2.0, 1.6, .1
    blk('Bucket__Copper', -W / 2, W / 2, -D / 2, D / 2, 0, t, e)
    blk('Bucket__Copper', -W / 2, W / 2, -D / 2, -D / 2 + t, 0, H, e); blk('Bucket__Copper', -W / 2, W / 2, D / 2 - t, D / 2, 0, H, e)
    blk('Bucket__Copper', -W / 2, -W / 2 + t, -D / 2, D / 2, 0, H, e); blk('Bucket__Copper', W / 2 - t, W / 2, -D / 2, D / 2, 0, H, e)
    for s in (-1, 1):
        blk('Rim__Brass', -W / 2 - .05, W / 2 + .05, s * D / 2 - .07, s * D / 2 + .07, H - .14, H + .02, e)
        blk('Rim__Brass', s * W / 2 - .07, s * W / 2 + .07, -D / 2 - .05, D / 2 + .05, H - .14, H + .02, e)
        blk('Rim__Brass', -W / 2 - .04, W / 2 + .04, s * D / 2 - .06, s * D / 2 + .06, .1, .22, e)
    for x in [-1.1 + .44 * i for i in range(6)]:
        for z in (.4, 1.15): sphere('Rivets__Brass', (x, -D / 2 - .01, z), .045, e, seg=(6, 4))
    # the bail handle: from the sides at 1.4 up to 2.6 in the middle, the hook above (its top at 3.0)
    pts = []
    for k in range(9):
        a = math.pi * k / 8
        pts.append((-math.cos(a) * 1.36, 0, 1.4 + math.sin(a) * 1.2))
    tube('Bail__Iron', pts, .045, e, 6)
    for s in (-1, 1): cyl_x('Pivot__Brass', (s * 1.36, 0, 1.4), .09, .1, e, 8)
    rod('Hook__Iron', (0, 0, 2.6), (0, 0, 2.9), .05, e, 6)
    ring('Hook__Iron', (0, 0, 2.95), .06, .04, e, 8, 'Y')
    pts = [(0, 0, 2.58)] + [(math.sin(a) * .18, 0, 2.58 - .18 + math.cos(a) * .18) for a in [math.pi * k / 6 for k in range(1, 9)]]
    tube('Hook__Iron', pts, .045, e, 6)


def chain(e, x, y, ztop, length, nm='Chain__Iron'):
    n = int(length / .1)
    for k in range(n):
        z = ztop - .05 - k * .1
        for j in range(6):
            a = j / 6 * math.tau
            if k % 2 == 0: cube(nm, (x + math.cos(a) * .045, y, z + math.sin(a) * .065), (.022, .022, .05), e, 0, (0, -a + math.pi / 2))
            else: b = cube(nm, (x, y + math.cos(a) * .045, z + math.sin(a) * .065), (.022, .05, .022), e); b.rotation_euler = (a + math.pi / 2, 0, 0)


def chain_line(e):
    blk('ChainRail__Iron', -20, 20, -.16, .16, .24, .3, e); blk('ChainRail__Iron', -20, 20, -.03, .03, -.24, .24, e)
    blk('ChainRail__Iron', -20, 20, -.16, .16, -.3, -.24, e)
    for x in range(-20, 21, 5): blk('RailBolt__Brass', x - .05, x + .05, -.17, .17, .3, .34, e)
    for x in range(-18, 19, 4):
        blk('Trolley__Iron', x - .15, x + .15, -.2, .2, -.42, -.3, e)
        chain(e, x, 0, -.42, 1.2)
        rod('ChainHook__Iron', (x, 0, -1.62), (x, 0, -1.8), .03, e, 6); bar('ChainHook__Iron', (x, 0, -1.8), (x + .12, 0, -1.72), .05, e)


def press_frame(e):
    """origin (0, 0, 84) world"""
    for s in (-1, 1):
        x = s * 4.6
        blk('PressPillar__Iron', x - .6, x + .6, -.6, .6, 0, 15, e)
        blk('PressFoot__Iron', x - .9, x + .9, -.9, .9, 0, .6, e)
        for z in range(2, 15, 2):
            for yy in (-.62, .62): cube('Rivet__Brass', (x, yy, z), (.9, .04, .12), e)
        blk('Collar__Brass', x - .7, x + .7, -.7, .7, 12.9, 13.4, e)
        rod('Pipe__Copper', (x + s * .8, -.3, 0), (x + s * .8, -.3, 13.6), .16, e, 10)
        tube('Pipe__Copper', [(x + s * .8, .3, .6), (x + s * .8, .3, 9), (x + s * .3, .8, 9.5), (s * 1.0, .8, 14.0)], .12, e)
        chain(e, x - s * .7, -.65, 12.8, 3.0)
    blk('PressBeam__Iron', -5.8, 5.8, -.9, .9, 14, 15.5, e)
    blk('BeamCap__Brass', -6.0, 6.0, -1.0, 1.0, 15.5, 15.75, e)
    for x in (-4, -2, 0, 2, 4): cube('Rivet__Brass', (x, -.92, 14.75), (.14, .04, .14), e)
    cone('PressPiston__Brass', (0, 0, 11), .8, .8, 3.0, e, 18)
    cone('PistonBand__Copper', (0, 0, 12.9), .86, .86, .3, e, 18)
    cone('PistonBand__Copper', (0, 0, 11.0), .86, .86, .25, e, 18)
    gear_y('PressGear__Brass', (0, -.95, 14.75), .55, .1, e, teeth=10)


def press_dome(e, g=None):
    g = g or e
    blk('PressPlate__Iron', -3.5, 3.5, -3, 3, 0, .6, g)
    for s in (-1, 1):
        blk('PressRing__Iron', -3.55, 3.55, s * 3 - .06, s * 3 + .06, .15, .45, g); blk('PressRing__Iron', s * 3.5 - .06, s * 3.5 + .06, -3.05, 3.05, .15, .45, g)
    cone('PressRing__Iron', (0, 0, .6), 3.45, 3.3, .4, g, 32)
    d = sphere('Dome__Brass', (0, 0, 1.0), 3.25, g, (1, 1, 1.0), half=True, seg=(28, 12))
    for k in range(16):
        a = k / 16 * math.tau
        sphere('Bolt__Brass', (math.cos(a) * 3.3, math.sin(a) * 3.3, 1.12), .17, g, seg=(8, 5))
    for k in range(4):
        a = k / 4 * math.tau + .4
        sphere('Bolt__Brass', (math.cos(a) * 3.2, math.sin(a) * 2.5, .45), .14, g, seg=(8, 5))
    cone('DomeTop__Iron', (0, 0, 4.1), .8, .7, .9, g, 16)
    cone('DomeTop__Brass', (0, 0, 4.25), .86, .86, .15, g, 16)
    return d


def press_wreck(e):
    g = empty('wreck'); g.parent = e
    d = press_dome(e, g)
    for v in d.data.vertices:   # crushed: dented and squashed
        v.co.z *= .5; v.co += Vector((noise.noise(v.co * .9), noise.noise(v.co * .9 + Vector((3, 1, 0))), noise.noise(v.co * .9 + Vector((7, 2, 0))))) * .35
    for o in list(g.children):
        if o.name.startswith('DomeTop'): o.location.z -= 1.1; o.rotation_euler = (.3, .2, 0)
    g.rotation_euler = (math.radians(17), math.radians(-6), math.radians(15))
    bpy.context.view_layer.update()
    settle(e)
    ps = [o.matrix_world @ v.co for o in g.children_recursive if o.type == 'MESH' for v in o.data.vertices]
    g.location.x -= (min(p.x for p in ps) + max(p.x for p in ps)) / 2; g.location.y -= (min(p.y for p in ps) + max(p.y for p in ps)) / 2
    # the plate bent: a torn corner on the floor, bolts and gear bits round it
    t = blk('PressPlate__Iron', -1.2, 1.2, -1.0, 1.0, -.25, .25, e); t.rotation_euler = (0, .2, .5); t.location = (3.4, -3.2, .5)
    for k in range(9):
        a = random.uniform(0, math.tau); r = random.uniform(3.4, 4.6)
        sphere('Bolt__Brass', (math.cos(a) * r, math.sin(a) * r, .12), .14, e, seg=(8, 5))
    for c, r in (((-3.8, -2.2, .05), .6), ((2.8, 3.4, .05), .45), ((-2.0, 3.6, .05), .35)):
        gear_z('Gear__Brass', c, r, .1, e, teeth=10)


def press_stub(e):
    for s in (-1, 1):
        x = s * 4.6
        blk('PressPillar__Iron', x - .6, x + .6, -.6, .6, 0, 2.6, e)
        blk('PressFoot__Iron', x - .9, x + .9, -.9, .9, 0, .6, e)
        for k in range(6):
            cube('PressPillar__Iron', (x + random.uniform(-.35, .35), random.uniform(-.35, .35), 2.75 + random.uniform(0, .25)),
                 (random.uniform(.35, .6), random.uniform(.35, .6), random.uniform(.3, .6)), e, random.uniform(0, 1), (random.uniform(-.5, .5), random.uniform(-.5, .5)))
        rod('Pipe__Copper', (x + s * .8, -.3, 0), (x + s * .8, -.3, 2.2), .16, e, 10)
        tube('Pipe__Copper', [(x + s * .8, -.3, 2.2), (x + s * 1.1, -.6, 2.6), (x + s * 1.6, -.9, 2.5)], .16, e)
        for k in range(4):
            cube('Rubble__Rubble', (x + random.uniform(-2, 2), random.uniform(-2, 2), .15), (.4, .3, .3), e, random.uniform(0, 3))


def gear_socket(e):
    cone('Socket__Brass', (0, 0, 0), .65, .65, .08, e, 28, False)
    for k in range(14):
        a = (k + .5) / 14 * math.tau
        cube('Socket__Brass', (math.cos(a) * .58, math.sin(a) * .58, .11), (.12, .1, .06), e, a)
    cone('Socket__Brass', (0, 0, .08), .12, .1, .12, e, 12)
    cone('SocketRing__Iron', (0, 0, .08), .4, .4, .01, e, 24, False)


def magnet_pillar(e):
    blk('MagPillar__Iron', -.35, .35, -.35, .35, 0, 11.7, e)
    blk('PillarBase__Iron', -.5, .5, -.5, .5, 0, .5, e)
    for k in range(19):
        sphere('Rivets__Brass', (0, -.36, .8 + k * .58), .1, e, (1, .6, 1), seg=(8, 5))
    blk('Girder__Iron', -1.5, 1.5, -.7, .7, 11.7, 12.0, e)
    for s in (-1, 1):
        blk('Girder__Iron', -1.5, 1.5, s * .7 - .04, s * .7 + .04, 11.45, 11.7, e)
        bar('Girder__Iron', (s * .35, 0, 10.4), (s * 1.3, 0, 11.7), .12, e)
    for x in (-1.2, -.4, .4, 1.2): cube('Rivet__Brass', (x, -.75, 11.58), (.1, .03, .1), e)


def lift(e):
    blk('LiftDeck__IronGrate', -1.5, 1.5, -1.5, 1.5, -.25, 0, e)
    for x in (-1.45, 1.45):
        for y in (-1.45, 1.45): blk('LiftFrame__Brass', x - .05, x + .05, y - .05, y + .05, 0, 2.4, e)
    for s in (-1, 1):
        blk('LiftFrame__Brass', -1.5, 1.5, s * 1.45 - .05, s * 1.45 + .05, 2.35, 2.45, e); blk('LiftFrame__Brass', s * 1.45 - .05, s * 1.45 + .05, -1.5, 1.5, 2.35, 2.45, e)
    for s in (-1, 1): bar('LiftFrame__Brass', (-1.45, s * 1.45, 2.4), (0, 0, 2.9), .06, e); bar('LiftFrame__Brass', (1.45, s * 1.45, 2.4), (0, 0, 2.9), .06, e)
    ring('LiftRing__Iron', (0, 0, 3.0), .12, .05, e, 8, 'Y')


def lift_shaft(e):
    for x in (-1.8, 1.8):
        for y in (-1.8, 1.8): blk('Shaft__Iron', x - .12, x + .12, y - .12, y + .12, 0, 13, e)
    for z in (3.25, 6.5, 9.75, 13.0):
        # back (+Y) and sides at every level; the front (-Y) only at the top
        blk('Shaft__Iron', -1.8, 1.8, 1.72, 1.88, z - .08, z + .08, e)
        for s in (-1, 1): blk('Shaft__Iron', s * 1.8 - .08, s * 1.8 + .08, -1.8, 1.8, z - .08, z + .08, e)
    blk('Shaft__Iron', -1.8, 1.8, -1.88, -1.72, 12.92, 13.08, e)
    for z0, z1 in ((0, 3.25), (3.25, 6.5), (6.5, 9.75)):
        bar('Shaft__Iron', (-1.8, 1.8, z0), (1.8, 1.8, z1), .08, e)
        for s in (-1, 1): bar('Shaft__Iron', (s * 1.8, -1.8, z0), (s * 1.8, 1.8, z1), .08, e)
    bar('Shaft__Iron', (-1.8, 1.8, 9.75), (1.8, 1.8, 13.0), .08, e)
    cyl_y('Pulley__Brass', (0, 1.8, 13.35), .35, .2, e, 14)


def catwalk_bridge(e):
    blk('Bridge__IronGrate', -1.5, 1.5, -5, 5, -.25, 0, e)
    for s in (-1, 1):
        blk('BridgeBeam__Iron', s * 1.3 - .1, s * 1.3 + .1, -5, 5, -.55, -.25, e)
        rail_run(e, (s * 1.47, -5), (s * 1.47, 5), 0.0, name='BridgeRail__Iron')


def gear_door(e):
    gear_y('GearDoor__Brass', (0, 0, 2.1), 1.82, .4, e, teeth=16, tooth=(.28, .32))
    for k in range(6):
        a = k / 6 * math.tau + .26
        cube('Spoke__Copper', (math.cos(a) * .85, -.22, 2.1 + math.sin(a) * .85), (1.3, .06, .22), e, 0, (0, -a))
    cyl_y('Hub__Copper', (0, -.2, 2.1), .4, .12, e, 16)
    ring('Rim__Copper', (0, -.22, 2.1), 1.6, .1, e, 28, 'Y')


def gear_gate(e):
    gear_y('GearGate__Brass', (0, 0, 4.5), 4.0, .8, e, teeth=24, tooth=(.5, .62), tname='Teeth__Copper')
    for k in range(8):
        a = k / 8 * math.tau + .2
        cube('Spoke__Copper', (math.cos(a) * 2.0, -.44, 4.5 + math.sin(a) * 2.0), (2.9, .1, .45), e, 0, (0, -a))
    cyl_y('Hub__Copper', (0, -.42, 4.5), .9, .2, e, 20)
    ring('Rim__Copper', (0, -.44, 4.5), 3.5, .2, e, 40, 'Y')
    for k in range(16):
        a = k / 16 * math.tau
        sphere('Bolt__Brass', (math.cos(a) * 3.75, -.42, 4.5 + math.sin(a) * 3.75), .1, e, seg=(6, 4))


def gear_gate_frame(e):
    F = 'Frame__CreamStone'
    blk(F, -7.7, -4.7, -1, 1, 0, 12, e); blk(F, 4.7, 7.7, -1, 1, 0, 12, e)
    arch_fill(F, 'X', -4.7, 4.7, 4.7, 4.7, 12, -1, 1, e, 16)
    voussoirs('Voussoir__Brick', 'X', -4.7, 4.7, 4.7, 4.7, -1, -1, e, 11, .9)
    blk('Cornice__CreamStone', -8.0, 8.0, -1.3, 1.3, 12, 12.5, e)
    blk('Plinth__BrickDark', -7.9, -4.7, -1.2, 1.2, 0, .8, e); blk('Plinth__BrickDark', 4.7, 7.9, -1.2, 1.2, 0, .8, e)
    cube('Keystone__Brass', (0, -1.1, 9.8), (.9, .2, 1.1), e)


def oil_patch(e):
    pts = []
    for k in range(18):
        a = k / 18 * math.tau; r = 1.0 + .25 * noise.noise(Vector((math.cos(a) * 1.7, math.sin(a) * 1.7, .3)))
        pts.append((math.cos(a) * r * 1.2, math.sin(a) * r))
    prism('Oil__Oil', pts, 0, .02, e)


def crank_box(e):
    blk('Machine__Iron', -1.2, 1.2, -.8, .8, 0, 1.8, e)
    blk('Band__Brass', -1.23, 1.23, -.83, .83, 1.5, 1.62, e); blk('Band__Brass', -1.23, 1.23, -.83, .83, .1, .22, e)
    ring('Wheel__Brass', (1.4, 0, 1.0), .6, .08, e, 18, 'X')
    for k in range(4):
        a = k / 4 * math.tau
        b = cube('Wheel__Brass', (1.4, math.cos(a) * .3, 1.0 + math.sin(a) * .3), (.06, .6, .06), e); b.rotation_euler = (a, 0, 0)
    cyl_x('Axle__Iron', (1.3, 0, 1.0), .1, .35, e, 8)
    rod('Handle__Iron', (1.45, .6, 1.0), (1.75, .6, 1.0), .05, e, 6)
    disc_y('Gauge__ClockFace', (0, -.83, 1.2), .2, .04, e, 14)
    for x in (-.8, .8): cube('Rivet__Brass', (x, -.82, .9), (.1, .04, .1), e)


def bar_grate(e, bent=False):
    blk('BarFrame__Iron', -1.5, -1.25, -.15, .15, 0, 3, e); blk('BarFrame__Iron', 1.25, 1.5, -.15, .15, 0, 3, e)
    blk('BarFrame__Iron', -1.5, 1.5, -.15, .15, 2.75, 3, e); blk('BarFrame__Iron', -1.5, 1.5, -.15, .15, 0, .1, e)
    for i in range(7):
        x0 = -1.2 + .4 * i
        if not bent: rod('Bars__Iron', (x0, 0, .1), (x0, 0, 2.75), .05, e, 8); continue
        if i == 3:   # the middle bar: snapped, a stub top and bottom, the rest lying on the floor in front
            rod('Bars__Iron', (0, 0, 2.45), (0, 0, 2.75), .05, e, 8); rod('Bars__Iron', (0, 0, .1), (0, -.15, .3), .05, e, 8)
            rod('Bars__Iron', (-.6, -.9, .05), (.9, -1.3, .05), .05, e, 8); continue
        xb = {0: -1.2, 1: -1.12, 2: -.95, 4: .95, 5: 1.12, 6: 1.2}[i]
        if abs(xb - x0) < 1e-3: rod('Bars__Iron', (x0, 0, .1), (x0, 0, 2.75), .05, e, 8); continue
        tube('Bars__Iron', [(x0, 0, .1), (xb, -.08, .18), (xb, -.12, 2.4), (x0, 0, 2.75)], .05, e, 8)


def big_gear(e):
    gear_y('Gear__Brass', (0, 0, 0), 1.3, .3, e, teeth=18, tooth=(.22, .2), spokes=6, rim=.74)


# ---------------------------------------------------------------------------------------------------------------- the streets
def house(e, w, d, h, wall, trim, floors, door_x, chimneys):
    """a terrace house: a solid box w x d x h, front -Y; a flat walkable roof at h with a 0.6 m parapet"""
    hw, hd = w / 2, d / 2
    blk('HouseWall__' + wall, -hw, hw, -hd, hd, 0, h - .2, e)
    blk('Roof__Slate', -hw + .3, hw - .3, -hd + .3, hd - .3, h - .2, h, e)
    for s in (-1, 1):   # the parapet (0.3 thick, 0.6 high) round the roof's edge
        blk('Parapet__' + wall if wall == 'Brick' else 'Parapet__Brick', -hw, hw, s * hd - (.3 if s > 0 else 0), s * hd + (0 if s > 0 else .3), h - .2, h + .6, e)
        blk('Parapet__' + wall if wall == 'Brick' else 'Parapet__Brick', s * hw - (.3 if s > 0 else 0), s * hw + (0 if s > 0 else .3), -hd + .3, hd - .3, h - .2, h + .6, e)
    T = 'Trim__' + trim
    blk(T, -hw - .05, hw + .05, -hd - .25, hd + .25, h - .9, h - .55, e)                       # the cornice
    blk(T, -hw - .05, hw + .05, -hd - .32, hd + .32, h - .6, h - .4, e)
    blk(T, -hw, hw, -hd - .08, hd + .08, 0, .5, e)                                               # the plinth
    for f in range(1, floors): blk(T, -hw, hw, -hd - .1, hd + .1, f * 4.0 - .15, f * 4.0, e)       # string courses
    def win(x, z, yf, s, ww=1.3, wh=2.0):
        cube('HouseWindow__Glass', (x, yf + s * .03, z + wh / 2), (ww, .08, wh), e)
        cube(T, (x, yf + s * .06, z - .1), (ww + .4, .22, .18), e)                 # sill
        cube(T, (x, yf + s * .06, z + wh + .15), (ww + .4, .2, .3), e)             # lintel
        for dx in (-1, 1): cube(T, (x + dx * (ww / 2 + .07), yf + s * .05, z + wh / 2), (.14, .14, wh), e)
        cube(T, (x, yf + s * .06, z + wh * .55), (ww, .1, .07), e)                 # the sash bar
        cube(T, (x, yf + s * .06, z + wh / 2), (.06, .1, wh), e)
    cols = [x for x in ([-3, 0, 3] if w >= 9 else [-1.4, 1.4])]
    for f in range(floors):
        z = f * 4.0 + 1.0
        for x in cols:
            if f == 0 and abs(x - door_x) < 1.6: continue
            win(x, z, -hd, -1)
        for x in cols: win(x, z, hd, 1)
    # the door with its steps, a fanlight and a lamp
    cube('Door__Teal', (door_x, -hd - .03, 1.75), (1.2, .08, 2.5), e)
    cube(T, (door_x, -hd - .07, 3.2), (1.7, .22, .35), e)
    for dx in (-.72, .72): cube(T, (door_x + dx, -hd - .07, 1.75), (.2, .16, 2.6), e)
    disc_y('Fanlight__Glass', (door_x, -hd - .03, 3.02), .45, .06, e, 12)
    cube('Knob__Brass', (door_x + .4, -hd - .1, 1.6), (.08, .06, .08), e)
    blk('Step__CreamStone', door_x - 1.0, door_x + 1.0, -hd - .9, -hd, 0, .25, e)
    blk('Step__CreamStone', door_x - .9, door_x + .9, -hd - .5, -hd, .25, .5, e)
    for cx in chimneys:
        blk('Chimney__BrickDark', cx - .6, cx + .6, hd - 1.8, hd - .6, h, h + 3, e)
        blk('ChimneyCap__CreamStone', cx - .7, cx + .7, hd - 1.9, hd - .5, h + 2.7, h + 2.95, e)
        for dx in (-.25, .25): cone('Pot__Terracotta', (cx + dx, hd - 1.2, h + 2.95), .14, .11, .45, e, 8)


def chimney(e):
    blk('Chimney__BrickDark', -.7, .7, -.7, .7, 0, 4, e)
    blk('Cap__CreamStone', -.8, .8, -.8, .8, 3.65, 3.9, e)
    blk('Cap__CreamStone', -.78, .78, -.78, .78, .0, .3, e)
    for dx in (-.3, .3): cone('Pot__Terracotta', (dx, 0, 3.9), .17, .13, .55, e, 10)


def lamp_post(e):
    cone('Post__Teal', (0, 0, 0), .28, .2, .25, e, 10); cone('Post__Teal', (0, 0, .25), .2, .11, .5, e, 10)
    cone('Post__Teal', (0, 0, .75), .085, .07, 3.0, e, 10)
    for z in (1.0, 2.2, 3.7): cone('Post__Teal', (0, 0, z), .12, .12, .1, e, 10)
    for s in (-1, 1): tube('Post__Teal', [(0, 0, 3.4), (s * .35, 0, 3.6), (s * .2, 0, 3.8)], .025, e, 5)
    cone('Lamp__TealGlow', (0, 0, 3.8), .14, .24, .5, e, 6, False)
    for k in range(6):
        a = k / 6 * math.tau
        bar('LampFrame__Brass', (math.cos(a) * .14, math.sin(a) * .14, 3.8), (math.cos(a) * .24, math.sin(a) * .24, 4.3), .025, e)
    cone('LampCap__Teal', (0, 0, 4.3), .3, .05, .2, e, 6, False)
    sphere('LampCap__Brass', (0, 0, 4.5), .05, e, seg=(6, 4))


def bench(e):
    for k in range(3): blk('Bench__Wood', -.9, .9, -.2 + k * .14 - .05, -.2 + k * .14 + .05, .42, .47, e)
    for k in range(2): cube('Bench__Wood', (0, .2, .7 + k * .18), (1.8, .04, .12), e, 0, (math.radians(-12), 0))
    for x in (-.75, .75):
        bar('BenchLeg__Iron', (x, -.25, 0), (x, -.2, .45), .05, e); bar('BenchLeg__Iron', (x, .15, 0), (x, .18, .45), .05, e)
        bar('BenchLeg__Iron', (x, .18, .42), (x, .26, .92), .05, e); bar('BenchLeg__Iron', (x, -.27, .42), (x, .2, .42), .05, e)
        tube('BenchLeg__Iron', [(x, -.24, .42), (x, -.3, .62), (x, -.2, .66)], .025, e, 5)


def clock_face(g, socket):
    """one face of the great clock in g's frame: facing -Y, its front at y -1.25, its centre at z 6.4"""
    C = (0, -1.2, 6.4)
    if socket:
        annulus_y('Face__ClockFace', C, 1.2, .3, .1, g, 40)
        disc_y('Socket__Iron', (0, -1.16, 6.4), .31, .02, g, 20)
        for k in range(10):   # the gear-shaped edge of the empty socket (dia 0.7 at the tips)
            a = (k + .5) / 10 * math.tau
            cube('Socket__Iron', (math.cos(a) * .31, -1.252, 6.4 + math.sin(a) * .31), (.08, .006, .1), g, 0, (0, -a))
    else:
        disc_y('Face__ClockFace', C, 1.2, .1, g, 40)
        cyl_y('Hands__Iron', (0, -1.27, 6.4), .07, .04, g, 10)
    annulus_y('Bezel__Brass', (0, -1.24, 6.4), 1.3, 1.17, .14, g, 40)
    for k in range(12):
        a = k / 12 * math.tau; big = k % 3 == 0
        cube('Numerals__Iron', (math.sin(a) * .98, -1.26, 6.4 + math.cos(a) * .98), (.05 if not big else .1, .02, .2 if big else .14), g, 0, (0, a))
    r0 = .42 if socket else 0
    for ang, ln, wd in ((math.radians(-60), .62, .09), (math.radians(60), .95, .06)):   # ten past ten, the hour and the minute hands
        m = (r0 + ln) / 2
        cube('Hands__Iron', (math.sin(ang) * m, -1.27, 6.4 + math.cos(ang) * m), (wd, .02, ln - r0), g, 0, (0, ang))
    for x in (-1.0, 1.0):   # small gears in the housing's lower corners
        gear_y('Gear__Brass', (x, -1.17, 5.3), .2, .05, g, teeth=8)


def street_clock(e):
    blk('Base__CreamStone', -1.5, 1.5, -1.5, 1.5, 0, 1, e)
    blk('Base__CreamStone', -1.65, 1.65, -1.65, 1.65, 0, .25, e)
    blk('BaseCap__CreamStone', -1.6, 1.6, -1.6, 1.6, .9, 1.05, e)
    cone('Column__ClockGreen', (0, 0, 1.05), .7, .5, .5, e, 16)
    cone('Column__ClockGreen', (0, 0, 1.55), .4, .32, 3.1, e, 16)
    for z in (1.55, 2.6, 4.3): cone('ColumnRing__Brass', (0, 0, z), .45, .45, .12, e, 16)
    cone('Column__ClockGreen', (0, 0, 4.55), .32, .9, .45, e, 16)
    blk('ClockHead__Brass', -1.15, 1.15, -1.15, 1.15, 5.0, 7.8, e)
    blk('ClockHead__Brass', -1.25, 1.25, -1.25, 1.25, 4.95, 5.15, e)
    blk('ClockHead__Brass', -1.3, 1.3, -1.3, 1.3, 7.8, 8.05, e)
    for x in (-1.15, 1.15):
        for y in (-1.15, 1.15): blk('Pilaster__ClockGreen', x - .1, x + .1, y - .1, y + .1, 5.0, 7.8, e)
    for k in range(4):
        g = empty('face'); g.parent = e; g.rotation_euler = (0, 0, k * math.pi / 2)
        clock_face(g, socket=(k == 0))
    sphere('Dome__ClockGreen', (0, 0, 8.05), 1.15, e, (1, 1, .75), half=True, seg=(20, 8))
    cone('Finial__Brass', (0, 0, 8.85), .12, .12, .3, e, 8)
    sphere('Finial__Brass', (0, 0, 9.25), .14, e, seg=(10, 6))
    cone('Finial__Brass', (0, 0, 9.35), .06, .0, .3, e, 8)
    for x in (-1.35, 1.35):   # teal glass lamps at the corners
        for y in (-1.35, 1.35):
            bar('LampArm__Brass', (x * .8, y * .8, 7.9), (x, y, 8.15), .05, e)
            cone('Lamp__TealGlow', (x, y, 8.15), .12, .18, .38, e, 6, False)
            cone('LampCap__Brass', (x, y, 8.53), .2, .03, .14, e, 6, False)


def teal_gear_shard(e):
    gear_y('Shard__TealShard', (0, 0, 0), .23, .18, e, teeth=8, tooth=(.07, .1))
    sphere('Shard__TealShard', (0, 0, 0), .16, e, (1, .9, 1), seg=(6, 4))


def steam_carriage(e):
    W = 'Wheel__Brass'
    for x in (-1.0, 1.0):
        for y in (-1.3, 1.3):
            ring(W, (x, y, .7), .64, .12, e, 18, 'X')
            for k in range(8):
                a = k / 8 * math.tau
                b = cube('Spoke__Iron', (x, y + math.cos(a) * .32, .7 + math.sin(a) * .32), (.05, .62, .05), e); b.rotation_euler = (a, 0, 0)
            cyl_x('Hub__Brass', (x, y, .7), .13, .2, e, 10)
    blk('Chassis__Iron', -.9, .9, -1.9, 1.9, .55, .8, e)
    for y in (-1.3, 1.3): cyl_x('Axle__Iron', (0, y, .7), .06, 2.0, e, 8)
    blk('Carriage__Brass', -.9, .9, -1.4, 1.8, .8, 2.6, e)
    poly('Carriage__Brass', [(-.9, -1.4, .8), (.9, -1.4, .8), (.9, -1.4, 1.6), (-.9, -1.4, 1.6), (-.9, -1.95, .8), (.9, -1.95, .8)],
         [(0, 3, 2, 1), (4, 5, 2, 3), (0, 1, 5, 4), (0, 4, 3), (1, 2, 5)], e)                     # the nose, sloping
    cyl_y('Boiler__Copper', (0, -1.6, 1.15), .32, .7, e, 14)
    blk('CarriageRoof__Copper', -1.0, 1.0, -1.8, 1.8, 2.6, 2.8, e)
    blk('Trim__Copper', -.92, .92, -1.42, 1.82, 1.5, 1.58, e)
    rod('Chimney__Copper', (0, 1.6, 2.8), (0, 1.6, 3.5), .16, e, 10)
    cone('Chimney__Copper', (0, 1.6, 3.5), .16, .26, .2, e, 10)
    for s in (-1, 1):
        for y in (-.7, .5): cube('CarriageWindow__Glass', (s * .91, y, 2.05), (.04, .95, .8), e)
    cube('CarriageWindow__Glass', (0, -1.41, 2.1), (1.5, .04, .7), e)
    for s in (-1, 1):
        cone('Lamp__WarmGlass', (s * .7, -1.97, 1.25), .1, .1, .2, e, 8).rotation_euler = (math.radians(90), 0, 0)
        cube('Fender__Copper', (s * 1.0, -1.3, 1.42), (.22, 1.2, .06), e); cube('Fender__Copper', (s * 1.0, 1.3, 1.42), (.22, 1.2, .06), e)
    cube('Door__Copper', (.92, -.1, 1.5), (.03, .1, .5), e)
    gear_x('Gear__Brass', (-.93, .0, 1.15), .22, .05, e)


def turntable(e):
    cone('Turntable__IronGrate', (0, 0, 0), 4.0, 4.0, .3, e, 40, False)
    for k in range(40):
        a = (k + .5) / 40 * math.tau
        cube('TurnRim__Brass', (math.cos(a) * 4.02, math.sin(a) * 4.02, .16), (.08, .66, .3), e, a)
    for x in (-.6, .6): blk('Rail__Iron', x - .035, x + .035, -4, 4, .3, .36, e)
    for k in range(7): blk('Sleeper__Wood', -.85, .85, -3.6 + k * 1.2 - .12, -3.6 + k * 1.2 + .12, .3, .33, e)
    cone('Pivot__Brass', (0, 0, .3), .3, .25, .06, e, 12)


def rubble(e):
    for k in range(16):
        a = random.uniform(0, math.tau); r = random.uniform(0, .8) ** .7
        h = .5 * (1 - r) + .1
        cube('Rubble__Rubble', (math.cos(a) * r, math.sin(a) * r, h * .5), (.36, .18, min(.12, h)), e, random.uniform(0, 3), (random.uniform(-.2, .2), random.uniform(-.2, .2)))
    sphere('Rubble__Rubble', (0, 0, 0), .8, e, (1.2, 1.2, .55), half=True, seg=(10, 4))


def yard_wall(e):
    blk('YardWall__Brick', -5, 5, -.3, .3, 0, 2.8, e)
    blk('Coping__CreamStone', -5.05, 5.05, -.38, .38, 2.8, 3.0, e)
    for x in (-5, 0, 5): blk('Pier__BrickDark', x - .35, x + .35, -.38, .38, 0, 2.85, e)


def pipe(e):
    cyl_x('Pipe__Copper', (0, 0, 0), .25, 6, e, 14)
    for x in (-3, 0, 3): cyl_x('Flange__Brass', (x - (.08 if x > 0 else -.08 if x < 0 else 0), 0, 0), .33, .14, e, 14)
    for x in (-2, 2):
        cyl_x('PipeBracket__Iron', (x, 0, 0), .29, .12, e, 12)
        blk('PipeBracket__Iron', x - .06, x + .06, 0, .7, -.05, .05, e)
        blk('PipeBracket__Iron', x - .2, x + .2, .7, .76, -.25, .25, e)


def pipe_fence(e, bent=False):
    N = 'PipeFence__Copper'
    cyl_x(N, (0, 0, .15), .12, 3.0, e, 10); cyl_x(N, (0, 0, 2.85), .12, 3.0, e, 10)
    for x in (-1.5, 1.5):
        for z in (.15, 2.85): cyl_x('Flange__Brass', (x - (.05 if x > 0 else -.05), 0, z), .17, .1, e, 10)
    for i, x0 in enumerate((-1.2, -.6, 0, .6, 1.2)):
        if not bent:
            rod(N, (x0, 0, .15), (x0, 0, 2.85), .1, e, 10)
            cone('Flange__Brass', (x0, 0, 1.4), .13, .13, .08, e, 10)
            continue
        if i == 2:   # the middle pipe: broken, its two ends bent out of the way
            tube(N, [(0, 0, 2.85), (0, -.1, 2.55), (-.2, -.5, 2.35)], .1, e, 10)
            tube(N, [(0, 0, .15), (0, -.15, .35), (.25, -.6, .25)], .1, e, 10)
            continue
        xb = {0: -1.3, 1: -.95, 3: .95, 4: 1.3}[i]
        tube(N, [(x0, 0, .15), (xb, -.08, .3), (xb, -.12, 2.55), (x0, 0, 2.85)], .1, e, 10)


# ---------------------------------------------------------------------------------------------------------------- the list
def props():
    obs = []
    def P(n): o = empty(n); obs.append(o); return o
    factory_hall(P('FactoryHall'))
    wall_patch(P('WallPatch'))
    wall_break(P('WallBreak'))
    conveyor(P('ConveyorBelt'))
    belt_guard(P('BeltGuard'))
    belt_gate(P('BeltGate'))
    stamper_frame(P('StamperFrame'))
    stamper_head(P('StamperHead'))
    steam_vent(P('SteamVent'))
    control_panel(P('ControlPanel'))
    crate(P('Crate'))
    barrel(P('Barrel'))
    boiler(P('Boiler'))
    bolt_bucket(P('BoltBucket'))
    chain_line(P('ChainLine'))
    press_frame(P('PressFrame'))
    press_dome(P('PressDome'))
    press_wreck(P('PressWreck'))
    press_stub(P('PressStub'))
    gear_socket(P('GearSocket'))
    magnet_pillar(P('MagnetPillar'))
    lift(P('Lift'))
    lift_shaft(P('LiftShaft'))
    catwalk_bridge(P('CatwalkBridge'))
    gear_door(P('GearDoor'))
    gear_gate(P('GearGate'))
    gear_gate_frame(P('GearGateFrame'))
    oil_patch(P('OilPatch'))
    crank_box(P('CrankBox'))
    bar_grate(P('BarGrate'))
    bar_grate(P('BentBars'), bent=True)
    big_gear(P('BigGear'))
    house(P('HouseBrick'), 10, 8, 12, 'Brick', 'CreamStone', 3, 2.8, (-3.0, 3.2))
    house(P('HouseCream'), 10, 8, 9, 'CreamStone', 'Brick', 2, -2.8, (2.8,))
    house(P('HouseNarrow'), 6, 8, 14, 'Brick', 'CreamStone', 3, 1.4, (-1.4,))
    chimney(P('Chimney'))
    lamp_post(P('LampPost'))
    bench(P('Bench'))
    street_clock(P('StreetClock'))
    teal_gear_shard(P('TealGearShard'))
    steam_carriage(P('SteamCarriage'))
    turntable(P('Turntable'))
    rubble(P('Rubble'))
    yard_wall(P('YardWall'))
    pipe(P('Pipe'))
    pipe_fence(P('PipeFence'))
    pipe_fence(P('PipeFenceBent'), bent=True)
    return obs


# ---------------------------------------------------------------------------------------------------------------- tidy, render
def merge_parts(e):
    bpy.context.view_layer.update()   # (new parts' matrix_world is stale until the depsgraph runs)
    groups = {}
    for c in list(e.children_recursive):
        if c.type != 'MESH': continue
        groups.setdefault(c.name.split('.')[0], []).append(c)
    for name, objs in groups.items():
        for o in objs:
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        if len(objs) > 1: bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active; j.name = name
        j.parent = e; j.matrix_parent_inverse = e.matrix_world.inverted()
    for c in list(e.children_recursive):
        if c.type == 'EMPTY': bpy.data.objects.remove(c)


def tris(obs): return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs if o.type == 'MESH')


def bbox(e):
    """min / max of the prop's parts in its own frame, as Unity (x, y up, z)"""
    inv = e.matrix_world.inverted()
    ps = [inv @ (o.matrix_world @ v.co) for o in e.children if o.type == 'MESH' for v in o.data.vertices]
    lo = Vector((min(p.x for p in ps), min(p.y for p in ps), min(p.z for p in ps))); hi = Vector((max(p.x for p in ps), max(p.y for p in ps), max(p.z for p in ps)))
    return (lo.x, lo.z, lo.y), (hi.x, hi.z, hi.y)


def look(cam, at):
    d = Vector(at) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def shot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, name); bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    ground_obs = ground()
    export(os.path.join(OUT, 'steampunk_terrain.fbx'), ground_obs)
    print('STEAM terrain: %d tris' % tris(ground_obs))
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'; sc.display.shading.light = 'STUDIO'
    sc.display.shading.show_backface_culling = False
    sc.render.resolution_x = 1000; sc.render.resolution_y = 1000
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    obs = props()
    for o in obs: merge_parts(o)
    byname = {o.name: o for o in obs}
    # placed as the world will be (Blender coords = Unity (x, z, y)), for the renders
    place = {'FactoryHall': (0, 0, 0), 'WallPatch': (0, 96, 0), 'ConveyorBelt': (0, 8, 0), 'BeltGuard': (0, 8, 0), 'BeltGate': (0, 80, 3),
             'StamperFrame': (0, 30, 3), 'StamperHead': (0, 30, 5.0), 'PressFrame': (0, 84, 0), 'PressDome': (0, 84, 6.0),
             'StreetClock': (0, 200, 0), 'LiftShaft': (-28, 50, 0), 'Lift': (-28, 50, .25), 'MagnetPillar': (14, 40, 0), 'Boiler': (20, 70, 0),
             'ControlPanel': (6, 60, 0), 'SteamVent': (10, 20, 0), 'ChainLine': (0, 50, 14), 'BoltBucket': (8, 50, 6),
             'GearGateFrame': (0, 300, 0), 'GearGate': (0, 300, 0), 'SteamCarriage': (6, 150, 0), 'Turntable': (40, 200, 0)}
    shown = set(place)
    for n in ('HouseBrick', 'HouseCream', 'HouseNarrow'): shown.add(n)
    for n, p in place.items(): byname[n].location = p
    byname['HouseBrick'].location = (-12, 236, 0); byname['HouseCream'].location = (0, 236, 0); byname['HouseNarrow'].location = (10, 236, 0)
    extra = []
    for k, n in enumerate(('HouseBrick', 'HouseNarrow', 'HouseCream', 'HouseBrick')):
        for s in (-1, 1):
            c = byname[n].copy(); sc.collection.objects.link(c); extra.append(c)
            for ch in byname[n].children:
                cc = ch.copy(); sc.collection.objects.link(cc); cc.parent = c; extra.append(cc)
            c.location = (s * 16, 120 + k * 12, 0); c.rotation_euler = (0, 0, -s * math.pi / 2)
    for k in range(6):
        for s in (-1, 1):
            c = byname['LampPost'].copy(); sc.collection.objects.link(c); extra.append(c)
            for ch in byname['LampPost'].children:
                cc = ch.copy(); sc.collection.objects.link(cc); cc.parent = c; extra.append(cc)
            c.location = (s * 10, 116 + k * 12, 0)
    shown.add('LampPost')
    byname['LampPost'].location = (12, 196, 0)
    for o in obs:
        if o.name not in shown:
            for c in o.children: c.hide_render = True
    # the roof hides the hall from above: a top view without it
    roof = [c for c in byname['FactoryHall'].children if c.name.split('__')[0] in ('Roof', 'Skylight', 'SkyFrame', 'Ridge')]
    for c in roof: c.hide_render = True
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 320; cam.location = (0, 150, 300); cam.data.clip_end = 2000; cam.rotation_euler = (0, 0, 0)
    shot(sc, 'steampunk_top.png')
    cam.data.ortho_scale = 110; cam.location = (0, 48, 300); shot(sc, 'steampunk_factory_top.png')
    for c in roof: c.hide_render = False
    cam.data.type = 'PERSP'; cam.data.lens = 20; sc.render.resolution_x = 1400; sc.render.resolution_y = 900
    cam.location = (0, 1.5, 8.6); look(cam, (0, 60, 3)); shot(sc, 'steampunk_hall_south.png')
    cam.location = (26, 4, 9); look(cam, (32, 18, 4)); shot(sc, 'steampunk_hall_eaststair.png')
    cam.location = (-12, 60, 6); look(cam, (0, 84, 6)); shot(sc, 'steampunk_press.png')
    cam.location = (4, 117, 5); look(cam, (0, 96, 5)); shot(sc, 'steampunk_yard.png')
    cam.location = (60, -40, 40); look(cam, (0, 48, 8)); shot(sc, 'steampunk_hall_outside.png')
    cam.location = (20, 170, 10); look(cam, (0, 205, 5)); shot(sc, 'steampunk_square.png')
    cam.location = (0, 192, 6.4); look(cam, (0, 198.75, 6.4)); cam.data.lens = 50; shot(sc, 'steampunk_clock_face.png')
    cam.data.lens = 24
    cam.location = (0, 110, 6); look(cam, (0, 150, 4)); shot(sc, 'steampunk_avenue.png')
    for c in extra: bpy.data.objects.remove(c)
    # every prop on a sheet (small and mid-size ones; the hall and the belt pieces are in the shots above)
    for g in ground_obs: g.hide_render = True
    for o in obs:
        for c in o.children: c.hide_render = True
        o.location = (0, 0, 0)
    sheets = (('steampunk_props_factory.png', ('WallPatch', 'WallBreak', 'BeltGate', 'StamperFrame', 'StamperHead', 'SteamVent', 'ControlPanel', 'Crate',
                                               'Barrel', 'Boiler', 'BoltBucket', 'PressDome', 'PressWreck', 'PressStub', 'GearSocket', 'MagnetPillar',
                                               'Lift', 'LiftShaft', 'CatwalkBridge', 'GearDoor', 'OilPatch', 'CrankBox', 'BarGrate', 'BentBars',
                                               'BigGear', 'PressFrame', 'GearGate', 'GearGateFrame'), 14.0, 7),
              ('steampunk_props_street.png', ('HouseBrick', 'HouseCream', 'HouseNarrow', 'Chimney', 'LampPost', 'Bench', 'StreetClock', 'TealGearShard',
                                              'SteamCarriage', 'Turntable', 'Rubble', 'YardWall', 'Pipe', 'PipeFence', 'PipeFenceBent', 'ChainLine'), 14.0, 4))
    lifted = {'StamperFrame': 3, 'StamperHead': 0, 'BigGear': 1.6, 'TealGearShard': .5, 'Pipe': .5, 'ChainLine': 2.0, 'Lift': .25, 'BeltGate': 0}
    for fn, names, sp, cols in sheets:
        for i, n in enumerate(names):
            o = byname[n]; o.location = ((i % cols) * sp, -(i // cols) * sp, lifted.get(n, 0))
            for c in o.children: c.hide_render = False
        bpy.context.view_layer.update()
        rows = (len(names) + cols - 1) // cols
        mid = Vector(((cols - 1) * sp / 2, -(rows - 1) * sp / 2, 2))
        cam.data.type = 'ORTHO'; cam.rotation_euler = (math.radians(62), 0, math.radians(-20)); cam.data.ortho_scale = cols * sp * 1.0
        sc.render.resolution_x = 1600; sc.render.resolution_y = int(1600 * min(1.0, (rows * sp * .75 + 14) / (cols * sp)))
        cam.location = mid + cam.rotation_euler.to_matrix() @ Vector((0, 0, 300)); cam.data.clip_end = 2000
        shot(sc, fn)
        for n in names:
            o = byname[n]; o.location = (0, 0, 0)
            for c in o.children: c.hide_render = True
    # the clock and the shard up close
    for n in ('StreetClock',):
        for c in byname[n].children: c.hide_render = False
    cam.data.type = 'PERSP'; cam.data.lens = 35; sc.render.resolution_x = 900; sc.render.resolution_y = 1200
    cam.location = (7, -11, 6); look(cam, (0, 0, 4.8)); shot(sc, 'steampunk_clock.png')
    bpy.data.objects.remove(cam)
    for o in obs:
        o.location = (0, 0, 0)
        for c in o.children: c.hide_render = False
    for g in ground_obs: bpy.data.objects.remove(g)
    export(os.path.join(OUT, 'steampunk_props.fbx'), obs)
    words = {}
    for o in obs:
        (x0, y0, z0), (x1, y1, z1) = bbox(o)
        print('STEAM prop %-14s %6d tris %3d parts  x %7.2f..%7.2f  y %6.2f..%6.2f  z %7.2f..%7.2f' % (o.name, tris(o.children), len(o.children), x0, x1, y0, y1, z0, z1))
        for c in o.children: words.setdefault(c.name.split('__')[0], set()).add(c.name.split('__')[1])
    print('STEAM parts:', ', '.join('%s(%s)' % (k, '/'.join(sorted(v))) for k, v in sorted(words.items())))
    print('STEAM written:', len(obs), 'props, %d tris in all' % sum(tris(o.children) for o in obs))


main()
