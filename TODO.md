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
Current round: none yet. Round 1 is delivered (below); the planning chat
writes Round 2 here. Round 1's plan is in `TODO-ARCHIVE.md`.
---------------------------------------------------------------------------

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
### Round 1: project setup + graybox playground (0.1.0, build 100, 2026-11-09)
Final check `local/round1/release3` (built exe, batch mode, muted, no window,
virtual pad from the title screen): RESULT all checks passed; audio output
0.000 RMS. Evidence: `Data/screenshots/round1/` (+ the two MP4s in `Recordings/`,
git-ignored).
1. Setup: **done.** Unity 6000.6.1f1 URP, Input System only; Cinemachine 6.6.0 and
   Timeline 6.6.0 (built into 6000.6), Recorder 5.1.7, ProBuilder 6.1.4; HH's
   .gitignore (+ `Recordings/`); namespace `ShardsOfTime`; scripts by system.
2. Ported: **done.** From HH: `TestRun.cs`, `ScreenSetting.cs`, `SteamDeck.cs`,
   `LauncherBridge.cs`, `PlayerBuild`, `run_check.py`, `cleanup_round.py`,
   `meshy_generate.py`, `import_characters.py` (both adapted to our six models),
   the GameSettings / UiKit / ExeCheck patterns, `Play Shards of Time.bat`. New:
   `Tools/unity_batch.py` (Unity batch runs at Below Normal, no window).
3. Launcher: **done.** `ShardsOfTimeLauncher.exe`, own signing key (ID 61aa227f),
   README rewritten, `Play Shards of Time (launcher).bat`. Local self-check: update
   1->2, game started hidden, ready handshake. 0.1.0 published (see item 14).
4. Motor: **done.** Ground/Air/Swim/LowGravity in one motor. Measured: Dan 7.0 m/s,
   jump 1.57 m held / 0.41 tapped; coyote jump at 0.07 s yes, 0.25 s no; buffered
   jump 0.04 s after landing; 20 deg ramp climbed, 55 deg slid back; carried 7.6 m
   on the moving platform; Dan dives, breath runs out at 10 s (-1 heart); Kira no
   breath meter; low gravity (x0.3): 5.3 m / 2.2 s jump vs 1.6 m / 0.64 s.
5. Camera: **done.** Cinemachine orbit: 160 deg/s on the stick, recenter (R3) to
   0 deg off, auto-recenter while running, a wall behind pulls it to 1.2 m, works
   and tints under water.
6. Heroes + switching: **done.** Stats as `Resources/Data/Characters/*.asset`.
   Dan punch + pushes blocks (4.4 m in 3 s; Abi 0); Abi 8.2 m/s, 2.1 m, double
   jump 3.6 m once unlocked; Kira 0.5 m, 7.6 m/s, only one through the 0.7 m crawl
   gap. RB/LB/1/2/3 switch on the spot, camera moved 0.5 m in 4 frames.
7. Followers: **done.** Trail-following, flanking left/right. Whole-run check:
   both arrived at every stop, 6 warps (all "not caught up in 3 s", out of view),
   none stuck.
8. Health/combat: **done.** 3 shared hearts, 1.3 s invincibility, heal heart,
   heart upgrade (3->4, cap 10), enemy chases + hurts + dies to one punch, spikes,
   pit -> checkpoint -1 heart, out of hearts -> checkpoint full hearts.
9. Shards/necklace/story/portal: **done.** 10 of 16 non-double-jump shards wake
   the necklace (cutscene), it leads to the story shard (HUD arrow + beam), which
   opens the portal (cutscene) to Area B (double jump pickup, portal back);
   double-jump ledge + "needs Muscle Man" boulder. Rule-2 check: data report +
   the play-through reached 10 without the double jump. Rumble: coded, **not
   felt** (no real pad in a batch check).
10. Save: **done.** `save.json`; checked: Quit to title -> Continue restores 19
    shards, ability, portals, 4 hearts; New game asks, then clears.
11. Cutscenes: **done.** NecklaceWakes (6.5 s), PortalOpens (6 s), Timeline +
    Cinemachine prefabs; hold A skips (checked); Shards of Time > Render Cutscene
    to Video wrote both MP4s (1920x1080, 30 fps); `Docs/CUTSCENES.md`.
12. Art prompts: **done, stopped for Dan.** `Docs/MODELS-NEEDED.md`: six prompts
    with the Kira lock and the Mixamo clip list. Dan then saved six clean full-body
    sheets, now `SourceArt/Concepts/*.png`, ready for Meshy in Round 2. Open: the
    red-pyjama woman (shared in chat): who is she? No Meshy calls.
13. Title + pause: **done.** Continue / New game / Settings / Controls / Credits /
    Quit; pause menu; Settings: volume, effects, screen, resolution, Steam Deck,
    rumble, invert X/Y, camera speed, reset; Controls follows the device in use.
14. Build/check/publish: **done.** Exe 0.1.0 (99.8 MB); checks above; release
    `build-100` + starter `starter-100` on shards-of-time-releases; a fresh
    starter install from GitHub started 0.1.0 (ready handshake). Disk clean-up:
    project folders 2.2 GB -> 1.7 GB.

Decisions Code made (Dan can overrule): the world is built in code at start (one
scene, as HH); requirement 10 of 16; low gravity x0.3; followers flank and hide
when the camera is pulled in on them; cutscenes anchor at the player (necklace)
or the portal; skip = hold A / Space 1 s; check runs cap at 144 fps; placeholder
code-made sounds; title art is a placeholder made from a screenshot.

**For Dan to check:** run / jump / camera feel on a pad and on the Steam Deck
(install the starter ZIP; Deck frame rate not measured); switching; followers
(do the warps feel OK?); swim, dive, low gravity; the two cutscenes in play and
the MP4s in `Recordings/`; rumble on a real pad; who the red-pyjama woman is.

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
