"""Shards of Time, Round 10: the Pirate Seas' ground and its stylized props, by script (as medieval_world.py, whose helpers it uses).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/pirate_world.py

Writes (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z, Blender Z = height; the sea's surface is at 0):
  Assets/Resources/World/pirate_terrain.fbx  Seabed__SeaSand (the sea floor: the reef west of the ship, the wreck's trench north-west, the
                                             cave's basin far west), Island__Jungle + Beach__SeaSand (the tropical island east of the ship, its
                                             cove on the north side)
  Assets/Resources/World/pirate_props.fbx    one empty per prop, parts <part>__<Material>: Ship, Shipwreck, SeaCave, Jetty, Gangplank, Plank,
                                             Cannon, CannonBalls, Barrel, Crate, Lantern, TreasureChest (its lid: Lid__*), Anchor, GreenShard,
                                             PalmTree, PalmSmall, RockA, RockB, CoralBrain, CoralFan, CoralTube, CoralBranch, Kelp, BubbleVent,
                                             Campfire, LeanTo, RopeLadder, Starfish
  Data/screenshots/round10/world/pirate_*.png
A prop's front faces Blender -Y (Unity -z). The Ship's bow points +Y (Unity +z), its keel on x 0.

The ship (the same numbers as World/Pirate.cs): hull z -22..24, half beam W(z) (6 amidships); main deck top 3.5 (z -22..15; the
captain's cabin under the quarterdeck z -21.5..-13, its door x -1..1 in the bulkhead at z -13); quarterdeck top 6.0 (z -22..-13, ramps
up x +-3..4.8 from z -7); forecastle top 5.5 (z 15..24, ramps x +-2.4..4.2 from z 10; its hatch x -0.9..0.9, z 19.1..20.9 into the
bosun's locker below at 3.5); the hold (floor 0.6, z -12..14, a ramp x -1.1..1.1 from the main deck's hatch z 7.2 down to z 1.5; a
plank partition at z -6 with a 3 m gap at x -1.5..1.5 where the crates stand); rails 1.1 m of turned balusters (a gangway gap on the
starboard side, x +, z -3.2..-0.8); masts: main (0, 0) to 27 with the main top (r 2.2, top 13) and the crow's nest (r 1.1, floor 21),
fore (0, 12) to 23 with the fore top (r 1.8, top 11.5), mizzen (0, -17) on the quarterdeck.
"""
import bpy, bmesh, math, os, random, sys
from mathutils import Vector, noise

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, 'medieval_world.py'), encoding='utf-8').read()
exec(_src.split('# ---------------------------------------------------------------------------------------------------------------- the castle')[0])

SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round10', 'world')
X0, X1, Z0, Z1 = -200.0, 200.0, -150.0, 200.0
COL.update({'ShipRed': (.55, .2, .14), 'DeckWood': (.74, .54, .33), 'SailPink': (.98, .72, .8), 'Brass': (.86, .66, .3), 'SeaSand': (.93, .84, .64),
            'Jungle': (.24, .55, .26), 'PalmTrunk': (.62, .46, .3), 'PalmLeaf': (.25, .62, .28), 'CoralPink': (1, .45, .6), 'CoralOrange': (1, .55, .25),
            'CoralPurple': (.6, .35, .9), 'CoralYellow': (1, .85, .3), 'Seaweed': (.2, .58, .38), 'WreckWood': (.36, .3, .25), 'ChestWood': (.5, .3, .15),
            'GreenShard': (.35, 1, .5), 'Rock': (.52, .45, .42), 'Starfish': (1, .5, .35), 'Black': (.08, .07, .07)})
random.seed(10)


def poly2(name, verts, faces, parent, smooth=False):
    """a two-sided sheet: every face and a reversed twin on its own vertices (no normal recalculation: open shells, sails)"""
    bm = bmesh.new(); a = [bm.verts.new(v) for v in verts]; b = [bm.verts.new(v) for v in verts]
    for f in faces:
        bm.faces.new([a[i] for i in f]); bm.faces.new([b[i] for i in reversed(f)])
    ob = part(name, bm, parent)
    for p in ob.data.polygons: p.use_smooth = smooth
    return ob


# ---------------------------------------------------------------------------------------------------------------- ground
def island_d(x, z): return math.hypot((x - 80) / 55, (z - 12) / 62)


def floor(x, z):
    h = -14.0 + 1.2 * n2(x, z, 30, 1.7)
    reef = box_mask(x, z, -115, -25, -70, 70, 25)
    h = h * (1 - reef) + (-10.5 + 1.6 * max(0, n2(x, z, 9, 4.2) + .2)) * reef
    clear = 1 - sstep((math.hypot(x + 45, z - 10) - 6) / 4)          # the green shard's clearing among the corals
    h = h * (1 - clear) + -11.0 * clear
    trench = 1 - sstep((math.hypot((x + 70) / 1.3, z - 118) - 18) / 18)  # the wreck's trench
    h = h * (1 - trench) + -21.0 * trench
    basin = box_mask(x, z, -175, -115, -45, 45, 12)                    # the cave's basin
    h = h * (1 - basin) + -18.0 * basin
    edge = max(abs(x) - 170, abs(z - 25) - 160, 0)
    return h - 6 * sstep(edge / 30)


def height(x, z):
    d = island_d(x, z)
    land = 9.5 * sstep((1.0 - d) / .7) + 1.4 * max(0, n2(x, z, 14, 6.1)) * sstep((.85 - d) / .3) - 22 * sstep((d - 1.0) / .7)
    h = max(floor(x, z), land)
    # the cove: a shallow lagoon on the north side and its channel to the sea
    r = math.hypot(x - 82, z - 64)
    cove = max(1 - sstep((r - 8) / 14), box_mask(x, z, 77, 87, 64, 92, 6))   # (a gentle bank: walk out anywhere)
    h = h * (1 - cove) + -2.4 * cove if cove > 0 and h > -2.4 else h
    return h


