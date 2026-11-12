"""Shards of Time (Round 1, item 2; Hostile Horizon's Fix 3.1 tool, ported): concept images -> textured 3D models with the Meshy
image-to-3D API. Not used until Dan's concept images are in (Docs/MODELS-NEEDED.md).

python Tools/meshy_generate.py [--only dan_iceage,kira] [--force]

Reads SourceArt/Concepts/<name>.png (a second view, <name>_front.png, is for Meshy's multi-image option: not sent yet), writes SourceArt/Meshy/<name>/ (model.glb, model.fbx, textures, preview.png, task.json). The API key comes from MESHY_API_KEY
(process environment, else the Windows user environment); it is never printed, logged or written to a file. A finished model is
skipped unless --force; an unfinished task is resumed.
"""
import argparse, base64, json, os, sys, time, urllib.request, urllib.error
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = "https://api.meshy.ai/openapi/v1/image-to-3d"
# triangle budgets: people 20k, Kira's forms 15k (she's small on screen)
MODELS = {"dan_home": 20000, "dan_iceage": 20000, "abi_home": 20000, "abi_iceage": 20000, "kira": 15000, "kira_sabertooth": 15000,
          # Round 5: the Violinist (Mixamo) and the Ice Age creatures (Code's Blender rigs)
          "violinist_iceage": 20000, "wolf": 12000, "snow_critter": 8000, "mammoth": 15000,
          # Round 6: Egypt (people + mummy -> Mixamo; Kira, camel, scarab, guardian -> Code's rigs; the Sphinx is a static landmark)
          "dan_egypt": 20000, "abi_egypt": 20000, "violinist_egypt": 20000, "mummy": 15000, "kira_mummy": 15000,
          "camel": 15000, "scarab": 6000, "guardian": 12000, "sphinx_kira": 30000,
          # Round 7: Bethlehem (people -> Mixamo; lamb Kira, goat, sheep, guard dog, scorpion -> Code's rigs)
          "dan_bethlehem": 20000, "abi_bethlehem": 20000, "violinist_bethlehem": 20000, "wiseman_santa": 18000, "wiseman_crown": 18000,
          "wiseman_old": 18000, "watchman": 15000, "kira_lamb": 15000, "goat": 12000, "sheep": 8000, "guard_dog": 12000, "scorpion": 6000,
          # Round 8: the Medieval Kingdom (people -> Mixamo; dragon Kira, rat, horse -> Code's rigs)
          "dan_medieval": 20000, "abi_medieval": 20000, "muscleman_medieval": 22000, "violinist_medieval": 20000, "knight": 15000,
          "archer": 15000, "wagon_driver": 15000, "kira_dragon": 15000, "rat": 6000, "horse": 15000,
          # Round 10: the Pirate Seas (people -> Mixamo; otter Kira, crab, pufferfish, fish -> Code's rigs)
          "dan_pirate": 20000, "abi_pirate": 20000, "muscleman_pirate": 22000, "violinist_pirate": 20000, "pirate": 15000,
          "kira_otter": 15000, "crab": 8000, "pufferfish": 6000, "fish": 4000,
          # Round 11: Victorian Steampunk (people -> Mixamo; mechanical Kira, the clockwork horse -> Code's rigs; carriage, clock, press static)
          "dan_steampunk": 20000, "abi_steampunk": 20000, "muscleman_steampunk": 22000, "violinist_steampunk": 20000, "factory_worker": 15000,
          "kira_mechanical": 15000, "clockwork_horse": 15000, "steam_carriage": 15000, "street_clock": 12000, "steam_press": 20000}
CHARACTERS = {"dan_home", "dan_iceage", "abi_home", "abi_iceage", "violinist_iceage", "dan_egypt", "abi_egypt", "violinist_egypt", "mummy",
              "dan_bethlehem", "abi_bethlehem", "violinist_bethlehem", "wiseman_santa", "wiseman_crown", "wiseman_old", "watchman",
              "dan_medieval", "abi_medieval", "muscleman_medieval", "violinist_medieval", "knight", "archer", "wagon_driver",
              "dan_pirate", "abi_pirate", "muscleman_pirate", "violinist_pirate", "pirate",
              "dan_steampunk", "abi_steampunk", "muscleman_steampunk", "violinist_steampunk", "factory_worker"}   # humanoids: Meshy's A-pose option (Kira stands on four legs)


