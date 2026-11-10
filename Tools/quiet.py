"""Shards of Time: the one way a tool starts a process on Dan's PC (CLAUDE.md, "Testing on Dan's PC"; Round 5, after a Unity editor
cutscene render played sound on his speakers and took his focus).

Every process run()/popen() starts lives on a desktop of its own that is never shown ("ShardsOfTimeQuiet"): its windows, even the
hidden one a batch-mode Unity player makes (Round 5: that one still took the foreground on Dan's desktop), can't appear or take
focus. Also hidden (SW_HIDE, CREATE_NO_WINDOW), Below Normal priority, no console input. sub()/output() (git, gh, small console
helpers) run hidden on the normal desktop. Sound is each caller's job (games run with -mute, the Unity editor is muted by Editor/QuietEditor.cs, Blender runs -b and
plays nothing), but everything goes through here so nothing pops up.

Use from Python:   import quiet; quiet.run([exe, args...], timeout=600)  -> exit code ('timeout' if killed)
                   quiet.output([exe, args...]) -> (code, stdout text)    (git, gh, small helpers)
From the shell:     python Tools/quiet.py [--timeout S] [--log FILE] -- <exe> <args...>
Blender is refused without -b / --background (it would open its window).
"""
import os, subprocess, sys

HIDE = 0            # SW_HIDE
FLAGS = subprocess.BELOW_NORMAL_PRIORITY_CLASS | subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0


def _si():
    if sys.platform != 'win32': return None
    si = subprocess.STARTUPINFO(); si.dwFlags |= subprocess.STARTF_USESHOWWINDOW; si.wShowWindow = HIDE
    return si


def _check(args):
    exe = os.path.basename(str(args[0])).lower()
    if exe.startswith('blender') and not ({'-b', '--background'} & set(map(str, args))):
        raise SystemExit('quiet.py: Blender must run with -b (background): it would open its window')
    if exe == 'unity.exe' and '-batchmode' not in map(str, args):
        raise SystemExit('quiet.py: the Unity editor only runs with -batchmode here')


DESKTOP = 'ShardsOfTimeQuiet'   # a desktop of its own, never shown: nothing started on it can take Dan's focus


class _Proc:
    """A process started with CreateProcessW on the quiet desktop (Popen can't pick a desktop)."""
    def __init__(self, handle, pid): self.handle, self.pid, self.returncode = handle, pid, None
    def poll(self):
        import ctypes
        if self.returncode is None and ctypes.windll.kernel32.WaitForSingleObject(self.handle, 0) == 0:
            code = ctypes.c_ulong(); ctypes.windll.kernel32.GetExitCodeProcess(self.handle, ctypes.byref(code)); self.returncode = code.value
        return self.returncode
    def wait(self, timeout=None):
        import ctypes
        r = ctypes.windll.kernel32.WaitForSingleObject(self.handle, 0xFFFFFFFF if timeout is None else int(timeout * 1000))
        if r != 0: raise subprocess.TimeoutExpired('process', timeout)
        return self.poll()
    def kill(self):
        import ctypes
        ctypes.windll.kernel32.TerminateProcess(self.handle, 1)


def _desktop_popen(args, log):
    import ctypes, msvcrt
    from ctypes import wintypes
    k, u = ctypes.windll.kernel32, ctypes.windll.user32
    u.CreateDesktopW.restype = wintypes.HANDLE
    if not u.CreateDesktopW(DESKTOP, None, None, 0, 0x10000000, None):   # GENERIC_ALL; the handle stays open while this process lives
        raise OSError('quiet.py: could not create the quiet desktop')
    class SI(ctypes.Structure):
        _fields_ = [('cb', wintypes.DWORD), ('lpReserved', wintypes.LPWSTR), ('lpDesktop', wintypes.LPWSTR), ('lpTitle', wintypes.LPWSTR),
                    ('dwX', wintypes.DWORD), ('dwY', wintypes.DWORD), ('dwXSize', wintypes.DWORD), ('dwYSize', wintypes.DWORD),
                    ('dwXCountChars', wintypes.DWORD), ('dwYCountChars', wintypes.DWORD), ('dwFillAttribute', wintypes.DWORD),
                    ('dwFlags', wintypes.DWORD), ('wShowWindow', wintypes.WORD), ('cbReserved2', wintypes.WORD), ('lpReserved2', ctypes.c_void_p),
                    ('hStdInput', wintypes.HANDLE), ('hStdOutput', wintypes.HANDLE), ('hStdError', wintypes.HANDLE)]
    class PI(ctypes.Structure):
        _fields_ = [('hProcess', wintypes.HANDLE), ('hThread', wintypes.HANDLE), ('dwProcessId', wintypes.DWORD), ('dwThreadId', wintypes.DWORD)]
    si = SI(); si.cb = ctypes.sizeof(SI); si.lpDesktop = 'WinSta0\\' + DESKTOP
    si.dwFlags = 0x1 | 0x100; si.wShowWindow = HIDE   # STARTF_USESHOWWINDOW | STARTF_USESTDHANDLES
    out = open(log or os.devnull, 'w', encoding='utf-8', errors='replace'); nul = open(os.devnull, 'r')
    ho, hi = msvcrt.get_osfhandle(out.fileno()), msvcrt.get_osfhandle(nul.fileno())
    for h in (ho, hi): k.SetHandleInformation(wintypes.HANDLE(h), 1, 1)   # inheritable
    si.hStdInput, si.hStdOutput, si.hStdError = hi, ho, ho
    pi = PI()
    cmd = ctypes.create_unicode_buffer(subprocess.list2cmdline([str(a) for a in args]))
    if not k.CreateProcessW(None, cmd, None, None, True, FLAGS, None, None, ctypes.byref(si), ctypes.byref(pi)):
        raise OSError(f'quiet.py: could not start {args[0]} (error {k.GetLastError()})')
    k.CloseHandle(pi.hThread)
    proc = _Proc(pi.hProcess, pi.dwProcessId); proc._files = (out, nul)
    return proc


