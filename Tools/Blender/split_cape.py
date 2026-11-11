"""Shards of Time, Round 8: split a cape off a cleaned Meshy person, so Mixamo rigs the body alone (Dan: the Violinist's long
cape wouldn't rig cleanly). The cape goes back on after Mixamo with Tools/Blender/attach_cape.py.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/split_cape.py -- violinist_medieval [debug]

Reads SourceArt/Meshy/<model>/<model>.blend (from meshy_cleanup.py). The cape's faces are picked by their texture colour (the
cape's brown, RULES below) and kept only where they make one big connected piece (so boots, belt and bracers of the same brown
stay on the body); the hole the cape leaves in the body (Meshy modelled no back under it) is closed with a plain cap.
Writes:
  SourceArt/Mixamo/<model>/upload/<model>_nocape.fbx     the body alone, for Mixamo
  SourceArt/Meshy/<model>/<model>_split.blend           objects "body" and "cape" (same space as the cleaned model)
  Data/screenshots/round8/cape/<model>_{pick,body,cape}_{front,side,back}.png
With "debug" it only renders the pick (red) and writes nothing else.
"""
import bpy, bmesh, colorsys, math, os, sys
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SHOTS = os.path.join(ROOT, 'Data', 'screenshots', 'round8', 'cape')
# per model: hue range (0..1), saturation range, value range, lowest z (m) of the cape, smallest piece kept (share of the picked faces)
# front: the torso's front (|x| < front[0], in front of the body's middle, below z front[1]) never belongs to the cape (belt, trim)
RULES = {'violinist_medieval': dict(h=(0.0, 0.14), s=(0.12, 0.85), v=(0.0, 0.62), zmin=0.2, piece=0.04, front=(0.21, 1.30), legs=(0.5, 0.26, 0.12))}
# legs: below z legs[0], faces with |x| < legs[1] and less than legs[2] behind the body's middle are legs / boots, never cape


def log(*a): print('CAPE', *a, flush=True)


def image_of(ob):
    for m in ob.data.materials:
        for l in (m.node_tree.links if m and m.use_nodes else []):
            if l.to_socket.name == 'Base Color' and l.from_node.type == 'TEX_IMAGE':
                return l.from_node.image
    raise SystemExit('no base colour image')


def pick(ob, r):
    img = image_of(ob); w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    me = ob.data; uv = me.uv_layers.active.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    ymid = sum(v.co.y for v in me.vertices) / len(me.vertices)
    def leg(cc): return cc.z < r['legs'][0] and abs(cc.x) < r['legs'][1] and cc.y < ymid + r['legs'][2]
    cand = set()
    for f in bm.faces:
        p = me.polygons[f.index]
        u = sum(uv[i].uv.x for i in p.loop_indices) / p.loop_total; v = sum(uv[i].uv.y for i in p.loop_indices) / p.loop_total
        c = px[min(h - 1, max(0, int((v % 1) * h))), min(w - 1, max(0, int((u % 1) * w)))]
        hh, ss, vv = colorsys.rgb_to_hsv(*map(float, c[:3]))
        cc = f.calc_center_median()
        if abs(cc.x) < r['front'][0] and cc.y < ymid and cc.z < r['front'][1]: continue
        if leg(cc): continue
        if cc.z >= r['zmin'] and r['h'][0] <= hh <= r['h'][1] and r['s'][0] <= ss <= r['s'][1] and r['v'][0] <= vv <= r['v'][1]:
            cand.add(f.index)
    # connected pieces of the picked faces (through shared edges)
    seen, pieces = set(), []
    for i in cand:
        if i in seen: continue
        stack, piece = [i], []
        seen.add(i)
        while stack:
            f = bm.faces[stack.pop()]; piece.append(f.index)
            for e in f.edges:
                for g in e.link_faces:
                    if g.index in cand and g.index not in seen: seen.add(g.index); stack.append(g.index)
        pieces.append(piece)
    pieces.sort(key=len, reverse=True)
    keep = set(i for p in pieces if len(p) >= r['piece'] * len(cand) for i in p)
    # fill: unpicked faces fully surrounded by the cape (texture specks, seams) join it; twice, for 2-face specks
    for _ in range(6):
        add = [f.index for f in bm.faces if f.index not in keep and f.calc_center_median().z >= r['zmin'] and not leg(f.calc_center_median()) and
               sum(1 for e in f.edges for g in e.link_faces if g is not f and g.index in keep) >= len(f.edges) - 1]
        keep.update(add)
    log(f'{len(cand)} brown faces, {len(pieces)} pieces (largest {[len(p) for p in pieces[:5]]}), cape {len(keep)} of {len(bm.faces)}')
    bm.free()
    return keep


