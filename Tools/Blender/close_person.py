"""Shards of Time, Round 5 item 1b: a Mixamo-rigged person rebuilt as one closed surface (Abi's hair strands were open sheets:
see-through from behind), on the same skeleton.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/close_person.py -- <model>=<rigged fbx> ...

1. Imports the rigged FBX (abi_*: rigged_hairfix.fbx, whose hair weights Round 2 fixed) and gives its mesh Meshy's maps.
2. closed_mesh.rebuild: a solid of the surface (gaps up to ~2 voxels sealed, so hair and sleeve stay apart), voxel remesh,
   25k triangles, new UVs, Meshy's colour + normal baked -> SourceArt/Rigged/<model>/base_color.png, normal.png.
3. The skin weights copied from the original mesh (nearest face, interpolated), parented to the same armature.
Writes SourceArt/Mixamo/<model>/rigged_closed.fbx (Tools/import_characters.py takes it before rigged_hairfix.fbx) and previews
Data/screenshots/round5/models/<model>_closed_{front,back,34}.png.
"""
import bpy, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import closed_mesh

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CORE = {'abi_iceage': 14}
# Round 12 item 0a: legs pushed this far apart (metres, each side) before the solid is made, and back after: the voxel solid joined
# Egyptian Abi's calves, thighs and ankle wings below her skirt, and the walk stretched that into a dark spiky sheet between her legs
SPREAD = {'abi_egypt': .035, 'abi_tomorrowland': .035, 'abi_moon': .035}   # Round 12: the flared silver dress, the spacesuit's legs


def one_leg_below_knees(ob, arm):
    """Below the knees nothing belongs to both legs (Meshy's inner ankle wings touched and were weighted to both feet): a vertex there
    keeps only the leg bones of the side it is on."""
    M = ob.matrix_world
    bones = {b.name: arm.matrix_world @ b.head_local for b in arm.data.bones}
    knee = min(p.z for n, p in bones.items() if n.endswith('Leg') and not n.endswith('UpLeg'))
    lx = [p.x for n, p in bones.items() if n.endswith('LeftUpLeg')][0]; rx = [p.x for n, p in bones.items() if n.endswith('RightUpLeg')][0]; mid = (lx + rx) / 2
    legs = {g.index: ('Left' if 'Left' in g.name else 'Right') for g in ob.vertex_groups if any(k in g.name for k in ('UpLeg', 'Leg', 'Foot', 'Toe'))}
    fixed = 0
    for v in ob.data.vertices:
        p = M @ v.co
        if p.z >= knee: continue
        l = sum(g.weight for g in v.groups if legs.get(g.group) == 'Left'); r = sum(g.weight for g in v.groups if legs.get(g.group) == 'Right')
        mine = 'Left' if l > r else 'Right' if r > l else ('Left' if (p.x - mid) * (lx - mid) > 0 else 'Right')   # its heavier leg
        wrong = [(g.group, g.weight) for g in v.groups if legs.get(g.group) and legs[g.group] != mine and g.weight > 0]
        if not wrong: continue
        for gi, w in wrong:
            other = ob.vertex_groups.get(ob.vertex_groups[gi].name.replace(legs[gi], mine))
            if other: other.add([v.index], w, 'ADD')
            ob.vertex_groups[gi].remove([v.index])
        fixed += 1
    return fixed


def cut_between_legs(ob, arm):
    """Faces below the knees that join a left-leg vertex to a right-leg vertex (the wings' join) are cut out of the source."""
    import bmesh
    M = ob.matrix_world
    knee = min((arm.matrix_world @ b.head_local).z for b in arm.data.bones if b.name.endswith('Leg') and not b.name.endswith('UpLeg'))
    legs = {g.index: (1 if 'Left' in g.name else -1) for g in ob.vertex_groups if any(k in g.name for k in ('UpLeg', 'Leg', 'Foot', 'Toe'))}
    side = []
    for v in ob.data.vertices:
        w = sum(g.weight * legs[g.group] for g in v.groups if g.group in legs); side.append(0 if abs(w) < .05 else (1 if w > 0 else -1))
    bm = bmesh.new(); bm.from_mesh(ob.data)
    cut = [f for f in bm.faces if {side[v.index] for v in f.verts} >= {1, -1} and all((M @ v.co).z < knee for v in f.verts)]
    bmesh.ops.delete(bm, geom=cut, context='FACES'); bm.to_mesh(ob.data); bm.free(); ob.data.update()
    return len(cut)


def spread_legs(ob, arm, d):
    """Moves every vertex sideways by d x (its left-leg weight - its right-leg weight): the legs apart, a skirt stretched evenly."""
    M = ob.matrix_world; Mi = M.inverted()
    side = 1 if (arm.matrix_world @ arm.data.bones[[b.name for b in arm.data.bones if b.name.endswith('LeftUpLeg')][0]].head_local).x > 0 else -1
    legs = {g.index: (1 if 'Left' in g.name else -1) for g in ob.vertex_groups if any(k in g.name for k in ('UpLeg', 'Leg', 'Foot', 'Toe'))}
    from mathutils import Vector
    moved = 0
    for v in ob.data.vertices:
        w = sum(g.weight * legs[g.group] for g in v.groups if g.group in legs)
        if abs(w) < 1e-4: continue
        v.co = Mi @ ((M @ v.co) + Vector((d * w * side, 0, 0))); moved += 1
    ob.data.update()
    return moved   # voxels: the widest opening into the body the core fill closes (her outfit's openings are wider)


