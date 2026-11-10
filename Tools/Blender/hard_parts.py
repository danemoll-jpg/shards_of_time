"""Shards of Time, Round 6 item 3: the hard parts of a furry Meshy model (the mammoth's tusks, sabertooth Kira's fangs, the wolf's
claws) get clean geometry and a plain material of their own, so no fur is baked onto them and no fur pokes through them.

Used by kira_rig.py (Round 5's closed rebuild voxelized tusks and fur together and baked the fur texture across both):
  split(ob, name, L)  before the closed rebuild: the faces of Meshy's mesh that are hard (by their colour in Meshy's texture and where
                      they sit: RULES) are taken out of `ob` into a new object (None if the model has none)
  close(hard, L, out_dir, name)  that object as clean closed pieces (voxel solid at a fine size, remeshed, smoothed, decimated), a
                      UV layer, one plain material; writes <out_dir>/hard.txt ("r g b", sRGB 0-1: the game's colour for submesh 1)
  rigid(ob, first, L)  after skinning: every vertex from index `first` on (the hard pieces, joined after the body) follows one bone
                      only: the head (tusks, fangs, teeth) or the nearest paw (claws)
"""
import bpy, bmesh, colorsys, math, os
from mathutils import Vector

IVORY = (.93, .88, .76)
# what is hard, per model: a colour test on Meshy's texel (h, s, v) and a place test (p = position, L = landmarks), the colour to use
# (None = the parts' own mean colour from Meshy's texture), and which bone they follow
RULES = {
    'mammoth': [dict(part='tusks', col=lambda h, s, v: v > .5 and s < .42, at=lambda p, L: p.y < L['yf'] - .01 and L['H'] * .3 < p.z < L['H'] * .78,
                     colour=IVORY, bone='Head')],
    'kira_sabertooth': [dict(part='fangs', col=lambda h, s, v: v > .65 and s < .32, at=lambda p, L: p.y < L['yf'] and L['H'] * .4 < p.z < L['H'] * .62,
                             colour=IVORY, bone='Head')],
    'wolf': [dict(part='claws', col=lambda h, s, v: v < .48 and s > .28, at=lambda p, L: p.z < L['H'] * .035 and min((p.xy - L['paws'][k].xy).length for k in L['paws']) < L['H'] * .12,
                  colour=None, bone='Paw')],
}


def _texture(ob):
    for m in ob.data.materials:
        for n in (m.node_tree.nodes if m and m.use_nodes else []):
            if n.type == 'TEX_IMAGE' and n.image: return n.image
    return None


def split(ob, name, L, log=print):
    rules = RULES.get(name)
    img = _texture(ob)
    if not rules or not img: return None, None
    W, H = img.size; px = img.pixels[:]
    me = ob.data; uv = me.uv_layers.active.data; mw = ob.matrix_world
    pick = set(); cols = []
    for p in me.polygons:
        u = sum((uv[i].uv for i in p.loop_indices), Vector((0, 0))) / len(p.loop_indices)
        x = min(W - 1, int(u.x % 1 * W)); y = min(H - 1, int(u.y % 1 * H)); i = (y * W + x) * 4
        r, g, b = px[i:i + 3]; h, s, v = colorsys.rgb_to_hsv(r, g, b)
        c = mw @ p.center
        for rule in rules:
            if rule['col'](h, s, v) and rule['at'](c, L): pick.add(p.index); cols.append((r, g, b)); break
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    # one ring more (their edges), then only pieces of a useful size (stray texels on the fur go back)
    sel = set(pick)
    for f in [bm.faces[i] for i in pick]:
        for e in f.edges:
            for g in e.link_faces: sel.add(g.index)
    seen = set(); keep = set()
    for i in sel:
        if i in seen: continue
        stack = [i]; piece = []; seen.add(i)
        while stack:
            j = stack.pop(); piece.append(j)
            for e in bm.faces[j].edges:
                for g in e.link_faces:
                    if g.index in sel and g.index not in seen: seen.add(g.index); stack.append(g.index)
        if len(piece) >= 4: keep.update(piece)
    if not keep and pick:
        # Meshy's mesh can be split at every seam (no shared edges round small parts like fangs): group by distance instead, and take
        # the faces right round each picked one too
        cen = {p.index: mw @ p.center for p in me.polygons}
        near = L['H'] * .03; pts = list(pick); groups = []
        for i in pts:
            g = next((g for g in groups if any((cen[i] - cen[j]).length < near for j in g)), None)
            if g is None: groups.append({i})
            else: g.add(i)
        for g in groups:
            if len(g) < 3: continue
            keep.update(g)
            for p in me.polygons:
                if p.index not in keep and min((cen[p.index] - cen[j]).length for j in g) < L['H'] * .008: keep.add(p.index)
    bm.free()
    if not keep:
        log(f'hard parts {name}: none found ({len(pick)} faces matched the colour and place)'); return None, None
    hard = ob.copy(); hard.data = ob.data.copy(); hard.name = name + '_hard'; bpy.context.scene.collection.objects.link(hard)
    for o, drop in ((hard, lambda i: i not in keep), (ob, lambda i: i in keep)):
        bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if drop(f.index)], context='FACES')
        bm.to_mesh(o.data); bm.free()
    rule = rules[0]
    mean = tuple(sum(c[k] for c in cols) / len(cols) for k in range(3)) if cols else IVORY
    colour = rule['colour'] or mean   # Image.pixels of a byte image = its stored (sRGB) values
    log(f'hard parts {name}: {rule["part"]}: {len(keep)} of {len(me.polygons)} faces taken out of the fur, colour {tuple(round(c, 2) for c in colour)}')
    return hard, dict(rule=rule, colour=colour)


