"""Shards of Time, Round 5 item 1b: rebuild a Meshy mesh as one closed surface, so no camera can ever see a back face (Round 4's
reversed twins cut the see-through spots down but didn't finish them: Meshy's fur tufts, whiskers and gaps are open sheets).

rebuild(src, voxel, tris, out_dir):
1. a solid (solid()): Meshy's surface drawn into a voxel grid, gaps up to ~4 voxels sealed (a morphological closing: thicken,
   flood-fill the outside, thin back), its outside as voxel faces; then a voxel remesh (smooth closed surface) and a light
   volume-keeping smooth. (A plain voxel remesh of Meshy's open mesh gives a thin hollow skin: Round 5 found that.)
2. decimated to `tris` triangles (collapse keeps it closed), smooth shaded,
3. new UVs (smart project), Meshy's colour and normal maps baked onto them from the original (Cycles, selected to active),
4. a material with the baked maps; the images are saved as <out_dir>/base_color.png and normal.png (the game imports those).
Returns the new object (named like the source; the source is removed). Used by kira_rig.py before the rig is built.
Also: open_edges(ob) = the number of edges with only one face (0 = closed).
"""
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def open_edges(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data)
    n = sum(1 for e in bm.edges if len(e.link_faces) < 2)
    nm = sum(1 for e in bm.edges if len(e.link_faces) > 2)
    bm.free()
    return n, nm


def solid(src, voxel, seal=2, thin=0, core=0, thin_box=None, log=print):
    """A solid copy of an open mesh: its surface drawn into a voxel grid, thickened by `seal` voxels (closes gaps up to about
    2 x seal voxels), the outside flood-filled from the grid's border, everything not reached = solid, eroded back by `seal`
    (a morphological closing). Returns a new mesh object: the solid's voxel faces (closed), for a voxel remesh to smooth."""
    me = src.data; mw = src.matrix_world
    V = np.array([tuple(mw @ v.co) for v in me.vertices])
    tris = [t for p in me.polygons for t in [(p.vertices[0], p.vertices[i], p.vertices[i + 1]) for i in range(1, len(p.vertices) - 1)]]
    T = np.array(tris)
    pad = (seal + 3) * voxel; lo = V.min(0) - pad; dims = np.ceil((V.max(0) + pad - lo) / voxel).astype(int) + 1
    wall = np.zeros(dims, bool)
    a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
    longest = np.maximum(np.maximum(np.linalg.norm(b - a, axis=1), np.linalg.norm(c - b, axis=1)), np.linalg.norm(a - c, axis=1))
    for n in np.unique(np.clip(np.ceil(longest / (voxel * .5)).astype(int), 1, 400)):
        sel = np.where(np.clip(np.ceil(longest / (voxel * .5)).astype(int), 1, 400) == n)[0]
        i, j = np.meshgrid(np.arange(n + 1), np.arange(n + 1)); m = (i + j) <= n; u = (i[m] / n)[:, None, None]; w = (j[m] / n)[:, None, None]
        pts = a[sel][None] * (1 - u - w) + b[sel][None] * u + c[sel][None] * w
        idx = np.floor((pts.reshape(-1, 3) - lo) / voxel).astype(int)
        wall[idx[:, 0], idx[:, 1], idx[:, 2]] = True
    def grow(x, k):
        for _ in range(k):
            y = x.copy()
            for ax in range(3):
                y[tuple(slice(1, None) if d == ax else slice(None) for d in range(3))] |= x[tuple(slice(None, -1) if d == ax else slice(None) for d in range(3))]
                y[tuple(slice(None, -1) if d == ax else slice(None) for d in range(3))] |= x[tuple(slice(1, None) if d == ax else slice(None) for d in range(3))]
            x = y
        return x
    def outside(walls):
        out = np.zeros(dims, bool); out[0, :, :] = out[-1, :, :] = out[:, 0, :] = out[:, -1, :] = out[:, :, 0] = out[:, :, -1] = True
        out &= ~walls; it = 0
        while True:
            nxt = grow(out, 1) & ~walls; it += 1
            if (nxt == out).all(): return out, it
            out = nxt
    out, it = outside(grow(wall, seal))
    sol = ~grow(out, seal)   # eroding the solid = growing the outside back by `seal`
    if core:
        # a body with a hole into it (Abi: the strip Round 2 cut between hair and shoulder) floods hollow above; its thick core
        # comes from a closing with a big seal, opened by the same amount so the bridges that seal makes (hair to sleeve) go
        out_b, _ = outside(grow(wall, core))
        clos = ~grow(out_b, core)
        sol |= grow(~grow(~clos, core), core)
    if thin_box:   # Round 8: a stronger opening inside a box only (lo, hi, k): dragon Kira's whisker roots lie along her cheeks
        blo, bhi, k = thin_box
        a = np.clip(np.floor((np.array(blo) - lo) / voxel).astype(int), 0, dims - 1); b = np.clip(np.ceil((np.array(bhi) - lo) / voxel).astype(int), 0, dims - 1)
        op = grow(~grow(~sol, k), k); box = (slice(a[0], b[0] + 1), slice(a[1], b[1] + 1), slice(a[2], b[2] + 1))
        sol = sol.copy(); sol[box] = op[box]
    if thin:   # an opening: strands thinner than ~2 x `thin` voxels go (home Kira's whiskers), the rest comes back to size
        sol = grow(~grow(~sol, thin), thin)
    log(f'solid: grid {tuple(dims)}, {int(wall.sum())} surface voxels, outside filled in {it} steps, {int(sol.sum())} solid voxels '
        f'({sol.sum() * voxel ** 3 * 1e6:.0f} cm3)')
    # the solid's boundary as quads (closed): a face wherever solid meets not-solid
    bm = bmesh.new(); vmap = {}
    def vert(p):
        v = vmap.get(p)
        if v is None: v = vmap[p] = bm.verts.new(Vector(lo + np.array(p) * voxel))
        return v
    pad1 = np.pad(sol, 1)
    for ax in range(3):
        d = np.diff(pad1.astype(np.int8), axis=ax)
        for sign in (1, -1):
            for q in np.argwhere(d == sign):
                q = q.copy(); q -= 1; q[ax] += 1   # the corner where the face sits (in grid units)
                e1, e2 = [k for k in range(3) if k != ax]
                c0 = tuple(q); c1 = list(q); c1[e1] += 1; c2 = list(q); c2[e1] += 1; c2[e2] += 1; c3 = list(q); c3[e2] += 1
                vs = [vert(c0), vert(tuple(c1)), vert(tuple(c2)), vert(tuple(c3))]
                if (sign == -1) != (ax == 1): vs.reverse()
                try: bm.faces.new(vs)
                except ValueError: pass
    me2 = bpy.data.meshes.new(src.name + '_solid'); bm.to_mesh(me2); bm.free()
    ob = bpy.data.objects.new(src.name + '_solid', me2); bpy.context.scene.collection.objects.link(ob)
    return ob


