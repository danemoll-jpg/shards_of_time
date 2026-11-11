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
  chat), **Mixamo** for rigging/animation, **ElevenLabs** for sound effects and short music (stings, the Violinist's tune).
- **Level music is Dan's (standing rule, Dan 2026-11-09).** For every world /
  level song (and other full-length tracks: title, vortex, credits), Code never
  generates the music, stand-ins included. Code writes a Suno prompt in
  `Docs/MUSIC.md` (style text, length, instrumental, where it plays, loop
  needs) and the path to save it as; Dan makes it in Suno, picks the take he
  likes and saves it there. Until then that spot plays silence. **Short music
  stays with Code via ElevenLabs:** stings, jingles and short tunes such as
  the Violinist's tune, plus all sound effects.
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
   threshold is met with the shards available on arrival: every earlier world
   with the moves the party has by then, plus this world's first-visit shards
   (`WorldData.RequirementReport`; Egypt: Ice Age 43 + Egypt 50 = 93 >= 90,
   and 50 >= 20 from Egypt itself).
3. **Shards are universal (Dan, 2026-11-10; Fix 6.1), plus 20 from each world
   (Dan, 2026-11-11; Fix 8.1):** many small shards per world, but the player
   has **one total** across all worlds (every small shard ever collected,
   never spent), plus one colored story shard per world. Each world's necklace
   wakes when **both** are true: the **total** reaches that world's cumulative
   threshold (about **75%** of all shards that exist up to it: Ice Age 40,
   Egypt 90, Bethlehem 135, Medieval 180; the Ice Age stays at 40 because only
   43 of its 60 are reachable on arrival), **and** at least **20 shards from
   this world** (`AreaData.fromHere`) are collected. Extra shards from any world
   still count toward later thresholds; a world that is already awake never
   relocks. Rule 2's report checks both parts. The necklace leads to the story
   shard; the story shard opens the vortex. HUD: the total, "Next: 180 (this
   world: 12 / 20)" under it; per-world counts in the pause menu. Tomorrowland
   has no story shard: the rocket plays that role.
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
   **When Dan's film clip of a scene exists** (`SourceArt/Reference/Clips/` or a
   world folder), the cutscene follows its staging: shot order, angles, timing.
9. **Kira lock (every form, every asset, every prompt):** Scottish Fold, folded
   ears that never rise or change shape, solid gray fur with no white anywhere,
   golden-amber eyes. Each transformation keeps these.
10. **Look:** stylized, colorful, Pixar-like 3D. Art direction comes from Dan's
    own film art in `SourceArt/Reference/` (see its README).
11. **Original assets only:** nothing from Banjo-Kazooie, Mario or any other
    game. The inspiration is the genre, not the content.
