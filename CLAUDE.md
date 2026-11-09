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
All from the repo root. Unity: `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`.
Heavy jobs one at a time, Below Normal, 3 worker threads: `python Tools/unity_batch.py <log name> [Unity args]`
(no window; log in `local/logs/<name>.log`; with no `-executeMethod` it just imports and compiles).
- **Build the exe:** Unity menu **Shards of Time > Build Executable**, or
  `python Tools/unity_batch.py build -executeMethod ShardsOfTime.PlayerBuild.BuildFromCommandLine [-buildVersion 0.1.0]`.
  Remakes `Assets/Scenes/Main.unity` (one `Game` object; everything else is built in code at start), makes missing
  data / material assets (`Editor/DataAssets.cs`) and missing graybox cutscenes (`Editor/CutsceneAuthoring.cs`), writes
  `Builds/ShardsOfTime/ShardsOfTime.exe` + `BUILD_INFO.txt`. About 2.5 min.
- **Play:** `Play Shards of Time.bat` (the exe) or `Play Shards of Time (launcher).bat` (a local launcher install in
  `Builds/LauncherLocal/`, remade by `python Tools/make_local_launcher.py` when the build is newer).
- **Save, settings, Player.log:** `%USERPROFILE%\AppData\LocalLow\Shards of Time\Shards of Time\` (`save.json`,
  `settings.json`, `display.json`). A check run saves into `<check dir>/save` and never touches them.
- **Check (muted, no window, Below Normal):** `python Tools/run_check.py local/round<N>/<name> [--only all|play|models|measure|perf|mute] [--extra "-steamDeck 1"] [--size 1280x800]`.
  Runs `ShardsOfTime.exe -batchmode -mute -exeCheck <dir>` (`Assets/Scripts/Checks/ExeCheck*.cs`): a virtual gamepad
  plays from the title screen through the real input actions. `play` = the whole playground as a player (about 4 min),
  `models` = Dan and Abi animated, both outfits (Round 3, about 3 min), `measure` = one measurement per Round 1 item (about 4 min), `perf` = 12 s of frame rate, `all` = the four. Writes
  `exe_check.txt` (ends with `RESULT:` and the measured audio output level) and screenshots. Check runs cap at 144 fps
  (vsync can't run off-screen; thousands of fps make the character controller skip moves).
- **Render a cutscene to MP4:** Unity menu **Shards of Time > Render Cutscene to Video** (`Recordings/<Name>.mp4`,
  1080p 30 fps, no audio), or with no window
  `python Tools/unity_batch.py render --gpu -executeMethod ShardsOfTime.CutsceneRender.RenderFromCommandLine -cutscene NecklaceWakes`
  (`--gpu`: no `-nographics`, no `-quit`; the render exits by itself). How to author: `Docs/CUTSCENES.md`.
- **Data (edit in the Inspector):** heroes `Assets/Resources/Data/Characters/{Dan,Abi,Kira}.asset` (CharacterStats);
  the playground's shards / requirement / what each shard needs `Assets/Resources/Data/Worlds/Playground.asset`
  (WorldData; positions match `World/Playground.cs`). The rule-2 check: `WorldData.RequirementReport`, logged by every
  check run.
- **Art pipeline tools (from Hostile Horizon):** `python Tools/meshy_generate.py [--only a,b] [--force]` (concepts in
  `SourceArt/Concepts/<name>.png`, key from `MESHY_API_KEY`), `python Tools/import_characters.py` (all six models + the people's shared clips into
  `Assets/Resources/Characters/`). Prompts and Mixamo status: `Docs/MODELS-NEEDED.md`.
  Blender: `F:\blender\blender.exe`; scripts run as `blender --background --factory-startup --python Tools/Blender/<x>.py -- <args>`:
  `meshy_cleanup.py <names>` (metres, feet at 0, front; Mixamo upload FBX; previews in `Data/screenshots/round2/models/`),
  `mixamo_check.py <model>=<rigged fbx>` (renders a rig on a few clips), `fix_hair_weights.py <model>=<rigged fbx>` (long
  hair off the arm bones -> `rigged_hairfix.fbx`), `kira_rig.py kira kira_sabertooth` (Kira's rig, 9 clips, LODs ->
  `SourceArt/Rigged/<name>/model.fbx`), `necklace.py` (-> `Assets/Resources/Characters/necklace.fbx`).
- **Title art / icons (placeholders):** `python Tools/make_title_art.py local/round1/<check>/00_key_art.png` writes
  `SourceArt/Title/Steam/ShardsOfTime-*.png` (launcher cover, Steam art), `Launcher/ShardsOfTime.ico` and
  `Assets/HandMade/Title/GameIcon.png`.
- **Launcher (Hostile Horizon's, renamed):** `powershell -File Tools/Build-Launcher.ps1` (and `-CoreOnly` for
  `LauncherChecks.exe`) -> `Builds/Launcher/ShardsOfTimeLauncher.exe`. Signing key
  `Builds/PublisherPrivate/launcher-signing.pem` (made once, key ID `61aa227f...`; never delete, print or replace it).
  Local self-check (no upload): `python Tools/launcher_selfcheck.py` (two signed local releases, update 1 to 2, start,
  ready handshake; the game runs hidden with `-batchmode -racerTestSave`).
- **Releases (every round; version `0.<round>.<fix>`, build `<round>*100+<fix>`).** First release (Round 1, build 100,
  tag `build-100`): `python Tools/Prepare-LauncherRelease.py --game Builds/ShardsOfTime --bundle-music <dir with placeholder.txt> --out Builds/LauncherRelease-100 --version 0.1.0 --build 100 --music-build 1 --notes "..."`
  then `python Tools/Publish-LauncherRelease.py Builds/LauncherRelease-100 --publish --confirm-repository danemoll-jpg/shards-of-time-releases`
  (gh: Racer's `Builds/PublisherTools`; a new draft may not be listed at once: re-run with `--resume-draft`).
  **Next rounds** publish the game alone: `python Tools/Prepare-ComponentUpdate.py game --source Builds/ShardsOfTime --previous-catalog Builds/LauncherRelease-<last>/assets/update-catalog.json --previous-manifest Builds/LauncherRelease-<last>/assets/game-manifest.json --out Builds/LauncherRelease-<build> --build <build> --version 0.<round>.<fix> --notes "..."`
  (tag `game-<build>`); keep the last release folder (it is the next one's "previous"; `cleanup_round.py` keeps the
  newest). **Starter package** (what a new player downloads):
  `python Tools/Package-LauncherStarter.py --game <installed game folder> --game-manifest <its signed game-manifest.json> --music <soundtrack folder> --music-manifest <its soundtrack-manifest.json> --out Builds/LauncherStarter-<n> --zip-name ShardsOfTime-Starter.zip --tag starter-<n> --title "..." --notes "..." --not-latest`,
  published the same way. Starter README: `Docs/Starter/README-LAUNCHER.md`. Check a fresh install against GitHub:
  extract the starter ZIP to a short folder inside the repo (e.g. `local\sc`: Code can't delete folders it makes in `C:\`), then
  `ShardsOfTimeLauncher.exe --auto-check --root <extracted ShardsOfTime> --test-save <dir> --evidence <dir>`.
- **Clean-up at the end of a round:** `python Tools/cleanup_round.py [--dry-run]`.

## Where things stand
**Round 3 delivered: 0.3.0 (build 300), 2026-11-09.** Dan and Abi are their real models in the game (player and followers),
animated from one shared Mixamo clip set, dressed per area (A home, B Ice Age); Dan's necklace sits on his chest in both
outfits; both cutscenes use the real models and were rendered again. Published: `game-300` (a Round 1 starter install
updated itself to it). Report and "For Dan to check": `TODO.md`.

Standing facts from Round 3:
- People: `Resources/Characters/{dan,abi}_{home,iceage}.fbx` (Humanoid, Unity mesh LODs) + `.mat`; clips shared in
  `Resources/Characters/Clips/<game name>.fbx` (Mixamo name map: `Tools/import_characters.py PEOPLE_CLIPS`; trims, loops,
  root settings: `Editor/CharacterImport.cs PeopleClips`). Import report at every build: `Resources/Data/Models.txt`.
- `CreatureAnimator` plays both Kira (Generic, her FBX's takes) and people (Humanoid): gait blend walk / run_slow / run at
  the rate of the measured clip speeds (x hips height / 0.88), upper-body layer for punch / cheer / hurt while moving.
  Heroes always animate (no culling: a batch-mode check only renders at screenshots, culling froze the bones).
- Outfits: `AreaData.outfit` -> `Hero.SetOutfit` (model `<name>_<outfit>`), applied in `Game.ApplyOutfits` like Kira's form.
  Humanoid models are turned to face +Z from their legs (`CharacterModel.HumanFront`); feet go down by the controller's
  skin width.
- Necklace: `NecklaceModel.Fit` measures each model's chest in its bind pose and parents to `Spine2` (3 cm off the clothes).
- Cutscene actors are the real models with clip tracks (`CutsceneAuthoring.Kit.Actor/Clips`); Menu: Shards of Time >
  Cutscenes > Rebuild Cutscenes.
- Exe check: `--only models` = Round 3's per-hero checks (also part of `all`).

**Round 2 delivered: 0.2.0 (build 200), 2026-11-09.** Kira's real models (home Scottish Fold, Ice Age sabertooth) with
her own Blender rig and nine clips are in the game; she changes form per area (Area B: sabertooth, claws through an ice
wall to shard B06). Dan wears the necklace model. Dan rigged all four people in Mixamo; they go in with Round 3.
Published: `game-200` on `danemoll-jpg/shards-of-time-releases` (a Round 1 starter install updated itself to it).
Report and "For Dan to check": `TODO.md`.

Standing facts from Round 2:
- Characters in game: `Assets/Resources/Characters/<model>.fbx` + `<model>_albedo/_normal.png` + `<model>.mat` (URP Lit
  made by `Editor/CharacterImport.cs`, which also sets the import settings and runs the Kira lock check at every build:
  `Resources/Data/KiraLock.txt`, logged by the exe check). Blender models face -Y; in Unity they get a 180 deg turn
  (`CharacterModel.BlenderFront`). Meshy's metallic map is noise: not used.
- Hero bodies: `Body/Graybox` (Round 1 shapes, the fallback) + `Body/Model <name>` when a model exists. Clips play through
  `CreatureAnimator` (Playables, no Animator Controller asset): loops idle/walk/run/fall/swim, one-shots jump/land/attack/hurt.
- Kira's forms: `Resources/Data/KiraForms/<id>.asset` (model, abilities); `AreaData.kiraForm` per area; `Game.ApplyKiraForm`
  swaps on arrival. Abilities from a form: `Hero.HasAbility` (e.g. `Abilities.ClawIce`). `WorldProgress.opened` saves
  broken ice walls. `DataAssets.Upgrade` adds new default data (areas' forms, new shards) to an existing world asset.
- Necklace: `NecklaceModel` (socket: a bone ending in `Spine2`, else the capsule); `Hero.NecklaceGem` is the shard.
- Art in git: Meshy outputs, Mixamo downloads, Kira's rig FBX; ignored: Meshy raw FBX / .blend, rejected `*_v1`, OBJ zips,
  rig .blend files (the scripts remake them).

**Round 1 delivered: 0.1.0 (build 100), 2026-11-09.** A graybox playground (Area A + Area B) with every core
system: one motor (Ground / Air / Swim / LowGravity), Cinemachine camera, Dan / Abi / Kira switching with
followers, shared hearts, enemy, hazards, checkpoints, shards, necklace, story shard, portal, Abi's double jump,
save, title / pause / settings, and two Timeline cutscenes that also render to MP4. Published on
`danemoll-jpg/shards-of-time-releases`: `build-100` (Latest: the launcher's catalog) and `starter-100`
(`ShardsOfTime-Starter.zip`, what a new install downloads). Report and "For Dan to check": `TODO.md`.

Standing facts from Round 1:
- The game's one scene holds a `Game` object; the world, party, camera and UI are built in code at start
  (`Core/Game.cs`, `World/Playground.cs`). Shard data and hero stats are ScriptableObjects in `Resources/Data/`.
- Layers: 8 Player, 9 Follower, 10 Enemy (followers never collide with the player); 4 Water; 2 for trigger volumes.
- Kira is 0.5 m; low gravity is x0.3 (jumps 3.3x higher).
- Art: Dan's six concept sheets are in `SourceArt/Concepts/` (`dan_home`, `dan_iceage`, `abi_home`, `abi_iceage`,
  `kira`, `kira_sabertooth`). Abi: wavy brown hair, green eyes; Ice Age outfits barefoot. Meshy is the next step
  (Round 2). The red-pyjama woman with the Santa hat is Abi's original film outfit; the green Santa pyjamas are fine for the game (Dan, 2026-11-09).
- Python file edits on this PC: always pass `encoding='utf-8'` (the default is cp1252).
- A check on a busy PC: check runs cap a frame at 1/30 s and wait in game time, so OBS / Chrome load doesn't
  change the result (players: 0.1 s cap).
