# Project Context & Rules — Shards of Time

A colorful 3D action-adventure platformer / collectathon (Banjo-Kazooie,
Super Mario Odyssey, Spyro) based on Dan's animated short film *The Shards of
Time*. Dan, Abi and their cat Kira chase the shards of a broken Christmas
ornament through eight eras, with Muscle Man joining in the Medieval Kingdom.

**Always check `TODO.md` for the current round before starting work.** TODO.md
is the source of truth for what to build; this file is the standing context.

Design sources (read these when a round touches story, worlds or characters):
- `Docs/Design/design-decisions.md`: everything decided with Dan, plus the
  per-world canon table. **Wins over the other two when they disagree.**
- `Docs/Design/film-synopsis.md`: the film, act by act.
- `Docs/Design/concept-summary.md`: Dan's original game concept.

## Tech Stack
- **Unity 6000.6.1f1**, URP, Input System only (no old Input Manager). Same
  versions as Chastain Rush / Hostile Horizon so code can be ported.
  Packages: Cinemachine, Timeline, Unity Recorder, ProBuilder (graybox).
- **Toolchain (paid licences, use freely):** Blender (scripted, `Tools/Blender/*.py`),
  **Meshy** API for characters/creatures (image-to-3D, key from the
  `MESHY_API_KEY` environment variable, never written to the repo, logs or
  chat), **Mixamo** for rigging/animation, **Suno** for long music,
  **ElevenLabs** for sound effects and short music/stings.
- **Platforms:** Windows PC + Steam Deck (Proton). No VR planned.
- **Dan's PC:** Dell XPS 8930, i7-9700, GTX 1660 Ti 6 GB, 64 GB RAM, Win 11.
  Target 60 fps at 1080p there; 40+ fps on the Steam Deck.
- **Controls:** gamepad first (Xbox layout, Steam Deck built-in controls),
  keyboard + mouse as well. All input through one Input Actions asset;
  prompts name only the buttons of the device in use.
- **Repos:** code `C:\Users\danmo\shards_of_time`
  (github.com/danemoll-jpg/shards_of_time). Releases / update catalog:
  `danemoll-jpg/shards-of-time-releases` (local clone
  `C:\Users\danmo\shards-of-time-releases`).
- **Reference projects (read-only, copy from them, never edit them):**
  `C:\Users\danmo\Hostile-Horizon` (newest: TestRun mute/measure, `run_check.py`,
  `meshy_generate.py`, `import_characters.py`, ScreenSetting, cleanup) and
  `C:\Users\danmo\chastain_rush` (launcher origin, Steam Deck setting).

## Standing design rules
1. **The film is the backbone, not a script.** Its eight worlds, story shards,
   Kira forms, key moments and ending stay; everything around them is free to
   expand or change for gameplay (`design-decisions.md`).
2. **The main story is always completable without full collection,** and no
   portal's shard requirement may need an ability or companion the player
   doesn't have yet at that point in the story. Every world's level data lists
   which shards are reachable with which abilities, and a check proves the
   requirement is met with the abilities available on first visit.
3. **Shards:** many small shards per world (a shared, never-spent total) plus
   one colored story shard per world. Reaching the world's small-shard
   requirement wakes Dan's necklace, which leads to the story shard; the story
   shard opens the portal. Tomorrowland has no story shard: the rocket plays
   that role.
4. **All characters are visible.** The two inactive heroes (and Muscle Man from
   Medieval on) follow the player. A follower that falls behind, falls off, or
   can't make a move warps in just behind the player out of camera view. No
   per-move pathfinding. Followers never block the player.
5. **Shared health pool:** 3 hearts at start, max 10 via hidden upgrades.
6. **Moves stay useful across worlds,** never a key for a single door.
7. **Movement has modes from day one:** ground, air, swim (surface + dive) and
   low gravity are modes of one character motor, so Pirate Seas and the Moon
   plug in without a rewrite.
8. **Cutscenes are in-engine and double as a film pipeline.** Every cutscene is
   a Timeline + Cinemachine asset using the game's own models and rigs, and can
   be rendered to a video file (Unity Recorder, 1080p MP4) from an editor menu.
   Dan wants to make future films this way, so the tooling stays reusable and
   documented in `Docs/CUTSCENES.md`.
9. **Kira lock (every form, every asset, every prompt):** Scottish Fold, folded
   ears that never rise or change shape, solid gray fur with no white anywhere,
   golden-amber eyes. Each transformation keeps these.
10. **Look:** stylized, colorful, Pixar-like 3D. Art direction comes from Dan's
    own film art in `SourceArt/Reference/` (see its README).
11. **Original assets only:** nothing from Banjo-Kazooie, Mario or any other
    game. The inspiration is the genre, not the content.

## Code Style & Architecture
- **Systems, not scripted one-offs:** character switching, motor, camera,
  combat, health, collectibles, abilities, companions, portals, save,
  cutscenes. Worlds are data on top of them.
- Character stats, abilities and world data as ScriptableObjects.
- **World scale is real:** 1 unit = 1 metre.
- **KISS.** The smallest change that does what the round asks.
- **Port before writing.** If Hostile Horizon / Chastain Rush has a finished
  system, copy and adapt it, and list what came with it in the round report.
