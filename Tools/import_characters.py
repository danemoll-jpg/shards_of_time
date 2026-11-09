"""Shards of Time (Round 1, item 2; Hostile Horizon's Fix 3.1 tool, ported): copy the rigged models, their animations and Meshy's
textures into the Unity project. Not used until the models exist (Docs/MODELS-NEEDED.md, pipeline step 5).

python Tools/import_characters.py [--only dan_iceage,abi_iceage]

From SourceArt/Mixamo/<name>/ (Dan's Mixamo downloads; Kira's forms: SourceArt/Rigged/<name>/ from Code's Blender rig) and
SourceArt/Meshy/<name>/ (Meshy's textures) to Assets/Resources/Characters/:
  <name>.fbx                    the rigged model (people: Mixamo's T-pose With Skin, or rigged_hairfix.fbx when the hair fix ran;
                                Kira's forms: model.fbx from Tools/Blender/kira_rig.py, every clip as a take inside it)
  <name>_albedo.png             base colour
  <name>_normal.png             normal map
  Clips/<name>_<clip>.fbx       animations (Without Skin)
The Unity import settings (Humanoid for people, Generic for Kira, loops, LODs) come with the import editor script in the round
that wires the models in.
"""
import argparse, shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
MIX, RIGGED, MESHY = ROOT / "SourceArt" / "Mixamo", ROOT / "SourceArt" / "Rigged", ROOT / "SourceArt" / "Meshy"
OUT = ROOT / "Assets" / "Resources" / "Characters"
NAMES = ["dan_home", "dan_iceage", "abi_home", "abi_iceage", "kira", "kira_sabertooth"]
KIRA_NOW = {"kira", "kira_sabertooth"}   # Round 2 puts Kira's forms in the game; Dan and Abi follow in Round 3


def textures(n):
    d = MESHY / n
    shutil.copy2(d / "base_color.png", OUT / f"{n}_albedo.png")
    if (d / "normal.png").exists(): shutil.copy2(d / "normal.png", OUT / f"{n}_normal.png")
    if False and (d / "metallic.png").exists() and (d / "roughness.png").exists():   # Round 2: Meshy's metallic map is noise; not used
        metal = Image.open(d / "metallic.png").convert("L")
        smooth = Image.eval(Image.open(d / "roughness.png").convert("L"), lambda v: 255 - v)
        Image.merge("RGBA", (metal, metal, metal, smooth)).save(OUT / f"{n}_mask.png")


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--only", default=""); a = ap.parse_args()
    (OUT / "Clips").mkdir(parents=True, exist_ok=True)
    for n in [x for x in NAMES if not a.only or x in a.only.split(",")]:
        if (RIGGED / n / "model.fbx").exists():
            src, model = RIGGED / n, RIGGED / n / "model.fbx"
        else:
            src = MIX / n
            model = next((src / f for f in ("rigged_hairfix.fbx", "rigged.fbx", "T-Pose.fbx") if (src / f).exists()), None)
        if not model:
            print(f"{n}: no rigged model yet ({MIX / n} or {RIGGED / n}), skipped"); continue
        if n not in KIRA_NOW and src.parent == MIX:
            print(f"{n}: rigged ({model.name}); people go in with their clips in Round 3, skipped"); continue
        shutil.copy2(model, OUT / f"{n}.fbx")
        textures(n)
        clips = [f for f in sorted(src.glob("*.fbx")) if f != model and src.parent == MIX]
        for f in clips: shutil.copy2(f, OUT / "Clips" / f"{n}_{f.stem}.fbx")
        print(f"{n}: model + {len(clips)} clips")
    print("copied:", len(list(OUT.rglob("*.fbx"))), "fbx,", len(list(OUT.rglob("*.png"))), "png")


if __name__ == "__main__":
    main()