12. **Never stuck (Dan, 2026-11-10; Fix 7.2):** every pit, well, pool, hole,
    crawl space and enclosed area in every world has a way out with the moves
    the player has at that point, or can't be entered without them. Each
    world's data lists them (`WorldData.lowSpots`, defaults in
    `WorldData.LowSpotDefaults`: add every new one there), and the exe check
    `stuck` drops each hero into every listed spot and proves the bot gets out
    within 20 s (it also fails any water volume that isn't on the list). Run
    it for every new world. Safety net: Pause > "Return to checkpoint", and a
    "Stuck?" hint after ~15 s of pushing without getting anywhere.

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
- **Don't end your turn mid-round (Dan, 2026-11-11).** A stop for Dan (Mixamo, an image) is a
  message, then keep working in the same turn on everything that doesn't need him. Never write
  "I'll carry on" and then end the turn: Dan goes to bed trusting it. End your turn only when the
  round is published, or when nothing at all can continue without him (say so plainly). When his
  files arrive, import them and keep going to the end of the round without stopping again.
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
  **This covers every process Code starts, not just exe checks (Dan,
  2026-11-09, Round 5: the cutscene render played sound and he noticed it while
  working).** Unity editor batch jobs (builds, cutscene renders, imports) mute
  the editor's audio (`EditorUtility.audioMasterMute = true` and
  `AudioListener.volume = 0` before Play mode) and report the level like a
  check; Blender always `-b`; the launcher self-check and any helper exe start
  hidden / minimized without activation; no media players. Every tool that
  starts a process goes through one helper (`Tools/quiet.py`: hidden window,
  no focus, Below Normal). If something can't be made silent and windowless,
  stop and ask Dan first. **Cutscene videos are rendered from the built exe**
  (batch mode, off-screen, fixed 30 fps frames encoded with ffmpeg), never by
  the Unity editor with graphics: that run took Dan's focus and played sound.
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
- **Check (muted, no window, Below Normal):** `python Tools/run_check.py local/round<N>/<name> [--only all|play|fixes|perf|playground|models|measure|perfplayground|diag|mute] [--extra "-steamDeck 1"] [--size 1280x800]`.
  Runs `ShardsOfTime.exe -batchmode -mute -exeCheck <dir>` (`Assets/Scripts/Checks/ExeCheck*.cs`): a virtual gamepad
  plays from the title screen through the real input actions. `play` = the Ice Age from New game to the Egypt path as a
  player (Round 4, about 9 min), `fixes` = see-through pixels of Kira's forms and Abi's outfits in the game's renderer +
  running shots, `perf` = the Ice Age's frame rate with the view drawn every frame (3 places), `all` = those three (about 13 min;
  use `--minutes 40`). Round 6: `full` = `play` + the Egypt route + Egypt's frame rate (about 35 min, `--minutes 60`); `egypt` (Egypt
  alone), `tomb`, `camel`, `perfegypt`, `straight`, `underice`, `heads`, `violinist`, `zones`, `tour`, `film`. Round 7: `bethlehem` (from the vortex with the Ice Age and Egypt done, about 20 min), `fullbeth` (`play` + the Egypt route + the Bethlehem route + its frame rate, about 60 min, `--minutes 100`), `perfbeth`, `bethdiag` (teleport looks). Fix 7.2: `fix72`, `stuck`, `well` (never stuck, rule 12; `stuck` now sweeps the Medieval Kingdom too). Round 8: `medieval` (from the vortex with three worlds done, about 25 min, `--minutes 45`), `perfmed`, `meddiag` (teleport looks), `stuckmed` (the Medieval never-stuck sweep alone). Fix 8.1: `fix81` (all four worlds: feet vs the visible floor, invisible colliders, every moving platform with every hero, the bot walking to every shard, the necklace rule; about 45 min, `--minutes 90`), `fix81med` (the Medieval alone, ~12 min), `floors`, `platforms`, `reach`, `rule81`. **Run `fix81` (or its parts) for every new world.** Fix 6.1: `fix61` (the lyre close-ups + 0.6.0 saves; needs
  `--extra "-oldSave <repo>/Data/fixtures/save_0.6.0_after_egypt.json"`). The test area (Round 1-3 checks): `playground`, `models`, `measure`, `perfplayground`. `diag` = quick
  teleport probes (edit `Diag()` as needed). Writes
  `exe_check.txt` (ends with `RESULT:` and the measured audio output level) and screenshots. Check runs cap at 144 fps
  (vsync can't run off-screen; thousands of fps make the character controller skip moves).
- **Render a cutscene to MP4 (Round 5: from the built exe, never the editor):** `python Tools/render_cutscene.py <Name> [...]`
  (or the menu **Shards of Time > Render Cutscene to Video**, which runs it): the exe in batch mode, muted, frames off-screen at a
  fixed 30 fps, ffmpeg -> `Recordings/<Name>.mp4` (1080p, no audio); prints the audio level and the window watcher's report.
  Uses the last build. How to author: `Docs/CUTSCENES.md`.
- **Every process goes through `Tools/quiet.py`** (Round 5): a desktop of its own that is never shown (nothing can appear or take
  focus), hidden, Below Normal; Blender only with `-b`, Unity only with `-batchmode`. Shell use:
  `python Tools/quiet.py --log <file> -- <exe> <args>` (prints the watcher's report). `unity_batch.py`, `run_check.py`,
  `launcher_selfcheck.py`, `render_cutscene.py` and the release tools all use it. The editor mutes itself in batch mode
  (`Editor/QuietEditor.cs`).
- **Data (edit in the Inspector):** heroes `Assets/Resources/Data/Characters/{Dan,Abi,Kira}.asset` (CharacterStats);
  the Ice Age's shards / requirement / what each needs `Assets/Resources/Data/Worlds/IceAge.asset` (made once from
  `WorldData.IceAgeDefaults()`; delete it to remake it after changing the defaults); the playground's shards / requirement / what each shard needs `Assets/Resources/Data/Worlds/Playground.asset`
  (WorldData; positions match `World/Playground.cs`). The rule-2 check: `WorldData.RequirementReport`, logged by every
  check run.
  Egypt: `Assets/Resources/Data/Worlds/Egypt.asset` (made once from `WorldData.EgyptDefaults()`; delete it to remake it). Bethlehem:
  `Bethlehem.asset` the same way (`WorldData.BethlehemDefaults()`).
- **Art pipeline tools (from Hostile Horizon):** `python Tools/meshy_generate.py [--only a,b] [--force]` (concepts in
  `SourceArt/Concepts/<name>.png`, key from `MESHY_API_KEY`), `python Tools/import_characters.py` (all six models + the people's shared clips into
  `Assets/Resources/Characters/`). Prompts and Mixamo status: `Docs/MODELS-NEEDED.md`.
  Blender: `F:\blender\blender.exe`; scripts run as `blender --background --factory-startup --python Tools/Blender/<x>.py -- <args>`:
  `meshy_cleanup.py <names>` (metres, feet at 0, front; Mixamo upload FBX; previews in `Data/screenshots/round2/models/`),
  `mixamo_check.py <model>=<rigged fbx>` (renders a rig on a few clips), `fix_hair_weights.py <model>=<rigged fbx>` (long
  hair off the arm bones -> `rigged_hairfix.fbx`), `kira_rig.py [shots=<dir>] kira kira_sabertooth` (Kira's rig, 9 clips, LODs, face close-ups ->
  `SourceArt/Rigged/<name>/model.fbx`), `necklace.py` (-> `Assets/Resources/Characters/necklace.fbx`).
  Round 4: `iceage_world.py` (the valley's terrain, volcanoes, props -> `Assets/Resources/World/iceage_*.fbx`; the layout numbers
  match `World/IceAge.cs`), `home_room.py` (the living room + the ornament -> `World/home_room.fbx`), `mesh_holes.py [fix=1]
  [out=<dir>] <label>=<fbx> ...` (see-through report + turntable), `mesh_fix.py` (gives faces seen from behind a reversed twin;
  used by `kira_rig.py` and `fix_hair_weights.py`).
- **Sounds and music:** `python Tools/make_sounds.py [--only a,b] [--force]` (ElevenLabs, key from `ELEVENLABS_API_KEY`;
  prompts in `Docs/SOUNDS.md`; effects -> `Assets/Resources/Sounds/`, the music stand-in -> `Assets/Resources/Music/iceage.mp3`).
  Dan's Suno theme: `Docs/MUSIC.md`.
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
  (tag `game-<build>`; Fix 8.1: `game-801`, previous `Builds/LauncherRelease-801`); keep the last release folder (it is the next one's "previous"; `cleanup_round.py` keeps the
  newest). **Starter package** (what a new player downloads):
  `python Tools/Package-LauncherStarter.py --game <installed game folder> --game-manifest <its signed game-manifest.json> --music <soundtrack folder> --music-manifest <its soundtrack-manifest.json> --out Builds/LauncherStarter-<n> --zip-name ShardsOfTime-Starter.zip --tag starter-<n> --title "..." --notes "..." --not-latest`,
  published the same way. Starter README: `Docs/Starter/README-LAUNCHER.md`. Check a fresh install against GitHub:
  extract the starter ZIP to a short folder inside the repo (e.g. `local\sc`: Code can't delete folders it makes in `C:\`), then
  `ShardsOfTimeLauncher.exe --auto-check --root <extracted ShardsOfTime> --test-save <dir> --evidence <dir>`.
- **Clean-up at the end of a round:** `python Tools/cleanup_round.py [--dry-run]`.

## Where things stand
**Fix 8.1 delivered: 0.8.1 (build 801), 2026-11-11.** The great hall's floor is where it's drawn (the keep's plinth was a solid
block over the hall: the wading), the drawbridge is solid and moves with its collider, no invisible walls by the village (or
anywhere in the Medieval), every shard in every world reached by the bot, and the necklace's new rule (75 % of all shards so far AND
20 from the world). Published: `game-801`. Report and "For Dan to check": `TODO.md`.

Standing facts from Fix 8.1:
- Necklace rule: `AreaData.fromHere` (20; the playground 0) with `requirement` (Ice Age 40, Egypt 90, Bethlehem 135, Medieval 180);
  `Game.Ready(area)` (both parts), `Game.RuleText` / `Game.I.Progress` for hints; HUD "Next: N (this world: n / 20)";
  `RequirementReport` checks both. New world: threshold ~75 % of all shards up to it, and >= 20 of its own reachable on arrival.
- `ExeCheck.Fix81.cs`: a probe collider (layer 31, collides with nothing, off while the bot moves) from every visible mesh. Floors fail
  only where the visible surface over the feet is a floor part (names in `FloorLike`); props whose look reaches past their collider are
  listed. Invisible-collider scan uses each collider's own surface (`ClosestPoint`, convex shapes only). Enemies are switched off during
  the probes (arrows come and go).
- Medieval: the drawbridge's deck collider is a child of the drawbridge, placed on its seen planks at build time (`Medieval.Drawbridge`);
  round towers are convex cylinders (`Medieval.Round`, r 5, from the ground up); colliders for the keep steps, feast table and benches,
  hearth, arcade lip, north wall parapets, the hall's north gallery rail; the village alley and chimney walls are drawn boxes; bramble
  colliders are unturned children ("Bramble block"). `medieval_world.py`: the keep's plinth is a ring outside its walls.
- Lesson: a collider line ended up inside a `//` comment twice in Round 8 (the drawbridge, the feast table); `grep -nE '//.*(Solid|Slab|Box|Wall|Stair)\('`
  finds that. `AddComponent<SphereCollider>()` auto-sizes at once: find the ground before adding it.

**Round 8 delivered: 0.8.0 (build 800), 2026-11-11.** The Medieval Kingdom, the fourth world: the hay-wagon arrival (the blast), the
courtyard and the stone, the walls and towers, the great hall (chandeliers), the dungeon, the moat, the tournament field (the high
striker), the village; dragon Kira (glide, fire puff); Dan's charged punch from the bard; knights, archers, rats; Muscle Man the rival,
then `TheStone` and Muscle Man the companion (boulders in every world, portcullises, boosts). 60 shards / 150 wake the necklace.
Published: `game-800`. Report and "For Dan to check": `TODO.md`.

Standing facts from Round 8:
- Worlds: `Game.InMedieval`; `World/Medieval.cs` builds it (the castle island x -36..36, z -6..90: courtyard x -30..30, z 0..63, the
  stone at (0, 34), the keep / great hall x -15..15, z 66..84; the moat ring out to x +-48, z -18..102, water top -1.2, its deep pit in
  the NW corner; the field x 40..130, z -125..-40; the village x -140..-30, z -135..-40; the dungeon its own space at x 400). Its pieces
  in `World/MedievalWorld.cs` (FireTarget / FireGroup, PunchTarget / HighStriker, RivalMuscleMan, CellDoor / CellKey, StoneHum).
  Ground and props: `Tools/Blender/medieval_world.py` (-> `medieval_terrain.fbx`, `medieval_props.fbx`; prop fronts face -z; Torch
  leans out to -z; Chandelier's origin is its hook, the ring 3 m below; Drawbridge hinges at its origin). `Medieval.asset` is made
  once from `WorldData.MedievalDefaults()` (delete it to remake). `WorldData.StoryOrder` = Ice Age, Egypt, Bethlehem, Medieval.
- Vortex: six paths 24 deg apart (Ice Age -60, Egypt -36, Bethlehem -12, Medieval 12, Pirate Seas 36 "to be continued" until the
  purple shard, Home 60); checks read the angles from `Vortex.Paths` (`ExeCheck.Yaw`). The stone's shard plays `TheStone`
  (`Game.StoneFinale`), adds `Abilities.MuscleMan` and the companion, then the vortex; `Medieval.Gate` by the stone is the way back.
  Only Dan can take the stone's shard (`Pickup`).
- Muscle Man: `Party.Companion` (a `Hero` "MuscleMan", not in `Heroes`, Follower slot 2; `Party.Everyone`); stats
  `Data/Characters/MuscleMan.asset`; model `muscleman_medieval` in every world. His jobs: `World/MuscleJobs.cs` (`MuscleJob`: X near
  one; `Boulder` (every world, ids `IA-boulder-grotto`, `IA-boulder-lake`, `EG-block-quarry`, `BE-boulder-cave`, `ME-boulder-yard`),
  `Portcullis`, `BoostSpot` with `landAt` (`CharacterMotor.Throw` keeps its sideways speed); `OpenNow()` for checks). The rival before
  he joins: `RivalMuscleMan` (spots courtyard / field / hall; `Boulder.Tried` makes him laugh).
- Moves: `Abilities.Glide`, `FirePuff` (dragon Kira, `KiraForm.Dragon`), `ChargedPunch` (Dan; `Abilities.Learns` lists each hero's
  learnable moves; `Party.ChargeTime` 0.55 s; `Hero.ChargedPunch`, `RockWall.Punch(by, charged)`). `CharacterMotor.Gliding`,
  `GlideFall` 1.1 m/s, `GlideSpeed` 7.8.
- Enemies `Combat/EnemyMedieval.cs`: Knight (shield up at the front, `HitCharged`), Archer (`Arrow`), Rat (`Scare`; runs off after a nip).
- People clips added to `CreatureAnimator.PeopleClips`: `mm_*` (Muscle Man's own), block, draw, aim, shoot, nervous, dazed, limp; the
  knight's own set as `knight_<clip>` (like the mummy's). `import_characters.py EXTRA_CLIPS` has Dan's Mixamo names.
- The Violinist's lute: `instruments.py` "Lute" (the lyre's markers), held across his body by IK (`Violinist.LateUpdate`), the
  lyre's clip; `violinist_medieval` is rigged without his cape (`Tools/Blender/split_cape.py`), the cape put back by
  `Tools/Blender/attach_cape.py` (-> `rigged.fbx`).
- Kira lock: the dragon's eyes are a deeper amber (the eye test takes r - g < 72 and 0.02% for that form).
- Never stuck: `LowSpot.opens` also names a cracked wall or a Muscle Man job the check opens first; `TaughtSoFar` includes the
  charged punch and Muscle Man. Cutscenes `MedievalArrival`, `TheStone`, `NecklaceWakesMedieval`, `ViolinistTeachesMedieval`;
  `CutsceneAuthoring.Version` 18; `VortexRings(..., colors:)`.
- `Follower`: the "not caught up" distance grows with the place in line (Muscle Man walks further back).

**Fix 7.2 delivered: 0.7.2 (build 702), 2026-11-11.** Never stuck: Bethlehem's well has a rope out (and was rebuilt: it sat in a
dry crater); every world's pits, pools and enclosed spots are listed and checked (rule 12); Pause > "Return to checkpoint" and a
"Stuck?" hint. Published: `game-702`. Report and "For Dan to check": `TODO.md`.

