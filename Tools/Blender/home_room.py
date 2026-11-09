"""Shards of Time, Round 4 item 3: Dan and Abi's living room for the Act I cutscene, and the ornament, by script.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/home_room.py

From Dan's references (SourceArt/Reference/Worlds/00-Home/): purple-magenta walls in warm light, red velvet curtains tied back,
a tall window with snow outside and a garland of lights on the sill, a cream couch with red and lime cushions, a lit wreath
round a framed picture, a decorated tree with red / purple / silver baubles, warm fairy lights and a red heart ornament. The
ornament that shatters is the dark silver glass bauble Dan holds in home-decorating-tree-ornament.png: its own object
("Ornament", it is animated in the cutscene and comes back whole in Act XI). Stylized, not photo-real.

Writes Assets/Resources/World/home_room.fbx (1 unit = 1 m; Blender X = Unity x, Blender Y = Unity z; parts named
<part>__<Material>, coloured by World/WorldArt.cs). Top-level objects: Room (everything still), Ornament (the bauble, its
origin at the hook), HeartOrnament. The room: floor 8 x 7 m (x -4..4, z -2..5), back wall at z 5, side walls at x +-4,
ceiling at 3.2; the window in the back wall at x 1..3; the tree at (2.5, 3.3); the couch against the back wall at x -3.2..-0.6.
Also renders Data/screenshots/round4/home/room_preview.png.
"""
import bpy, bmesh, math, os, random
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World', 'home_room.fbx')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round4', 'home')
TREE = Vector((2.5, 3.3, 0))
ORNAMENT_AT = Vector((2.28, 2.55, 1.38))   # hanging on the tree's front (toward the camera, -Y), where Kira can reach it


def link(ob, parent):
    bpy.context.scene.collection.objects.link(ob)
    if parent: ob.parent = parent
    return ob


def obj(name, bm, parent, smooth=False):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
    return link(bpy.data.objects.new(name, me), parent)


def box(name, x0, x1, y0, y1, z0, z1, parent, bevel=0.0):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = Vector(((x0 + x1) / 2 + v.co.x * (x1 - x0), (y0 + y1) / 2 + v.co.y * (y1 - y0), (z0 + z1) / 2 + v.co.z * (z1 - z0)))
    if bevel > 0: bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=3, affect='EDGES')
    return obj(name, bm, parent, smooth=False)


def sphere(name, c, r, parent, seg=16, ring=10, scale=(1, 1, 1)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=ring, radius=r)
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + c
    return obj(name, bm, parent, smooth=True)


def cone(name, c, r0, r1, h, parent, seg=16, smooth=False):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r0, radius2=r1, depth=h)
    for v in bm.verts: v.co += c + Vector((0, 0, h / 2))
    return obj(name, bm, parent, smooth=smooth)


def torus(name, c, R, r, parent, seg=40, sides=10, rot_x=90):
    bm = bmesh.new(); rings = []
    for i in range(seg):
        a = i / seg * math.tau; ring = []
        for j in range(sides):
            b = j / sides * math.tau
            p = Vector(((R + r * math.cos(b)) * math.cos(a), (R + r * math.cos(b)) * math.sin(a), r * math.sin(b)))
            if rot_x == 90: p = Vector((p.x, -p.z, p.y))
            ring.append(bm.verts.new(p + c))
        rings.append(ring)
    for i in range(seg):
        for j in range(sides):
            a, b = rings[i], rings[(i + 1) % seg]
            bm.faces.new((a[j], b[j], b[(j + 1) % sides], a[(j + 1) % sides]))
    return obj(name, bm, parent, smooth=True)


