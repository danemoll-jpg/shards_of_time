"""Shards of Time (Round 1, item 2; Hostile Horizon's Fix 3.1 tool, ported): copy the rigged models, their animations and Meshy's
textures into the Unity project. Not used until the models exist (Docs/MODELS-NEEDED.md, pipeline step 5).

python Tools/import_characters.py [--only dan_iceage,abi_iceage]

From SourceArt/Mixamo/<name>/ (Dan's Mixamo downloads; Kira's forms: SourceArt/Rigged/<name>/ from Code's Blender rig) and
SourceArt/Meshy/<name>/ (Meshy's textures) to Assets/Resources/Characters/:
  <name>.fbx                    the rigged model (people: Mixamo's T-pose With Skin, or rigged_hairfix.fbx when the hair fix ran;
                                Kira's forms: model.fbx from Tools/Blender/kira_rig.py, every clip as a take inside it)
  <name>_albedo.png             base colour
  <name>_normal.png             normal map
  Clips/<clip>.fbx              the people's shared Mixamo clips (Without Skin), named as the game calls them
The Unity import settings (Humanoid for people, Generic for Kira, loops, LODs) come with the import editor script in the round
that wires the models in.
"""
import argparse, shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
MIX, RIGGED, MESHY = ROOT / "SourceArt" / "Mixamo", ROOT / "SourceArt" / "Rigged", ROOT / "SourceArt" / "Meshy"
OUT = ROOT / "Assets" / "Resources" / "Characters"
NAMES = ["dan_home", "dan_iceage", "abi_home", "abi_iceage", "kira", "kira_sabertooth", "wolf", "snow_critter", "mammoth", "violinist_iceage",
         # Round 6: Egypt (the people and the mummy from Dan's Mixamo files when they're in; mummy Kira, the camel, the guardian: Code's rigs)
         "kira_mummy", "camel", "guardian", "dan_egypt", "abi_egypt", "violinist_egypt", "mummy",
         # Round 7: Bethlehem (lamb Kira, the goat, the sheep, the guard dog: Code's rigs; the people from Dan's Mixamo files when they're in)
         "kira_lamb", "goat", "sheep", "guard_dog", "dan_bethlehem", "abi_bethlehem", "violinist_bethlehem", "wiseman_santa", "wiseman_crown",
         "wiseman_old", "watchman",
         # Round 8: the Medieval Kingdom (the people from Dan's Mixamo files; dragon Kira, the rat, the horse: Code's rigs)
         "dan_medieval", "abi_medieval", "muscleman_medieval", "violinist_medieval", "knight", "archer", "wagon_driver",
         "kira_dragon", "rat", "horse",
         # Round 10: the Pirate Seas (the people from Dan's Mixamo files; otter Kira: Code's rig)
         "dan_pirate", "abi_pirate", "muscleman_pirate", "violinist_pirate", "pirate", "kira_otter",
         # Round 11: Victorian Steampunk (the people from Dan's Mixamo files; mechanical Kira, the clockwork horse: Code's rigs)
         "dan_steampunk", "abi_steampunk", "muscleman_steampunk", "violinist_steampunk", "factory_worker", "kira_mechanical", "clockwork_horse"]
