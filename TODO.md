Active Development Plan — Shards of Time

A 3D platformer / collectathon based on Dan's film *The Shards of Time*.
Planned by Dan with the planning chat; built by Code. Standing context and
rules are in `CLAUDE.md`; design in `Docs/Design/`; process notes in
`collaboration-process-notes.md`.

---------------------------------------------------------------------------
Design decisions (DECIDED with Dan, 2026-11-09; full list and per-world canon
in `Docs/Design/design-decisions.md`)
---------------------------------------------------------------------------
- Eight worlds in film order: Ice Age, Ancient Egypt, Bethlehem, Medieval
  Kingdom, Pirate Seas, Victorian Steampunk, Tomorrowland, the Moon.
- Playable: Dan, Abi, Kira (switchable, all visible, followers warp in).
  Muscle Man joins in Medieval as a companion, leaves after the Moon, rejoins
  for post-credits free-roam.
- Shards: many small shards + one story shard per world; the small-shard
  requirement wakes Dan's necklace, which leads to the story shard; the story
  shard opens the portal. Return to unlocked worlds any time.
- Acts I, X and XI of the film are non-playable cutscenes. All cutscenes are
  remade in-engine (no film clips) and the cutscene tooling doubles as a
  pipeline for Dan's future films.
- The film is the backbone, not a script; the game expands around it.
- Publish a release every round.

---------------------------------------------------------------------------
Current round — Round 1: project setup + graybox playground (target 0.1.0)
---------------------------------------------------------------------------
Goal: prove the core systems and the feel in a graybox before any real art.
No real models, music or worlds this round. Capsules / simple shapes, colored
per character, are fine. Report per item: done / not done / deferred, and why.

1. **Project setup.** Unity 6000.6.1f1 URP project in this repo, Input System
   only, packages Cinemachine, Timeline, Unity Recorder, ProBuilder.
   `.gitignore` from Hostile Horizon (incl. `Builds/PublisherPrivate/`).
   Namespace `ShardsOfTime`. Folder layout under `Assets/Scripts/` by system.
2. **Port the standard pieces** from Hostile Horizon (newest) / Chastain Rush:
   `Core/TestRun.cs` (`-mute`, measured audio level, off-screen batch checks),
   `Tools/run_check.py`, `Core/ScreenSetting.cs`, the Steam Deck setting,
   the build menu + command-line build (`PlayerBuild`), `Tools/cleanup_round.py`,
   `Tools/meshy_generate.py` and `Tools/import_characters.py` (not used until
   models arrive, but in place). `Play Shards of Time.bat` in the repo root.
   List what came across in the report.
3. **Launcher + first release.** Port the launcher as `ShardsOfTimeLauncher.exe`,
   make the signing key once (`python Tools/New-LauncherKey.py`), catalog repo
   `danemoll-jpg/shards-of-time-releases`, `Docs/Starter/README-LAUNCHER.md`
   rewritten for this game, and `Play Shards of Time (launcher).bat`. Publish
   0.1.0 at the end of the round so Dan can play it on the Steam Deck.
4. **Character motor with movement modes.** One motor with Ground, Air, Swim
   and LowGravity modes. Ground/Air: run with analog speed, jump with variable
   height, coyote time, jump buffering, slopes, landing on moving platforms.
   Swim: surface swimming and diving in a water volume (up/down controls), with
   a breath meter that only applies to non-swimmer characters. LowGravity: a
   volume that scales gravity and jump. All four are shown in the playground.
5. **Third-person camera.** Cinemachine orbit camera: right stick / mouse orbits,
   gentle auto-recenter behind the player while running, never clips into
   walls (pulls in instead), recenter button, works underwater.
6. **Three characters + switching.** Stats as ScriptableObjects:
   - Dan: average speed and jump; punch; can push heavy blocks.
   - Abi: faster, higher jump, more air control; kick. Double jump exists as a
     locked ability (see item 9).
   - Kira: small (tune her size so she reads on screen; start ~0.5 m tall),
     fits through crawl gaps the others can't; claws; best swimmer.
   Switch instantly with LB/RB (keyboard Q/E, or 1/2/3 for a direct pick). The
   new character takes the current spot; camera doesn't jump.