def room():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    R = link(bpy.data.objects.new('Room', None), None)
    # shell: floor, walls (the back wall has the window hole x 1..3, z 0.9..2.7), ceiling
    box('Floor__Floor', -4, 4, -2, 5, -.1, 0, R)
    box('WallBackL__Wall', -4, 1, 5, 5.2, 0, 3.2, R); box('WallBackR__Wall', 3, 4, 5, 5.2, 0, 3.2, R)
    box('WallBackLow__Wall', 1, 3, 5, 5.2, 0, .9, R); box('WallBackHigh__Wall', 1, 3, 5, 5.2, 2.7, 3.2, R)
    box('WallLeft__Wall', -4.2, -4, -2, 5.2, 0, 3.2, R); box('WallRight__Wall', 4, 4.2, -2, 5.2, 0, 3.2, R)
    box('Ceiling__Ceiling', -4.2, 4.2, -2, 5.2, 3.2, 3.3, R)
    box('Skirting__Wood', -4, 4, 4.94, 5, 0, .12, R)
    # the window: frame, mullions, the snowy night outside (a backdrop with snow on the sill and flakes)
    box('WinFrameL__Frame', .95, 1.05, 4.95, 5.25, .85, 2.75, R); box('WinFrameR__Frame', 2.95, 3.05, 4.95, 5.25, .85, 2.75, R)
    box('WinFrameT__Frame', .95, 3.05, 4.95, 5.25, 2.65, 2.75, R); box('WinSill__Frame', .9, 3.1, 4.8, 5.25, .82, .9, R)
    box('WinMullionV__Frame', 1.97, 2.03, 5.08, 5.12, .9, 2.65, R)
    for z in (1.5, 2.1): box(f'WinMullionH{z}__Frame', 1.05, 2.95, 5.08, 5.12, z - .03, z + .03, R)
    box('Night__NightSky', .5, 3.5, 6.2, 6.3, .2, 3.4, R)
    box('SnowOutside__Snow', .5, 3.5, 5.3, 6.2, .2, .7, R)
    rnd = random.Random(3)
    for i in range(40): sphere(f'Flake{i}__Snow', Vector((rnd.uniform(1.05, 2.95), rnd.uniform(5.4, 6.1), rnd.uniform(.9, 2.6))), rnd.uniform(.012, .025), R, 6, 4)
    # the sill garland: a green rope with warm lights
    for i in range(14):
        t = i / 13; x = 1.0 + t * 2.0
        sphere(f'Garland{i}__Green', Vector((x, 4.88, .95 + .03 * math.sin(i))), .09, R, 8, 6, (1.4, 1, .9))
        if i % 2 == 0: sphere(f'GarlandLight{i}__WarmLight', Vector((x, 4.8, 1.0)), .03, R, 6, 4)
    # curtains: red velvet, gathered and tied back on both sides of the window
    for side, x in (('L', .6), ('R', 3.4)):
        bm = bmesh.new(); folds = 7; rows = 12; grid = []
        for r_ in range(rows + 1):
            z = 3.05 - r_ / rows * 3.0
            tie = 1 - .55 * math.exp(-((z - 1.1) / .35) ** 2)   # pinched at the tie-back
            row = []
            for f in range(folds * 2 + 1):
                u = f / (folds * 2); w = .5 * tie
                xx = x + (u - .5) * w; yy = 4.88 - .06 * math.sin(f * math.pi / 2) - .04
                row.append(bm.verts.new((xx, yy, z)))
            grid.append(row)
        for r_ in range(rows):
            for f in range(folds * 2): bm.faces.new((grid[r_][f], grid[r_][f + 1], grid[r_ + 1][f + 1], grid[r_ + 1][f]))
        obj(f'Curtain{side}__Velvet', bm, R, smooth=True)
        sphere(f'Tieback{side}__Gold', Vector((x, 4.84, 1.1)), .06, R, 10, 6)
    box('CurtainRod__Gold', .3, 3.7, 4.86, 4.9, 3.08, 3.12, R)
    # the couch: cream, with red and lime cushions
    box('CouchBase__Cream', -3.2, -.6, 4.1, 5, 0, .45, R, .06); box('CouchBack__Cream', -3.2, -.6, 4.7, 5, .45, 1.0, R, .08)
    box('CouchArmL__Cream', -3.4, -3.1, 4.1, 5, 0, .7, R, .08); box('CouchArmR__Cream', -.7, -.4, 4.1, 5, 0, .7, R, .08)
    box('SeatCushion1__Cream', -3.1, -1.95, 4.15, 4.7, .45, .58, R, .05); box('SeatCushion2__Cream', -1.85, -.7, 4.15, 4.7, .45, .58, R, .05)
    box('Cushion1__Red', -3.0, -2.4, 4.55, 4.72, .58, 1.05, R, .08); box('Cushion2__Lime', -1.3, -.8, 4.55, 4.72, .58, 1.0, R, .08)
    # the wreath round a framed picture (above the couch), lit
    torus('Wreath__Green', Vector((-1.9, 4.94, 2.1)), .55, .11, R)
    for i in range(18):
        a = i / 18 * math.tau; p = Vector((-1.9 + math.cos(a) * .55, 4.84, 2.1 + math.sin(a) * .55))
        sphere(f'WreathLight{i}__WarmLight', p, .03, R, 6, 4)
        if i % 3 == 0: sphere(f'WreathBerry{i}__Red', p + Vector((.06, 0, .05)), .04, R, 8, 6)
    box('PictureFrame__Frame', -2.2, -1.6, 4.92, 4.97, 1.75, 2.45, R); box('Picture__Picture', -2.12, -1.68, 4.9, 4.93, 1.83, 2.37, R)
    # the tree: trunk, stand, three tiers of branches, baubles, fairy lights, a star
    box('TreeStand__Red', TREE.x - .3, TREE.x + .3, TREE.y - .3, TREE.y + .3, 0, .22, R, .03)
    cone('TreeTrunk__Wood', TREE, .09, .07, .5, R, 8)
    tiers = ((.95, .45, 1.0), (.75, 1.0, .9), (.55, 1.5, .8), (.32, 1.95, .55))
    for k, (r, z, h) in enumerate(tiers): cone(f'TreeTier{k}__Green', TREE + Vector((0, 0, z)), r, .05, h, R, 14)
    cone('TreeStar__Gold', TREE + Vector((0, 0, 2.45)), .09, 0, .18, R, 5)
    rnd = random.Random(8); mats = ['Red', 'Purple', 'Silver', 'Red', 'Gold']
    for i in range(34):
        z = rnd.uniform(.55, 2.2); k = 0 if z < 1.05 else 1 if z < 1.55 else 2 if z < 1.95 else 3
        r_, base, h = tiers[k]; rr = r_ * (1 - (z - base) / h) * .95 + .04
        a = rnd.uniform(0, math.tau)
        if math.sin(a) < -.5 and abs(z - ORNAMENT_AT.z) < .25 and abs(math.cos(a)) < .5: continue   # room for the silver bauble
        p = TREE + Vector((math.cos(a) * rr, math.sin(a) * rr, z))
        sphere(f'Bauble{i}__{mats[i % len(mats)]}', p, rnd.uniform(.05, .08), R, 12, 8)
    for i in range(70):
        z = .5 + i / 70 * 1.8; k = 0 if z < 1.05 else 1 if z < 1.55 else 2 if z < 1.95 else 3
        r_, base, h = tiers[k]; rr = max(.05, r_ * (1 - (z - base) / h)) * .98
        a = i * 2.4
        sphere(f'FairyLight{i}__WarmLight', TREE + Vector((math.cos(a) * rr, math.sin(a) * rr, z)), .022, R, 6, 4)
    # presents under the tree
    box('Present1__Red', 1.7, 2.1, 2.7, 3.1, 0, .32, R, .02); box('Present2__Purple', 2.9, 3.35, 2.75, 3.1, 0, .25, R, .02)
    box('Present1Ribbon__Gold', 1.87, 1.93, 2.69, 3.11, 0, .33, R)
    # the red heart ornament (initials added by the game), on the tree's front
    H = link(bpy.data.objects.new('HeartOrnament', None), None)
    bm = bmesh.new()
    for s in (-1, 1):
        bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=.07)
    obj('HeartBody__Red', bm, H)
    sphere('HeartLobeL__Red', Vector((-.045, 0, .03)), .06, H, 12, 8, (1, .6, 1)); sphere('HeartLobeR__Red', Vector((.045, 0, .03)), .06, H, 12, 8, (1, .6, 1))
    cone('HeartTip__Red', Vector((0, 0, -.1)), .0, .085, .1, H, 12)
    H.location = TREE + Vector((-.15, -.62, .78))
    # the ornament: Dan's dark silver glass bauble, with a gold cap and loop; its origin at the hook
    O = link(bpy.data.objects.new('Ornament', None), None)
    sphere('OrnamentGlass__DarkSilver', Vector((0, 0, -.16)), .1, O, 24, 16)
    for k in range(3): torus(f'OrnamentBand{k}__Silver', Vector((0, 0, -.16 + (k - 1) * .045)), .1 * math.cos((k - 1) * .45) + .002, .006, O, 32, 6, rot_x=0)
    cone('OrnamentCap__Gold', Vector((0, 0, -.075)), .035, .03, .035, O, 12)
    torus('OrnamentLoop__Gold', Vector((0, 0, -.025)), .018, .004, O, 16, 6)
    O.location = ORNAMENT_AT
    return [R, H, O]