def ground():
    xs = [X0 + 4 * i for i in range(int((X1 - X0) / 4) + 1)]
    zs = [Z0 + 4 * i for i in range(int((Z1 - Z0) / 4) + 1)]
    # fine (2 m) over the island
    xs = sorted(set(xs + [20 + 1 * i for i in range(126)])); zs = sorted(set(zs + [-58 + 1 * i for i in range(160)]))   # 1 m over the island: a smooth grass edge
    H = {(x, z): height(x, z) for x in xs for z in zs}
    out = []
    for nm, keep in (('Seabed__SeaSand', lambda m: m < .4), ('Beach__SeaSand', lambda m: .4 <= m < 1.6), ('Island__Jungle', lambda m: m >= 1.6)):
        bm = bmesh.new(); vmap = {}
        def V(x, z):
            k = (x, z)
            if k not in vmap: vmap[k] = bm.verts.new((x, z, H[k]))
            return vmap[k]
        for j in range(len(zs) - 1):
            for i in range(len(xs) - 1):
                q = [(xs[i], zs[j]), (xs[i + 1], zs[j]), (xs[i + 1], zs[j + 1]), (xs[i], zs[j + 1])]
                m = sum(H[k] for k in q) / 4
                if keep(m): bm.faces.new([V(*k) for k in q])
        me = bpy.data.meshes.new(nm); bm.to_mesh(me); bm.free()
        for p in me.polygons: p.use_smooth = True
        ob = bpy.data.objects.new(nm, me); bpy.context.scene.collection.objects.link(ob); ob.data.materials.append(mat(nm.split('__')[1]))
        out.append(ob)
    return out


# ---------------------------------------------------------------------------------------------------------------- the ship
def W(z):
    if z < -8: return 4.6 + 1.4 * sstep((z + 22) / 14)
    if z <= 10: return 6.0
    t = min(1.0, (z - 10) / 14)
    return max(.35, 6.0 * (1 - t * t) ** .6)


def deck_top(z):
    return 6.0 if z < -13 or z > 15 else 3.5


def keel(z): return -3.0 + 1.6 * sstep((z - 12) / 12) + .4 * sstep((-z - 14) / 8)


def section(z, top):
    w = W(z); k = keel(z)
    return [(0, k), (.45 * w, k + .45), (.86 * w, -.5), (w, 1.4), (w, top)]


def half_width_at(z, y):
    pts = section(z, 10)
    for (a, ya), (b, yb) in zip(pts, pts[1:]):
        if ya <= y <= yb: return a + (b - a) * (y - ya) / max(1e-6, yb - ya)
    return W(z)


def hull(e, z0=-22.0, z1=24.0, step=1.0, mname='Hull__ShipRed', top=None, hole=None):
    top = top or deck_top
    zs = []; z = z0
    while z < z1 + 1e-6: zs.append(round(z, 3)); z += step
    for s in (-13.0, 15.0):
        if z0 < s < z1: zs += [s - .001, s + .001]
    zs = sorted(set(zs))
    verts, faces = [], []
    rows = []
    for z in zs:
        sec = section(z, top(z)); row = []
        for sign in (-1, 1):
            for i, (x, y) in enumerate(sec if sign > 0 else sec):
                if sign < 0 and i == 0: continue
                row.append(len(verts)); verts.append((sign * x, z, y))
        rows.append(row)
    # row order: [-(1..4)] then [+(0..4)] -> make one polyline: -4,-3,-2,-1, 0, +1..+4
    n = len(section(0, 1))
    def ordered(r): return list(reversed(r[:n - 1])) + r[n - 1:]
    rows = [ordered(r) for r in rows]
    for a, b in zip(rows, rows[1:]):
        za = verts[a[0]][1]
        for i in range(len(a) - 1):
            if hole and hole(za, verts[a[i]][2], verts[a[i]][0]): continue
            faces.append((a[i], a[i + 1], b[i + 1], b[i]))
    faces.append(tuple(rows[0]))
    faces.append(tuple(reversed(rows[-1])))
    return poly2(mname, verts, faces, e, True)


def strip_deck(e, name, z0, z1, top, thick=.3, holes=(), inset=.12, y_for_width=None):
    z = z0
    while z < z1 - 1e-6:
        zb = min(z1, z + 1.0); zm = (z + zb) / 2
        w = (half_width_at(zm, y_for_width) if y_for_width is not None else W(zm)) - inset
        xs = [(-w, w)]
        for hx0, hx1, hz0, hz1 in holes:
            if zm > hz0 and zm < hz1:
                xs = [(a, min(b, hx0)) for a, b in xs if a < hx0] + [(max(a, hx1), b) for a, b in xs if b > hx1]
        for a, b in xs:
            if b - a > .05: blk(name, a, b, z, zb, top - thick, top, e)
        z = zb


