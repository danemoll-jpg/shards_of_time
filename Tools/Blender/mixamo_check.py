"""Shards of Time, Round 2: look at Mixamo's rigged T-poses. Renders each rigged model in its T-pose and on a frame of a few
clips (so bad skin weights show), and prints the bone count.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/mixamo_check.py -- <model>=<rigged fbx> ...
Writes Data/screenshots/round2/mixamo/<model>_{tpose,run,cheer,kick}.png
"""
import bpy, math, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Data', 'screenshots', 'round2', 'mixamo')
CLIPS = {'run': ('Fast Run.fbx', .3), 'cheer': ('Cheering.fbx', .5), 'kick': ('Mma Kick.fbx', .45)}


def load_action(path):
    before = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=path)
    act = [a for a in bpy.data.actions if a not in before][0]
    for o in list(bpy.context.selected_objects): bpy.data.objects.remove(o)
    return act


def setup(h):
    sc = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    sc.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1024, 1024
    sc.view_settings.view_transform = 'Standard'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[0].default_value = (.62, .64, .66, 1)
    for rot, e in (((50, 0, 30), 3.0), ((60, 0, -140), 1.5), ((70, 0, 180), 1.0)):
        l = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); l.data.energy = e
        l.rotation_euler = [math.radians(a) for a in rot]; sc.collection.objects.link(l)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = h * 1.3
    return cam


def shot(cam, h, ang, path):
    a = math.radians(ang)
    cam.location = (10 * math.sin(a), -10 * math.cos(a), h / 2); cam.rotation_euler = (math.radians(90), 0, a)
    bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)


def check(model, fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    arm.animation_data_clear() if arm.animation_data else None
    zs = [(o.matrix_world @ v.co).z for o in meshes for v in o.data.vertices]
    h = max(zs) - min(zs)
    print(f'MIXAMO {model}: {len(arm.data.bones)} bones, {len(meshes)} mesh(es), height {h:.2f} (file units), '
          f'first bone {arm.data.bones[0].name}')
    cam = setup(h)
    os.makedirs(OUT, exist_ok=True)
    shot(cam, h, 0, os.path.join(OUT, f'{model}_tpose.png'))
    shot(cam, h, 180, os.path.join(OUT, f'{model}_tposeback.png'))
    for tag, (clip, t) in CLIPS.items():
        p = os.path.join(ROOT, 'SourceArt', 'Mixamo', clip)
        if not os.path.exists(p): continue
        act = load_action(p)
        arm.animation_data_create(); arm.animation_data.action = act
        f0, f1 = act.frame_range; bpy.context.scene.frame_set(int(f0 + (f1 - f0) * t))
        shot(cam, h, 35, os.path.join(OUT, f'{model}_{tag}.png'))


for a in sys.argv[sys.argv.index('--') + 1:]:
    m, f = a.split('=', 1); check(m, f)