def popen(args, log=None, **kw):
    _check(args)
    if sys.platform == 'win32' and not kw: return _desktop_popen(args, log)
    out = open(log, 'w', encoding='utf-8', errors='replace') if log else kw.pop('stdout', subprocess.DEVNULL)
    return subprocess.Popen([str(a) for a in args], startupinfo=_si(), creationflags=FLAGS, stdin=subprocess.DEVNULL,
                            stdout=out, stderr=subprocess.STDOUT if log else kw.pop('stderr', subprocess.DEVNULL), **kw)


def run(args, timeout=None, log=None, **kw):
    """Start on the quiet desktop and wait; meanwhile every 0.25 s note any visible window of the process (or its children) on
    Dan's desktop and whether one has the foreground (quiet.watch)."""
    import time
    p = popen(args, log=log, **kw)
    watch.update(visible=[], foreground=False, samples=0)
    end = time.time() + timeout if timeout else None
    while p.poll() is None:
        if end and time.time() > end: p.kill(); return 'timeout'
        if sys.platform == 'win32':
            seen, fg = _look(_tree(p.pid)); watch['samples'] += 1
            for w in seen:
                if w not in watch['visible']: watch['visible'].append(w)
            if fg and not watch['foreground']: watch['foreground'] = fg
        time.sleep(.25)
    return p.returncode


# what the last run() saw: windows of its process tree that were visible, and whether one ever had the foreground (the proof Dan
# asked for in Round 5)
watch = {'visible': [], 'foreground': False, 'samples': 0}


def _tree(pid):
    """pid and its children (Windows: a snapshot via the toolhelp API)."""
    import ctypes
    from ctypes import wintypes
    class PE(ctypes.Structure):
        _fields_ = [('dwSize', wintypes.DWORD), ('cntUsage', wintypes.DWORD), ('th32ProcessID', wintypes.DWORD), ('th32DefaultHeapID', ctypes.c_void_p),
                    ('th32ModuleID', wintypes.DWORD), ('cntThreads', wintypes.DWORD), ('th32ParentProcessID', wintypes.DWORD), ('pcPriClassBase', ctypes.c_long),
                    ('dwFlags', wintypes.DWORD), ('szExeFile', ctypes.c_char * 260)]
    k = ctypes.windll.kernel32; snap = k.CreateToolhelp32Snapshot(2, 0); e = PE(); e.dwSize = ctypes.sizeof(PE); kids = {}
    ok = k.Process32First(snap, ctypes.byref(e))
    while ok:
        kids.setdefault(e.th32ParentProcessID, []).append(e.th32ProcessID); ok = k.Process32Next(snap, ctypes.byref(e))
    k.CloseHandle(snap)
    out, todo = set(), [pid]
    while todo:
        x = todo.pop()
        if x in out: continue
        out.add(x); todo += kids.get(x, [])
    return out


def _look(pids):
    import ctypes
    from ctypes import wintypes
    u = ctypes.windll.user32; seen = []
    def cb(h, _):
        pid = wintypes.DWORD(); u.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value in pids and u.IsWindowVisible(h):
            t = ctypes.create_unicode_buffer(200); u.GetWindowTextW(h, t, 200); seen.append(f'{pid.value}:{t.value!r}')
        return True
    u.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)(cb), 0)
    h = u.GetForegroundWindow(); fg = wintypes.DWORD(); u.GetWindowThreadProcessId(h, ctypes.byref(fg))
    if fg.value in pids:
        c = ctypes.create_unicode_buffer(200); u.GetClassNameW(h, c, 200); t = ctypes.create_unicode_buffer(200); u.GetWindowTextW(h, t, 200)
        return seen, f'{fg.value}:{c.value}:{t.value!r}:visible={bool(u.IsWindowVisible(h))}'
    return seen, False


def watch_report():
    return (f"windows: {'none visible' if not watch['visible'] else 'VISIBLE ' + ', '.join(watch['visible'])}, "
            f"{'TOOK THE FOREGROUND (' + watch['foreground'] + ')' if watch['foreground'] else 'never in the foreground'} ({watch['samples']} looks, every 0.25 s)")


def sub(args, **kw):
    """subprocess.run, hidden (a drop-in for the release tools: capture_output, check, text... as usual)."""
    _check(args)
    kw.setdefault('stdin', subprocess.DEVNULL)
    return subprocess.run([str(a) for a in args], startupinfo=_si(), creationflags=FLAGS, **kw)


def output(args, timeout=None, check=False, **kw):
    _check(args)
    r = subprocess.run([str(a) for a in args], startupinfo=_si(), creationflags=FLAGS, stdin=subprocess.DEVNULL,
                       capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=timeout, **kw)
    if check and r.returncode: raise subprocess.CalledProcessError(r.returncode, args, r.stdout, r.stderr)
    return r.returncode, r.stdout + r.stderr


if __name__ == '__main__':
    a = sys.argv[1:]; timeout = None; log = None
    while a and a[0] != '--':
        if a[0] == '--timeout': timeout = float(a[1]); a = a[2:]
        elif a[0] == '--log': log = a[1]; a = a[2:]
        else: raise SystemExit(__doc__)
    if not a[1:]: raise SystemExit(__doc__)
    if log: code = run(a[1:], timeout=timeout, log=log); print(watch_report())
    else:
        code, text = output(a[1:], timeout=timeout); sys.stdout.reconfigure(encoding='utf-8', errors='replace'); print(text, end='')
    sys.exit(code if isinstance(code, int) else 1)
