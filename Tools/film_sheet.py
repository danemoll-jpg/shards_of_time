"""Shards of Time (Round 9): Dan's film clips next to the game's cutscenes.

python Tools/film_sheet.py frames <clip> [--every 1.0] [--start 0] [--end 999] [--out file.jpg]
    a contact sheet of a clip in SourceArt/Reference/Clips/ (a frame every N s, labelled with its time): for studying the staging.
python Tools/film_sheet.py sheet <Scene> [<Scene> ...]
    the side-by-side sheet: film frames on top, the game's (Recordings/<Scene>.mp4, rendered from the exe) below, at the matching
    moments listed in MATCH -> Data/screenshots/round9/<Scene>.jpg.

Every ffmpeg runs through Tools/quiet.py (hidden, never-shown desktop, Below Normal). No sound is played: frames only.
"""
import argparse, glob, os, pathlib, shutil, sys, tempfile
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent)); import quiet
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
CLIPS = ROOT / 'SourceArt' / 'Reference' / 'Clips'

# scene -> (clip file, [(film seconds, game seconds, label), ...]): the matching moments, in the film's order
MATCH = {
    # Round 12: Tomorrowland (Dan's 6 s clip; the game's scene adds Dan placing the shard first, and the rise into the sky after)
    'RocketLaunch': ('07-Tomorrowland_RocketLaunch.mp4', [(.5, 4.4, 'the four, small, the huge rocket'), (2, 7.6, 'the rocket alone'), (3.5, 8.8, 'the doors glow warmer'), (5.5, 10.4, 'the engines ignite')]),
    # Round 11: Victorian Steampunk (film seconds, game seconds, what)
    'SteampunkArrival': ('06-Steampunk_Arrival.mp4', [(1, 1, 'gear, teal streak'), (6, 5.5, 'teal spiral'), (12, 9, 'the big spiral'), (16, 10.8, 'over the domes'),
                          (18, 13.6, 'onto the chain'), (19, 15.2, 'into the bucket'), (23, 18, 'carried along'), (25, 20.4, 'tipped out')]),
    'MuscleManHero': ('06-Steampunk_MuscleManHero.mp4', [(1, 1.5, 'tumbling on the belt'), (3, 3, 'flat, heads covered'), (4, 5, 'the press comes'), (7, 6.9, 'heads down'),
                       (8, 8.2, 'he holds it up'), (9, 9, 'alarms'), (10, 10.2, 'heave'), (12, 12.5, 'through the wall')]),
    'ActOne': ('00-Home_ActOne.mp4', [(4, 3, 'decorating'), (10.5, 10.4, 'Kira swats'), (13.5, 12.6, 'the fumble'), (16.5, 16, 'lunge, gasp'), (21, 19.8, 'on the floor'),
                                      (24, 23, 'glow, Kira sniffs'), (27, 26.5, 'Abi holds Kira'), (30, 30.3, 'beams, shards'), (37.5, 35.5, 'rainbow spiral'), (40.5, 39.8, 'holding hands'),
                                      (44, 44, 'tumbling in'), (49, 47.5, 'the swirl'), (52, 51, 'mammoth, vortex'), (55, 55, 'the vortex'), (58, 58, 'falling')]),
    'IceAgeArrival': ('01-IceAge_Arrival.mp4', [(.3, .95, 'landed'), (1.2, 1.4, 'transforms'), (1.8, 1.9, 'the glow'), (3, 3.5, 'high angle'), (6, 6.5, 'low: snarl'),
                                                (8.4, 9, 'stalks'), (10.8, 11, 'scared'), (12.6, 12.9, 'the leap'), (13.8, 13.8, 'Dan down'), (16.2, 16, 'necklace blue')]),
    'EgyptArrival': ('02-Egypt_Arrival.mp4', [(1, 1, 'establishing'), (3.6, 3.5, 'the camel'), (9.6, 8.5, 'red vortex'), (13.2, 12.5, 'sand plume'), (19, 16, 'empty dune'),
                                              (25.2, 19.5, 'they pop up'), (27.6, 22.3, 'Kira?'), (33.6, 26, 'digging'), (40.8, 29.2, 'mummy Kira'), (43.2, 32, 'the hug')]),
    'SphinxGag': ('02-Egypt_Sphinx.mp4', [(.6, .7, 'from behind'), (2.4, 2.8, 'push-in'), (4.2, 4.8, "Kira's face"), (6.6, 7, 'they react'), (8.4, 8.9, 'Dan looks down'),
                                          (10.2, 10.5, 'mummy Kira'), (13.2, 13.2, 'innocent'), (15.6, 15.6, 'the look'), (17.4, 17.4, 'held')]),
    'BethlehemArrival': ('03-Bethlehem_Arrival.mp4', [(1, 1, 'Wise Men walk'), (3.5, 4.5, 'golden spiral'), (7.7, 7.6, 'it flattens'), (9.8, 10, 'ring over barn'), (13.3, 13.4, 'flash'),
                                                      (14.7, 14.9, 'top-down crash'), (16.8, 16.6, 'puzzled'), (18.2, 18, 'falling in'), (19.6, 19.5, 'Abi, lamb Kira'), (20.6, 20.6, 'Dan, gold glow')]),
    'WiseMenArrive': ('03-Bethlehem_WiseMenArrive.mp4', [(1.5, 1.5, 'silhouettes'), (4, 4.3, 'the doors'), (6, 6.5, 'the manger'), (8.5, 8.5, 'the shard'), (10, 10, 'staring'),
                                                         (11.5, 11.2, 'disappointed'), (12.5, 12.4, 'walk away'), (13, 12.9, 'present tossed'), (14, 13.8, 'the hug'), (14.6, 15.4, 'hay tornado')]),
    'MedievalArrival': ('04-Medieval_Arrival.mp4', [(1.5, 2, 'castle, vortex'), (5.5, 5.5, 'the fall'), (7.7, 6.8, 'falling'), (9.9, 9.5, 'top-down wagon'), (12.1, 11, 'into the hay'),
                                                    (14.3, 14, 'hay heaves'), (17.6, 17.5, 'pop up'), (20.9, 21.2, 'the sneeze'), (25.3, 24.5, 'flames'), (27.5, 26, 'out'),
                                                    (29.7, 29, 'wagon careens'), (31.9, 31.8, 'crash'), (35.2, 38, 'Kira flying'), (37.4, 40.5, 'hurry off')]),
    'TheStone': ('04-Medieval_TheStone_05-Pirate_Arrival.mp4', [(1.8, 1.8, 'he heaves'), (5.4, 6, 'falls back'), (7.2, 7.5, 'embarrassed'), (9, 9.2, 'smug'), (12.6, 12, 'points'),
                                                               (16.2, 16.5, 'Dan steps up'), (21.6, 22, 'reaches'), (27, 27.3, 'lifts it'), (30.6, 29, 'holds it'), (32.4, 31, 'laughs'),
                                                               (34.2, 34.5, 'My hero!'), (37.8, 38, 'stones stir'), (41.4, 41.5, 'they swirl'), (46.8, 47, 'whirlpool'), (50.4, 50, 'pulled in'),
                                                               (54, 54, 'Muscle Man too'), (59.4, 58, 'the tunnel')]),
    # Round 10: the Pirate Seas (the arrival is the second half of the TheStone clip; the overboard clip is in the world's folder)
    'PirateArrival': ('04-Medieval_TheStone_05-Pirate_Arrival.mp4', [(71, 1.5, 'tunnel'), (77, 5, 'ship at night'), (83, 9.5, 'green vortex'), (87, 11.3, 'past the sails'), (88, 12.0, 'onto the deck'), (88.5, 12.55, 'into a barrel'), (89, 14, 'barrels'),
                                                                    (90.5, 15.6, 'pop out'), (92, 17.6, 'feet, otter'), (93.5, 18.8, 'standing'), (95, 20.4, 'through the sails'),
                                                                    (96.5, 22.5, 'the crash'), (101, 25, 'lying there'), (105.5, 26.8, 'flat'), (110, 29.6, 'the grin'),
                                                                    (114.5, 31.3, 'Abi laughs'), (116, 33.3, 'green shard')]),
    'ShardOverboard': (str(ROOT / 'SourceArt' / 'Reference' / 'Worlds' / '05-PirateSeas' / 'shard-rolls-overboard.mp4'),
                       [(0.3, 3.8, 'held up'), (1.0, 5.4, 'it slips'), (1.5, 6.0, 'it falls'), (2.5, 7.4, 'it rolls'), (3.5, 8.6, 'rolling'), (4.5, 10.1, 'to the edge'),
                        (5.0, 11.0, 'over the side'), (5.5, 11.8, 'into the sea')]),
}
ROUND = {'RocketLaunch': 'round12', 'PirateArrival': 'fix101', 'ShardOverboard': 'round10', 'SteampunkArrival': 'round11', 'MuscleManHero': 'round11'}