def baluster_rail(e, side, z0, z1, base, skip=None):
    """turned balusters every 0.3 m along one side, a cap rail on top (1.1 m over the deck)"""
    z = z0 + .15
    pts = []
    while z < z1:
        if not any(a < z < b for a, b in (skip or ())):
            x = side * (W(z) - .08)
            cone('Baluster__Cream', (x, z, base), .06, .06, .22, e, 6, False)
            cone('Baluster__Cream', (x, z, base + .22), .06, .1, .3, e, 6, False)
            cone('Baluster__Cream', (x, z, base + .52), .1, .05, .38, e, 6, False)
            cone('Baluster__Cream', (x, z, base + .9), .05, .05, .12, e, 6, False)
        pts.append((side * (W(z) - .08), z)); z += .3
    # the cap rail, segment by segment (follows the hull's curve), and a bottom rail
    zz = z0
    while zz < z1 - 1e-6:
        zb = min(z1, zz + 1.0)
        if not any(a < (zz + zb) / 2 < b for a, b in (skip or ())):
            xa, xb = side * (W(zz) - .08), side * (W(zb) - .08)
            ln = math.hypot(xb - xa, zb - zz); ang = math.atan2(xb - xa, zb - zz)
            cube('RailCap__ShipRed', ((xa + xb) / 2, (zz + zb) / 2, base + 1.06), (.22, ln + .04, .12), e, -ang)
            cube('RailFoot__ShipRed', ((xa + xb) / 2, (zz + zb) / 2, base + .04), (.18, ln + .04, .08), e, -ang)
        zz = zb


def ramp(e, name, x0, x1, za, ya, zb, yb, thick=.25):
    ln = math.hypot(zb - za, yb - ya); ang = math.atan2(yb - ya, zb - za)
    c = ((x0 + x1) / 2, (za + zb) / 2, (ya + yb) / 2 - thick / 2 * math.cos(ang))
    cube(name, c, (x1 - x0, ln, thick), e, 0, (ang, 0))
    # solid underneath (a wedge down to the floor at the low end's height): nobody small gets under a ramp and stuck there
    lo = min(ya, yb) - .02
    poly('RampSide__DeckWood', [(x0, za, ya - .02), (x1, za, ya - .02), (x1, zb, yb - .02), (x0, zb, yb - .02), (x0, zb if yb > ya else za, lo), (x1, zb if yb > ya else za, lo)],
         [(0, 1, 2, 3), (0, 3, 4), (1, 5, 2), (3, 2, 5, 4), (0, 4, 5, 1)] if yb > ya else [(0, 1, 2, 3), (0, 4, 3), (1, 2, 5), (0, 1, 5, 4), (4, 5, 2, 3)], e)


def sail(e, name, cx, cy, ztop, zbot, wtop, wbot, belly=.8):
    nx, nz = 6, 5; verts = []; faces = []
    for j in range(nz + 1):
        t = j / nz; zz = ztop + (zbot - ztop) * t; w = wtop + (wbot - wtop) * t
        for i in range(nx + 1):
            u = i / nx; x = cx - w / 2 + w * u
            b = belly * math.sin(math.pi * u) * math.sin(math.pi * min(1, t * 1.2 + .1))
            verts.append((x, cy + b, zz))
    for j in range(nz):
        for i in range(nx):
            a = j * (nx + 1) + i; faces.append((a, a + 1, a + nx + 2, a + nx + 1))
    return poly2(name, verts, faces, e, True)


def rope(e, a, b, r=.035, name='Rope__Rope'):
    a = Vector(a); b = Vector(b); d = b - a
    ob = cone(name, (0, 0, 0), r, r, d.length, e, 5, False)
    ob.location = a; ob.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return ob


def lantern(e, c):
    cube('LanternFrame__Brass', (c[0], c[1], c[2] + .32), (.26, .26, .05), e)
    cube('LanternFrame__Brass', (c[0], c[1], c[2] - .02), (.24, .24, .05), e)
    cube('LanternGlow__WarmLight', (c[0], c[1], c[2] + .15), (.18, .18, .3), e)
    cone('LanternTop__Brass', (c[0], c[1], c[2] + .34), .13, .02, .15, e, 6, False)