# Round 5: the Violinist's own Mixamo clips (SourceArt/Mixamo/violinist_iceage/<Mixamo name>.fbx -> Clips/violinist_<clip>.fbx);
# his idle and walk are the shared Idle / Walking clips (Clips/idle.fbx, walk.fbx), like Dan's and Abi's
VIOLINIST_CLIPS = {"play": "Playing The Violin", "bow": "Quick Informal Bow", "nod": "Head Nod Yes"}
# Round 3: one Mixamo clip set for all four people (Without Skin, SourceArt/Mixamo/<Mixamo name>.fbx), copied as
# Assets/Resources/Characters/Clips/<game clip>.fbx. Unity's Humanoid retargeting plays them on every person; trims, loops and
# root settings per clip are in Assets/Editor/CharacterImport.cs (PeopleClips).
PEOPLE_CLIPS = {
    "idle": "Idle", "walk": "Walking", "run_slow": "Slow Run", "run": "Fast Run", "jump": "Jump", "fall": "Falling",
    "land": "Falling To Landing", "land_hard": "Hard Landing", "swim": "Swimming", "tread": "Treading Water", "push": "Push",
    "punch": "Punching", "kick": "Mma Kick", "hurt": "Hit On Side Of Body", "cheer": "Cheering (1)", "look": "Looking Around",
    "fall_back": "Falling Down",   # Round 4: Dan's backward fall (the Ice Age arrival cutscene)
    "laugh": "Laughing", "hold_up": "Standing Idle 03 Examine",   # (Dan's picks, 2026-11-10)   # Round 6: Dan's relieved laugh and holding up the necklace (the Ice Age arrival)
    "surprised": "Surprised",   # Round 7: Dan and Abi see lamb Kira
}
# Round 7: the Wise Men's clips (SourceArt/Mixamo/wisemen/) and the watchman's (SourceArt/Mixamo/watchman/), shared people clips by game name
# (Clips/<game name>.fbx: Humanoid, any person can play them); also looked for in SourceArt/Mixamo/ itself
EXTRA_CLIPS = {"sit": ("wisemen", "Sitting Idle"), "argue": ("wisemen", "Arguing|Standing Arguing"), "map": ("wisemen", "Looking At Map|Looking Down"),
               "offer": ("wisemen", "Offering|Carrying"), "sad": ("wisemen", "Disappointed"), "yell": ("watchman", "Yelling|Yelling Out"),
               "doze": ("watchman", "Sleeping Idle"),
               # Round 8: Muscle Man's own clips (SourceArt/Mixamo/muscleman_medieval/), the knight's, the archer's, the wagon driver's
               "mm_flex": ("muscleman_medieval", "Mutant Flexing Muscles|Flexing|Flex"), "mm_taunt": ("muscleman_medieval", "Taunt|Pointing"),
               "mm_lift": ("muscleman_medieval", "Lifting Object|Lifting|Lifting Heavy|Picking Up"), "mm_push": ("muscleman_medieval", "Pushing"),
               "mm_heave": ("muscleman_medieval", "Pull Heavy Object|Pulling A Rope|Pulling|Tug Of War"), "mm_fall": ("muscleman_medieval", "Falling Back Death|Fall Flat"),
               "mm_cheer": ("muscleman_medieval", "Victory|Victory Idle"), "mm_boost": ("muscleman_medieval", "Toss|Throwing|Throw"),
               "mm_kneel": ("muscleman_medieval", "Kneeling|Kneel|Kneeling Down"),
               # the knight's own set replaces the shared clip of the same name on him (Clips/knight_<clip>, like the mummy's)
               "knight_idle": ("knight", "Sword And Shield Idle"), "knight_walk": ("knight", "Sword And Shield Walk"),
               "knight_run_slow": ("knight", "Sword And Shield Walk"), "knight_run": ("knight", "Sword And Shield Walk"),
               "knight_punch": ("knight", "Sword And Shield Slash"), "block": ("knight", "Sword And Shield Block"),
               "draw": ("archer", "Standing Draw Arrow"), "aim": ("archer", "Standing Aim Overdraw|Standing Aim Idle 01|Standing Aim Idle"),
               "shoot": ("archer", "Standing Aim Recoil"), "nervous": ("archer", "Nervously Look Around|Looking Around"),
               "dazed": ("wagon_driver", "Drunk Idle|Dizzy Idle|Stunned"), "limp": ("wagon_driver", "Injured Walk|Drunk Walk"),
               # Round 10: Muscle Man's struggling swim, getting up after the crash, his leap overboard
               "mm_struggle": ("muscleman_pirate", "Drowning|Struggling|Treading Water"), "mm_getup": ("muscleman_pirate", "Getting Up|Stand Up"),
               "mm_leap": ("muscleman_pirate", "Jumping Down|Jump"),
               # Round 11: holding the press up / lifting Dan to the clock; heaving the press off
               "mm_hold": ("muscleman_steampunk", "Overhead Hold|Overhead Squat|Overhead"), "mm_throw": ("muscleman_steampunk", "Heavy Throw|Overhead Squat")}   # Fix 11.2: Dan's Overhead Squat is both (CharacterImport trims it: the dip under the weight, the drive up that heaves it off)   # Fix 7.1: a|b = any of these Mixamo names (Dan saved them in the Mixamo root)