Standing facts from Fix 7.2:
- `WorldData.lowSpots` (`LowSpot`: drop, via, exit, radius, needs, heroes, opens, checkpoint); defaults in
  `WorldData.LowSpotDefaults(world)`; `DataAssets` adds missing ones to existing world assets by id. Exe check modes: `stuck` (the
  sweep, all three worlds, ~12 min), `well` (Bethlehem's well + the reset), `fix72` (both, ~15 min, `--minutes 40`).
- `WellExit` (World/WellExit.cs): a round water shaft's way out; in it Jump (any depth) or swimming into the wall (not with the deep
  dive) climbs straight up and over (`CharacterMotor.ClimbOut(to, straightUp: true)`, `WellClimbs`; climbs are logged in Player.log).
- Bethlehem's well: `wy` from the flat ground at (108, 82); the stone ring solid from -15 to wy + 1.8; water to the bottom; a stone
  apron x 98.5..117.5, z 62.5..81.5 over the terrain's hole. The Ice Age cave's floor slabs go down to -4.6 (no hollow).
- `Game.ReturnToCheckpoint` (pause row, not while `Busy`), `Game.CheckpointReturns`; the stuck hint `Game.WatchStuck`
  (`StuckSeconds` 15 of pushing the stick within `StuckRange` 4 m), `Game.StuckHints`.