def export(tops):
    objs = [o for o in bpy.context.scene.objects]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = tops[0]
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, object_types={'MESH', 'EMPTY'}, apply_scale_options='FBX_SCALE_ALL',
                             add_leaf_bones=False, bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    print('HOME wrote', os.path.relpath(OUT, ROOT), sum(len(o.data.polygons) for o in objs if o.type == 'MESH'), 'faces')


def preview():
    sc = bpy.context.scene
    cols = {'Wall': (.62, .25, .62), 'Floor': (.55, .33, .2), 'Ceiling': (.8, .6, .75), 'Cream': (.95, .89, .78), 'Red': (.82, .1, .14),
            'Velvet': (.62, .06, .1), 'Lime': (.66, .82, .2), 'Green': (.1, .42, .18), 'Gold': (.95, .72, .25), 'Purple': (.55, .2, .75),
            'Silver': (.75, .77, .82), 'DarkSilver': (.28, .3, .35), 'WarmLight': (1, .82, .45), 'NightSky': (.5, .62, .85), 'Frame': (.96, .95, .92),
            'Picture': (.85, .75, .8), 'Wood': (.45, .28, .16), 'Snow': (.9, .94, 1)}
    for o in sc.objects:
        if o.type == 'MESH' and '__' in o.name: o.color = cols.get(o.name.split('__')[1].split('.')[0], (1, 0, 1)) + (1,)
    sc.render.engine = 'BLENDER_WORKBENCH'; sc.render.resolution_x = 1280; sc.render.resolution_y = 720
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'OBJECT'
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.lens = 24; cam.location = (-1.8, -1.6, 1.6)
    cam.rotation_euler = (Vector((1.2, 3.6, 1.2)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    os.makedirs(SHOTS, exist_ok=True); sc.render.filepath = os.path.join(SHOTS, 'room_preview.png'); bpy.ops.render.render(write_still=True)


tops = room()
export(tops)
preview()
