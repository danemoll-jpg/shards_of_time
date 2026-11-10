"""Shards of Time: start an exe check the way Dan's test rules require (CLAUDE.md, Process: "Testing on Dan's PC"). Hostile Horizon's
run_check.py, ported. Muted from the first frame (-mute; the game also mutes any -exeCheck run itself) and with no window at all
(-batchmode: nothing pops up or takes focus; the game draws its view off-screen for the screenshots, Core/TestRun.cs), Below Normal
priority. Waits, then prints <dir>/exe_check.txt.
Usage: python Tools/run_check.py <dir> [--only all|play|models|measure|perf|mute] [--minutes N] [--extra "-steamDeck 1"] [--size 1920x1080]"""
import os, argparse, pathlib, shutil, subprocess, sys
import sys as _s, pathlib as _p; _s.path.insert(0, str(_p.Path(__file__).resolve().parent)); import quiet   # every process hidden (Tools/quiet.py)
sys.stdout.reconfigure(encoding='utf-8', errors='replace')

ROOT = pathlib.Path(__file__).resolve().parents[1]
a = argparse.ArgumentParser(); a.add_argument('dir'); a.add_argument('--only', default='all'); a.add_argument('--minutes', type=float, default=20)
a.add_argument('--extra', default=''); a.add_argument('--size', default='1920x1080')
a.add_argument('--exe', default=str(ROOT / 'Builds/ShardsOfTime/ShardsOfTime.exe'))
o = a.parse_args()
d = pathlib.Path(o.dir).resolve()
if d.exists(): shutil.rmtree(d)
d.mkdir(parents=True)
w, h = o.size.split('x')
args = [o.exe, '-batchmode', '-mute', '-exeCheck', str(d), '-exeCheckOnly', o.only, *o.extra.split(),
        '-screen-fullscreen', '0', '-screen-width', w, '-screen-height', h, '-logFile', str(d / 'Player.log')]
code = quiet.run(args, timeout=o.minutes * 60)   # hidden, no focus, Below Normal (Tools/quiet.py)
if code == 'timeout': code = 'killed after %s min' % o.minutes
print('check exit', code); print(quiet.watch_report())
f = d / 'exe_check.txt'
print(f.read_text(encoding='utf-8') if f.exists() else '\n'.join(l for l in (d / 'Player.log').read_text(encoding='utf-8', errors='replace').splitlines() if '[ExeCheck]' in l or 'Crash!!!' in l or 'Exception' in l))