def ship(e):
    hull(e)
    # gun ports and a gold stripe along both sides, the stern's windows
    for side in (-1, 1):
        for z in range(-10, 13, 3):
            cube('GunPort__Black', (side * (W(z) + .02), z, 2.2), (.08, .7, .6), e)
        zz = -21.5
        while zz < 22.5:
            zb = zz + 1.0; xa, xb = side * (half_width_at(zz, 3.0) + .03), side * (half_width_at(zb, 3.0) + .03)
            cube('Stripe__Gold', ((xa + xb) / 2, (zz + zb) / 2, 3.0), (.05, math.hypot(xb - xa, 1) + .03, .14), e, -math.atan2(xb - xa, 1))
            zz = zb
    for x in (-2.6, -1.0, 1.0, 2.6): cube('Window__WarmLight', (x, -22.05, 4.9), (.9, .06, .9), e)
    cube('Transom__Gold', (0, -22.06, 6.4), (8.6, .05, .18), e)
    # decks
    strip_deck(e, 'Deck__DeckWood', -21.5, 15, 3.5, holes=[(-1.3, 1.3, 1.5, 7.2)])
    strip_deck(e, 'Deck__DeckWood', -22, -13, 6.0)
    strip_deck(e, 'Deck__DeckWood', 15, 23.6, 6.0)
    strip_deck(e, 'Deck__DeckWood', 15, 23.2, 3.5, y_for_width=3.3)         # the bosun's locker's floor
    strip_deck(e, 'HoldFloor__DeckWood', -12, 14, .6, y_for_width=.6)
    # deck planks' lines (cosmetic, slightly proud)
    for x in [i * .9 - 4.95 for i in range(12)]:
        if abs(x) > 1.35: cube('PlankLine__WreckWood', (x, 1.0, 3.505), (.03, 27.5, .01), e)
    # bulkheads: the quarterdeck's front with the cabin door, the forecastle's front, the hold's ends and its partition
    w = W(-13) - .1
    blk('Bulkhead__ShipRed', -w, -1.0, -13.15, -12.95, 3.5, 6.0, e); blk('Bulkhead__ShipRed', 1.0, w, -13.15, -12.95, 3.5, 6.0, e)
    blk('Bulkhead__ShipRed', -1.0, 1.0, -13.15, -12.95, 5.8, 6.0, e)
    w = W(15) - .1; blk('Bulkhead__ShipRed', -w, -1.0, 14.95, 15.15, 3.5, 6.0, e); blk('Bulkhead__ShipRed', 1.0, w, 14.95, 15.15, 3.5, 6.0, e)
    blk('Bulkhead__ShipRed', -1.0, 1.0, 14.95, 15.15, 5.8, 6.0, e)
    for z in (-12.2, 14.2):
        hw = half_width_at(z, .6); blk('HoldEnd__DeckWood', -hw, hw, z - .1, z + .1, .6, 3.2, e)
    hw = half_width_at(-6, .6); blk('Partition__DeckWood', -hw, -1.5, -6.1, -5.9, .6, 3.2, e); blk('Partition__DeckWood', 1.5, hw, -6.1, -5.9, .6, 3.2, e)
    blk('Partition__DeckWood', -1.5, 1.5, -6.1, -5.9, 2.75, 3.2, e)   # (the lintel over the gap: 2.15 m clear, Dan is 1.8)
    # ramps (stairs drawn as ramps with treads)
    for s in (-1, 1):
        ramp(e, 'Ramp__DeckWood', min(s * 3.0, s * 4.8), max(s * 3.0, s * 4.8), -7, 3.5, -13, 6.0)
        ramp(e, 'Ramp__DeckWood', min(s * 2.4, s * 4.2), max(s * 2.4, s * 4.2), 9, 3.5, 15, 6.0)
    ramp(e, 'Ramp__DeckWood', -1.1, 1.1, 7.2, 3.5, 1.5, .6)
    # rails of turned balusters: the main deck, the quarterdeck, the forecastle; a gangway gap (starboard) and the bulwark under the
    # quarterdeck / forecastle rails are the hull itself
    for s in (-1, 1):
        baluster_rail(e, s, -12.9, 14.9, 3.5, skip=[(-3.2, -.8)] if s > 0 else [(3.45, 4.55), (-6.7, -5.3)])   # gaps: the gangway (starboard), the plank and the rope ladder (port)
        baluster_rail(e, s, -21.9, -13.1, 6.0)
        baluster_rail(e, s, 15.1, 23.0, 6.0)
    # the quarterdeck's and the forecastle's front rails: between their ramps and outside them (the ramps' tops are the way up)
    for z, base, spans in ((-12.95, 6.0, ((-2.95, 2.95), (-W(-13) + .1, -4.85), (4.85, W(-13) - .1))), (15.05, 6.0, ((-2.35, 2.35), (-W(15) + .1, -4.25), (4.25, W(15) - .1)))):
        for x0, x1 in spans:
            n = max(2, int((x1 - x0) / .3) + 1)
            for i in range(n):
                x = x0 + (x1 - x0) * i / (n - 1)
                cone('Baluster__Cream', (x, z, base), .06, .1, .5, e, 6, False); cone('Baluster__Cream', (x, z, base + .5), .1, .05, .5, e, 6, False)
            cube('RailCap__ShipRed', ((x0 + x1) / 2, z, base + 1.06), (x1 - x0 + .1, .22, .12), e)
    # masts, tops, the crow's nest, yards
    cone('Mast__Wood', (0, 0, .6), .45, .32, 26.4, e, 12)
    cone('Mast__Wood', (0, 12, .6), .38, .26, 22.4, e, 12)
    cone('Mast__Wood', (0, -17, 6.0), .32, .22, 14.0, e, 12)
    cone('Top__DeckWood', (0, 0, 12.75), 2.2, 2.2, .25, e, 20); cone('TopRim__ShipRed', (0, 0, 13.0), 2.2, 2.2, .35, e, 20)
    cone('Top__DeckWood', (0, 12, 11.25), 1.8, 1.8, .25, e, 18); cone('TopRim__ShipRed', (0, 12, 11.5), 1.8, 1.8, .3, e, 18)
    cone('Nest__DeckWood', (0, 0, 20.8), 1.1, 1.1, .2, e, 16)
    for k in range(18):
        a = k / 18 * math.tau
        if 1.85 < a < 2.8: continue   # the way in (the climb comes up on the front)
        cube('NestWall__ShipRed', (math.cos(a) * 1.08, math.sin(a) * 1.08, 21.55), (.1, .42, 1.1), e, a)
    for (z, y, ln, r) in ((0, 9.5, 13, .2), (0, 17, 10, .17), (12, 8.5, 11, .18), (12, 14.5, 8, .15), (-17, 11, 8, .14)):
        cyl_x('Yard__Wood', (0, z + .5, y), r, ln, e, 8)
    # pink sails under the yards
    sail(e, 'Sail__SailPink', 0, .7, 9.3, 5.4, 12.4, 13.0)
    sail(e, 'Sail__SailPink', 0, .66, 16.8, 10.0, 9.6, 12.0)
    sail(e, 'Sail__SailPink', 0, 12.7, 8.3, 5.3, 10.6, 11.0)
    sail(e, 'Sail__SailPink', 0, 12.66, 14.3, 8.9, 7.6, 10.0)
    sail(e, 'Sail__SailPink', 0, -16.4, 10.8, 7.4, 7.6, 8.0, .6)
    # the jib (a triangle from the foremast to the bowsprit)
    poly2('Sail__SailPink', [(0, 13, 15.5), (0, 31, 9.0), (0, 14.5, 6.2)], [(0, 1, 2)], e)
    # the bowsprit, the stern's flag pole
    b = rope(e, (0, 23.0, 6.4), (0, 33, 9.6), .3, 'Bowsprit__Wood')
    rope(e, (0, -21.6, 6.0), (0, -23.4, 11.0), .06, 'FlagPole__Wood')
    poly2('Flag__Black', [(0, -23.3, 10.9), (0, -25.6, 10.5), (0, -23.3, 9.6)], [(0, 1, 2)], e)
    # rigging: stays and shrouds, and the ratline nets (rope rungs) from the rails up to the tops (the climb: World/Pirate.cs RopeClimb)
    for s in (-1, 1):
        for zc, top, rtop, zs in ((-1.0, 13.35, 2.0, (-2.0, 0.0)), (12, 11.8, 1.6, (11, 13))):
            for zz in (zs[0], (zs[0] + zs[1]) / 2, zs[1]):
                rope(e, (s * (W(zz) - .05), zz, 4.6), (s * rtop, zc, top))
            for k in range(1, 15):
                t = k / 15; y = 4.6 + (top - 4.6) * t; x = s * ((W(zc) - .05) + (rtop - (W(zc) - .05)) * t)
                rope(e, (x, zs[0] + (zc - zs[0]) * t * .15, y), (x, zs[1] - (zs[1] - zc) * t * .15, y), .025)
    for zc in (0, 12):   # up to the crow's nest / the masthead on the mast's front
        for k in range(14):
            y = 13.2 + k * .55
            if zc == 0 and y > 21: break
            if zc == 12: break
            rope(e, (-1.35, 1.0, y), (-.7, 1.45, y), .025)
    rope(e, (-1.35, 1.0, 13.3), (-1.3, 1.0, 22.0), .03); rope(e, (-.75, 1.45, 13.3), (-.7, 1.45, 22.0), .03)
    rope(e, (0, 0, 26.8), (0, 12, 22.8)); rope(e, (0, 12, 22.8), (0, 33, 9.6)); rope(e, (0, 0, 26.8), (0, -17, 19.8)); rope(e, (0, -17, 19.8), (0, -21.8, 7.0))
    # lanterns: the stern, the masts, the forecastle
    for c in ((-4.2, -21.7, 7.4), (4.2, -21.7, 7.4), (0, -.6, 6.8), (0, 11.5, 6.8), (-3.6, 22.0, 7.4), (3.6, 22.0, 7.4), (-4.6, -6.5, 5.0), (4.6, 6.5, 5.0)):
        lantern(e, c)
    # the ship's wheel on the quarterdeck, the hold's beams
    cube('WheelPost__Wood', (0, -18.6, 6.5), (.2, .2, 1.0), e)
    wheel(e, (0, -18.45, 7.2), .7, .08, 8)
    for z in range(-11, 14, 3):
        hw = half_width_at(z, 3.0) - .1
        if 1.0 < z < 7.7: blk('Beam__WreckWood', -hw, -1.4, z - .125, z + .125, 2.95, 3.15, e); blk('Beam__WreckWood', 1.4, hw, z - .125, z + .125, 2.95, 3.15, e)   # (not over the hatch's ramp)
        else: cube('Beam__WreckWood', (0, z, 3.05), (2 * hw, .25, .2), e)


