"""Shards of Time, Round 7 item 1: how far a four-legged model's head is turned from its body's line (the guard dog's check).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/head_turn.py -- guard_dog goat sheep kira_lamb

Reads SourceArt/Meshy/<name>/<name>.blend (meshy_cleanup.py: nose to -Y, paws at 0). The head = the mesh above 55 % of the height in
front of the front legs; the nose = the darkest texture spot on it (dog, goat, sheep: a dark nose; Kira: her gray nose is the
darkest bit of her face too). turn = the angle between (nose - head's back) and -Y, seen from above: 0 = straight ahead.
"""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))


def measure(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend'))
    ob = next(o for o in bpy.context.scene.objects if o.type == 'MESH'); me = ob.data
    co = [ob.matrix_world @ v.co for v in me.vertices]
    H = max(c.z for c in co); low = [c for c in co if c.z < .03 * H]
    yf = sorted(c.y for c in low)[len(low) // 4]   # the front paws' y (the front quarter of the floor points)
    head = [i for i, c in enumerate(co) if c.z > H * .55 and c.y < yf + .08 * H]
    img = next((n.image for m in me.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)
    W, Hh = img.size; px = img.pixels[:]; uv = me.uv_layers.active.data
    lum = {}
    hs = set(head)
    for l in me.loops:
        vi = l.vertex_index
        if vi not in hs or vi in lum: continue
        u, w = uv[l.index].uv; x = min(W - 1, int(u % 1 * W)); y = min(Hh - 1, int(w % 1 * Hh)); i = (y * W + x) * 4
        lum[vi] = sum(px[i:i + 3]) / 3
    dark = sorted(lum, key=lum.get)[:max(8, len(lum) // 60)]
    nose = sum((co[i] for i in dark), Vector()) / len(dark)
    back = sum((co[i] for i in head), Vector()) / len(head)   # the head's middle (ears and all)
    d = (nose - back).xy
    turn = math.degrees(math.atan2(d.x, -d.y))
    print(f'HEADTURN {name}: head {len(head)} verts, nose ({nose.x:.3f},{nose.y:.3f},{nose.z:.3f}) lum {sum(lum[i] for i in dark) / len(dark):.2f}, '
          f'head middle ({back.x:.3f},{back.y:.3f}), turned {turn:+.0f} deg (+ = toward +X, the side view\'s camera)', flush=True)


for n in sys.argv[sys.argv.index('--') + 1:]:
    measure(n)
