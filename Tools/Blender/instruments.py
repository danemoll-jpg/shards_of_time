"""Shards of Time, Round 6 item 4: the Violinist's instruments, by script (one per world: the Ice Age's bone fiddle and bow, Egypt's lyre).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/instruments.py

Writes Assets/Resources/World/instruments.fbx: one empty per instrument (Fiddle, Bow, Lyre), its parts named <part>__<Material>
(the game colours each part by the word after "__": World/WorldArt.cs), plus marker empties the game uses to hold it, whatever the
axes come out as after the FBX trip:
  Fiddle (and SteamViolin, Round 11: the same fiddle with brass gears set into its body, the same markers):
          M_Chin (the chin rest end), M_Scroll (the far end of the neck), M_Up (above the strings), M_Grip (where the left hand holds
          the neck), M_Bow (where the bow crosses the strings)
  Bow:    M_Frog (the right hand), M_Tip
  Lyre:   M_Base (the bottom of the sound box), M_Top (the middle of the yoke), M_Front (in front of the strings), M_Hold (the left
          hand on the left arm), M_Pluck (the middle of the strings)
And Data/screenshots/round6/instruments/instruments.png, a preview. Sizes are real (a fiddle 0.6 m long; the game may scale).
Blender X = Unity x, Y = Unity z, Z = up (as iceage_world.py).
"""
import bpy, bmesh, math, os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round11', 'instruments')
COL = {'Bone': (.93, .88, .76), 'Ebony': (.12, .09, .08), 'Gut': (.95, .92, .8), 'Wood': (.45, .28, .16), 'Gold': (.95, .72, .25),
       'Lapis': (.12, .25, .7), 'Bark': (.42, .28, .18), 'Hair': (.97, .96, .92), 'Plank': (.55, .36, .2), 'Dark': (.15, .12, .1),
       'Brass': (.86, .66, .3), 'Copper': (.78, .45, .28)}


def mat(name):
    m = bpy.data.materials.get(name)
    if not m:
        m = bpy.data.materials.new(name); m.diffuse_color = (*COL.get(name, (1, 0, 1)), 1)
    return m


def mesh_obj(name, bm, parent):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob); ob.parent = parent
    ob.data.materials.append(mat(name.split('__')[1]))
    for p in ob.data.polygons: p.use_smooth = True
    return ob


def empty(name, parent=None, at=(0, 0, 0)):
    e = bpy.data.objects.new(name, None); bpy.context.scene.collection.objects.link(e); e.parent = parent; e.location = at
    e.empty_display_size = .03
    return e


def prism(name, outline, z0, z1, parent, bevel=0.0):
    """an outline (list of (x, y), counter-clockwise) extruded from z0 to z1"""
    bm = bmesh.new()
    vs = [bm.verts.new((x, y, z0)) for x, y in outline]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in r['geom']:
        if isinstance(v, bmesh.types.BMVert): v.co.z = z1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    ob = mesh_obj(name, bm, parent)
    if bevel:
        m = ob.modifiers.new('bevel', 'BEVEL'); m.width = bevel; m.segments = 2
    return ob


def box(name, c, s, parent, rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = Vector((v.co.x * s[0], v.co.y * s[1], v.co.z * s[2]))
    ob = mesh_obj(name, bm, parent); ob.location = c; ob.rotation_euler = rot
    for p in ob.data.polygons: p.use_smooth = False
    return ob


def cyl(name, a, b, r0, r1, parent, n=10):
    """a tapered rod from point a to point b"""
    a, b = Vector(a), Vector(b); d = b - a
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=n, radius1=r0, radius2=r1, depth=d.length)
    ob = mesh_obj(name, bm, parent)
    ob.location = (a + b) / 2; ob.rotation_mode = 'QUATERNION'; ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    return ob


def ball(name, c, r, parent, scale=(1, 1, 1)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=r)
    for v in bm.verts: v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    ob = mesh_obj(name, bm, parent); ob.location = c
    return ob