def wheel(e, c, r, thick=.14, spokes=8):
    ring('Wheel__Wood', c, r, thick, e, 16, 'Y')
    for k in range(spokes):
        a = k / spokes * math.tau
        cube('Spoke__Wood', (c[0] + math.cos(a) * r * .55, c[1], c[2] + math.sin(a) * r * .55), (r * 1.25, thick * .6, thick * .6), e, 0, (0, -a))
    cyl_y('Hub__Brass', c, r * .15, thick * 1.5, e, 8)


# ---------------------------------------------------------------------------------------------------------------- other props
def wreck(e):
    """an old sunken ship lying on its port side (rolled 22 deg): a hole in its starboard side, its deck half gone (World/Pirate.cs
    places it in the trench, its origin on the floor)"""
    def top(z): return 3.0
    def hole(z, y, x): return (x > 2 and -3 < z < 3 and -.6 < y < 2.6)
    g = empty('WreckBody'); g.parent = e
    hull(g, -14, 15, 1.0, 'Hull__WreckWood', top, hole)
    strip_deck(g, 'Deck__WreckWood', -13.5, -3, 3.0); strip_deck(g, 'Deck__WreckWood', 6, 14, 3.0)
    strip_deck(g, 'Floor__WreckWood', -12, 12, .4, y_for_width=.4)
    cone('Mast__WreckWood', (0, -6, .4), .35, .3, 5.0, g, 10); cone('Mast__WreckWood', (0, 5, .4), .4, .25, 7.5, g, 10)
    for z in range(-12, 14, 2):
        for k in range(10):
            a = k / 10 * math.tau
            if math.sin(a) > .1: continue   # the lower half only
            b = cube('Rib__WreckWood', (math.cos(a) * 4.0, z, 1.0 + math.sin(a) * 4.0), (.22, .22, 2 * math.pi * 4.0 / 10 * 1.15), g); b.rotation_euler = (0, -a + math.pi / 2, 0)
    g.rotation_euler = (0, math.radians(-22), 0); g.location = (0, 0, 2.2)
    b = rope(e, (2, 18, .4), (11, 26, .8), .4, 'BrokenMast__WreckWood')
    for k in range(6): sphere('Barnacle__Rock', (random.uniform(-3, 3), random.uniform(-10, 10), random.uniform(0, 1)), .3, e, (1, 1, .5), seg=(6, 4))