def close(hard, info, L, out_dir, name, log=print):
    import closed_mesh
    voxel = L['H'] * .0022
    sol = closed_mesh.solid(hard, voxel, seal=1, log=log)
    bpy.ops.object.select_all(action='DESELECT'); sol.select_set(True); bpy.context.view_layer.objects.active = sol
    r = sol.modifiers.new('remesh', 'REMESH'); r.mode = 'VOXEL'; r.voxel_size = voxel; r.adaptivity = 0
    bpy.ops.object.modifier_apply(modifier='remesh')
    sm = sol.modifiers.new('smooth', 'LAPLACIANSMOOTH'); sm.iterations = 6; sm.lambda_factor = .6; sm.use_volume_preserve = True
    bpy.ops.object.modifier_apply(modifier='smooth')
    have = sum(len(p.vertices) - 2 for p in sol.data.polygons); budget = 1600 if name == 'mammoth' else 700
    if have > budget:
        d = sol.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = budget / have; d.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier='dec')
    for p in sol.data.polygons: p.use_smooth = True
    sol.data.uv_layers.new(name='UVMap')
    m = bpy.data.materials.new(name + '_hard'); m.diffuse_color = (*info['colour'], 1); sol.data.materials.append(m)
    oe, nm = closed_mesh.open_edges(sol)
    log(f'hard parts {name}: {sum(len(p.vertices) - 2 for p in sol.data.polygons)} triangles, {oe} open edges, colour {info["colour"]}')
    os.makedirs(out_dir, exist_ok=True)
    with open(os.path.join(out_dir, 'hard.txt'), 'w', encoding='utf-8') as f:
        f.write('%.3f %.3f %.3f\n' % info['colour'])
    bpy.data.objects.remove(hard)
    return sol


def join(body, sol):
    """the hard pieces joined into the body as material 1; returns the index of the first hard vertex"""
    first = len(body.data.vertices)
    bpy.ops.object.select_all(action='DESELECT'); sol.select_set(True); body.select_set(True); bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    return first


def rigid(ob, first, info, L, log=print):
    bone = info['rule']['bone']; mw = ob.matrix_world; n = 0
    names = [g.name for g in ob.vertex_groups]
    for v in ob.data.vertices[first:]:
        target = 'Head'
        if bone == 'Paw':
            p = mw @ v.co
            k = min(L['paws'], key=lambda k: (p.xy - L['paws'][k].xy).length); target = f'{k}_Paw'
        for g in names:
            ob.vertex_groups[g].remove([v.index])
        ob.vertex_groups[target].add([v.index], 1.0, 'REPLACE'); n += 1
    log(f'hard parts: {n} vertices follow {bone.lower()} bones only')
