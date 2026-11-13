"""Shards of Time, Rounds 12-13: a cleaned model as a static world model made of rigid parts (a tin-toy robot's propeller and arms, the
moon critter's eye stalks), so the game can spin, wave and wobble them without a skin. Also cuts parts away (the flying car's solid
canopy: Dan, 2026-11-12: Tripo made the clear bubble a solid half-dome; the game puts a see-through glass dome on instead).

Run:  F:\\blender\\blender.exe -b --factory-startup --python Tools/Blender/static_parts.py -- <name> [<name> ...]
Reads SourceArt/Meshy/<name>/<name>.blend (meshy_cleanup.py: metres, on the floor, facing -Y). Each part is the faces whose centre is
inside its box (model metres), split off as its own object named after the part with its origin at the part's pivot; "cut" parts are
deleted. Writes Assets/Resources/World/<name>.fbx (no textures inside) + <name>_albedo.png and prints what each part got.
"""
import bpy, bmesh, math, os, shutil, sys
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'World')
INF = 99
# name: (turn about z in degrees (applied first), [(part, (xmin, xmax, ymin, ymax, zmin, zmax), pivot or 'cut'), ...]); boxes after the turn
PARTS = {
    'robot_greeter': (0, [('Propeller', (-INF, INF, -INF, INF, 1.19, INF), (0, .04, 1.2)),
                          ('ArmL', (.29, INF, -INF, INF, .62, 1.0), (.29, 0, .82)),
                          ('ArmR', (-INF, -.29, -INF, INF, .62, 1.0), (-.29, 0, .82)),
                          ('Wheel', (-.2, .2, -INF, INF, -INF, .27), (0, 0, .14))]),
    'cleanbot': (0, [('Top', (-INF, INF, -INF, INF, .47, INF), (0, 0, .47))]),
    'moon_critter': (0, [('Eyes', (-INF, INF, -INF, -.05, .42, INF), (0, -.2, .42))]),
    # the car: its nose was at -X; turned +90 deg it faces -Y like everything else (Unity +Z). The canopy: everything over the body
    # behind the tail fins' start (z > 1.12) goes; the game adds the glass bubble and its chrome arch
    'flying_car': (90, [('Canopy', (-INF, INF, -INF, .8, 1.12, INF), 'cut')]),
    'rocket': (0, []), 'lunar_rover': (0, []),
}


def log(*a): print('PARTS', *a, flush=True)


def export(name):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'SourceArt', 'Meshy', name, name + '.blend'))
    ob = bpy.data.objects[name]
    turn, parts = PARTS[name]
    if turn:
        ob.rotation_euler = (0, 0, math.radians(turn)); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    made = [ob]
    for part, (x0, x1, y0, y1, z0, z1), pivot in parts:
        bm = bmesh.new(); bm.from_mesh(ob.data)
        inside = [f for f in bm.faces if x0 <= f.calc_center_median().x <= x1 and y0 <= f.calc_center_median().y <= y1 and z0 <= f.calc_center_median().z <= z1]
        n = len(inside)
        if pivot == 'cut':
            bmesh.ops.delete(bm, geom=inside, context='FACES'); bm.to_mesh(ob.data); bm.free(); ob.data.update()
            log(f'{name}: {part} cut off ({n} faces)'); continue
        if not inside: bm.free(); log(f'{name}: {part}: no faces in its box'); continue
        # the part as its own mesh: copy the whole mesh, keep only its faces; the body loses them
        keep = set(f.index for f in inside)
        bm2 = bmesh.new(); bm2.from_mesh(ob.data); bm2.faces.ensure_lookup_table()
        bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in keep], context='FACES')
        me = bpy.data.meshes.new(part); bm2.to_mesh(me); bm2.free()
        bmesh.ops.delete(bm, geom=inside, context='FACES'); bm.to_mesh(ob.data); bm.free(); ob.data.update()
        po = bpy.data.objects.new(part, me); bpy.context.scene.collection.objects.link(po)
        pv = Vector(pivot); me.transform(__import__('mathutils').Matrix.Translation(-pv)); po.location = pv
        po.parent = ob
        made.append(po); log(f'{name}: {part}: {n} faces, pivot {tuple(pivot)}')
    bpy.ops.object.select_all(action='DESELECT')
    for o in made: o.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, object_types={'MESH'}, path_mode='STRIP',
                             embed_textures=False, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False)
    shutil.copy2(os.path.join(ROOT, 'SourceArt', 'Meshy', name, 'base_color.png'), os.path.join(OUT, name + '_albedo.png'))
    log(f'{name}: {len(ob.data.polygons)} body faces -> Assets/Resources/World/{name}.fbx')


for n in sys.argv[sys.argv.index('--') + 1:]:
    export(n)