def keep_biggest(ob, log=print):
    """Only the biggest connected piece stays (bits the remesh left floating, e.g. the ends of home Kira's whiskers)."""
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.faces.ensure_lookup_table(); seen = set(); pieces = []
    for f in bm.faces:
        if f.index in seen: continue
        stack = [f]; piece = []; seen.add(f.index)
        while stack:
            g = stack.pop(); piece.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); stack.append(h)
        pieces.append(piece)
    pieces.sort(key=len, reverse=True)
    drop = [f for p in pieces[1:] for f in p]
    if drop:
        bmesh.ops.delete(bm, geom=drop, context='FACES')
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(ob.data); bm.free()
    log(f'kept the biggest piece; {len(pieces) - 1} floating bits ({len(drop)} faces) removed')


def _select_only(*obs, active=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = active or obs[-1]


def rebuild(src, voxel, tris, out_dir, res=2048, thin=0, seal=2, core=0, reach=0.0, keep_src=False, solid_src=None, extra=None, bake_also=(), thin_box=None, log=print):
    """Round 8: solid_src = the mesh the solid is made from (default src; dragon Kira's body without her wings), extra = an object
    joined in before the UVs and the bake (her wings as a thin two-sided sheet), so it shares the baked maps; bake_also = more bake
    sources (the caps over her wing roots)."""
    name = src.name
    new = solid(solid_src or src, voxel, seal=seal, thin=thin, core=core, thin_box=thin_box, log=log); new.name = name + '_closed'
    _select_only(new)
    r = new.modifiers.new('remesh', 'REMESH'); r.mode = 'VOXEL'; r.voxel_size = voxel; r.adaptivity = 0
    bpy.ops.object.modifier_apply(modifier='remesh')
    keep_biggest(new, log)
    sm = new.modifiers.new('smooth', 'LAPLACIANSMOOTH'); sm.iterations = 4; sm.lambda_factor = .5; sm.use_volume_preserve = True
    bpy.ops.object.modifier_apply(modifier='smooth')
    have = sum(len(p.vertices) - 2 for p in new.data.polygons)
    log(f'closed mesh {name}: voxel {voxel * 1000:.1f} mm -> {len(new.data.vertices)} vertices; open edges {open_edges(new)}')
    if have > tris:
        d = new.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = tris / have; d.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier='dec')
    for p in new.data.polygons: p.use_smooth = True
    bm = bmesh.new(); bm.from_mesh(new.data); bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.to_mesh(new.data); bm.free()   # all outward
    oe, nm = open_edges(new)
    bm = bmesh.new(); bm.from_mesh(new.data); bm.faces.ensure_lookup_table(); tree = BVHTree.FromBMesh(bm); cross = set()
    for i, j in tree.overlap(tree):
        if not (set(v.index for v in bm.faces[i].verts) & set(v.index for v in bm.faces[j].verts)): cross.update((i, j))
    bm.free()
    log(f'closed mesh {name}: {sum(len(p.vertices) - 2 for p in new.data.polygons)} triangles, {oe} open edges, {nm} edges with 3+ faces, '
        f'{len(cross)} faces crossing another')
    if extra is not None:   # Round 8: joined after the closed body's checks (a sheet is open by design)
        extra.data.materials.clear(); ename = extra.name
        _select_only(extra, new, active=new); bpy.ops.object.join()
        for p in new.data.polygons: p.material_index = 0
        log(f'closed mesh {name}: joined {ename} -> {sum(len(p.vertices) - 2 for p in new.data.polygons)} triangles')
    # UVs
    while new.data.uv_layers: new.data.uv_layers.remove(new.data.uv_layers[0])
    new.data.uv_layers.new(name='UVMap')
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=.004)
    bpy.ops.object.mode_set(mode='OBJECT')
    # the baked material
    mat = bpy.data.materials.new(name + '_closed'); mat.use_nodes = True
    nt = mat.node_tree; bsdf = nt.nodes['Principled BSDF']
    imgs = {}
    for kind, cs in (('base_color', 'sRGB'), ('normal', 'Non-Color')):
        img = bpy.data.images.new(f'{name}_{kind}', res, res, alpha=False)
        img.colorspace_settings.name = cs
        node = nt.nodes.new('ShaderNodeTexImage'); node.image = img; node.name = kind
        imgs[kind] = (img, node)
    nt.links.new(imgs['base_color'][1].outputs['Color'], bsdf.inputs['Base Color'])
    nm_node = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(imgs['normal'][1].outputs['Color'], nm_node.inputs['Color'])
    nt.links.new(nm_node.outputs['Normal'], bsdf.inputs['Normal'])
    bsdf.inputs['Roughness'].default_value = .8
    new.data.materials.clear(); new.data.materials.append(mat)
    # bake from the original
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = 4
    b = sc.render.bake; b.use_selected_to_active = True; b.cage_extrusion = voxel * 3; b.max_ray_distance = max(voxel * 10, reach);   # reach: filled parts far from Meshy's surface
    b.margin = 8
    os.makedirs(out_dir, exist_ok=True)
    for kind in ('base_color', 'normal'):
        img, node = imgs[kind]
        for n in nt.nodes: n.select = False
        node.select = True; nt.nodes.active = node
        _select_only(src, *bake_also, new, active=new)
        if kind == 'base_color': bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
        else: bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT')
        img.filepath_raw = os.path.join(out_dir, kind + '.png'); img.file_format = 'PNG'; img.save()
        img.pack()
    log(f'closed mesh {name}: baked base_color + normal ({res} px) -> {os.path.relpath(out_dir)}')
    if keep_src: return new
    bpy.data.objects.remove(src)
    new.name = name; new.data.name = name
    return new