7. **Followers.** The two inactive characters follow at a short distance. A
   follower that is more than ~12 m behind, falls off, or hasn't reached the
   player within ~3 s of a jump/climb warps in just behind the player, out of
   camera view. Followers never block the player (no collision with the player).
   In water they swim. Check: a run through the whole playground with both
   followers arriving at the end, no follower stuck.
8. **Shared health, combat, one enemy.** 3 hearts shared by all characters;
   damage with brief invincibility; heal pickup; a heart upgrade that raises
   the max (cap 10). Each character's basic attack. One placeholder enemy
   (patrols, chases when close, touching hurts, one or two hits defeat it) and
   one hazard (spikes or a pit). Respawn at the last checkpoint when hearts run
   out or the player falls out of the world.
9. **Shards, necklace, story shard, portal, abilities.**
   - Small shards with a HUD total that persists; collected ones stay collected.
   - Necklace: a HUD icon on Dan that pulses faster the nearer the closest
     uncollected shard (plus light controller rumble, on by default with a
     setting to turn it off).
   - Area A has a requirement (e.g. 10 shards). Reaching it plays a short
     in-engine moment (item 11) and wakes the necklace; it now leads to the
     story shard. Collecting the story shard opens the portal to Area B.
   - Area B: a pickup that unlocks Abi's double jump, and a portal back to A.
   - Area A has a ledge with shards only reachable with the double jump, and a
     boulder marked "needs Muscle Man" (not yet movable; it proves the gate).
   - A check proves Area A's requirement can be met without the double jump.
10. **Save.** One save file (`save.json` in the persistent data folder):
    collected shard ids per world, total, unlocked abilities, opened portals,
    max hearts, current world. Saved at checkpoints, portals and on quit;
    loaded on start. A "New game" option clears it.
11. **Cutscene pipeline, first proof.** Timeline + Cinemachine cutscene system:
    plays in game, skippable (hold a button), returns control cleanly. Build
    two short graybox cutscenes with the placeholder characters: the necklace
    waking (item 9) and the portal opening. Editor menu
    **Shards of Time > Render Cutscene to Video** renders a chosen cutscene to a
    1080p MP4 with Unity Recorder (muted render is fine). Write
    `Docs/CUTSCENES.md`: how to author a scene, add shots, and render it, written
    so it can be reused for a film project.
12. **Art references + first model prompts.** Dan is putting his film art in
    `SourceArt/Reference/` (folders already made; see its README). Write
    `Docs/MODELS-NEEDED.md` with prompts for: Dan (home clothes + Ice Age
    caveman outfit), Abi (home clothes + Ice Age outfit), Kira (normal Scottish
    Fold + Ice Age saber-toothed kitten), using Dan's reference images where
    they exist and saying so where none exist yet. Follow the art pipeline in
    `CLAUDE.md`; Kira lock in every Kira prompt; include each model's Mixamo
    animation list (real search names). **Stop after writing them and tell Dan**
    (no Meshy calls this round).
13. **Title screen + pause menu.** Minimal: Continue / New game / Settings
    (volume, screen, rumble, invert camera X/Y, camera speed) / Credits / Quit.
    Controls page showing the bindings for the device in use.
14. **Build, check, publish, report.** Built exe, checks muted and unfocused,
    release 0.1.0 published, `CLAUDE.md` Commands and Where things stand filled
    in, round report in this file, disk cleanup.

For Dan to check (Code fills in): feel of run/jump/camera on gamepad and Steam
Deck, character switching, followers, swim and low gravity, the two cutscenes,
the MP4 render.

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Round 2 (likely):** tuning from Dan's notes on Round 1; character models via
  the art pipeline once Dan has the concept images; Kira transformation system
  (form per world, swap model + ability set).
- **Then:** Ice Age as the first real world (the original concept's first
  milestone), with its film moments as in-engine cutscenes; Act I cutscene.
- Muscle Man companion (Medieval): heavy-object interactions, the "lift Dan"
  boost, follower rules. Deferred until the Medieval world.
- Memory Fragments (Dan's time-echo visions when touching temporal artifacts)
  and the Violinist's per-world appearances. Deferred, not decided against.

---------------------------------------------------------------------------
Technical notes
---------------------------------------------------------------------------
- Open design question: Tomorrowland has no story shard in the film; default is
  that the rocket plays that role (shard total fuels the launch). Confirm with
  Dan before building Tomorrowland.