def cave(e):
    """the sea cave: a rock dome over a chamber x -10..10, z -20..20 (local; floor 0), its mouth east (+x) z -4..4 up to 6, a 0.7 m gap in
    the north wall at x -2..-1.3, y 0..0.95 into a small grotto z 20.6..28 (only otter Kira fits)"""
    R = 'Cave__Rock'
    def rock_box(x0, x1, z0, z1, y0, y1):
        blk(R, x0, x1, z0, z1, y0, y1, e)
    rock_box(-14, -10, -24, 32, -1, 12)                 # west wall
    rock_box(10, 14, -24, -4, -1, 12); rock_box(10, 14, 4, 32, -1, 12); rock_box(10, 14, -4, 4, 6, 12)   # east wall and its mouth
    rock_box(-14, 14, -24, -20, -1, 12)                 # south
    rock_box(-14, 14, 28, 32, -1, 12)                   # the grotto's back
    rock_box(-10, -2, 20, 20.6, -1, 12); rock_box(-1.3, 10, 20, 20.6, -1, 12); rock_box(-2, -1.3, 20, 20.6, .95, 12)   # the north wall's gap
    rock_box(-14, 14, -24, 32, 10, 14)                  # the roof
    # lumps on the outside so it reads as a rock, not a box
    for k in range(26):
        a = random.uniform(0, math.tau); x = math.cos(a) * 15 + random.uniform(-2, 2); z = 4 + math.sin(a) * 30 * .95
        if x > 6 and abs(z) < 11: continue   # (the mouth, east)
        sphere('Lump__Rock', (x, z, random.uniform(0, 12)), random.uniform(3, 5), e, (1, 1, .8), seg=(8, 6))
    sphere('Lump__Rock', (0, 4, 13), 13, e, (1.1, 2.3, .35), seg=(12, 8))


def palm(e, s=1.0, lean=.3):
    p = Vector((0, 0, 0)); h = 7 * s
    for k in range(8):
        t = k / 8; q = Vector((lean * s * 2.4 * t * t, 0, h * (k + 1) / 8))
        rope(e, p, q, (.28 - .1 * t) * s, 'Trunk__PalmTrunk'); p = q
    for k in range(7):
        a = k / 7 * math.tau
        tip = p + Vector((math.cos(a) * 3.4 * s, math.sin(a) * 3.4 * s, -1.4 * s))
        mid = p + Vector((math.cos(a) * 1.8 * s, math.sin(a) * 1.8 * s, .4 * s))
        for u, v in ((p, mid), (mid, tip)):
            d = v - u; ob = cube('Frond__PalmLeaf', ((u + v) / 2), (.9 * s, d.length, .05), e)
            ob.rotation_euler = d.to_track_quat('Y', 'Z').to_euler()
    for k in range(3): sphere('Coconut__PalmTrunk', p + Vector((math.cos(k * 2.1) * .3, math.sin(k * 2.1) * .3, -.35)), .2 * s, e, seg=(8, 6))


def rock(e, r, seed, name='Rock__Rock', sq=(1, 1, .7)):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r)
    for v in bm.verts:
        v.co *= 1 + .25 * noise.noise(v.co * 1.3 / r + Vector((seed, seed * .7, 0)))
        v.co = Vector((v.co.x * sq[0], v.co.y * sq[1], v.co.z * sq[2]))
    ob = part(name, bm, e); ob.location = (0, 0, r * sq[2] * .55)
    return ob