# ---------------------------------------------------------------------------------------------------------------- the fiddle
def fiddle(name='Fiddle', gears=False, chrome=False):
    e = empty(name)
    W = 'Chrome' if chrome else 'Wood' if gears else 'Bone'   # Round 12: Tomorrowland's chrome electric violin   # the steam violin: varnished wood, the Ice Age's: bone
    L = .355   # the body, chin end at y 0
    def half(y):   # the body's half width along its length: two bouts and a waist (a violin's figure eight)
        lo = .105 * math.sqrt(max(0, 1 - ((y - .1) / .1) ** 2))
        up = .085 * math.sqrt(max(0, 1 - ((y - .27) / .086) ** 2))
        w = max(lo, up)
        if .17 < y < .215: w = max(w, .058)
        return max(w, .004)
    ys = [L * i / 48 for i in range(49)]
    right = [(half(y), y) for y in ys]; left = [(-half(y), y) for y in reversed(ys)]
    outline = right + left
    prism('Body__' + W, outline, 0, .042, e, bevel=.006)
    # the top plate's arch, the f-holes, the bridge, the tailpiece, the chin rest
    inner = [(x * .9, .018 + y * .9) for x, y in outline]
    prism('Top__' + W, inner, .042, .052, e, bevel=.004)
    for s in (-1, 1):
        box('FHole__Ebony', (s * .045, .19, .058), (.008, .07, .006), e, rot=(0, 0, math.radians(s * 8)))
        ball('FHoleDot__Ebony', (s * .05, .225, .058), .008, e, (1, 1, .4))
        ball('FHoleDot__Ebony', (s * .04, .155, .058), .008, e, (1, 1, .4))
    box('Bridge__Wood', (0, .15, .066), (.075, .006, .03), e)
    prism('Tailpiece__Ebony', [(-.012, .025), (.012, .025), (.022, .1), (-.022, .1)], .052, .062, e)
    ball('ChinRest__Ebony', (-.045, .03, .058), .035, e, (1.2, .9, .3))
    # the neck, the fingerboard, the peg box, the scroll and the pegs
    box('Neck__' + W, (0, L + .065, .045), (.03, .14, .03), e)
    prism('Fingerboard__Ebony', [(-.014, .2), (.014, .2), (.021, L + .135), (-.021, L + .135)], .06, .07, e)
    box('PegBox__' + W, (0, L + .165, .05), (.032, .06, .04), e)
    ball('Scroll__' + W, (0, L + .205, .058), .022, e, (.9, 1, 1))
    ball('ScrollEye__Ebony', (.017, L + .205, .06), .008, e, (.5, 1, 1))
    ball('ScrollEye__Ebony', (-.017, L + .205, .06), .008, e, (.5, 1, 1))
    for i, y in enumerate((L + .145, L + .165, L + .155, L + .175)):
        s = -1 if i % 2 else 1
        cyl('Peg__Ebony', (s * .015, y, .05), (s * .05, y, .05), .006, .009, e, 6)
    # the strings: tailpiece -> over the bridge -> the peg box
    for i, x in enumerate((-.012, -.004, .004, .012)):
        cyl('String__Gut', (x * .9, .1, .064), (x * 1.0, .15, .082), .0012, .0012, e, 4)
        cyl('String__Gut', (x * 1.0, .15, .082), (x * .5, L + .14, .073), .0012, .0012, e, 4)
    if gears:   # Round 11: the Victorian Steampunk violin: small brass gears set into the top plate (clear of the strings and the bridge)
        for (x, y, r, n) in ((.062, .085, .024, 10), (-.045, .285, .018, 8), (.042, .3, .014, 7), (-.07, .12, .015, 7)):
            gear_flat('Gear__Brass', (x, y, .052), r, .007, e, n)
        box('Plate__Copper', (0, -.003, .024), (.05, .008, .026), e)   # the end button's plate
        ball('Pin__Brass', (0, L + .205, .084), .008, e)
    if chrome:   # Round 12: teal light strips down both edges and across the lower bout, a glowing tuning dot on the scroll
        for s in (-1, 1):
            box('Strip__NeonTeal', (s * .088, .1, .047), (.006, .11, .006), e, rot=(0, 0, math.radians(-s * 10)))
            box('Strip__NeonTeal', (s * .07, .28, .047), (.006, .08, .006), e, rot=(0, 0, math.radians(s * 8)))
        box('Strip__NeonTeal', (0, .03, .056), (.07, .006, .004), e)
        ball('Dot__NeonPink', (0, L + .205, .084), .009, e)
    empty('M_Chin', e, (-.045, .02, .04)); empty('M_Scroll', e, (0, L + .21, .05)); empty('M_Up', e, (0, .15, .2))
    empty('M_Grip', e, (0, L + .1, .04)); empty('M_Bow', e, (0, .2, .08))
    return e


