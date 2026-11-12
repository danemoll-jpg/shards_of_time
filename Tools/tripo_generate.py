"""Shards of Time, Round 11: concept images -> textured 3D models with the Tripo image-to-model API (Meshy ran out of credits; Dan, 2026-11-12).

python Tools/tripo_generate.py --only violinist_steampunk,factory_worker [--force] [--faces 20000]

Reads SourceArt/Concepts/<name>.png, writes SourceArt/Meshy/<name>/model.glb (the same folder as Meshy's, so Tools/Blender/meshy_cleanup.py,
kira_rig.py and static_export.py work unchanged), preview.png, task.json ({"source": "tripo", ...}). The API key comes from TRIPO_API_KEY
(the process environment, else the Windows user environment); it is never printed, logged or written to a file. A finished model is
skipped unless --force; an unfinished task is resumed.
"""
import argparse, json, os, sys, time, uuid, urllib.request, urllib.error
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = "https://api.tripo3d.ai/v2/openapi"
FACES = {"violinist_steampunk": 20000, "factory_worker": 15000, "kira_mechanical": 15000, "clockwork_horse": 15000,
         "steam_carriage": 15000, "street_clock": 12000, "steam_press": 20000}


def key():
    k = os.environ.get("TRIPO_API_KEY")
    if not k and sys.platform == "win32":
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as r:
                k = winreg.QueryValueEx(r, "TRIPO_API_KEY")[0]
        except OSError:
            k = None
    if not k:
        sys.exit("TRIPO_API_KEY is not set")
    return k


def call(method, url, k, body=None, data=None, ctype="application/json"):
    req = urllib.request.Request(url, method=method, headers={"Authorization": "Bearer " + k, "Content-Type": ctype, "User-Agent": "ShardsOfTime-tools/1.0"},
                                 data=data if data is not None else (json.dumps(body).encode() if body is not None else None))
    try:
        with urllib.request.urlopen(req, timeout=180) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {url.split('?')[0]}: HTTP {e.code} {e.read()[:400].decode(errors='replace')}") from None


def upload(k, path):
    b = uuid.uuid4().hex
    body = (f"--{b}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{path.name}\"\r\nContent-Type: image/png\r\n\r\n").encode() + path.read_bytes() + f"\r\n--{b}--\r\n".encode()
    r = call("POST", API + "/upload", k, data=body, ctype="multipart/form-data; boundary=" + b)
    return r["data"]["image_token"]


def download(url, path):
    with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "ShardsOfTime-tools/1.0"}), timeout=300) as r, open(path, "wb") as f:
        f.write(r.read())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args()
    names = [n for n in FACES if (n in a.only.split(",") if a.only else True)]
    k = key()
    tasks = {}
    for n in names:
        out = ROOT / "SourceArt" / "Meshy" / n
        out.mkdir(parents=True, exist_ok=True)
        tj = out / "task.json"
        if tj.exists() and not a.force:
            t = json.loads(tj.read_text(encoding="utf-8"))
            if t.get("source") == "tripo" and t.get("status") == "success" and (out / "model.glb").exists():
                print(f"{n}: done already"); continue
            if t.get("source") == "tripo" and t.get("task_id"):
                tasks[n] = t["task_id"]; print(f"{n}: resuming {t['task_id']}"); continue
        img = ROOT / "SourceArt" / "Concepts" / f"{n}.png"
        token = upload(k, img)
        body = {"type": "image_to_model", "file": {"type": "png", "file_token": token}, "texture": True, "pbr": True,
                "texture_quality": "detailed", "face_limit": FACES[n], "auto_size": False}
        r = call("POST", API + "/task", k, body)
        tid = r["data"]["task_id"]; tasks[n] = tid
        tj.write_text(json.dumps({"source": "tripo", "task_id": tid, "status": "queued"}), encoding="utf-8")
        print(f"{n}: started task {tid}")
    while tasks:
        time.sleep(10)
        for n, tid in list(tasks.items()):
            d = call("GET", f"{API}/task/{tid}", k)["data"]
            st = d.get("status")
            out = ROOT / "SourceArt" / "Meshy" / n
            if st in ("success",):
                o = d.get("output", {})
                url = o.get("pbr_model") or o.get("model") or o.get("base_model")
                download(url, out / "model.glb")
                if o.get("rendered_image"): download(o["rendered_image"], out / "preview.png")
                (out / "task.json").write_text(json.dumps({"source": "tripo", "task_id": tid, "status": "success", "credits": d.get("consumed_credit")}), encoding="utf-8")
                print(f"{n}: SUCCEEDED, saved to {out.relative_to(ROOT)}"); del tasks[n]
            elif st in ("failed", "cancelled", "banned", "expired", "unknown"):
                print(f"{n}: {st.upper()}"); del tasks[n]
            else:
                print(f"{n}: {st} {d.get('progress', '')}%")


if __name__ == "__main__":
    main()
