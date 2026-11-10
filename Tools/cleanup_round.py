"""Shards of Time (Round 1, item 2): Hostile Horizon's Tools/cleanup_round.py (Chastain Rush's Round 76 D), ported. The clean-up at
the end of every round, after its final build. Prints the disk use per folder before and after, and one line for the round's status.

Deletes: local/'s check output of rounds older than the last three (round<N>, r<N>..., _to_delete; a file a rule or a script names by
path is kept), local/logs/ older than the last three rounds' logs, Builds/LauncherCheck (the launcher self-check's test install and
catalog), Recordings/ cutscene renders older than 14 days, Temp/, Unity's Library/BuildHistory (old build reports), then git gc.
Keeps: the player build, the launcher build, the newest release folder, Builds/PublisherPrivate (the signing key: never deleted),
Data/, Library's import caches (deleting them only makes the next build slower), and everything else in local/.
Usage: python Tools/cleanup_round.py [--dry-run]"""
import os
import re
import shutil
import subprocess
import sys as _s, pathlib as _p; _s.path.insert(0, str(_p.Path(__file__).resolve().parent)); import quiet   # every process hidden (Tools/quiet.py)
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = ["Builds", "local", ".git", "Data/screenshots", "Recordings", "Library", "Temp"]
SCRATCH = re.compile(r"(round\d+[a-z0-9]*|r\d+[a-z0-9]*|determinism|_to_delete)$")


def size_mb(p):
    total = 0
    for dp, _, fns in os.walk(p):
        for fn in fns:
            try: total += os.path.getsize(os.path.join(dp, fn))
            except OSError: pass
    return total / 1e6


def named_paths():
    """local/... paths that CLAUDE.md, TODO.md or a script names: kept."""
    pat = re.compile(r"local/[A-Za-z0-9_]+/[A-Za-z0-9_./-]+")
    out = set()
    for base in ("CLAUDE.md", "TODO.md", "Tools", "Assets/Scripts", "Assets/Editor"):
        p = os.path.join(ROOT, base)
        files = [p] if os.path.isfile(p) else [os.path.join(dp, f) for dp, _, fs in os.walk(p) for f in fs if f.endswith((".py", ".cs", ".md"))]
        for f in files:
            try: text = open(f, encoding="utf-8", errors="ignore").read()
            except OSError: continue
            out.update(m.rstrip(".,;)`/") for m in pat.findall(text))
    return out


def main():
    dry = "--dry-run" in sys.argv
    before = {f: size_mb(os.path.join(ROOT, f)) for f in FOLDERS}
    local = os.path.join(ROOT, "local")
    os.makedirs(local, exist_ok=True)
    nums = sorted({int(m.group(1)) for d in os.listdir(local) for m in [re.match(r"round(\d+)", d)] if m and os.path.isdir(os.path.join(local, d))})
    last3 = set(nums[-3:])
    keep = named_paths()
    gone = []
    for d in sorted(os.listdir(local)):
        p = os.path.join(local, d)
        if not os.path.isdir(p) or not SCRATCH.match(d): continue
        m = re.match(r"round(\d+)", d)
        if m and int(m.group(1)) in last3: continue
        for dp, dns, fns in os.walk(p, topdown=False):
            for fn in fns:
                rel = os.path.relpath(os.path.join(dp, fn), ROOT).replace("\\", "/")
                if rel in keep or any(rel.startswith(k + "/") for k in keep): continue
                if not dry: os.remove(os.path.join(dp, fn))
            if not dry:
                for dn in dns:
                    q = os.path.join(dp, dn)
                    if os.path.isdir(q) and not os.listdir(q): os.rmdir(q)
        if not dry and not os.listdir(p): os.rmdir(p)
        gone.append(d)
    # older release folders (the newest is the next release's "previous": kept)
    rel_dirs = sorted([d for d in os.listdir(os.path.join(ROOT, "Builds")) if d.startswith("LauncherRelease-")], key=lambda d: int(re.sub(r"\D", "", d) or 0)) if os.path.isdir(os.path.join(ROOT, "Builds")) else []
    for d in rel_dirs[:-1]:
        gone.append("Builds/" + d)
        if not dry: shutil.rmtree(os.path.join(ROOT, "Builds", d), ignore_errors=True)
    rec = os.path.join(ROOT, "Recordings")
    if os.path.isdir(rec):
        import time
        for fn in os.listdir(rec):
            f = os.path.join(rec, fn)
            if os.path.isfile(f) and time.time() - os.path.getmtime(f) > 14 * 86400:
                gone.append("Recordings/" + fn)
                if not dry: os.remove(f)
    for rel in ("Builds/LauncherCheck", "Temp", "Library/BuildHistory"):
        p = os.path.join(ROOT, rel)
        if os.path.isdir(p):
            gone.append(rel)
            if not dry: shutil.rmtree(p, ignore_errors=True)
    if not dry: quiet.sub(["git", "-C", ROOT, "gc", "-q"])
    after = {f: size_mb(os.path.join(ROOT, f)) for f in FOLDERS}
    for f in FOLDERS:
        print(f"{f:20s} {before[f] / 1000:6.2f} GB -> {after[f] / 1000:6.2f} GB")
    print(f"cleared: {', '.join(gone) or 'nothing'}")
    print(f"Disk use {sum(before.values()) / 1000:.1f} GB -> {sum(after.values()) / 1000:.1f} GB (these folders){' (dry run)' if dry else ''}")


if __name__ == "__main__":
    main()
