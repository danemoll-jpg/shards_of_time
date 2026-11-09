"""Shards of Time, Round 4 items 1-2: close see-through holes in a Meshy mesh at the source (used by kira_rig.py and
fix_hair_weights.py; tried out by mesh_holes.py fix=1).

Meshy's hair strands and fur tufts are single sheets: from behind the game culls them (back faces) and the background shows
through. Closed surfaces are fine. So only faces whose back side can be seen from outside get a twin facing the other way:
from just behind each face, rays go out over the face's back hemisphere; if enough of them leave the model without hitting
anything, that back is visible (a sheet). Its twin copies the face's corners (UVs, skin weights) with the winding reversed, so
it is lit correctly from that side. Thick, closed parts (body, head, limbs) get nothing.
"""
import bmesh, math, os, random
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def _dirs(n, k):
    """k directions over the hemisphere around n, biased to n."""
    t = n.orthogonal().normalized(); b = n.cross(t)
    out = [n]
    rnd = random.Random(7)
    for _ in range(k - 1):
        u, v = rnd.random(), rnd.random()
        r = math.sqrt(u) * .9; a = 2 * math.pi * v
        out.append((n * math.sqrt(1 - r * r) + t * (r * math.cos(a)) + b * (r * math.sin(a))).normalized())
    return out


def _sphere(k):
    """k directions spread evenly over the sphere (Fibonacci)."""
    out = []
    for i in range(k):
        z = 1 - 2 * (i + .5) / k; r = math.sqrt(1 - z * z); a = i * math.pi * (3 - math.sqrt(5))
        out.append(Vector((r * math.cos(a), r * math.sin(a), z)))
    return out


def double_sheets(ob, views=int(os.environ.get("MESHFIX_VIEWS", 256)), log=print):
    """Give every face that a camera outside can see from behind a reversed twin. Each face is looked at from `views` directions
    spread over the sphere; a direction on the face's back side counts when a ray from far away along it hits this face first.
    Returns (faces doubled, faces total)."""
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
    tree = BVHTree.FromBMesh(bm)
    pts = [v.co for v in bm.verts]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    far = (hi - lo).length * 1.5
    dirs = _sphere(views)
    sheets = []
    for f in bm.faces:
        n = f.normal
        if n.length < .5: continue
        c = f.calc_center_median(); tol = max(1e-6, min(e.calc_length() for e in f.edges) * .1)
        spots = [c] + [c.lerp(v.co, .6) for v in f.verts]   # the middle and toward each corner: a gap may show only part of it
        seen = False
        for d in dirs:
            if d.dot(n) <= .05: continue          # d = the viewing direction; a back view looks along the normal
            for p in spots:
                hit = tree.ray_cast(p - d * far, d, far * 1.01)
                if hit[0] is not None and (hit[2] == f.index or (hit[0] - p).length < tol): seen = True; break
            if seen: break
        if seen: sheets.append(f)
    deform = bm.verts.layers.deform.active
    uvs = list(bm.loops.layers.uv.values())
    made = 0; twin = {}
    for f in sheets:
        vs = []
        for v in reversed(f.verts):
            nv = twin.get(v.index)
            if nv is None:            # one twin vertex per vertex: the twins form smooth sheets, not loose faces
                nv = twin[v.index] = bm.verts.new(v.co)
                if deform is not None: nv[deform] = v[deform]
            vs.append(nv)
        try: nf = bm.faces.new(vs)
        except ValueError: continue
        nf.material_index = f.material_index; nf.smooth = f.smooth
        for lay in uvs:
            src = [l[lay].uv.copy() for l in reversed(f.loops)]
            for l, uv in zip(nf.loops, src): l[lay].uv = uv
        made += 1
    bm.normal_update(); bm.to_mesh(me); bm.free()
    me.update()
    log(f'MESHFIX {ob.name}: {made} of {len(me.polygons) - made} faces were single sheets seen from behind (hair, fur tufts, '
        f'whiskers): each got a reversed twin')
    return made, len(me.polygons) - made