- **Credits screen** lists tools and third-party code licences.

## Character / creature art pipeline (every round that needs models)
1. Code writes the image prompts in `Docs/MODELS-NEEDED.md` (full body, A-pose,
   front view, plain light background, no cropping, no props; design note,
   file name to save as, animation list), using Dan's film references in
   `SourceArt/Reference/`. Code **stops** and tells Dan.
2. Dan has the concept images made and saves them where the doc says.
3. Code generates the models with the Meshy API, cleans them up in Blender
   (scale, origin, facing, LODs), renders previews and **stops** to show Dan.
4. Dan auto-rigs them in Mixamo and downloads the listed animations (real
   Mixamo search names, what each is for). **Mixamo only rigs two-legged
   humanoids.** Kira (every form) and other animals (camel, sabertooth, etc.)
   take another path: Code first checks whether the Meshy API can rig and
   animate quadrupeds; if not, Code builds a quadruped rig in Blender by
   script and makes their animation set (idle, walk, run, jump, fall, land,
   swim, attack, hurt). Kira's folded ears get no ear bones that could raise.
5. Code imports them (Humanoid rig for people, Generic for animals), wires up
   the animations, replaces the placeholders and keeps the old model as
   fallback.

**Concept image rules for Meshy (Dan's film art already has the right style:
Pixar-like, plain green background):** at least 1024 px tall; full body with
feet and paws in frame; front view for people (side or 3/4 for animals, plus
a second view if possible); people in an A-pose (arms ~45° out, hands not
touching the body, feet shoulder-width apart, so limbs don't fuse); mouth
closed and neutral face; no glowing effects or held props. Dan's necklace is a
separate model (its shard glows and pulses in game), so concept images of Dan
are made without it.

## Launcher / auto-update
Same launcher as Chastain Rush / Hostile Horizon, renamed:
- `Launcher/` (C++), `Assets/Scripts/LauncherBridge.cs`, and in `Tools/`:
  `Build-Launcher.ps1`, `New-LauncherKey.py`, `Prepare-LauncherRelease.py`,
  `Prepare-ComponentUpdate.py`, `Package-LauncherStarter.py`,
  `Publish-LauncherRelease.py`, `make_local_launcher.py`, plus
  `Docs/Starter/README-LAUNCHER.md` rewritten for Shards of Time.
- Exe name `ShardsOfTimeLauncher.exe`; catalog repo
  `danemoll-jpg/shards-of-time-releases`.
- Signing key: made **once** with `python Tools/New-LauncherKey.py` into
  `Builds/PublisherPrivate/` (git-ignored). Never delete, print or replace it.
- **Publish every round** (default for new games, Dan 2026-11-09): every round
  ends with a release the launcher picks up, after the round's checks pass.
  Version `0.<round>.<fix>`. A follow-up fix is published too.

## Process (Dan's standing rules)
- **Commit before starting a round** (safety checkpoint); commit + push at the end.
- **Planning chat writes the round** into `TODO.md` / `CLAUDE.md`; Code builds it.
- **Code updates `TODO.md` and `CLAUDE.md` at the end of every round:** per-item
  status (done / not done / deferred, and why; never silently skip an item),
  standing facts that changed, and "Where things stand" below.
- **Keep `TODO.md` small:** standing decisions, current round, queued rounds,
  last three deliveries, technical notes. Older deliveries move word for word
  to `TODO-ARCHIVE.md` (grep it, don't read it whole).
- **Short results:** about 20 lines per round: one line per item (what changed
  + its one check), decisions made, "For Dan to check". Evidence in
  `Data/screenshots/round<N>/`.
- **Check it as Dan plays it:** in the built Windows exe, from where a player
  starts, with the real input path; screenshots from the player's view that
  Code actually looks at and describes. "Checked by script" alone is not done.
- **Targeted testing:** one check per change, where it changed. Dan plays the rest.
- **Tuning limit:** if a number the round asks for is not hit after two
  attempts, stop, report the numbers, Dan decides.
- **Testing on Dan's PC: always muted, never steal focus.**
  1. Every test run is muted from the first frame by `-mute` (master volume /
     `AudioListener.volume` 0 before any sound starts). Every check reports the
     measured audio output level, and it must be 0. Unmute only for a real sound
     check, as briefly as possible; say which step played sound and how long.
  2. Test windows never pop up or take focus. Prefer batch mode with off-screen
     rendering; otherwise minimized / not activated with run-in-background on.
     If a test truly needs a visible or focused window, stop and ask Dan first.
  Port Hostile Horizon's `Core/TestRun.cs` and `Tools/run_check.py` for this.
- **One heavy job at a time** (Unity build, exe check) at Below Normal priority
  with fewer worker threads.
- **Clean up the disk at the end of every round** (`Tools/cleanup_round.py`)
  and give disk use before/after in one line.

## Commands
(Code fills this in during Round 1: build, play, check, render cutscene,
publish, save/settings locations.)

## Where things stand
Nothing built yet. Round 1 is the project setup + graybox playground
(`TODO.md`).
