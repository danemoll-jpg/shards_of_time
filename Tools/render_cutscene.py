"""Shards of Time: render a cutscene to a 1080p MP4 from the built game, silently (Round 5, Dan's rule: the Unity editor render played
sound on his speakers and took his focus; never again).

python Tools/render_cutscene.py <Name> [<Name> ...] [--exe Builds/ShardsOfTime/ShardsOfTime.exe]

1. The built exe in batch mode, muted from the first frame, no window (Tools/quiet.py: hidden, never activated, Below Normal):
   -exeCheck <dir> -exeCheckOnly film -film <Name> plays the cutscene at a fixed 30 fps and saves every frame off-screen
   (Assets/Scripts/Checks/ExeCheck.Film.cs), then reports the measured audio output (must be 0.000).
2. ffmpeg (hidden too) encodes the frames: Recordings/<Name>.mp4, 1920x1080, 30 fps, H.264, no audio. The frames are deleted.
Prints, per cutscene: frames, the audio report, and what the window watcher saw (quiet.watch_report). Needs a current build
(Shards of Time > Build Executable). The editor menu Shards of Time > Render Cutscene to Video runs this script.
"""
import argparse, glob, os, pathlib, re, shutil, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent)); import quiet

ROOT = pathlib.Path(__file__).resolve().parents[1]


def ffmpeg():
    f = shutil.which('ffmpeg')
    if f: return f
    hits = glob.glob(os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\WinGet\Packages\*FFmpeg*\*\bin\ffmpeg.exe'))
    if hits: return hits[0]
    sys.exit('ffmpeg not found (winget install Gyan.FFmpeg)')


def render(name, exe):
    d = ROOT / 'local' / 'film' / name
    if d.exists(): shutil.rmtree(d)
    d.mkdir(parents=True)
    code = quiet.run([exe, '-batchmode', '-mute', '-exeCheck', d, '-exeCheckOnly', 'film', '-film', name, '-screen-fullscreen', '0',
                      '-screen-width', '1920', '-screen-height', '1080', '-logFile', d / 'Player.log'], timeout=1800)
    game_windows = quiet.watch_report()
    report = (d / 'exe_check.txt').read_text(encoding='utf-8') if (d / 'exe_check.txt').exists() else ''
    frames = sorted((d / 'frames').glob('*.png'))
    if code != 0 or not frames or 'RESULT: all checks passed' not in report:
        print(f'{name}: FAILED (game exit {code}, {len(frames)} frames)\n{report}'); return False
    out = ROOT / 'Recordings' / f'{name}.mp4'; out.parent.mkdir(exist_ok=True)
    code = quiet.run([ffmpeg(), '-y', '-loglevel', 'error', '-framerate', '30', '-i', d / 'frames' / '%05d.png', '-c:v', 'libx264', '-preset', 'slow',
                      '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', out], log=d / 'ffmpeg.log', timeout=1800)
    enc_windows = quiet.watch_report()
    audio = next((l.strip() for l in report.splitlines() if 'measured output level' in l and l.strip().startswith('sound')), '')
    level = re.search(r'measured output level ([0-9.]+) RMS', report)
    print(f'{name}: {len(frames)} frames -> {out.relative_to(ROOT)} ({out.stat().st_size / 1048576:.1f} MB, ffmpeg exit {code})')
    print(f'  audio: {audio}')
    print(f'  game exe: {game_windows}')
    print(f'  ffmpeg:   {enc_windows}')
    keep = d / 'stills'; keep.mkdir()
    for i in range(0, len(frames), 45): shutil.copy2(frames[i], keep / frames[i].name)   # a still every 1.5 s, for looking at
    shutil.rmtree(d / 'frames')
    return code == 0 and level and float(level.group(1)) == 0.0


if __name__ == '__main__':
    a = argparse.ArgumentParser(); a.add_argument('names', nargs='+'); a.add_argument('--exe', default=str(ROOT / 'Builds/ShardsOfTime/ShardsOfTime.exe'))
    o = a.parse_args()
    ok = all([render(n, o.exe) for n in o.names])
    sys.exit(0 if ok else 1)
