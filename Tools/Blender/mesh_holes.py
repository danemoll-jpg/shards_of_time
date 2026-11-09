"""Shards of Time, Round 4 items 1-2: find and measure see-through holes in a character model.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/mesh_holes.py -- [out=<folder under Data/screenshots>] <label>=<fbx|glb> ...

For each model (in its rest pose):
 - Mesh report: open edges (edges with one face) and how many open loops they make (the biggest listed with where they are),
   faces whose normal points the other way from their neighbours (flipped), and faces that are hidden inside a duplicate.
 - Turntable: 8 angles around it plus close-ups (head front / back / top, from below), each rendered twice in Workbench:
   back faces drawn and back faces culled (Unity's default). A pixel that is model in the first and background in the second
   is a see-through hole as the game draws it. Writes <out>/<label>_<view>.png (holes painted magenta on the culled render)
   and prints the hole pixels per view (and the share of the model's pixels).
"""
import bpy, bmesh, math, os, sys
import numpy as np
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Data', 'screenshots', 'round4', 'holes')
RES = 512
FIX = False
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


def log(*a): print('HOLES', *a, flush=True)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.endswith('.glb'): bpy.ops.import_scene.gltf(filepath=path)
    else: bpy.ops.import_scene.fbx(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    # the highest-detail mesh only (LOD0 or the one with the most faces)
    lod0 = [o for o in meshes if o.name.endswith('LOD0')]
    keep = lod0[0] if lod0 else max(meshes, key=lambda o: len(o.data.polygons))
    for o in meshes:
        if o != keep: bpy.data.objects.remove(o)
    return keep


def report(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    mw = ob.matrix_world
    open_edges = [e for e in bm.edges if len(e.link_faces) == 1]
    many = [e for e in bm.edges if len(e.link_faces) > 2]
    # open loops: connected groups of open edges
    seen = set(); loops = []
    adj = {}
    for e in open_edges:
        for v in e.verts: adj.setdefault(v.index, []).append(e)
    for e in open_edges:
        if e.index in seen: continue
        stack = [e]; group = []
        while stack:
            x = stack.pop()
            if x.index in seen: continue
            seen.add(x.index); group.append(x)
            for v in x.verts:
                for y in adj[v.index]:
                    if y.index not in seen: stack.append(y)
        pts = [mw @ v.co for x in group for v in x.verts]
        c = sum(pts, Vector()) / len(pts)
        span = max((p - c).length for p in pts)
        loops.append((len(group), c, span))
    loops.sort(key=lambda l: -l[0])
    # flipped faces: compare with the normals after a consistent recalculation
    before = [f.normal.copy() for f in bm.faces]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    flipped = sum(1 for f, n in zip(bm.faces, before) if f.normal.dot(n) < 0)
    zero = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    bm.free()
    h = max((mw @ v.co).z for v in ob.data.vertices) - min((mw @ v.co).z for v in ob.data.vertices)
    log(f'{ob.name}: {len(ob.data.vertices)} verts, {len(ob.data.polygons)} faces, height {h:.2f}; open edges {len(open_edges)} in '
        f'{len(loops)} loops; edges with 3+ faces {len(many)}; flipped faces {flipped}; zero-area faces {zero}')
    for n, c, span in loops[:8]:
        log(f'   open loop: {n} edges at ({c.x:.2f}, {c.y:.2f}, {c.z:.2f}), {span * 100:.1f} cm across')
    return h


def setup(ob, h):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x = sc.render.resolution_y = RES; sc.render.film_transparent = True
    sc.render.image_settings.color_mode = 'RGBA'; sc.view_settings.view_transform = 'Standard'
    sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'SINGLE'; sh.single_color = (.72, .72, .74)
    sh.show_cavity = False; sh.show_object_outline = False
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'
    pts = [ob.matrix_world @ v.co for v in ob.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return cam, (lo + hi) / 2, lo, hi


def shoot(cam, target, direction, scale, path_base):
    sc = bpy.context.scene
    d = direction.normalized()
    cam.location = target + d * 10; cam.data.ortho_scale = scale; cam.data.clip_end = 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    imgs = []
    for cull in (False, True):
        sc.display.shading.show_backface_culling = cull
        sc.render.filepath = path_base + ('_cull.png' if cull else '_both.png')
        bpy.ops.render.render(write_still=True)
        im = bpy.data.images.load(sc.render.filepath); a = np.array(im.pixels[:], dtype=np.float32).reshape(RES, RES, 4)
        imgs.append(a); bpy.data.images.remove(im)
    both, cull = imgs
    model = both[..., 3] > .5
    holes = model & (cull[..., 3] < .5)
    out = cull.copy(); out[..., 3] = np.where(model, 1, out[..., 3])
    out[holes] = (1, 0, 1, 1)
    im = bpy.data.images.new('holes', RES, RES, alpha=True); im.pixels = out.ravel().tolist()
    im.filepath_raw = path_base + '.png'; im.file_format = 'PNG'; im.save(); bpy.data.images.remove(im)
    os.remove(path_base + '_both.png'); os.remove(path_base + '_cull.png')
    return int(holes.sum()), int(model.sum())


def run(label, path):
    ob = load(path)
    if FIX:
        import mesh_fix; mesh_fix.double_sheets(ob, log=log)
    h = report(ob)
    cam, c, lo, hi = setup(ob, h)
    os.makedirs(OUT, exist_ok=True)
    size = max(hi - lo) * 1.15
    views = [(f'yaw{a:03d}', Vector((math.sin(math.radians(a)), -math.cos(math.radians(a)), .25)), c, size) for a in range(0, 360, 45)]
    views.append(('below', Vector((.2, -.3, -1)), c, size))
    # close-ups: the top fifth of the model (a person's head; Kira's head sits at the front, so aim at the top front)
    top = Vector((c.x, c.y, lo.z + (hi.z - lo.z) * .85))
    if hi.z - lo.z < 1: top = Vector((c.x, lo.y + (hi.y - lo.y) * .15, lo.z + (hi.z - lo.z) * .75))   # Kira: nose to -Y
    for n, d in (('head_front', Vector((0, -1, .1))), ('head_back', Vector((0, 1, .2))), ('head_top', Vector((.1, .2, 1))),
                 ('head_left', Vector((-1, 0, .1))), ('head_right', Vector((1, 0, .1)))):
        views.append((n, d, top, size * .4))
    total = 0
    for n, d, t, s in views:
        holes, model = shoot(cam, t, d, s, os.path.join(OUT, f'{label}_{n}'))
        total += holes
        log(f'   {label} {n}: {holes} hole pixels ({100 * holes / max(1, model):.2f}% of the model)')
    log(f'{label}: {total} hole pixels over {len(views)} views')


for a in sys.argv[sys.argv.index('--') + 1:]:
    if a == 'fix=1': FIX = True; continue
    if a.startswith('out='): OUT = os.path.join(ROOT, 'Data', 'screenshots', *a[4:].split('/')); continue
    lbl, p = a.split('=', 1); run(lbl, p if os.path.isabs(p) else os.path.join(ROOT, p))