def setup_render(h):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 600, 800
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = h * 1.15
    return cam


def shoot(cam, h, tag):
    os.makedirs(SHOTS, exist_ok=True)
    for view, ang in (('front', 0), ('side', 90), ('back', 180)):
        a = math.radians(ang)
        cam.location = (10 * math.sin(a), -10 * math.cos(a), h / 2); cam.rotation_euler = (math.radians(90), 0, a)
        bpy.context.scene.render.filepath = os.path.join(SHOTS, f'{tag}_{view}.png')
        bpy.ops.render.render(write_still=True)


def main(model, debug):
    r = RULES[model]
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', model, model + '.blend'))
    ob = bpy.data.objects[model]
    zs = [v.co.z for v in ob.data.vertices]; h = max(zs) - min(zs)
    # Meshy's mesh is split at every texture seam: weld it so faces connect (UVs are per corner, they stay)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=1e-5); bpy.ops.object.mode_set(mode='OBJECT')
    keep = pick(ob, r)
    cam = setup_render(h)
    # the pick in red
    red = bpy.data.materials.new('pick'); red.use_nodes = True
    red.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.9, .05, .05, 1)
    ob.data.materials.append(red); ri = len(ob.data.materials) - 1
    old = [p.material_index for p in ob.data.polygons]
    for p in ob.data.polygons:
        if p.index in keep: p.material_index = ri
    shoot(cam, h, f'{model}_pick')
    for p, m in zip(ob.data.polygons, old): p.material_index = m
    ob.data.materials.pop(index=ri)
    if debug: return
    # split: the cape becomes its own object
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
    for p in ob.data.polygons: p.select = p.index in keep
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
    cape = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o is not ob][0]
    ob.name = 'body'; cape.name = 'cape'; cape.data.name = 'cape'
    # loose bits left on the body (single faces cut off by the split) go away; then the body's holes are capped
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.delete_loose(use_verts=True, use_edges=True, use_faces=False)
    bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.mesh.select_non_manifold(extend=False, use_wire=False, use_boundary=True,
                                                                                 use_multi_face=False, use_non_contiguous=False, use_verts=False)
    bpy.ops.mesh.fill_holes(sides=0)
    bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.quads_convert_to_tris(quad_method='BEAUTY', ngon_method='BEAUTY')
    bpy.ops.object.mode_set(mode='OBJECT')
    bm = bmesh.new(); bm.from_mesh(ob.data); openings = sum(1 for e in bm.edges if e.is_boundary); bm.free()
    tri = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)
    log(f'{model}: body {tri(ob)} triangles ({openings} open edges left), cape {tri(cape)} triangles')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', model, model + '_split.blend'))
    up = os.path.join(ROOT, 'SourceArt', 'Mixamo', model, 'upload'); os.makedirs(up, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(up, model + '_nocape.fbx'), use_selection=True, object_types={'MESH'},
                             path_mode='COPY', embed_textures=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False)
    cape.hide_render = True; shoot(cam, h, f'{model}_body')
    cape.hide_render = False; ob.hide_render = True; shoot(cam, h, f'{model}_cape')
    log(f'wrote {os.path.relpath(os.path.join(up, model + "_nocape.fbx"), ROOT)}')


args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
main(args[0] if args else 'violinist_medieval', 'debug' in args)