def ffmpeg():
    f = shutil.which('ffmpeg')
    if f: return f
    hits = glob.glob(os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\WinGet\Packages\*FFmpeg*\*\bin\ffmpeg.exe'))
    if hits: return hits[0]
    sys.exit('ffmpeg not found')


def grab(video, t, out, w=640):
    """one frame of `video` at t seconds -> out (png), w px wide; returns an Image (grey if past the end)"""
    code = quiet.run([ffmpeg(), '-y', '-loglevel', 'error', '-ss', f'{t:.3f}', '-i', str(video), '-frames:v', '1', '-vf', f'scale={w}:-2', str(out)], timeout=120)
    if code == 0 and pathlib.Path(out).exists(): return Image.open(out).convert('RGB')
    return None


def font(size):
    for f in (r'C:\Windows\Fonts\segoeui.ttf', r'C:\Windows\Fonts\arial.ttf'):
        if os.path.exists(f): return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def label(img, text, size=18):
    d = ImageDraw.Draw(img); f = font(size)
    box = d.textbbox((0, 0), text, font=f)
    d.rectangle([0, 0, box[2] + 12, box[3] + 8], fill=(0, 0, 0))
    d.text((6, 3), text, fill=(255, 255, 255), font=f)
    return img


def frames(clip, every, start, end, out):
    video = CLIPS / clip if not os.path.exists(clip) else pathlib.Path(clip)
    tmp = pathlib.Path(tempfile.mkdtemp(prefix='sheet_'))
    dur = float(quiet.output(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', str(video)])[1].strip() or 0) if shutil.which('ffprobe') else 999
    ts, t = [], start
    while t < min(end, dur - .05): ts.append(t); t += every
    tiles = []
    for i, t in enumerate(ts):
        im = grab(video, t, tmp / f'{i}.png', 400)
        if im: tiles.append(label(im, f'{t:.1f}s', 16))
    cols = 6; w, h = tiles[0].size; rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * w, rows * h), (30, 30, 30))
    for i, im in enumerate(tiles): sheet.paste(im, ((i % cols) * w, (i // cols) * h))
    out = pathlib.Path(out); out.parent.mkdir(parents=True, exist_ok=True); sheet.save(out, quality=88)
    shutil.rmtree(tmp, ignore_errors=True)
    print(f'{out.relative_to(ROOT) if out.is_relative_to(ROOT) else out}: {len(tiles)} frames ({start}-{min(end, dur):.1f} s every {every} s)')


def sheet(scene):
    clip, moments = MATCH[scene]
    game = ROOT / 'Recordings' / f'{scene}.mp4'
    if not game.exists(): print(f'{scene}: no {game.relative_to(ROOT)} (render it first)'); return False
    tmp = pathlib.Path(tempfile.mkdtemp(prefix='sheet_'))
    w = 480; cols = min(len(moments), 5); rows = (len(moments) + cols - 1) // cols
    th = int(w * 9 / 16) + 2
    head = 40
    out = Image.new('RGB', (cols * w, head + rows * (2 * th + 34)), (24, 24, 24))
    d = ImageDraw.Draw(out); d.text((10, 8), f'{scene}: film (top) / game (bottom)', fill=(255, 255, 255), font=font(22))
    for i, (ft, gt, name) in enumerate(moments):
        x = (i % cols) * w; y = head + (i // cols) * (2 * th + 34)
        f = grab(CLIPS / clip, ft, tmp / f'f{i}.png', w) or Image.new('RGB', (w, th - 2), (60, 0, 0))
        g = grab(game, gt, tmp / f'g{i}.png', w) or Image.new('RGB', (w, th - 2), (60, 0, 0))
        f = f.resize((w, th - 2)); g = g.resize((w, th - 2))
        out.paste(label(f, f'film {ft:.1f}s', 14), (x, y + 30)); out.paste(label(g, f'game {gt:.1f}s', 14), (x, y + 30 + th))
        d.text((x + 6, y + 6), name, fill=(255, 220, 120), font=font(18))
    dst = ROOT / 'Data' / 'screenshots' / ROUND.get(scene, 'round9') / f'{scene}.jpg'; dst.parent.mkdir(parents=True, exist_ok=True)
    out.save(dst, quality=86)
    shutil.rmtree(tmp, ignore_errors=True)
    print(f'{scene}: {dst.relative_to(ROOT)} ({len(moments)} moments)')
    return True


if __name__ == '__main__':
    a = argparse.ArgumentParser(); sp = a.add_subparsers(dest='cmd', required=True)
    p = sp.add_parser('frames'); p.add_argument('clip'); p.add_argument('--every', type=float, default=1.0)
    p.add_argument('--start', type=float, default=0); p.add_argument('--end', type=float, default=999); p.add_argument('--out')
    p = sp.add_parser('sheet'); p.add_argument('scenes', nargs='+')
    o = a.parse_args()
    if o.cmd == 'frames':
        frames(o.clip, o.every, o.start, o.end, o.out or ROOT / 'local' / 'round9' / 'ref' / (pathlib.Path(o.clip).stem + '.jpg'))
    else:
        sys.exit(0 if all([sheet(s) for s in o.scenes]) else 1)
