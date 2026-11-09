"""Shards of Time: run the Unity editor in batch mode the way Dan's test rules require (CLAUDE.md, Process): no window, Below Normal
priority, 3 job worker threads, one heavy job at a time. Prints the log's errors and the exit code.
Usage: python Tools/unity_batch.py <log name> [--gpu] [-executeMethod ShardsOfTime.PlayerBuild.BuildFromCommandLine] [other Unity args]
--gpu: keep the GPU and let the method quit by itself (the cutscene render).
With no -executeMethod it only opens the project (imports, compiles) and quits. Logs go to local/logs/<log name>.log."""
import pathlib, subprocess, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = r'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe'
name, extra = sys.argv[1], sys.argv[2:]
gpu = '--gpu' in extra          # keep the GPU (no -nographics) and don't -quit: the cutscene render enters Play mode and exits itself
extra = [x for x in extra if x != '--gpu']
log = ROOT / 'local' / 'logs' / (name + '.log'); log.parent.mkdir(parents=True, exist_ok=True)
args = [UNITY, '-batchmode', *([] if gpu else ['-nographics', '-quit']), '-projectPath', str(ROOT), '-job-worker-count', '3', '-logFile', str(log), *extra]
si = subprocess.STARTUPINFO(); si.dwFlags |= subprocess.STARTF_USESHOWWINDOW; si.wShowWindow = 7   # SW_SHOWMINNOACTIVE: never takes focus
code = subprocess.call(args, startupinfo=si, creationflags=subprocess.BELOW_NORMAL_PRIORITY_CLASS)
text = log.read_text(encoding='utf-8', errors='replace') if log.exists() else ''
errs = [l for l in text.splitlines() if 'error CS' in l or ('Exception' in l and 'ValidationExceptions' not in l) or l.startswith('Error') or 'Build failed' in l or 'Scripts have compiler errors' in l]
print('\n'.join(dict.fromkeys(errs))[:6000] or 'no errors in the log')
print('unity exit', code, 'log', log)
sys.exit(code)