- The well's 1.8 m rim: only Abi clears it with a jump from the ground.

**Fix 7.1 delivered: 0.7.1 (build 701), 2026-11-10.** Bethlehem's people are Dan's Mixamo rigs (Dan, Abi, the Violinist in their
Bethlehem models, the three Wise Men, the watchman) with his eight clips; a new side-profile guard dog (head -1 deg). Nothing waits
on Dan for Bethlehem. Published: `game-701`. Report and "For Dan to check": `TODO.md`.

Standing facts from Fix 7.1:
- `import_characters.py EXTRA_CLIPS` takes alternatives "a|b" (Dan's names: Carrying = offer, Looking Down = map, Standing Arguing
  = argue, Yelling Out = yell).
- Caravan riders: with a real model + the `sit` clip they sit on the camel's own saddle (no cream block); `CutsceneAuthoring.Version` 17.
- Bethlehem frame rate with the real people: 108 fps (1080p), 146 (Steam Deck setting).

**Round 7 delivered: 0.7.0 (build 700), 2026-11-10.** Bethlehem, the third world: the roof-crash arrival into the barn, the
farmyard, the hills (camps, flocks, goats, the Violinist with his harp), the village (night watchmen with lanterns, guard dogs, the
inn's "no room"), the Wise Men riding the road, lamb Kira (flock hide, springy hay), Kira's pounce (also Egypt's far roof), the
golden shard in the manger and `WiseMenArrive`. 60 shards / 112 in all wake the necklace. (Dan's Mixamo files and the new dog came
in with Fix 7.1.) Published: `game-700`. Report and "For Dan to check": `TODO.md`.

Standing facts from Round 7:
- Worlds: `Game.InBethlehem`; `World/Bethlehem.cs` builds it (barn x -6..6, z 26..42, door south; the farmyard x -35..35, z -10..70;
  hills east; village west; the Wise Men's road north), its pieces in `World/BethlehemWorld.cs` (BouncePad, Flock, StarRing, StarDome,
  Caravan, NoRoom, Stealth). Shard y in its data = height over the terrain there (`Bethlehem.ShardPos`). `Bethlehem.asset` is made once
  from `WorldData.BethlehemDefaults()` (delete it to remake). `WorldData.StoryOrder` = Ice Age, Egypt, Bethlehem.
- Ground: `Tools/Blender/bethlehem_world.py` (terrain + props; prints the steepest ground: keep it under ~35 deg, Round 7: 29).
- Vortex: five paths (Ice Age -60, Egypt -30, Bethlehem 0, Medieval 30 "to be continued" until Bethlehem's golden shard, Home 60).
  Vortex doors and world gates trigger "inside the arch" (lamb Kira went under the old circles). Bethlehem's story shard plays
  `WiseMenFinale` (no gate walk: the hay's vortex takes them in; `Bethlehem.Gate` in the barn is the way back).
- Kira: `KiraForm.Lamb` (`kira_lamb`, Abilities.FlockHide + WoolBounce); the pounce is Kira's `lockedAbility` (B on the ground: aim
  0.28 s, 17 m/s, 1.8 m high: ~11.8 m; `CharacterMotor.Pouncing`, `LastPounceDistance`); `Motor.Launch(h)` for bounces.
  Egypt's far roof moved to x -43.5, z 40 (10.5 m from the west roof; E14 / E15 with it).
- Guards: `Guard` makes a Bethlehem night watchman in Bethlehem (lantern spot light = his sight; `dozes`; `Guard.Alert(pos)` from a
  dog's bark); `Stealth.Hidden(hero)` = lamb Kira inside a flock's 4.5 m (watchmen, dogs, goats, scorpions ignore her).
  Enemies `Combat/EnemyBethlehem.cs`: Goat (lower, charge, headbutt), Dog, Scorpion (static model + Skitter).
- People clips added to `CreatureAnimator.PeopleClips` (missing = skipped): sit, argue, map, offer, sad, yell, doze, surprised;
  `import_characters.py EXTRA_CLIPS` copies them from `SourceArt/Mixamo/wisemen|watchman/`. Dan and Abi wear their Egypt models in
  Bethlehem until `*_bethlehem` exist (`Hero.SetOutfit`). The Wise Men's riders stay placeholders until a `sit` clip is in.
- Cutscenes: `BethlehemArrival`, `WiseMenArrive`, `NecklaceWakesBethlehem`, `ViolinistTeachesBethlehem` (Kira learns);
  `VortexRings(..., golden)`; `CutsceneAuthoring.Version` 15. The harp: `instruments.fbx` "Harp" (the lyre's markers and clip).
- Head check for four-legged concepts: `Tools/Blender/head_turn.py <names>` (nose vs the head's middle, from above).

**Fix 6.1 delivered: 0.6.1 (build 601), 2026-11-10.** Shards are one universal total; each world's necklace wakes at a
cumulative threshold (Ice Age 35, Egypt 75); HUD "total / Next: N", per-world counts in the pause menu; 0.6.0 saves load
unchanged. The Violinist plays the lyre with his own clip. Published: `game-601`. Report and "For Dan to check": `TODO.md`.

Standing facts from Fix 6.1:
- `AreaData.requirement` = a threshold on `SaveGame.Total` (all worlds); `Game.Update` wakes the necklace when it is reached
  (also on arrival). `WorldData.StoryOrder` (worlds in story order: add each new world) drives rule 2's "available on arrival"
  in `RequirementReport`. New world: set its threshold to ~60-65% of all shards up to it and check the report.
- `Game.ShardTracker()` = the pause menu's per-world counts; `Hud.TotalShown` / `Hud.UnderTotal` for checks.
- Violinist: `WorldData.violinistPlayClip` (Ice Age `violinist_play`, Egypt `violinist_lyre`). `violinist_lyre.anim` is made by
  `Editor/LyreClip.cs` at every build when missing or when `LyreClip.Version` changes (stamp `Assets/Editor/violinist_lyre.version.txt`).
  The lyre's hold is IK in `Violinist.LateUpdate` (body frame from hips / neck / shoulders; `Palm` turns a hand by its finger
  bones, half the twist into the forearm); `ArmReport` / `MissL` / `MissR` for checks.

**Round 6 delivered: 0.6.0 (build 600), 2026-11-10.** Ice Age fixes (straight running, an easy under-ice swim with lit holes and
auto climb-out, ivory tusks / fangs, the Violinist's fiddle and bow) and Ancient Egypt, the second world: town, market, guards,
cat temple, quarry, the judgmental camel (rideable), the Sphinx with Kira's face (`SphinxGag`), the pyramid scaffolding and the
tomb, the red story shard; mummy Kira (cat plates, mummies stay asleep), Abi's double jump from the Violinist's lyre. 60 shards /
35 wake the necklace. Published: `game-600`. Report and "For Dan to check": `TODO.md`.

Standing facts from Round 6:
- Worlds: `Game.World` is the save's world (`SaveGame.Current.currentWorld`); `Game.InEgypt`; `GoToWorld` rebuilds the world through
  the vortex; Egypt's builder `World/Egypt.cs` (town around z 0..120, the Sphinx ~(-30, 200), the pyramid scaffolding x 60..80,
  the tomb at (500, 60, 0)), its pieces in `World/EgyptWorld.cs` (CatPlate, Guard, Quicksand, SwingBlade, DartWall, StepTurner,
  Counterweight, Camel, SphinxGagZone, StaticModel). Enemies: `Combat/EnemyEgypt.cs` (Mummy, ScarabSwarm, Guardian).
  `MovingPlatform.Hold(pos)` for things that move themselves (camel, lift, bridge).
- Vortex: four paths (Ice Age, Egypt, Bethlehem closed until Egypt's story shard, Home). `NecklaceModel.Tint` = the world's
  story-shard colour (blue / red). Story cutscenes per world: `NecklaceWakesEgypt`, `VortexOpensEgypt`, `ViolinistTeachesEgypt`;
  `EgyptArrival`, `SphinxGag`. `CutsceneAuthoring.Version` 11. `Game.Film` sets `Busy` so world triggers don't fire while filming.
- Camera: auto-recenter dead band 20 deg (`CameraRig.DeadBand`; camera-relative running fed back into the recenter); close and
  level under the ice (`IceHole.UnderIce`); further back and higher while riding the camel (`Camel.PlayerRiding`).
- Under the ice: `World/IceHole.cs` (holes, `Region`, rescue after 14 s / a third of the breath), `CharacterMotor.ClimbOut`;
  breath 16 s for Dan and Abi.
- Hard parts: `Tools/Blender/hard_parts.py` (rules per creature) through `kira_rig.py` -> `SourceArt/Rigged/<n>/hard.txt` ->
  `<model>_hard.mat` (`CharacterImport`), used for submeshes > 0 (`CharacterModel.HardMat`).
- Instruments: `Tools/Blender/instruments.py` -> `World/instruments.fbx` (Fiddle, Bow, Lyre + hand markers); `Violinist` holds them
  by two-bone IK; `WorldData.violinistInstrument` per world (Egypt: Lyre, model `violinist_egypt`).
- Models: Egypt's people (`dan_egypt`, `abi_egypt`, `violinist_egypt`, `mummy`: Mixamo, Dan's downloads) and Code's rigs
  (`kira_mummy` counter-turned 14 deg, `camel`, `guardian`); `scarab`, `sphinx_kira` static (`Tools/Blender/static_export.py`).
  Kira lock grey threshold 20% for the mummy form. `KiraForm` Mummy gives `Abilities.MummyHush`; new `Abilities.Pounce`.
- Music: `Game.MusicFor` adds `egypt`; Act I's music never comes back once faded; fades run in game time.

**Round 5 delivered: 0.5.0 (build 500), 2026-11-09.** The new arrival (Kira's silhouette, Dan's fall, "it's Kira"); props upright;
Dan's four Suno tracks by place; Kira and Abi closed meshes (0 see-through); a bigger Ice Age (frozen lake, glacier climb,
hot-spring grotto), 60 shards / 35 wake the necklace, 10 later-move gates as data; Meshy wolf, snow critter, mammoth (Code's
rig) and the Violinist (Mixamo) in the game. Every process Code starts goes through `Tools/quiet.py` (after a render played sound
and took Dan's focus). Report and "For Dan to check": `TODO.md`.

Standing facts from Round 5:
- `Tools/quiet.py`: every process on a never-shown desktop, hidden, Below Normal, with a window watcher (`quiet.watch_report`).
  Cutscene videos: `Tools/render_cutscene.py` (built exe, `-exeCheckOnly film`, fixed 30 fps frames, ffmpeg). `unity_batch.py
  --gpu` is refused. MonoBehaviours saved in prefabs must live in a file named after the class (Round 5 found `ScreenTint`,
  `Backlight`, `CutsceneMood` missing in the exe).
- Music: `Game.MusicFor` picks Dan's track by place (title / act1 fading from 16.6 s / iceage / vortex); missing file = silence.
- Closed meshes: `Tools/Blender/closed_mesh.py` (voxel solid + gap sealing + remesh + Meshy maps baked). Kira's forms and the
  creatures through `kira_rig.py` (`CREATURES` heights, `TRIS`, `THIN`; creatures rigged at Kira's size, armature scaled up, extra
  `stunned` clip); people through `close_person.py` (Mixamo weights copied; output `rigged_closed.fbx`, preferred by
  `import_characters.py`; maps in `SourceArt/Rigged/<model>/`).
- World: `WorldArt.Prop` keeps the FBX root's turn (props were lying down); `Resources/Data/WorldProps.txt` at every build.
  `IceAge.cs`: `Lake` (west basin through z 98..132; `SlickIce` lanes grip across, slide along; `DeepWater` pushes divers up
  without `Abilities.DeepDive`), `Glacier` (east through z 280..310; 18 ledges `LedgeStep` 1.3 m), `Grotto` (`RockWall` needs
  `Abilities.ChargedPunch`). `WorldData.gates` = the later-move places. New abilities: ChargedPunch, DeepDive, WallJump (not
  learnable yet). `IceAge.asset` is remade when it has no gates (delete it after changing the defaults).
- Creatures: `CharacterModel.Creature` puts a model on a placeholder (shapes kept in a hidden "Graybox"); `CreatureAnimator.Drive`
  feeds a motor-less creature its loop / speed. The Violinist: `Violinist.UseModel` (own playables; clips `Clips/violinist_*`
  + shared idle / walk).
- Exe check modes added: `tour` (valley viewpoints + prop audit), `zones` (the three new zones), `violinist`, `film`.

**Round 4 delivered: 0.4.0 (build 400), 2026-11-09.** The Ice Age is the game: New game plays Act I (the living room) and the
arrival in the snow, then one valley (snowfield and pond, mammoth ridge, gorge, ice caves, steam vents, standing stones) with
40 shards, the necklace, the blue story shard, the vortex (Ice Age / Egypt "to be continued" / Home blocked), the Violinist
teaching Dan the ground pound, wolves and snow critters (placeholders), music and sounds. Kira's and Abi's see-through holes
fixed. Dan's four creature concept images are in `SourceArt/Concepts/` (models next). Published: `game-400` (a Round 1 starter
install updated itself to it). Report and "For Dan to check": `TODO.md`.

Standing facts from Round 4:
- Worlds: `Game.World` is the Ice Age (`WorldData.IceAge`, `World/IceAge.cs` places the gameplay on the Blender terrain by
  raycasting; shard y in its data = the surface height hint, `heightsAreHints`). Areas have a `box`; `Game.Area` = the area the
  player is in (null in the vortex space). The playground = Settings > Test area (debug), `SaveGame.Slot = "testarea"`
  (`save_testarea.json`).
- Vortex: `World/Vortex.cs` (space at y 600, z 2500; checkpoint `VX`; doors per world; `SaveGame.StoryShards`, `worldsReached`).
  A world's gate is a `Portal` whose `toCheckpoint` is `VX` (`Game.EnterVortex`).
- Moves and world pieces: Dan's ground pound (`CharacterMotor`, Dive in the air; `Hero.Pound`; `Abilities.GroundPound`, Dan's
  `lockedAbility`), `UpdraftVolume` (steam), `CrumblePlatform`, `CrackedFloor`, `DarkVolume` (+ the necklace's light),
  `HintZone`, `MovingPlatform.path` (mammoths), `Violinist` (teaches a move via the cutscene `ViolinistTeaches`).
  Enemy kinds: Blob (playground), Critter, Wolf; `Enemy.Stun`.
- Cutscenes carry a `CutsceneVersion`; bump `CutsceneAuthoring.Version` when their code changes (older ones rebuild at the
  next build). `CutsceneMood` (a cutscene's own light), `ScreenTint` (white-outs). Docs: `Docs/CUTSCENES.md`.
- Look per world (`Look.ApplyWorld`), fog to 1.7 km and far clip 2.2 km in the Ice Age; `Look.SetDark`, `Look.InVortex`.
- World art: `Assets/Resources/World/*.fbx`, parts named `<part>__<Material>`, coloured by `World/WorldArt.cs` (import:
  `Editor/WorldImport.cs`).
- Frame-rate checks must draw the view every frame (batch mode only draws on request): `PerfIceAge` does.

**Fix 3.1 delivered: 0.3.1 (build 301), 2026-11-09.** Sabertooth Kira is a new Meshy model from a straight-headed
concept (her face no longer twists), fur matched to home Kira's gray. Published: `game-301` (a Round 1 starter install
updated itself to it). Report and "For Dan to check": `TODO.md`.

Standing facts from Fix 3.1:
- `kira_rig.py` counter-turns the head only for models in `COUNTER_TURN` (home Kira); new forms are modelled with a
  straight head instead. `shots=<folder under Data/screenshots>` sets where its renders go; it also renders face
  close-ups (front / side / 3/4 in idle, run, attack).
- `python Tools/match_fur.py <form> [--to kira]` moves a form's fur colour to home Kira's (keeps Meshy's texture as
  `base_color_meshy.png`; `meshy_cleanup.py` then uses the recoloured `base_color.png`). `meshy_cleanup.py` `YAW` is now
  empty (the new sabertooth faces front).
- Old sabertooth: `SourceArt/Rigged/kira_sabertooth_v1/model.fbx` (swap back by copying it over `model.fbx` and running
  `import_characters.py --only kira_sabertooth`).
- Exe check `play` now shoots sabertooth Kira's face in Area B (`12d` player's view, `12e`-`12g` close-ups).

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
