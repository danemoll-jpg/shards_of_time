"""Shards of Time: run the Unity editor in batch mode the way Dan's test rules require (CLAUDE.md, Process): no window, Below Normal
priority, 3 job worker threads, one heavy job at a time. Prints the log's errors and the exit code.
Usage: python Tools/unity_batch.py <log name> [-executeMethod ShardsOfTime.PlayerBuild.BuildFromCommandLine] [other Unity args]
Always -nographics -quit, hidden and muted (Tools/quiet.py, Editor/QuietEditor.cs). Cutscene videos: Tools/render_cutscene.py.
With no -executeMethod it only opens the project (imports, compiles) and quits. Logs go to local/logs/<log name>.log."""
import pathlib, subprocess, sys
import sys as _s, pathlib as _p; _s.path.insert(0, str(_p.Path(__file__).resolve().parent)); import quiet   # every process hidden (Tools/quiet.py)
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = r'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
name, extra = sys.argv[1], sys.argv[2:]
if '--gpu' in extra:            # Round 5: the editor with graphics took Dan's focus and played sound: never again
    sys.exit('unity_batch.py: --gpu is gone. Cutscene videos render from the built exe: python Tools/render_cutscene.py <Name>')
gpu = False
log = ROOT / 'local' / 'logs' / (name + '.log'); log.parent.mkdir(parents=True, exist_ok=True)
args = [UNITY, '-batchmode', *([] if gpu else ['-nographics', '-quit']), '-projectPath', str(ROOT), '-job-worker-count', '3', '-logFile', str(log), *extra]
code = quiet.run(args)   # hidden, no focus, Below Normal; the editor itself is muted (Editor/QuietEditor.cs)
text = log.read_text(encoding='utf-8', errors='replace') if log.exists() else ''
errs = [l for l in text.splitlines() if 'error CS' in l or ('Exception' in l and 'ValidationExceptions' not in l) or l.startswith('Error') or 'Build failed' in l or 'Scripts have compiler errors' in l]
print('\n'.join(dict.fromkeys(errs))[:6000] or 'no errors in the log')
print('unity exit', code, 'log', log); print(quiet.watch_report())
sys.exit(code)