def gear_flat(name, c, r, thick, parent, teeth):
    """a small gear lying on the fiddle's top (its bottom at c.z): a disc, teeth, a hub"""
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=r, radius2=r, depth=thick)
    ob = mesh_obj(name, bm, parent); ob.location = (c[0], c[1], c[2] + thick / 2)
    for k in range(teeth):
        a = (k + .5) / teeth * math.tau
        box(name, (c[0] + math.cos(a) * (r + r * .12), c[1] + math.sin(a) * (r + r * .12), c[2] + thick / 2), (r * .3, r * .32, thick), parent, (0, 0, a))
    cyl(name, (c[0], c[1], c[2]), (c[0], c[1], c[2] + thick * 1.8), r * .3, r * .3, parent, 8)


def bow():
    e = empty('Bow')
    cyl('Stick__Wood', (0, 0, .02), (0, .74, .015), .006, .0035, e, 8)
    box('Frog__Ebony', (0, .04, 0), (.012, .05, .022), e)
    box('Tip__Bone', (0, .735, .01), (.008, .014, .02), e)
    box('Hair__Hair', (0, .39, .0), (.009, .69, .002), e)
    empty('M_Frog', e, (0, .04, 0)); empty('M_Tip', e, (0, .74, 0))
    return e


def lyre():
    e = empty('Lyre')
    # the sound box (a rounded box) with a lapis band, then two arms curving out and up to the yoke; strings box -> yoke
    box('SoundBox__Wood', (0, 0, .07), (.3, .08, .14), e)
    m = e.children[-1].modifiers.new('bevel', 'BEVEL'); m.width = .02; m.segments = 3
    box('Band__Lapis', (0, -.041, .07), (.3, .004, .04), e)
    for s in (-1, 1):
        pts = [(s * (.12 + .06 * math.sin(t / 6 * math.pi)), 0, .13 + .45 * t / 6) for t in range(7)]
        for a, b in zip(pts, pts[1:]): cyl('Arm__Wood', a, b, .016, .016, e, 8); ball('Joint__Wood', b, .016, e)
        ball('ArmTip__Gold', (s * .12, 0, .59), .022, e)
    cyl('Yoke__Gold', (-.17, 0, .55), (.17, 0, .55), .013, .013, e, 8)
    for i in range(7):
        x = -.075 + i * .025
        cyl('String__Gut', (x, -.03, .1), (x * 1.15, -.0, .55), .0012, .0012, e, 4)
    box('Bridge__Gold', (0, -.04, .12), (.2, .008, .015), e)
    empty('M_Base', e, (0, 0, 0)); empty('M_Top', e, (0, 0, .55)); empty('M_Front', e, (0, -.2, .3))
    empty('M_Hold', e, (-.16, 0, .35)); empty('M_Pluck', e, (0, -.03, .3))
    return e