def props():
    obs = []
    def P(n): o = empty(n); obs.append(o); return o
    ship(P('Ship'))
    wreck(P('Shipwreck'))
    cave(P('SeaCave'))
    e = P('Jetty')   # 14 m along +x from its origin, 4 m wide (y -2..2), top 1.2
    blk('JettyDeck__DeckWood', 0, 14, -2, 2, 1.0, 1.2, e)
    for x in range(0, 15, 3):
        for y in (-1.8, 1.8): cone('Pile__WreckWood', (x, y, -4), .18, .18, 5.6, e, 8)
    for x in (0, 14):
        for y in (-1.8, 1.8): cone('Bollard__Wood', (x, y, 1.2), .15, .12, .5, e, 8)
    e = P('Gangplank')   # 8 m along +x, 1.2 wide, its top at 0 (World/Pirate.cs tilts it)
    blk('Gangplank__DeckWood', 0, 8, -.6, .6, -.12, 0, e)
    for x in range(1, 8): cube('Cleat__WreckWood', (x, 0, .02), (.08, 1.1, .05), e)
    e = P('Plank')       # 4 m along +x, 0.5 wide, its top at 0
    blk('Plank__DeckWood', 0, 4, -.25, .25, -.08, 0, e)
    e = P('Cannon')      # facing -y (its muzzle), 2 m long, on a carriage
    blk('Carriage__WreckWood', -.45, .45, -.6, .7, .2, .55, e)
    for x in (-.5, .5):
        for y in (-.4, .5): cyl_x('WheelC__WreckWood', (x, y, .25), .25, .12, e, 10)
    cyl_y('Barrel__Iron', (0, -.15, .75), .26, 2.0, e, 12); sphere('Breech__Iron', (0, .85, .75), .28, e, seg=(10, 6))
    cyl_y('Muzzle__Iron', (0, -1.15, .75), .32, .15, e, 12)
    e = P('CannonBalls')
    for i, (x, y, z) in enumerate(((-.2, -.2, .15), (.2, -.2, .15), (-.2, .2, .15), (.2, .2, .15), (0, 0, .42))): sphere('Ball__Iron', (x, y, z), .15, e, seg=(8, 6))
    e = P('Barrel')
    cone('Barrel__Wood', (0, 0, 0), .38, .45, .55, e, 14); cone('Barrel__Wood', (0, 0, .55), .45, .38, .55, e, 14)
    for z in (.12, .98): cone('Hoop__Iron', (0, 0, z), .41, .41, .07, e, 14)
    e = P('Crate')
    cube('Crate__Plank', (0, 0, .5), (1, 1, 1), e)
    for z in (.06, .94): cube('Slat__WreckWood', (0, 0, z), (1.02, 1.02, .1), e)
    for x in (-.47, .47): cube('Slat__WreckWood', (x, -.51, .5), (.1, .02, 1), e)
    e = P('Lantern'); lantern(e, (0, 0, .1))
    e = P('TreasureChest')   # 1.0 x 0.6 x 0.6, the lid a separate part (Lid__*: World/Pirate.cs opens it)
    blk('Chest__ChestWood', -.5, .5, -.3, .3, 0, .45, e)
    for x in (-.45, .45): blk('Band__Gold', x - .04, x + .04, -.31, .31, 0, .46, e)
    blk('Lock__Gold', -.08, .08, -.33, -.3, .25, .42, e)
    lid = cyl_x('Lid__ChestWood', (0, 0, .45), .3, 1.0, e, 12)
    e = P('Anchor')          # lying on its side, 2.6 m long along y, ring at +y
    cyl_y('Shank__Iron', (0, 0, .2), .12, 2.4, e, 8)
    cube('Stock__Wood', (0, 1.0, .2), (1.6, .18, .18), e)
    ring('Ring__Iron', (0, 1.35, .2), .25, .08, e, 10, 'Z')
    for s in (-1, 1):
        d = Vector((s * .9, -.5, 0)); a = Vector((0, -1.2, .2)); ob = cube('Arm__Iron', (a + d / 2), (.16, d.length, .16), e); ob.rotation_euler = (0, 0, math.atan2(-d.x, d.y))
        cone('Fluke__Iron', (s * .9, -1.75, .1), .25, .02, .2, e, 4)
    e = P('GreenShard')
    for k, (r, h, tilt) in enumerate(((.16, .9, 0), (.1, .55, .4), (.09, .45, -.5))):
        ob = cone('Crystal__GreenShard', (0, 0, 0), r, .01, h, e, 6, False); ob.rotation_euler = (tilt, tilt * .6, k)
    e = P('PalmTree'); palm(e, 1.0, .5)
    e = P('PalmSmall'); palm(e, .65, .2)
    e = P('RockA'); rock(e, 1.6, 1.0)
    e = P('RockB'); rock(e, 2.6, 2.3, sq=(1.2, 1, .8))
    e = P('CoralBrain'); sphere('Brain__CoralPink', (0, 0, .35), .6, e, (1, 1, .65), seg=(12, 8))
    for k in range(8): sphere('Bump__CoralPink', (math.cos(k) * .35, math.sin(k) * .35, .55), .18, e, seg=(6, 4))
    e = P('CoralFan')
    for k in range(3):
        ob = poly2('Fan__CoralPurple', [(-.9 + k * .2, 0, 0), (.9 - k * .1, 0, 0), (.7, 0, 1.6 - k * .2), (-.6, 0, 1.8 - k * .2)], [(0, 1, 2, 3)], e)
        ob.rotation_euler = (0, 0, k * .5)
    e = P('CoralTube')
    for k in range(7):
        a = k * 2.4; r = .25 * k ** .5
        cone('Tube__CoralOrange', (math.cos(a) * r, math.sin(a) * r, 0), .14, .17, .6 + .25 * (k % 3), e, 8)
    e = P('CoralBranch')
    def branch(p, d, l, depth):
        q = p + d * l; rope(e, p, q, .07 * (depth + 1), 'Branch__CoralYellow')
        if depth == 0: sphere('Tip__CoralYellow', q, .1, e, seg=(6, 4)); return
        for a in (-.6, .6):
            nd = Vector((d.x + a * .7, d.y + a * .5, d.z)).normalized(); branch(q, nd, l * .7, depth - 1)
    branch(Vector((0, 0, 0)), Vector((0, 0, 1)), .7, 3)
    e = P('Kelp')
    for k in range(5):
        x, y = math.cos(k * 1.3) * .4, math.sin(k * 1.3) * .4; p = Vector((x, y, 0))
        for j in range(6):
            q = p + Vector((.15 * math.sin(j + k), .1 * math.cos(j * 1.3), .9)); d = q - p
            ob = cube('Blade__Seaweed', (p + q) / 2, (.25, .04, d.length), e); ob.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); p = q
    e = P('BubbleVent')
    for k in range(7): sphere('Vent__Rock', (math.cos(k / 7 * math.tau) * .7, math.sin(k / 7 * math.tau) * .7, .2), .35, e, seg=(6, 4))
    e = P('Campfire')
    for k in range(5): cube('Log__WreckWood', (math.cos(k * 1.26) * .35, math.sin(k * 1.26) * .35, .12), (.9, .14, .14), e, k * 1.26)
    sphere('Embers__Ember', (0, 0, .1), .35, e, (1, 1, .35), seg=(8, 4))
    for k in range(8): sphere('Stone__Rock', (math.cos(k * .785) * .75, math.sin(k * .785) * .75, .1), .17, e, seg=(6, 4))
    e = P('LeanTo')   # a sail stretched on two poles, open to -y
    for x in (-1.4, 1.4): cone('Pole__Wood', (x, -1, 0), .06, .06, 2.2, e, 6)
    poly2('Sailcloth__SailPink', [(-1.6, -1, 2.2), (1.6, -1, 2.2), (1.6, 1.2, 0), (-1.6, 1.2, 0)], [(0, 1, 2, 3)], e)
    e = P('RopeLadder')   # 5 m down from its origin (hung from a rail)
    for x in (-.25, .25): rope(e, (x, 0, 0), (x, 0, -5))
    for k in range(10): cube('Rung__Wood', (0, 0, -.25 - k * .5), (.55, .06, .05), e)
    e = P('Starfish')
    for k in range(5):
        a = k / 5 * math.tau; ob = cone('Arm__Starfish', (0, 0, 0), .07, .01, .22, e, 5, False); ob.rotation_euler = (math.pi / 2, 0, a)
    sphere('Middle__Starfish', (0, 0, .02), .08, e, (1, 1, .4), seg=(8, 4))
    return obs