# Round 6: the mummy's own Mixamo clips (SourceArt/Mixamo/mummy/<Mixamo name>.fbx -> Clips/mummy_<clip>.fbx), played on it instead of
# the people's clip of the same name (CreatureAnimator)
MUMMY_CLIPS = {"idle": "Thriller Idle", "walk": "Zombie Walk", "run_slow": "Zombie Walk", "run": "Zombie Walk", "punch": "Zombie Attack",
               "hurt": "Zombie Reaction Hit", "cheer": "Zombie Scream"}


def textures(n):
    d = MESHY / n
    if (RIGGED / n / "base_color.png").exists(): d = RIGGED / n   # Round 5: maps baked onto a rebuilt closed mesh (closed_mesh.py)
    shutil.copy2(d / "base_color.png", OUT / f"{n}_albedo.png")
    if (d / "normal.png").exists(): shutil.copy2(d / "normal.png", OUT / f"{n}_normal.png")
    # Round 6, item 3: the plain colour of the hard parts (tusks, fangs, claws: submesh 1), from Tools/Blender/hard_parts.py
    hard = RIGGED / n / "hard.txt"
    if hard.exists(): shutil.copy2(hard, OUT / f"{n}_hard.txt")
    elif (OUT / f"{n}_hard.txt").exists(): (OUT / f"{n}_hard.txt").unlink()
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
            model = next((src / f for f in ("rigged_closed.fbx", "rigged_hairfix.fbx", "rigged.fbx") if (src / f).exists()), None)                 or next(iter(sorted(src.glob("T-Pose*.fbx"))), None)   # Mixamo names the T-pose download "T-Pose (2).fbx" etc.
        if not model:
            print(f"{n}: no rigged model yet ({MIX / n} or {RIGGED / n}), skipped"); continue
        shutil.copy2(model, OUT / f"{n}.fbx")
        textures(n)
        print(f"{n}: model {model.relative_to(ROOT)}")
    missing = [m for m in PEOPLE_CLIPS.values() if not (MIX / f"{m}.fbx").exists()]
    if missing: print("MISSING Mixamo clips:", ", ".join(missing))
    for game, m in PEOPLE_CLIPS.items():
        if (MIX / f"{m}.fbx").exists(): shutil.copy2(MIX / f"{m}.fbx", OUT / "Clips" / f"{game}.fbx")
    print(f"people's clips: {len(PEOPLE_CLIPS) - len(missing)} of {len(PEOPLE_CLIPS)}")
    vd = MIX / "violinist_iceage"
    got = [c for c, m in VIOLINIST_CLIPS.items() if (vd / f"{m}.fbx").exists()]
    for c in got: shutil.copy2(vd / f"{VIOLINIST_CLIPS[c]}.fbx", OUT / "Clips" / f"violinist_{c}.fbx")
    print(f"the Violinist's clips: {len(got)} of {len(VIOLINIST_CLIPS)}" + (f" (missing {', '.join(c for c in VIOLINIST_CLIPS if c not in got)})" if len(got) < len(VIOLINIST_CLIPS) else ""))
    def mummy_clip(m): return next((d / f"{m}.fbx" for d in (MIX / "mummy", MIX) if (d / f"{m}.fbx").exists()), None)   # Dan saved them with the shared clips
    got = [c for c, m in MUMMY_CLIPS.items() if mummy_clip(m)]
    for c in got: shutil.copy2(mummy_clip(MUMMY_CLIPS[c]), OUT / "Clips" / f"mummy_{c}.fbx")
    print(f"the mummy's clips: {len(got)} of {len(MUMMY_CLIPS)}")
    got = []
    for c, (folder, m) in EXTRA_CLIPS.items():
        src = next((p for n in m.split("|") for p in (MIX / folder / f"{n}.fbx", MIX / f"{n}.fbx") if p.exists()), None)
        if src: shutil.copy2(src, OUT / "Clips" / f"{c}.fbx"); got.append(c)
    print(f"extra clips (Wise Men, watchman, Muscle Man, knight, archer, driver): {len(got)} of {len(EXTRA_CLIPS)}" + (f" (missing {', '.join(c for c in EXTRA_CLIPS if c not in got)})" if len(got) < len(EXTRA_CLIPS) else ""))
    print("copied:", len(list(OUT.rglob("*.fbx"))), "fbx,", len(list(OUT.rglob("*.png"))), "png")


if __name__ == "__main__":
    main()