def harp():
    """Round 7, item 2: Bethlehem's small shepherd's harp (0.55 m), held like the lyre (the same markers): a straight pillar on the
    left, a slanted sound box on the right, a curved neck over the top, seven strings straight down from the neck to the box."""
    e = empty('Harp')
    cyl('Pillar__Wood', (-.15, 0, .02), (-.15, 0, .52), .018, .016, e, 8)
    cyl('SoundBox__Wood', (-.13, 0, .03), (.14, 0, .44), .05, .028, e, 10)
    pts = [(-.15 + .29 * t / 6, 0, .52 + .045 * math.sin(t / 6 * math.pi * 1.3) - .06 * t / 6) for t in range(7)]
    for a, b in zip(pts, pts[1:]): cyl('Neck__Wood', a, b, .02, .02, e, 8); ball('Joint__Wood', b, .02, e)
    ball('Knob__Gold', (-.15, 0, .54), .022, e); ball('Foot__Gold', (-.14, 0, .02), .02, e)
    for i in range(7):
        x = -.1 + i * .034
        top = .52 + .045 * math.sin((x + .15) / .29 * math.pi * 1.3) - .06 * (x + .15) / .29 - .02
        bot = .03 + (x + .13) / .27 * .41 + .03
        cyl('String__Gut', (x, -.005, bot), (x, -.005, top), .0012, .0012, e, 4)
    empty('M_Base', e, (0, 0, 0)); empty('M_Top', e, (0, 0, .55)); empty('M_Front', e, (0, -.2, .3))
    empty('M_Hold', e, (-.16, 0, .35)); empty('M_Pluck', e, (0, -.01, .3))
    return e


def lute():
    """Round 8, item 2: the Medieval bard's lute (0.8 m), held across his body (World/Violinist.cs: the lyre's markers, its own hold): a
    pear-shaped bowl back, a flat soundboard with a carved rose, a fretted neck and the pegbox bent back. Its front (the strings) is -Y,
    the neck up +Z from the body's bottom."""
    e = empty('Lute')
    ball('Bowl__Wood', (0, .1, .17), .17, e, (1.0, .55, 1.25))
    ball('Board__Plank', (0, -.005, .17), .165, e, (1.0, .06, 1.12))
    ball('Rose__Dark', (0, -.02, .2), .045, e, (1.0, .15, 1.0))
    box('Bridge__Ebony', (0, -.022, .08), (.12, .012, .015), e)
    box('Neck__Ebony', (0, -.0, .47), (.055, .03, .3), e)
    for i in range(6): box('Fret__Gold', (0, -.017, .36 + i * .05), (.058, .004, .004), e)
    box('Pegbox__Wood', (0, .03, .66), (.05, .03, .1), e, (math.radians(-35), 0, 0))
    for i in range(4): cyl('Peg__Gold', (-.04, .04 + i * .01, .63 + i * .025), (.04, .04 + i * .01, .63 + i * .025), .006, .006, e, 6)
    for i in range(6):
        x = -.02 + i * .008
        cyl('String__Gut', (x * 1.6, -.024, .08), (x, -.018, .62), .0011, .0011, e, 4)
    empty('M_Base', e, (0, 0, 0)); empty('M_Top', e, (0, 0, .7)); empty('M_Front', e, (0, -.2, .2))
    empty('M_Hold', e, (0, .0, .5)); empty('M_Pluck', e, (0, -.03, .17))
    return e


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    roots = [fiddle(), bow(), lyre(), harp(), lute(), fiddle('SteamViolin', gears=True), fiddle('ChromeViolin', chrome=True)]
    roots[1].location = (.3, 0, 0); roots[2].location = (.75, 0, 0); roots[3].location = (1.2, 0, 0); roots[4].location = (1.65, 0, 0)
    roots[5].location = (2.05, 0, 0)
    bpy.context.view_layer.update()
    for o in bpy.context.scene.objects:   # bake the bevels
        if o.type == 'MESH' and o.modifiers:
            bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
            for m in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=m.name)
    # preview
    os.makedirs(SHOTS, exist_ok=True)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'MATERIAL'
    sc.display.shading.light = 'STUDIO'; sc.render.resolution_x = 1400; sc.render.resolution_y = 700
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.location = (1.1, -2.7, 1.5); cam.rotation_euler = (math.radians(62), 0, 0); cam.data.lens = 35
    sc.render.filepath = os.path.join(SHOTS, 'instruments.png'); bpy.ops.render.render(write_still=True)
    for r in roots: r.location = (0, 0, 0)
    bpy.context.view_layer.update()
    bpy.data.objects.remove(cam)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'instruments.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, path_mode='STRIP')
    print('INSTRUMENTS written', len([o for o in bpy.context.scene.objects if o.type == 'MESH']), 'parts')


main()