def log(*a): print('PERSON', *a, flush=True)


def close(model, fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    ob = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    # the mesh is stored in its A-pose, the skeleton is posed in Mixamo's T-pose: work in the rest pose (bake and weights read the
    # evaluated mesh), back to the pose for the export (as the hair fix wrote it)
    arm.data.pose_position = 'REST'; bpy.context.view_layer.update()
    # Meshy's maps on the original (the FBX's own material may have lost its image paths)
    mat = bpy.data.materials.new(model + '_src'); mat.use_nodes = True; nt = mat.node_tree; bsdf = nt.nodes['Principled BSDF']
    d = os.path.join(ROOT, 'SourceArt', 'Meshy', model)
    tc = nt.nodes.new('ShaderNodeTexImage'); tc.image = bpy.data.images.load(os.path.join(d, 'base_color.png'))
    nt.links.new(tc.outputs['Color'], bsdf.inputs['Base Color'])
    if os.path.exists(os.path.join(d, 'normal.png')):
        tn = nt.nodes.new('ShaderNodeTexImage'); tn.image = bpy.data.images.load(os.path.join(d, 'normal.png')); tn.image.colorspace_settings.name = 'Non-Color'
        nm = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(tn.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    ob.data.materials.clear(); ob.data.materials.append(mat)
    zs = [(ob.matrix_world @ v.co).z for v in ob.data.vertices]; H = max(zs) - min(zs)
    if SPREAD.get(model): log(f'{model}: {one_leg_below_knees(ob, arm)} vertices below the knees given to one leg')
    if SPREAD.get(model): log(f'{model}: legs spread {SPREAD[model]} m each side ({spread_legs(ob, arm, SPREAD[model])} vertices), '
                              f'{cut_between_legs(ob, arm)} faces joining the ankles cut')
    out = os.path.join(ROOT, 'SourceArt', 'Rigged', model)
    new = closed_mesh.rebuild(ob, .003, 25000, out, seal=1, core=CORE.get(model, 6), reach=.08, keep_src=True, log=log)
    # the skin: weights from the original, nearest face interpolated
    for g in ob.vertex_groups: new.vertex_groups.new(name=g.name)
    dt = new.modifiers.new('dt', 'DATA_TRANSFER'); dt.object = ob; dt.use_vert_data = True
    dt.data_types_verts = {'VGROUP_WEIGHTS'}; dt.vert_mapping = 'POLYINTERP_NEAREST'
    dt.layers_vgroup_select_src = 'ALL'; dt.layers_vgroup_select_dst = 'NAME'; dt.mix_mode = 'REPLACE'; dt.mix_factor = 1
    bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = new; new.select_set(True)
    bpy.ops.object.modifier_apply(modifier='dt')
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    empty = sum(1 for v in new.data.vertices if not v.groups)
    if SPREAD.get(model):   # back together (the closed mesh by its own new weights, the original too)
        spread_legs(new, arm, -SPREAD[model]); spread_legs(ob, arm, -SPREAD[model])
    # into the original's place under the armature
    M = ob.matrix_world.copy()
    new.data.transform(M.inverted())
    new.parent = arm; new.matrix_parent_inverse = ob.matrix_parent_inverse.copy(); new.matrix_basis = ob.matrix_basis.copy()
    m = new.modifiers.new('Armature', 'ARMATURE'); m.object = arm
    name = ob.name; bpy.data.objects.remove(ob); new.name = name
    render(model, new)
    arm.data.pose_position = 'POSE'; bpy.context.view_layer.update()
    path = os.path.join(ROOT, 'SourceArt', 'Mixamo', model, 'rigged_closed.fbx')
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); new.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             path_mode='COPY', embed_textures=True, bake_anim=False, apply_scale_options='FBX_SCALE_ALL')
    log(f'{model}: height {H:.2f} m, {sum(len(p.vertices) - 2 for p in new.data.polygons)} triangles, {len(new.vertex_groups)} bone groups, '
        f'{empty} vertices without weights -> {os.path.relpath(path, ROOT)}')


def render(model, ob):
    """Front / back / 3/4 in the bind pose."""
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 600, 900; sc.view_settings.view_transform = 'Standard'
    w = sc.world or bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e; l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    bpy.context.view_layer.update()
    vs = [ob.matrix_world @ v.co for v in ob.data.vertices]; zc = (max(v.z for v in vs) + min(v.z for v in vs)) / 2; h = max(v.z for v in vs) - min(v.z for v in vs)
    cx = (max(v.x for v in vs) + min(v.x for v in vs)) / 2; cy = (max(v.y for v in vs) + min(v.y for v in vs)) / 2
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = h * 1.1
    shots = os.path.join(ROOT, 'Data', 'screenshots', 'round5', 'models'); os.makedirs(shots, exist_ok=True)
    for tag, ang in (('front', 0), ('back', 180), ('34', 35)):
        a = math.radians(ang); cam.location = (cx + 5 * math.sin(a), cy - 5 * math.cos(a), zc); cam.rotation_euler = (math.radians(90), 0, a)
        sc.render.filepath = os.path.join(shots, f'{model}_closed_{tag}.png'); bpy.ops.render.render(write_still=True)


for a in sys.argv[sys.argv.index('--') + 1:]:
    m, f = a.split('=', 1); close(m, f)