# ---------------------------------------------------------------------------------------------------------------- tidy, render
def merge_parts(e):
    bpy.context.view_layer.update()   # (new parts' matrix_world is stale until the depsgraph runs: the balusters all merged onto one spot)
    groups = {}
    for c in list(e.children_recursive):
        if c.type != 'MESH': continue
        groups.setdefault(c.name.split('.')[0], []).append(c)
    for name, objs in groups.items():
        if len(objs) < 2: continue
        # bake each part's own transform relative to the prop, then join
        for o in objs:
            mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active; j.name = name
        mw = j.matrix_world.copy(); j.parent = e; j.matrix_parent_inverse = e.matrix_world.inverted()
    for c in list(e.children_recursive):
        if c.type == 'EMPTY':
            for k in list(c.children):
                mw = k.matrix_world.copy(); k.parent = e; k.matrix_world = mw
            bpy.data.objects.remove(c)


def tris(obs): return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs if o.type == 'MESH')


def look(cam, at):
    d = Vector(at) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def shot(sc, name):
    sc.render.filepath = os.path.join(SHOTS, name); bpy.ops.render.render(write_still=True)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    os.makedirs(OUT, exist_ok=True); os.makedirs(SHOTS, exist_ok=True)
    ground_obs = ground()
    export(os.path.join(OUT, 'pirate_terrain.fbx'), ground_obs)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'; sc.display.shading.light = 'STUDIO'
    sc.display.shading.show_backface_culling = False
    sc.render.resolution_x = 1000; sc.render.resolution_y = 880
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 400; cam.location = (0, 25, 300); cam.data.clip_end = 2000
    obs = props()
    for o in obs: merge_parts(o)
    byname = {o.name: o for o in obs}
    # placed as World/Pirate.cs does, for the overview renders
    byname['Shipwreck'].location = (-70, 118, -21); byname['SeaCave'].location = (-135, 0, -18); byname['Jetty'].location = (14, -2, 0)
    for o in obs:
        if o.name not in ('Ship', 'Shipwreck', 'SeaCave', 'Jetty'):
            for c in o.children: c.hide_render = True
    shot(sc, 'pirate_top.png')
    cam.data.type = 'PERSP'; cam.data.lens = 24; sc.render.resolution_x = 1400; sc.render.resolution_y = 900
    cam.location = (28, -30, 18); look(cam, (0, 2, 6)); shot(sc, 'pirate_ship_se.png')
    cam.location = (-24, 30, 14); look(cam, (0, 0, 7)); shot(sc, 'pirate_ship_nw.png')
    cam.location = (0, -9, 5.3); look(cam, (0, 12, 6)); shot(sc, 'pirate_deck.png')
    cam.location = (3.5, 12, 2.2); look(cam, (0, -8, 1.5)); cam.data.lens = 18; shot(sc, 'pirate_hold.png')
    cam.data.lens = 24
    for g in ground_obs:
        if g.name.startswith('Seabed'): pass
    cam.location = (40, -70, 30); look(cam, (80, 20, 2)); shot(sc, 'pirate_island.png')
    cam.location = (-2, -4, 5.0); look(cam, (6, 4, 4.0)); shot(sc, 'pirate_rail.png')
    cam.location = (-90, 70, 6); look(cam, (-70, 118, -18)); shot(sc, 'pirate_wreck.png')
    cam.location = (-100, 0, -6); look(cam, (-135, 0, -12)); shot(sc, 'pirate_cave.png')
    # the small props on a sheet
    for g in ground_obs: g.hide_render = True
    for n in ('Ship', 'Shipwreck', 'SeaCave', 'Jetty'):
        for c in byname[n].children: c.hide_render = True
    names = ('Gangplank', 'Plank', 'Cannon', 'CannonBalls', 'Barrel', 'Crate', 'Lantern', 'TreasureChest', 'Anchor', 'GreenShard', 'PalmTree',
             'PalmSmall', 'RockA', 'RockB', 'CoralBrain', 'CoralFan', 'CoralTube', 'CoralBranch', 'Kelp', 'BubbleVent', 'Campfire', 'LeanTo',
             'RopeLadder', 'Starfish')
    cols, sp = 6, 9.0
    for i, n in enumerate(names):
        o = byname[n]; o.location = ((i % cols) * sp, -(i // cols) * sp, 5 if n == 'RopeLadder' else 0)
        for c in o.children: c.hide_render = False
    bpy.context.view_layer.update()
    rows = (len(names) + cols - 1) // cols
    mid = Vector(((cols - 1) * sp / 2, -(rows - 1) * sp / 2, 1))
    cam.data.type = 'ORTHO'; cam.rotation_euler = (math.radians(62), 0, math.radians(-25)); cam.data.ortho_scale = cols * sp * 1.15
    cam.location = mid + cam.rotation_euler.to_matrix() @ Vector((0, 0, 200)); cam.data.clip_end = 1000
    shot(sc, 'pirate_props.png')
    bpy.data.objects.remove(cam)
    for o in obs:
        o.location = (0, 0, 0)
        for c in o.children: c.hide_render = False
    for g in ground_obs: bpy.data.objects.remove(g)
    export(os.path.join(OUT, 'pirate_props.fbx'), obs)
    for o in obs: print('PIRATE prop %-14s %6d tris %3d parts' % (o.name, tris(o.children), len(o.children)))
    print('PIRATE written:', len(obs), 'props, %d tris in all' % sum(tris(o.children) for o in obs))


main()