def key():
    k = os.environ.get("MESHY_API_KEY")
    if not k and sys.platform == "win32":
        import winreg
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as r:
                k = winreg.QueryValueEx(r, "MESHY_API_KEY")[0]
        except OSError:
            k = None
    if not k:
        sys.exit("MESHY_API_KEY is not set")
    return k


def call(method, url, k, body=None):
    req = urllib.request.Request(url, method=method, headers={"Authorization": "Bearer " + k, "Content-Type": "application/json"},
                                 data=json.dumps(body).encode() if body is not None else None)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {url.split('?')[0]}: HTTP {e.code} {e.read()[:400].decode(errors='replace')}") from None


def download(url, path):
    with urllib.request.urlopen(url, timeout=300) as r, open(path, "wb") as f:
        f.write(r.read())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    a = ap.parse_args()
    names = [n for n in MODELS if (n in a.only.split(",") if a.only else True)]
    k = key()
    tasks = {}
    for n in names:
        out = ROOT / "SourceArt" / "Meshy" / n
        out.mkdir(parents=True, exist_ok=True)
        tj = out / "task.json"
        if tj.exists() and not a.force:
            t = json.loads(tj.read_text())
            if t.get("status") == "SUCCEEDED" and (out / "model.glb").exists():
                print(f"{n}: already done (task {t['id']})"); continue
            if t.get("status") not in ("FAILED", "CANCELED"):
                tasks[n] = t["id"]; print(f"{n}: resuming task {t['id']}"); continue
        img = ROOT / "SourceArt" / "Concepts" / f"{n}.png"
        body = {"image_url": "data:image/png;base64," + base64.b64encode(img.read_bytes()).decode(),
                "ai_model": "latest", "should_remesh": True, "topology": "triangle", "target_polycount": MODELS[n],
                "should_texture": True, "enable_pbr": True, "remove_lighting": True}
        if n in CHARACTERS: body["pose_mode"] = "a-pose"
        tid = call("POST", API, k, body)["result"]
        tj.write_text(json.dumps({"id": tid, "status": "PENDING", "polycount": MODELS[n]}, indent=1))
        tasks[n] = tid
        print(f"{n}: started task {tid}")
    while tasks:
        time.sleep(15)
        for n, tid in list(tasks.items()):
            t = call("GET", f"{API}/{tid}", k)
            out = ROOT / "SourceArt" / "Meshy" / n
            if t["status"] in ("PENDING", "IN_PROGRESS"):
                print(f"{n}: {t['status']} {t.get('progress', 0)}%", flush=True); continue
            del tasks[n]
            keep = {x: t.get(x) for x in ("id", "status", "consumed_credits", "task_error", "created_at", "finished_at")}
            keep["polycount"] = MODELS[n]
            if t["status"] != "SUCCEEDED":
                (out / "task.json").write_text(json.dumps(keep, indent=1)); print(f"{n}: {t['status']} {t.get('task_error')}"); continue
            for fmt in ("glb", "fbx"):
                if t["model_urls"].get(fmt):
                    download(t["model_urls"][fmt], out / f"model.{fmt}")
            for i, tex in enumerate(t.get("texture_urls") or []):
                for ch, url in tex.items():
                    if url: download(url, out / (f"{ch}.png" if i == 0 else f"{ch}_{i}.png"))
            if t.get("thumbnail_url"): download(t["thumbnail_url"], out / "preview.png")
            (out / "task.json").write_text(json.dumps(keep, indent=1))
            print(f"{n}: SUCCEEDED, {t.get('consumed_credits')} credits, saved to {out.relative_to(ROOT)}", flush=True)


if __name__ == "__main__":
    main()
