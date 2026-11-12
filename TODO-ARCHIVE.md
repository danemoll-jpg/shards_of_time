# TODO archive — Shards of Time

Older rounds' plans and deliveries, moved here word for word from `TODO.md`. Grep it, don't read it whole.

===========================================================================
Round 1 plan (as written by the planning chat, 2026-11-09)
===========================================================================
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
Round 2 plan (as written by the planning chat, 2026-11-09)
---------------------------------------------------------------------------
Current round — Round 2: first character models, stage A (Meshy) + Kira forms
(target 0.2.0)
Round 1's plan is in `TODO-ARCHIVE.md`. Dan played 0.1.0 (2026-11-09):
"everything seems ok", no tuning notes. The six concept sheets in
`SourceArt/Concepts/` were checked by the planning chat: all usable (A-pose,
full body, plain background, no necklace, Kira's ears folded, paws and tail in
frame). Report per item: done / not done / deferred, and why.

1. **Meshy, all six models.** `python Tools/meshy_generate.py` on `dan_home`,
   `dan_iceage`, `abi_home`, `abi_iceage`, `kira`, `kira_sabertooth`
   (image-to-3D, key from `MESHY_API_KEY`, never written anywhere). Use Kira's
   reference stills as extra views if the API takes several images.
2. **Blender clean-up** for each: metres, feet at 0, facing +Z, heights as in
   `Docs/MODELS-NEEDED.md` (Dan 1.8 m, Abi 1.65 m, Kira the in-game size from
   Round 1), sensible triangle count for the Steam Deck plus LODs, textures
   kept. Check Kira's ears came out folded; if not, say so (don't fix by hand
   yet).
3. **Previews:** render each model front, side, back and 3/4 into
   `Data/screenshots/round2/models/`, and look at them: report fused limbs,
   hair stuck to arms, missing feet/paws, texture smears, per model.
4. **Mixamo hand-off files:** export one FBX per person (`dan_home`,
   `dan_iceage`, `abi_home`, `abi_iceage`) ready to upload to Mixamo, in
   `SourceArt/Mixamo/<model>/upload/`. Put a short checklist at the top of
   `Docs/MODELS-NEEDED.md`: which file to upload, the clip list (already in the
   doc), download settings, where to save.
5. **Kira's rig path:** check whether the Meshy API can rig and animate a
   four-legged animal. If yes, rig both Kira models with it; if no, build the
   quadruped rig in Blender by script (no ear bones) and make her clip set
   (idle, walk, run, jump, fall, land, swim, attack, hurt). Say which path and
   why.
6. **STOP for Dan as early as possible (Dan, 2026-11-09: he wants to do Mixamo
   tonight).** Order: items 1–4 for the **four people first** (Meshy, clean-up,
   previews, Mixamo FBX + checklist), then stop and tell Dan the files are
   ready, with the preview renders. Do the two Kira models and items 5, 7–9
   after that stop; they don't need Dan.
7. **Kira transformation system.** Each world names Kira's form (data); a form
   swaps her model and her ability set (e.g. Ice Age: claw through ice walls;
   Pirate: otter swimming). Arriving in a world sets the form; Kira's lock
   (folded ears, gray, amber eyes) is a check on every form model. Graybox:
   Area A = home cat, Area B = sabertooth (with an ice wall only she can claw
   through).
8. **Dan's necklace** as its own small model (Blender script, hard-surface):
   cord + shard. The shard glows and pulses with the necklace logic from
   Round 1; it shows on Dan's chest in both outfits (a socket that works with
   the Mixamo skeleton later).
9. **Kira in the game now:** if item 5 gives a rigged, animated Kira this
   round, replace her capsule (keep the capsule as fallback). Dan and Abi stay
   capsules until Dan brings the Mixamo clips (Round 3).
10. Build, check, publish 0.2.0, update `TODO.md` / `CLAUDE.md`, clean up.

RESOLVED (Dan, 2026-11-09): the woman in red pyjamas with the Santa hat is
**Abi in her original film outfit**. Dan is happy with the green Santa pyjamas
for the game ("it doesn't really matter that it is different"), so
`abi_home` stays as it is. Not modeled this
round; add it to `Docs/MODELS-NEEDED.md` as a later Abi outfit and file her
image under `SourceArt/Reference/Characters/Abi/`.

After Round 2, **Round 3:** Dan's Mixamo clips imported, Dan and Abi replace
their capsules, then the Ice Age as the first real world.
---------------------------------------------------------------------------

---------------------------------------------------------------------------
Round 3 plan (as written by the planning chat, 2026-11-09)
---------------------------------------------------------------------------
Current round — Round 3: Dan and Abi in the game, animated (target 0.3.0)
Round 2's plan is in `TODO-ARCHIVE.md`. **Priority: Dan sees Dan and Abi
animated in the game in this round.** Dan already rigged all four in Mixamo
and downloaded the clips (`SourceArt/Mixamo/`). Report per item: done / not
done / deferred, and why.

**Standing rule (Dan, 2026-11-09):** the concept sheets Dan made from Code's
prompts (`SourceArt/Concepts/`) are the official designs. They override the
earlier film stills. The red-pyjama Abi is not planned; don't raise it again.

1. **Import Dan and Abi** (both outfits each): Humanoid rig from the rigged
   FBX (`rigged_hairfix.fbx` for Abi), the shared clip set wired to the motor:
   idle, walk, run (speed-blended), jump, fall, land / hard landing, swim and
   tread water, push, punch (Dan), kick (Abi), hit, cheer for shard pickups.
   LODs from the rigged file (keeps skin weights).
2. **Replace the capsules** for Dan and Abi as player and as followers. Outfit
   per area, the same way Kira's form works: Area A = home clothes, Area B =
   Ice Age. Capsules kept as fallback.
3. **Necklace on Dan's chest** at the `Spine2` socket in both outfits, offset
   tuned so it sits right and doesn't clip through the clothes when he runs.
4. **Cutscenes use the real models:** NecklaceWakes and PortalOpens with Dan,
   Abi and Kira instead of graybox actors; render both MP4s again.
5. **Check it as Dan sees it:** exe screenshots from the player's view of each
   character running, jumping, swimming and switching, plus the followers;
   look for foot sliding, sinking into the floor, clipping hair, and wrong
   facing; fix what's found or list it.
6. Build, check, publish 0.3.0, update `TODO.md` / `CLAUDE.md`, clean up.

After Round 3: the Ice Age as the first real world.

===========================================================================
Fix 3.1 plan (as written by the planning chat, 2026-11-09)
===========================================================================
---------------------------------------------------------------------------
Current round — Fix 3.1: sabertooth Kira's face (target 0.3.1)
Round 3's plan is in `TODO-ARCHIVE.md`. Next after this fix: the Ice Age as the
first real world (the planning chat writes it here).

**Problem (Dan, 2026-11-09): "Kira's face is messed up in the sabertooth form."**
Planning chat's diagnosis from `Data/screenshots/round2/kira_rig/kira_sabertooth_idle.png`:
the concept sheet had her head turned hard toward the viewer, Meshy built the
head that way, and the rig's counter-turn (Round 2, capped 45 deg) twists the
face: eyes skewed and cross-eyed, fangs splayed outward. Home Kira looks fine
(head only slightly turned). Fixing it with more bone tricks won't hold up, so
the fix is a new model from a straight-headed concept.

1. **New concept (Dan):** Dan makes a new sheet from the prompt below and saves
   it as `SourceArt/Concepts/kira_sabertooth.png` (keep the old one as
   `kira_sabertooth_v1.png`). Code checks it before using it: head in line with
   the body (looking the way she walks), ears folded, mouth closed with both
   fangs straight down, all paws and tail in frame. If it fails, stop and say
   exactly why.
   **Done (2026-11-09):** Dan made it; the planning chat checked it and saved it
   (1448 x 1086; old one is `kira_sabertooth_v1.png`). It passes. One note: her
   fur is a warmer, slightly brownish gray than home Kira's cooler gray. Match
   the sabertooth's fur color to home Kira's texture in the clean-up (item 2)
   so she's the same cat in both forms.
2. **Meshy + clean-up** as in Round 2, then the same Blender rig (`kira_rig.py`,
   no ear bones), **with no head counter-turn** (remove it for this form; check
   it isn't needed for home Kira either, and keep hers if it is).
3. **Previews of the face:** front, side and 3/4 close-ups of the head in idle,
   run and attack, plus an exe shot from the player's view. Look at them and
   report: both eyes looking the same way, face not stretched, fangs straight
   down and inside the lip line. Keep the old model as fallback.
4. Build, check, publish 0.3.1, update `TODO.md` / `CLAUDE.md`, clean up.

Prompt for Dan's image tool (planning chat, 2026-11-09):
> Pixar-style 3D render of Kira, a tiny gray Scottish Fold kitten transformed
> into a baby saber-toothed cat. Full body, side view, standing on all four
> legs, head facing straight ahead in line with her body (looking where she is
> walking, NOT turned toward the camera), tail up and away from the body.
> Two small white saber fangs pointing straight down from the upper lip, mouth
> closed. Folded-down Scottish Fold ears that lie flat and forward, never
> raised. Solid gray fur over the entire body with absolutely no white fur or
> markings. Golden-amber eyes. Plain white background, soft even light, no
> ground shadow, whole body in frame with space around it, at least 1536 px wide.
Negative: `head turned toward camera, looking at viewer, open mouth, cropped,
cut off paws, cut off tail, raised ears, pointed ears, white fur, multiple
views, props, text, watermark, ground shadow`.
---------------------------------------------------------------------------

===========================================================================
Round 1 delivery (moved from TODO.md, 2026-11-09)
===========================================================================
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

### Round 2: first character models + Kira forms (0.2.0, build 200, 2026-11-09)
Final check `local/round2/final` (built exe, batch mode, muted, no window,
virtual pad from the title screen, play + measure + perf): RESULT all checks
passed; audio output 0.000 RMS. Evidence: `Data/screenshots/round2/` (`models/`
previews, `mixamo/` rig tests, `kira_rig/` clip frames, `game/` exe shots).
1. Meshy: **done.** All six (30 credits each) + a second `abi_home` (+30, see 6).
2. Blender clean-up: **done.** Metres, feet at 0, facing front: Dan 1.8 m, Abi
   1.65 m, Kira 0.5 m; ~20.7k tris (people), ~15k (Kira). Kira's ears came out
   folded in both forms. LODs: Kira 100/50/20 %; people's LODs in Round 3 from
   the rigged file (keeps the skin weights).
3. Previews: **done.** No fused limbs or missing feet/paws. Faint smear on
   dan_home's trouser (3/4 view). Both Kira heads are turned (as the concept
   sheets): her rig counter-turns them (max 45 deg; sabertooth stays ~38 deg off).
4. Mixamo files + checklist: **done.** Dan rigged all four the same night.
   Abi's hair was tied to her arms: `Tools/Blender/fix_hair_weights.py` fixes it
   (`rigged_hairfix.fbx`); abi_home remade in Meshy, re-rigged by Dan.
5. Kira's rig: **done, Blender path** (Meshy's rigging is humanoid only):
   `Tools/Blender/kira_rig.py`, 23 bones, no ear bones, 9 clips, bone-heat skin.
6. Stop for Dan: **done** (stopped after the four people; then fixed Abi first).
7. Kira forms: **done.** `KiraForm` data (Home, Sabertooth), `AreaData.kiraForm`;
   Area A home cat, Area B sabertooth with `ClawIce`: ice cave + shard B06. Kira
   lock checked at build on every form model (no ear bones, grey, no white,
   amber eyes): both OK. Check: Dan's punch doesn't break the ice, Kira's does.
8. Necklace: **done.** `Tools/Blender/necklace.py` (cord, bail, shard); socket =
   Mixamo `Spine2` (offset to tune in Round 3), on the capsule meanwhile; pulses.
9. Kira in the game: **done.** Real model + clips (run/idle follow the motor),
   form swaps per area; capsule kept as fallback. Dan / Abi stay capsules.
10. Build / check / publish: **done.** 0.2.0 (123 MB), `game-200` published; a
   Round 1 starter install updated itself from GitHub to 0.2.0 (ready handshake).

Decisions Code made (Dan can overrule): one Mixamo clip set for everyone;
Meshy's metallic map dropped (fur came out speckled); the sabertooth's head
counter-turn capped at 45 deg; cutscene actors are still Round 1's graybox ones.

**For Dan to check:** Kira's look and moves in play (both forms; does the
sabertooth's turned head bother you?); the ice cave in Area B; the necklace on
Dan's capsule; the hair fix renders in `Data/screenshots/round2/mixamo/`.
Delete two test folders Code couldn't: `C:\Data` (empty), `C:\sot200` (launcher test install).

## Round 4 plan (as the planning chat wrote it)

---------------------------------------------------------------------------
Current round — Round 4: the Ice Age, first real world (target 0.4.0)
Fix 3.1's plan is in `TODO-ARCHIVE.md`. Dan played 0.3.1 (2026-11-09): new
sabertooth Kira "looks good". Report per item: done / not done / deferred, and
why. Defaults the planning chat chose are marked DEFAULT: Dan can overrule
them after playing.

**Carried-over fixes (Dan, 2026-11-09)**
1. **Kira has see-through holes** from some angles (sabertooth form; check home
   Kira too). Find the cause per hole (back-face culling on thin or one-sided
   geometry, flipped normals, real gaps in the Meshy mesh) and fix it at the
   source: recalculate normals, close gaps in Blender, two-sided material only
   where the geometry is genuinely thin. Check: a turntable of each form at
   8 angles plus close-ups, no background showing through.
2. **Abi's hair has see-through holes,** worst in her home outfit. Same
   approach (hair is often one-sided cards from Meshy: two-sided hair
   material, normals fixed, gaps closed). Check: turntable at 8 angles, both
   outfits, plus the player's-view camera while she runs.

**The Ice Age**
3. **Game start = the film's opening.** New game plays Act I as an in-engine
   cutscene (Christmas evening, Kira knocks the ornament off the tree, it
   shatters, the vortex pulls them in), then the Ice Age arrival (they land in
   the snow in caveman outfits; Dan falls backward; sabertooth Kira jumps on his
   chest and paws at the necklace, which starts pulsing blue), then play
   starts. Both skippable. Living room set from Dan's references in
   `SourceArt/Reference/Worlds/00-Home/` (added 2026-11-09): purple-magenta
   walls in warm golden light, red velvet curtains tied back, a tall window with
   snow outside and a garland of lights on the sill, a cream couch with red and
   lime cushions, a lit wreath around a framed picture, a decorated tree with
   red / purple / silver baubles, warm fairy lights and a red heart ornament
   with initials. **The ornament that shatters is the dark silver glass bauble
   Dan is holding** in `home-decorating-tree-ornament.png`: model it as its own
   prop (it reappears whole in Act XI). Stylized to match the characters, not
   photo-real. Our characters wear their concept-sheet outfits, not the film's. The Round 1 graybox
   playground stays reachable only from a debug option (test area).
4. **Level layout** (DEFAULT, from the film + concept): one connected frozen
   valley with distant volcanic mountains, about 5–8 minutes to cross, in zones:
   - **Landing snowfield:** start; teaches run / jump / switch / swim (a
     half-frozen pond) with a few easy shards.
   - **Mammoth ridge:** a slow herd of mammoths you can ride on the back of
     (moving platforms) to reach high shelves.
   - **Ice bridges and a frozen river:** jumping, a cracked bridge that breaks.
   - **Ice caves:** Kira-only crawl tunnels and ice walls only sabertooth Kira
     can claw through; a dark section lit by the necklace's glow.
   - **Volcano vents** at the valley edge: warm steam that lifts the player
     (a vertical route).
   - **Future-ability gates**, visible but closed: a high ledge for Abi's
     double jump, a big boulder for Muscle Man.
5. **Shards, story shard and the vortex** (numbers DEFAULT): 40 small shards
   in the Ice Age, 25 needed. At 25 the necklace wakes (Round 1 cutscene, now
   with the real characters) and leads to the **blue story shard**; collecting
   it **opens a vortex** (blue, the shard's color). **Vortex = travel between
   worlds (DECIDED, Dan 2026-11-09):** every world's story shard opens a vortex;
   stepping into it shows the paths: every world unlocked so far, the next
   world, and **Home**. Home is visible but **blocked until all shards are
   rebuilt** (shows how many story shards are still missing). Build the vortex
   space and its path choice now: paths = Ice Age, Egypt ("to be continued"
   this round, saves), Home (blocked, 1 / 8). The vortex replaces Round 1's
   portal. Rule-2 check: 25 reachable without double jump or
   Muscle Man, proved by data + a play-through.
6. **One new move this round** (DEFAULT): Dan's **ground pound**, learned in
   the Ice Age (breaks cracked ice floors to reach hidden shards, stuns
   enemies). **The Violinist teaches moves (DECIDED, Dan 2026-11-09):** he
   appears somewhere in each world (in the Ice Age with a bone flute or a
   simple prehistoric fiddle, DEFAULT), plays a short tune when a character
   reaches him, and that character learns the move (short in-engine moment,
   no dialogue: he never explains himself). Placeholder figure until his model
   exists; add the Violinist (Ice Age look) to item 9's prompts.
7. **Enemies:** wolves (circle, then lunge) and snow critters (simple
   patrollers). Placeholder shapes until their models exist (item 9). Light
   combat, per the concept. No boss this world (DEFAULT).
8. **Look:** stylized and colorful, matching the characters. Dan's reference
   `SourceArt/Reference/Worlds/01-IceAge/iceage-valley-volcanoes.jpg` sets the
   layout and palette: a wide snowy valley between warm brown rock cliffs,
   **two erupting volcanoes** with glowing lava and ash plumes at the far end,
   mammoths in the blowing snow, a rock cave mouth lit by firelight, standing
   stones and stone ruins in the snow. The reference is semi-realistic; make it
   stylized like the characters. Soft blue-white light, fog for depth. Terrain and ice by Blender script; rocks, bones and trees as
   simple stylized props (Meshy text-to-3D allowed for props). Must hold 60 fps
   at 1080p on Dan's PC and 40+ on the Steam Deck: measure both settings.
9. **Creature models: prompts only, then stop for Dan.** Add concept prompts to
   `Docs/MODELS-NEEDED.md` for: wolf, snow critter (design it: small, round,
   cute-grumpy), mammoth (rideable, big flat back), plus any prop Meshy can't
   do from text. Same rules as before (full body, side view for animals, plain
   background). Tell Dan when they're ready; he makes the images, and the
   models come in the next round. Keep building with placeholders meanwhile.
10. **Music and sound:** an Ice Age theme with Suno (whimsical, adventurous;
    loops cleanly), ambience (wind), and ElevenLabs effects for footsteps on
    snow, ice cracking, shard pickup, necklace pulse, wolf growl. Sound checks
    as short as possible, then mute again.
11. Build, check (as Dan plays it: a full run from New game to the Egypt
    portal), publish 0.4.0, update `TODO.md` / `CLAUDE.md`, clean up.

**Decided this round (Dan, 2026-11-09):** the Violinist teaches moves; the
vortex is the travel hub (path home blocked until all shards are rebuilt).

---------------------------------------------------------------------------

### Round 3: Dan and Abi in the game, animated (0.3.0, build 300, 2026-11-09)
Final check `local/round3/final` (built exe, batch mode, muted, no window,
virtual pad from the title screen; play + Round 3 models + measure + perf):
RESULT all checks passed (171 ok); audio output 0.000 RMS. Evidence:
`Data/screenshots/round3/` (`game/` 39 exe shots from the player's view +
necklace close-ups, `cutscenes/` frames of the two MP4s, `exe_check_final.txt`).
1. Import: **done.** Four models Humanoid (Mixamo T-poses for Dan, `rigged_hairfix.fbx`
   for Abi), 20-21k triangles, LODs by Unity's own mesh LODs (made from the rigged
   mesh, so skin weights kept; 10-11 levels). One Mixamo clip set for all four (16
   clips, trimmed in `CharacterImport.PeopleClips`): idle, walk / slow run / run
   blended by speed, jump, fall, land, hard landing (falls of 3.3 m+), swim + tread,
   push, punch (Dan), kick (Abi), hit, cheer (shard pickups; everyone on the story
   shard), look (cutscenes). Punch / cheer / hit go on the upper body while running.
   Check: a planted foot slides 0.3-0.7 m/s while running (4-9% of run speed; limit
   12%) after the clip speeds were tuned once from the check (x1.09).
2. Capsules replaced: **done.** Dan and Abi as player and followers; outfit per
   area like Kira's form (`AreaData.outfit`: A home, B Ice Age); capsules kept as
   fallback. Check: outfits swap both ways at the portal; feet on the ground
   (lowest toe -2.5 to -1.3 cm), every model faces its way (0.96-1.00).
3. Necklace: **done.** Fitted per outfit from the model's own chest (neck bone,
   clothes under the shard, neck width) on `Spine2`, 3 cm off the clothes. Check:
   shard outside the clothes while running (home: 1.5 cm in front at the
   deepest; Ice Age: touches the fur, 0.2 cm in at the deepest, limit 0.5 cm); close-ups in `game/r3_dan_*_necklace_closeup.png`.
4. Cutscenes: **done.** NecklaceWakes and PortalOpens rebuilt with Dan, Abi (home
   clothes) and Kira as actors with clip tracks (idle / look / cheer; Kira idle /
   jump); both MP4s rendered again (`Recordings/`, 1920x1080, 30 fps).
5. Checked as Dan sees it: **done.** New exe check `--only models`: per person and
   outfit idle / run front / run side / jump / attack / swim / tread / push / hard
   landing / cheer, followers, switching. Found and fixed: foot sliding, Abi's run
   speed, the necklace sinking into the sweater mid-stride, Dan's head in the push
   block (the model steps back 25 cm while pushing), the cutscene glow covering
   Dan's face. Left as is: followers can overlap each other in the pool (followers
   never collide, by design); Abi's long hair looks stringy close up (Meshy mesh).
6. Build / check / publish: **done.** 0.3.0 (166 MB), `game-300` published; a
   Round 1 starter install updated itself from GitHub to 0.3.0 (ready handshake).

Decisions Code made (Dan can overrule): Unity mesh LODs instead of Blender LODs;
heroes always animate (no off-screen culling: 3 characters, cheap); hard landing
from 14 m/s; cheer on every shard pickup (upper body if running); the model lifts
0.28 x height when swimming at the surface; Abi's double jump replays the jump clip.

**For Dan to check:** Dan and Abi running, jumping, swimming, punching / kicking on
a pad (both outfits: Area A and B); the necklace on both outfits; the two
cutscenes in play and the new MP4s; whether the cheer on every shard is too much.

---------------------------------------------------------------------------
Round 5 plan (as written by the planning chat)
---------------------------------------------------------------------------
Current round — Round 5: Ice Age polish, a bigger valley, creature models (target 0.5.0)
Round 4's plan is in `TODO-ARCHIVE.md`. Dan played 0.4.0 (2026-11-09): "for a
start not too bad", clean-up needed, and the notes below. Report per item:
done / not done / deferred, and why. DEFAULT = planning chat's choice, Dan can
overrule after playing.

**Dan's notes on 0.4.0**
1. **Rewrite `IceAgeArrival` (Dan's story beat).** They land in the snow. Dan
   gets up and sees what looks like a **sabertooth about to attack him**: shoot
   Kira from a low angle in silhouette / shadow against the bright sky so she
   reads as big and dangerous, a growl, fangs. Dan scrambles backward and
   **falls flat on his back in fright**. Kira leaps onto his chest; close-up:
   he sees the folded ears and amber eyes and realizes it's **Kira**: big
   relief (he laughs, slumps back). She paws at his chest, **pointing out the
   necklace** he didn't know he had; he holds it up, it pulses blue. Abi
   laughs. Comedy is situational, no dialogue needed. Keep it about the same
   length (13–16 s) and skippable; render the MP4 again.
1b. **Kira still has see-through spots (Dan, 2026-11-09, after 0.4.0).**
   Round 4's reversed-twin faces cut it down but didn't finish it (sabertooth
   still 168 px in the exe). Get to **zero** see-through pixels, both forms,
   any angle, in the exe. If the Meshy mesh itself is open or non-manifold,
   don't patch it again: rebuild a closed mesh in Blender (voxel remesh or
   shrink-wrap to a solid body), re-bake the texture onto it, re-skin to the
   same rig, keep the Kira lock checks. Also check the material isn't using
   alpha / transparency anywhere. Check: turntable at 16 angles + close-ups of
   ears, tail, paws and fangs, and a player's-view run, with the measured
   see-through count (must be 0). Same check for Abi's hair (32 / 30 px left).
2. **The trees are lying down** ("Why were all the trees down?"). Find the
   cause (likely the pine prop's up-axis from Blender, not its placement) and
   fix it at the source. Then check **every** Ice Age prop is upright and
   sitting on the ground (pines, rocks, bones, crystals, stones), with a shot.
3. **Clean-up pass.** Before adding anything: go through Round 4's own exe
   screenshots and new ones from the player's view across the whole valley,
   and list every visual problem found (things lying wrong, floating or sunk
   props, clipping, seams, stretched textures, holes, popping). Fix them, and
   put the list (fixed / not fixed) in the report. Dan will add his own list
   after playing.

**Music (standing rule in `CLAUDE.md`: level songs are Dan's)**
4. Remove Code's Ice Age stand-in (`Music/iceage.mp3`, and from
   `make_sounds.py`). Play Dan's files when they exist, silence until then:
   - `Music/title.mp3`: **title screen (new, Dan 2026-11-09)**: its own main
     theme, different from Act I and the Ice Age. Prompt in `Docs/MUSIC.md`.
   - `Music/act1.mp3`: during `ActOne`, fade out at the white-out.
   - `Music/iceage.mp3`: starts with `IceAgeArrival`, then loops in the world.
   - `Music/vortex.mp3`: **inside the vortex space (new, Dan 2026-11-09)**,
     loops; crossfade in when the party enters, back to the world's theme
     when they leave.
   The Violinist's tune and the sound effects stay (ElevenLabs).
   **Dan's four tracks are in (2026-11-09):** `Music/title.mp3` (3:00),
   `act1.mp3` (22.4 s), `iceage.mp3` (3:00, replaces Code's stand-in: don't
   regenerate it; remove the iceage entry from `make_sounds.py`), `vortex.mp3`
   (3:00). Act I's track runs 3.4 s past the 19 s scene: fade it out over the
   white-out (or hold the white-out a moment longer if it ends on a nicer
   note). Check all four load and play in the right places (muted run, level
   reported); Dan judges the sound.

**A bigger Ice Age with places for later (Dan, 2026-11-09)**
5. **Grow the valley** (DEFAULT): about 10–15 minutes for a first visit (now
   6.6). Keep what's there and add three zones:
   - **Frozen lake:** wide ice with skating slides, holes in the ice, and a deep
     part under the ice for later (see the gates below).
   - **Glacier climb:** a tall wall of ice and ledges up to a summit with a view
     over the whole valley and the volcanoes.
   - **Hot-spring grotto:** a warm hidden cave by the volcano side, behind a
     cracked rock wall.
6. **Places that open with later moves.** Every one is visible on the first
   visit so the player knows to come back, and holds shards, a heart upgrade
   or both. Use the move roadmap below (DEFAULT, see open question):
   - double-jump ledges (Abi, Egypt): keep the two, add one;
   - boulders (Muscle Man, Medieval): keep the one, add one;
   - cracked rock walls (Dan's charged punch, Medieval): the grotto entrance;
   - the deep lake under the ice (deep dive, Pirate Seas);
   - wall-jump chimneys in the glacier (Abi's wall jump, Steampunk).
   Build the gates now as data (which move opens each); the moves themselves
   come in their worlds.
7. **Shards and hearts:** grow to about 60 small shards (DEFAULT) with the
   requirement about 35; Rule 2 must still hold (35 reachable with the moves
   you have on the first visit: no double jump, no Muscle Man, and the ground
   pound only after the Violinist). Hide two heart upgrades in the valley, at
   least one behind a later-move gate.

**Creature models**
8. **Meshy + Mixamo for the Violinist first, then stop for Dan.** Dan's concept
   `SourceArt/Concepts/violinist_iceage.png` → Meshy → clean-up → Mixamo upload
   FBX + clip list (idle, play-instrument idle, walk, a small bow/nod). Stop and
   tell Dan as soon as his file is ready (Dan does Mixamo the same day).
   **When Dan's clips arrive, import them in this same round** (don't wait for
   a later round).
9. **Wolf, snow critter, mammoth:** Meshy from Dan's concepts, Blender rig and
   clips by script (four-legged, like Kira): wolf (idle, walk, run, circle,
   lunge, hit, stunned), critter (idle, waddle, hit, stunned), mammoth (walk,
   idle; ride collider stays flat). Replace the placeholders; keep them as
   fallback.
10. Build, check (a full run from New game: Act I, the new arrival, the whole
    valley, story shard, vortex), publish 0.5.0, update `TODO.md` / `CLAUDE.md`,
    clean up.

**Move roadmap (DEFAULT, open for Dan; moves are kept across worlds, so later
moves reopen earlier worlds):**
| World | The Violinist teaches | Also |
|---|---|---|
| Ice Age | Dan: ground pound | |
| Egypt | Abi: double jump | |
| Bethlehem | Kira: pounce (long leap) | |
| Medieval | Dan: charged punch | Muscle Man joins (boulders, lifts Dan to high ledges) |
| Pirate Seas | everyone: deep dive (longer breath) | |
| Steampunk | Abi: wall jump | |
| Tomorrowland | Abi: air dash | |
| Moon | none (the finale) | |
---------------------------------------------------------------------------

---------------------------------------------------------------------------
Round 6 plan (as written by the planning chat)
---------------------------------------------------------------------------
Current round — Round 6: Ice Age fixes + Ancient Egypt (target 0.6.0)
Round 5's plan is in `TODO-ARCHIVE.md`. Report per item: done / not done /
deferred, and why. DEFAULT = planning chat's choice, Dan can overrule after
playing. Testing rule as always: everything through `Tools/quiet.py`, muted
with the level reported, never a window in front of Dan.

**Dan's notes on 0.5.0 (do these first)**
1. **"They can't walk a straight line."** Measure before fixing: in the exe,
   hold the stick straight up for 10 s (and W on the keyboard) with each
   hero, on flat snow, and log heading drift per second, camera yaw, and the
   raw stick values. Likely suspects, in order: movement is camera-relative
   while the camera's auto-recenter keeps turning, so the path curves; the
   stick deadzone / a radial-vs-axial deadzone bending the direction; the
   heroes turning toward the input too slowly or overshooting; followers
   pushing the player. Fix the real cause; holding forward must go straight
   (drift under 1 deg/s) and still steer smoothly when the stick moves. Show the
   before / after numbers. Dan checks it on his pad.
2. **Under the ice was awkward and hard to get out of.** Rework the under-ice
   swim so getting out is always easy and obvious: every under-ice stretch is
   short with a lit opening ahead (light shafts through the holes), holes big
   enough to surface through from any angle, swimming up under a hole pops the
   hero out onto the ice (auto climb-out), and the camera never ends up inside
   the ice ceiling. Breath must last longer than the longest stretch with
   room to spare. Nobody can get stuck under the ice: if a hero is under it for
   too long without a way up, bring them to the nearest hole. Check with the
   bot and with screenshots from the player's view.
3. **Mammoth fur sticking out of its tusks.** The closed-mesh rebuild baked the
   fur texture onto the tusks / hair poked through them. Give the tusks (and any
   other hard parts: wolf and critter teeth and claws, Kira's fangs) clean
   geometry and their own plain material (ivory), no fur on them. Close-ups of
   each creature's head as the check.
4. **The Violinist had no instrument.** The Round 4 bone fiddle was lost when
   his real model came in. Model his Ice Age instrument (bone fiddle + bow) and
   attach it to his hands (fiddle in the left hand under the chin, bow in the
   right), so it moves with his `play` clip and sits right in idle / walk (or
   rests on his back when walking, DEFAULT). Make it data per world: each world
   gives him its own instrument (Egypt: lyre). Close-up shots playing and idle.

**Ancient Egypt (world 2)**
5. **Concept images: the planning chat wrote the prompts** (E1–E9 at the top of
   `Docs/MODELS-NEEDED.md`, from Dan's film art in `SourceArt/Reference/`); Dan
   is making the images. Don't rewrite the prompts; check each image when it
   arrives (the usual list). **All nine are in (Dan, 2026-11-09), saved in
   `SourceArt/Concepts/`** (`dan_egypt`, `abi_egypt`, `kira_mummy`, `camel`,
   `violinist_egypt`, `mummy`, `scarab`, `guardian`, `sphinx_kira`); the
   planning chat looked at each: all usable. Two notes: **mummy Kira's head is
   turned a little toward the viewer** (less than home Kira's 34 deg by eye):
   measure it; up to ~35 deg use the counter-turn as for home Kira and check the
   face close up (Fix 3.1's trouble was 83 deg); if it's worse, stop and ask Dan
   for a straight-headed image. Her fur is darker / browner than home Kira's:
   match it (`match_fur.py`). The Violinist is the older gray-bearded man of
   his Ice Age sheet (the prompt said "young": the image is right). Dan's
   Egypt level song is in too: `Music/egypt.mp3` (3:00). The set: Dan and Abi in
   Egyptian clothes (A-pose), **mummy Kira** (Kira lock: folded ears showing
   through the bandages, solid gray where fur shows, amber eyes), the
   **judgmental camel**, a mummy enemy, a scarab enemy, a temple guardian
   (stone jackal / cat statue that wakes up), and the Violinist with a **lyre**
   (only the instrument changes if his outfit can stay; DEFAULT: an Egyptian
   robe over the same body). Tell Dan as soon as the prompts are ready, and keep
   building with placeholders. When the images arrive: Meshy → clean-up →
   Mixamo files for Dan, Abi and the Violinist → **stop for Dan's Mixamo** →
   import in this same round. Add to Dan's Mixamo list: a laugh ("Laughing")
   and a "hold up / look at item" clip, so the Ice Age arrival can have Dan's
   relieved laugh and holding up the necklace (Round 5 left them out).
6. **Getting there:** the vortex's Egypt path is real now (Home blocked: 2 / 8
   after Egypt). `EgyptArrival` cutscene (skippable, ~10–15 s): the vortex
   drops them among pyramids and busy workers; they're in Egyptian clothes;
   Kira is a little mummy cat; Dan and Abi react to her. Look and staging from
   `Worlds/02-Egypt/trio-arrival-pyramids-*.png`.
   **Dan's necklace shard takes the color of the world's story shard** (the
   film: blue in the Ice Age, red in Egypt), for the necklace prop and its glow.
7. **Layout** (DEFAULT, from the film + concept), about 10–15 minutes first
   visit: a sunny desert town by the Nile and the pyramids:
   - **Town and market:** merchants with baskets, workers hauling huge stones on
     rollers (moving platforms), guards patrolling (they spot you and push you
     back to the last checkpoint: light stealth, not combat), and the
     **judgmental camel** (a running gag: it stares at whichever hero you
     play, snorts, turns away) and **rideable: the whole party rides it across the dunes, as in the film** (`camel-ride-trio.png`).
   - **The Great Sphinx with Kira's face: THE big gag of Egypt (Dan,
     2026-11-09), must be in.** Model from E9 (`sphinx_kira.png`); film shots
     `sphinx-kira-far / -mid / -closeup.png`. Make it a landmark seen from far
     away, and build the `SphinxGag` cutscene properly: the party walks up, the
     camera slowly reveals the face, Dan does a big double-take between the
     Sphinx and mummy Kira (Kira sits in the same pose), Abi rolls her eyes.
     Comic timing over length (~10 s). The Sphinx can also be climbed (shards
     on its paws, head and back).
   - **The pyramid under construction** (film: `market-workers-pyramid-scaffold.png`):
     wooden scaffolding up one face, ramps, workers pushing stone carts: a
     climbing route to the top and the way in.
   - **Pyramid interior:** traps (swinging blades, sliding blocks, sand pits,
     dart walls), dark corridors lit by the necklace, ancient machinery
     (rotating rooms, counterweights), hidden chambers.
   - **The Nile bank:** reeds, boats as moving platforms, a swim.
   - **Future-move gates** (data, visible first visit): Kira's pounce gaps
     (Bethlehem), Muscle Man blocks (Medieval), cracked walls (charged punch),
     deep water (deep dive), wall-jump shafts (Steampunk).
8. **Mummy Kira's ability** (DEFAULT): Egypt honors cats, so **cat-statue doors
   and cat mechanisms open only for her** (she sits on a cat plate and the
   mechanism wakes), and as a mummy she can walk past the mummies without
   them waking. Placeholder until her model exists.
9. **New move: Abi's double jump**, taught by the Violinist on his lyre. Once
   learned it works everywhere, so the Ice Age's double-jump ledges open too:
   check one in the Ice Age after learning it.
10. **Shards, hearts, story shard:** about 60 shards, 35 needed (DEFAULT); Rule 2
    holds (35 reachable with the moves you have on arrival; the double jump
    only after the Violinist). Two hearts. The **red story shard** (DEFAULT: in
    the pharaoh's burial chamber at the heart of the pyramid) opens the vortex.
11. **Enemies:** mummies (slow, shamble, grab), scarabs (small, fast, swarm a
    little), temple guardians (statues that wake when you pass, a short chase).
    Light combat; no boss (DEFAULT).
12. **Look:** from Dan's film references in `SourceArt/Reference/Worlds/02-Egypt/`
    (added 2026-11-09): golden dunes under a deep blue sky, two big pyramids,
    a colorful pastel town (pink, turquoise, lilac and gold buildings with
    domes and palm trees) beyond the dunes, a busy market with striped
    awnings, baskets and workers hauling stone on carts, camels everywhere.
    Stylized like the characters. Frame rate checked
    on both settings.
13. **Music:** Egypt's level song is Dan's (prompt in `Docs/MUSIC.md`, save as
    `Music/egypt.mp3`; silence until it exists). Sound effects with ElevenLabs
    (sand steps, trap clicks, stone grinding, mummy groan, scarab skitter,
    camel snort, the Violinist's lyre tune).
14. Build, check (a full run: Ice Age → vortex → Egypt → red shard → vortex),
    publish 0.6.0, update `TODO.md` / `CLAUDE.md`, clean up.

---------------------------------------------------------------------------
Fix 3.1 delivery (moved from TODO.md, 2026-11-10)
---------------------------------------------------------------------------
### Fix 3.1: sabertooth Kira's face (0.3.1, build 301, 2026-11-09)
Check `local/round3/fix31_play` (built exe, batch mode, muted, no window, virtual
pad from the title screen, `--only play`): RESULT all checks passed; audio output
0.000 RMS. Evidence: `Data/screenshots/round3/fix3.1/` (`kira_sabertooth_face_sheet.png`
= the 9 close-ups, clip frames, `12d`-`12g` exe shots, `exe_check_play.txt`).
1. New concept: **done** (checked again by Code before use: head in profile in line
   with the body, ears folded, mouth closed, both fangs straight down, all paws and
   tail in frame, amber eyes, no white: passes).
2. Meshy + clean-up + rig: **done.** New Meshy model (30 credits), 14.7k tris, 0.5 m;
   it came out facing front, so the old 90 deg turn in `meshy_cleanup.py` is gone.
   Fur matched to home Kira by `Tools/match_fur.py` (fur average rgb 115,100,97 ->
   118,111,109; home Kira 117,110,109; eyes / nose / fangs left alone). Rig: head
   measured 6 deg off straight (old model: 83 deg); **no counter-turn** for the
   sabertooth. Home Kira keeps hers (her head is 34 deg turned; she isn't re-rigged).
   Kira lock at build: OK (no ear bones, 0% white, amber eyes).
3. Face previews: **done.** Front / side / 3/4 of the head in idle, run and attack
   (Blender) and in the exe (player's view `12d`, close-ups `12e`-`12g`): both eyes
   look the same way in every shot, face not stretched, fangs straight down from
   under the upper lip. Her pupils sit slightly outward, mirrored (Meshy's eyes, not
   the rig). Old model kept: `SourceArt/Rigged/kira_sabertooth_v1/model.fbx` (in git)
   and `SourceArt/Meshy/kira_sabertooth_v1/` (local only, git-ignored).
4. Build / check / publish: **done.** 0.3.1 (166 MB), `game-301` published; a fresh
   Round 1 starter install from GitHub updated itself to 0.3.1 (ready handshake;
   game hidden, batch mode, master volume 0). Disk: 2.7 GB -> 2.3 GB.

Decisions Code made (Dan can overrule): the counter-turn is now per model
(`COUNTER_TURN` in `kira_rig.py`, home Kira only); the fur match also moves the
fur's brightness spread to home Kira's.

**For Dan to check:** sabertooth Kira in Area B on a pad, face toward the camera
(idle, running, clawing the ice); whether her gray now reads as the same cat as
home Kira.

===========================================================================
Fix 6.1 plan (as written by the planning chat)
===========================================================================
Current round — Fix 6.1: universal shards + the Violinist's lyre arms (target 0.6.1)
Round 6's plan is in `TODO-ARCHIVE.md`. Dan played 0.6.0 (2026-11-10). Report
per item: done / not done / deferred, and why. Bethlehem (Round 7) comes after
this fix.

1. **Shards are universal (Dan, 2026-11-10; the original concept, section 5:
   "a shared collection total, not consumed").** Rounds 4–6 wrongly made each
   world's requirement count only that world's shards (35 of Egypt's 60), so
   extra shards had no purpose. Change it:
   - The player has **one shard total** across all worlds (every small shard
     ever collected, never spent).
   - Each world's necklace wakes when the **total** reaches that world's
     threshold (cumulative). DEFAULT thresholds: Ice Age 35, Egypt 75. Later
     worlds get theirs when they're built, about 60–65% of all shards that
     exist up to that world.
   - So extra shards from any world (e.g. Ice Age ledges reopened by the double
     jump) count toward later worlds: collecting more means less hunting later.
   - **Rule 2, updated:** a world's threshold must be reachable with the shards
     available on arrival there: all earlier worlds with the moves you have by
     then, plus this world's first-visit shards. Report the numbers per world
     (e.g. Egypt: Ice Age 43 + Egypt 50 = 93 available on arrival >= 75).
   - **HUD:** the big number is the total; under it "Next: N" (the current
     world's threshold) until the necklace wakes there. The per-world count
     ("Egypt: 41 / 60") stays in the pause menu as the completion tracker.
   - **Saves:** existing saves keep their collected shards; the total is just
     their sum. If a save already has a world's story shard, nothing relocks.
   - Update `CLAUDE.md` rule 3 and `Docs/Design/design-decisions.md` to say
     this, so it doesn't drift back.
   - Check: a play-through where the Egypt necklace wakes from the total (with
     Ice Age extras counted), and a save from 0.6.0 loaded into 0.6.1.
2. **The Violinist's arms look wrong playing the lyre (Dan, 2026-11-10).** The
   Mixamo clip is a violin pose (left arm up under the chin, bow arm sawing),
   which the two-arm IK bends round a lyre. The lyre needs its own playing
   animation: hold the lyre upright against his chest / left hip with the left
   arm wrapped round the frame, the right hand plucking the strings in front of
   it, a gentle body sway, head tilted toward the strings. Make it per
   instrument (data next to `violinistInstrument`): fiddle keeps the violin clip,
   lyre gets the new one (author it in Blender or with Animation Rigging; no
   new Mixamo download needed unless Code finds a fitting clip, then say which).
   Check: close-ups from the front, side and the player's camera during
   `ViolinistTeachesEgypt` and his idle playing, hands on the instrument, no
   arm through the body or the lyre.
3. Build, check, publish 0.6.1, update `TODO.md` / `CLAUDE.md`, clean up.

===========================================================================
Round 4 delivery (moved from TODO.md, 2026-11-10)
===========================================================================
### Round 4: the Ice Age, the first real world (0.4.0, build 400, 2026-11-09)
Final check `local/round4/final2` (built exe, batch mode, muted, no window, virtual pad from the title screen; Ice Age
play-through + items 1-2 + frame rate): RESULT all checks passed (96 ok); audio output 0.000 RMS. Evidence:
`Data/screenshots/round4/` (`game/` exe shots from the player's view + turntable sheets, `cutscenes/` frames of the MP4s,
`holes/` Blender before/after, `exe_check_final.txt`). The plan as written is in `TODO-ARCHIVE.md`.
1. Kira's holes: **done.** Cause: single-sheet fur tufts and whiskers seen from behind (culled). Fix at the source: every face
   a camera can see from behind gets a reversed twin (`Tools/Blender/mesh_fix.py`, in `kira_rig.py`). See-through pixels over
   14 Blender views: home 298 -> 0, sabertooth 2,290 -> 499; in the exe (12 views, two-sided vs. normal): home 0, sabertooth 168 (worst view 44).
2. Abi's hair: **done.** Same cause (one-sided strands) plus the strip Round 2's hair fix cut between hair and shoulders. Same
   fix in `fix_hair_weights.py`. Blender: home 12,882 -> 63, Ice Age 9,469 -> 322; exe: 32 and 30 (worst view 19). Turntables at 8
   angles + head close-ups, and running shots in the player's view: `game/r4_turntable_*.png`, `game/r4_run_*`. Dan unchanged (0 before).
3. Opening: **done.** New game plays `ActOne` (19 s, the living room from Dan's references, Kira bats the dark silver bauble off
   the tree, it shatters, the vortex pulls them in) then `IceAgeArrival` (13.5 s: they land in the snow in caveman clothes, Dan falls
   flat on his back, sabertooth Kira jumps on his chest and paws at the necklace, which pulses blue). Both skippable (checked).
   Both rendered to MP4 (`Recordings/`). The graybox playground: Settings > Test area (debug), its own save file.
4. Layout: **done** as the DEFAULT: one valley (~500 m), snowfield + pond, mammoth ridge (two mammoths walk a loop; ride one up to a
   high shelf), gorge with a cracked bridge (planks drop), ice pillars and a river with a ramp out, ice caves (dark; Kira's crawl
   tunnel; an ice wall for sabertooth Kira; cracked ice over a pit), three steam-vent terraces, standing stones; future gates: two
   double-jump ledges, a boulder (Muscle Man). The bot crossed all of it with the pad: 6.6 min from landing to the story shard, 36 shards (target 5-8 min).
5. Shards / story shard / vortex: **done.** 40 shards, 25 needed (DEFAULT). Rule 2: 33 of 40 reachable without double jump, Muscle Man
   or the ground pound (data); the play-through had 27 before Dan learned the ground pound. Necklace wakes (cutscene, now Ice Age
   actors) -> blue story shard on the altar -> `VortexOpens` -> the vortex space: Ice Age (back), Egypt ("to be continued", saved),
   Home (blocked: "7 story shards are still missing"). Replaces the portal (the test area keeps its own).
6. Ground pound + Violinist: **done.** Dan: jump, then B / Ctrl: breaks cracked ice (2 places) and stuns enemies. The Violinist
   (placeholder figure with a Blender bone fiddle, by a campfire at the cave) plays (`ViolinistTeaches`) when Dan reaches him; to
   anyone else he "is looking past you, at Dan". Added to item 9's prompts.
7. Enemies: **done.** Wolves (spot you, circle at 5.5 m, one lunges at a time) and snow critters (patrol), placeholder shapes;
   the ground pound stuns them. No boss.
8. Look: **done.** Blender terrain (snow / faceted rock), two distant volcanoes with lava and ash plumes, fog to 1.7 km, blowing snow,
   pines, rocks, bones, ice crystals, standing stones. Frame rate with the view drawn every frame: 262 fps at 1080p on Dan's GTX 1660
   Ti (need 60); Steam Deck setting at 1280x800 on the same PC: 271 fps (need 40). The Deck itself was not measured (no Deck here).
9. Creature prompts: **done**, then Dan made the images the same day: `SourceArt/Concepts/wolf.png`, `snow_critter.png`,
   `mammoth.png`, `violinist_iceage.png` (renamed from Dan's files; checked: full body, plain background, >= 1024 px, no props).
   Models: **not started** (as planned: next round). The mammoth's back came out rounded, not flat: fine (the game rides a flat collider).
10. Music and sound: **done with one stand-in.** ElevenLabs: footsteps on snow, ice cracking, shard, necklace, wolf growl, ground
    pound, the Violinist's tune, wind loop, and the Ice Age theme (its music API worked: 90 s). The theme is meant to be Suno: Code
    can't call Suno, so the prompt is in `Docs/MUSIC.md` for Dan; his file replaces the stand-in. Checked: clips loaded and playing,
    1,022 effects fired; nothing audible was played on this PC (every run muted, 0.000 output).
11. Build / check / publish: **done.** 0.4.0 (179.5 MB), `game-400` published; a fresh Round 1 starter install from GitHub updated
    itself to 0.4.0 and started (ready handshake; muted test mode). Disk: 3.2 GB -> 2.5 GB.

Decisions Code made (Dan can overrule): the high shelf is one long shelf 1 m over a mammoth's back (three shelves 1.3 m up were too
hard to reach); ice pillars 2.1 m apart; the necklace wakes only once you're out of a cave (its cutscene needs open sky); a fall off
the vortex platform costs a heart like any fall (rails added); invisible walls round the valley; the music volume is a new setting.

**For Dan to check:** New game on a pad: the Act I cutscene (does the room feel like your film?) and the arrival (Dan's fall, Kira
on his chest); riding a mammoth up to the shelf; the cracked bridge; the dark cave with the necklace's light; the Violinist and the
ground pound; the steam vents; the vortex and "to be continued". Whether wolves are too easy / too hard. Kira's fur and Abi's hair
from behind and close up. The stand-in music and the sounds (they've only been checked as files, never heard). The volcanoes read
as distant brown cones (lava barely visible through the fog): say if you want them closer or brighter.

===========================================================================
Round 7 plan (as written by the planning chat, 2026-11-10)
===========================================================================
---------------------------------------------------------------------------
Current round — Round 7: Bethlehem (target 0.7.0)
Fix 6.1's plan is in `TODO-ARCHIVE.md`. Report per item: done / not done /
deferred, and why. DEFAULT = planning chat's choice, Dan can overrule after
playing. Testing rules as always (`Tools/quiet.py`, muted with the level
reported, never a window in front of Dan).

**Inputs, all in (Dan, 2026-11-10):** the 12 concept images B1–B12 in
`SourceArt/Concepts/` (`dan_bethlehem`, `abi_bethlehem`, `kira_lamb`,
`wiseman_santa`, `wiseman_crown`, `wiseman_old`, `violinist_bethlehem`,
`watchman`, `goat`, `sheep`, `guard_dog`, `scorpion`; the prompts are in
`Docs/MODELS-NEEDED.md`), Dan's film references in
`SourceArt/Reference/Worlds/03-Bethlehem/` and `Characters/*`, and Dan's song
`Music/bethlehem.mp3` (3:00, with choir). `guard_dog`'s head is turned toward
the camera a little: measure it; under ~35 deg use it with the counter-turn,
else stop and ask Dan for a side-profile redo (the goat and sheep prompts
show how).

**Tone (Dan, 2026-11-10):** irreverent, Monty Python-style humor is welcome
here. The film's own gags stay: one Wise Man wears a Santa hat, their gifts
are modern wrapped presents with bows, they mistake the vortex for the star
and leave disappointed. Situational comedy, minimal dialogue.

1. **Models.** Meshy → clean-up → previews for all 12. Mixamo upload files and
   clip lists for the people (`dan_bethlehem`, `abi_bethlehem`, the three Wise
   Men, `violinist_bethlehem`, `watchman`), reusing the shared idle / walk /
   run clips; **stop and tell Dan exactly what to download** as soon as those
   files are ready, then import his clips in this same round. Animals and the
   scorpion: Code's rigs and clips (four- / eight-legged), heads checked like
   Kira's. Lamb Kira: Kira lock check, fur matched to her gray. Keep building
   with placeholders while waiting.
2. **Props from the film shots** (Blender / Meshy text-to-3D): the barn (rustic
   boards, X-braced doors, a round window in the gable, thatched roof that can
   show the crash hole), the manger, hay bales, barrels, rail fences, the
   Wise Men's gift boxes with bows, the watchman's lantern, the Violinist's
   instrument (DEFAULT: a small shepherd's harp, held with its own playing
   clip like the lyre, not the violin clip). The Wise Men ride Egypt's camel
   model with plainer blankets.
3. **`BethlehemArrival`** (skippable, ~12–15 s): the golden vortex opens over
   the barn, they drop out of it and **crash through the roof** (film:
   `barn-roof-landing-top.png`, `barn-roof-hole-top.png`); Abi lands softly in
   the hay, Dan misses the hay and hits the floor; Kira pops up as a little gray
   lamb. The manger glows faintly but is empty. The hole stays in the roof for
   the rest of the world. Dan and Abi react to lamb Kira.
4. **Layout** (DEFAULT), about 10–15 minutes first visit, a starry night:
   - **The barn and its farmyard:** fences, hay stacks to climb, the golden
     vortex ring hovering above with its beam down (everyone in the world
     calls it "the star").
   - **The hills:** rolling dark grass hills with shepherds' campfires and
     flocks of sheep wandering; rocky outcrops, a well, an olive grove.
   - **The village:** a small night village beside the fields: flat rooftops to
     run across, a lantern-lit market with closed stalls, courtyards, an inn
     with a "no room" sign gag.
   - **The Wise Men's caravan:** the three Wise Men ride their camels along a
     road across the map toward the star the whole time (they stop, argue,
     check a map); they're always reachable and a few shards sit along their
     route.
   - **Future-move gates** as data, visible on the first visit: Muscle Man
     blocks, charged-punch walls, deep water (the well), wall-jump chimneys.
5. **Light stealth (DEFAULT):** night watchmen with lantern light cones patrol
   the village; caught = a short comic "Halt!" moment and back to the last
   checkpoint, not damage. Guard dogs sniff around and bark to call a watchman.
6. **Lamb Kira's abilities:** **hide in a flock** (step into a flock of sheep:
   watchmen, dogs and goats ignore her while she moves with it), and
   **springy bounce** on hay bales and wool piles (a high bounce only she gets).
   Cat-face lamb gags welcome (the sheep stare at her).
7. **New move: Kira's pounce** (a long leap with a short aim), taught by the
   Violinist on his harp. It works everywhere, so Egypt's pounce gaps (E14 /
   E15 and the far roof) open too: check one in Egypt after learning it.
8. **Enemies:** grumpy goats (lower their head, charge, headbutt), guard dogs
   (bark, chase a short way), scorpions (small, scuttle, snap). Light combat,
   no boss (DEFAULT).
9. **Shards (universal rule):** about 60 small shards in Bethlehem; the
   threshold for Bethlehem is cumulative, set by rule 2 (about 60–65% of all
   shards in the Ice Age + Egypt + Bethlehem, reachable on arrival with the
   moves you have, the pounce only after the Violinist). Two hearts. Report the
   numbers.
10. **The finale (`WiseMenArrive`, skippable):** when the necklace wakes it leads
    to the barn: the **golden story shard** now glows in the manger (film:
    `manger-golden-shard.png`). Collecting it: the Wise Men walk in, gifts held
    out, the choir swells ("Hallelujah!"), they see a glowing shard and three
    confused travelers (one of them a cat-faced lamb), look at each other,
    and walk out disappointed. Then the hay explodes upward into a spinning
    golden vortex that pulls the party in → the vortex space (Home: 3 / 8).
11. **Look:** from Dan's references: deep blue starry sky, warm golden light
    spilling from the barn, dark rolling hills, golden grass in the lamplight,
    stylized like the characters. Frame rate checked on both settings.
12. **Music and sound:** `Music/bethlehem.mp3` in the world, the vortex track in
    the vortex as before. ElevenLabs effects: hay rustle and bounce, sheep
    bleats, goat bleat-charge, dog bark, scorpion click, lantern creak, the
    roof crash, the harp tune.
13. Build, check (a full run: … Egypt → vortex → Bethlehem → golden shard →
    vortex; plus the pounce in Egypt), publish 0.7.0, update `TODO.md` /
    `CLAUDE.md`, clean up.

===========================================================================
Round 7 draft (superseded; from "Next rounds", 2026-11-10)
===========================================================================
- **(Superseded by the Round 7 above; kept for reference.) Round 7: Bethlehem (draft by the planning chat, 2026-11-10; written in full
  once Dan's film references are in `SourceArt/Reference/Worlds/03-Bethlehem/`
  and he's played 0.6.1).** From the film: a quiet starry night; the Wise Men
  follow a bright "star" that is really the vortex; the party crashes through a
  stable roof (Abi lands in the hay, Dan misses it); Kira is a tiny gray lamb
  (folded ears, amber eyes); the golden story shard glows in the manger; the Wise
  Men arrive expecting a miracle, find a glowing shard and confused travelers,
  and leave disappointed; the hay explodes into a golden vortex. **Comedy: Dan
  doesn't mind irreverent / Monty Python-style humor here (2026-11-10)**; the
  film's own gags: one Wise Man wears a Santa hat, their gifts are modern
  wrapped presents with bows. **The scene was very short in the film, so most of
  the world is the game's own.** Film look (Dan's references in
  `Worlds/03-Bethlehem/`, 2026-11-10): a rustic wooden barn (X-braced doors, a
  round window in the gable, thatched / shingled roof) inside a rail fence, hay
  bales and barrels, rolling dark hills, a deep blue starry sky, warm golden
  light pouring from the barn, the golden vortex ring hovering above it with a
  beam down (seen from far away as the "star"); the roof crash leaves a hole
  (`barn-roof-hole-top.png`). Dan: brown homespun tunic + cream sash; Abi:
  beige dress + head scarf; Kira: a round gray woolly lamb with a cat face.
  Image prompts B1–B12 in `Docs/MODELS-NEEDED.md`, music in `Docs/MUSIC.md`. **In so far (2026-11-10, checked by the
  planning chat):** `dan_bethlehem`, `abi_bethlehem`, `kira_lamb` (straight
  head, ears folded, all gray), `violinist_bethlehem`, `wiseman_old`,
  `watchman` (a small bell is part of his belt: fine), `scorpion`, and Dan's
  song `Music/bethlehem.mp3` (3:00, choir). **Then (same day):**
  `wiseman_santa`, `wiseman_crown`, and side-profile redos of `goat` and
  `sheep` (old ones kept as `*_v1.png`): all checked, all usable. So all 12
  are in. `guard_dog` is turned toward the camera a little: Code measures it
  and uses it if it's under ~35 deg, else asks Dan for a redo.
  Plan (DEFAULT): arrival = the roof crash into the barn (the manger glows
  faintly but is empty); the world = the barn's farm and fields, the hills with
  shepherds and flocks, and a small night village next to it (rooftops,
  lantern-lit market, wells, courtyards); light stealth (night
  watchmen with lantern light cones; getting seen sends you back a bit); lamb
  Kira hides in flocks (guards and enemies ignore her there) and bounces on
  springy hay / wool; the Violinist (shepherd's pipe or small harp) teaches
  Kira's pounce, which also opens Egypt's pounce gaps; enemies: grumpy goats
  (headbutt), guard dogs, scorpions; the Wise Men on camels cross the map toward
  the star the whole time; when the necklace wakes the golden shard appears in
  the manger, and the finale is the Wise Men cutscene + the hay vortex.
  Universal-shard threshold set by rule 2. Dan's to-do: film references, then
  the concept images (planning chat writes the prompts), Mixamo, Suno song.
- **Also suggested for Round 7 (Code's notes, the planning chat decides):** Dan's
  notes on 0.6.0 first; then Bethlehem (Kira's pounce, which opens Egypt's
  pounce gaps E14 / E15 and the far roof). Art left over: real meshes for the
  big plain boxes (walls, plinths, scaffold planks, Ice Age slabs), Abi's Egypt
  sandal strand, the Sphinx gag's double-take shot.

===========================================================================
Round 5 delivery (moved from TODO.md, 2026-11-10)
===========================================================================
### Round 5: Ice Age polish, a bigger valley, creature models (0.5.0, build 500, 2026-11-09)
Final checks so far (built exe, batch mode, muted, separate never-shown desktop, virtual pad from the title screen):
`local/round5/play4` play-through RESULT all passed (116 ok); `zones4`, `fixes2`, `perf`, `perf_deck` passed; `tour2` 1 prop
fixed after it. Audio output 0.000 RMS in every run; window watcher: no window, never in the foreground. Evidence:
`Data/screenshots/round5/`.
1. Arrival: **done.** New `IceAgeArrival` (15.5 s, skippable): white-out landing; Kira on a snow rock shot from below as a dark
   silhouette against a near-white sky (sun disk, growl x2), fangs; Dan flinches, scrambles back, falls flat; she leaps onto his
   chest; close-up from over his head: folded ears, amber eyes; she paws, the necklace pulses blue; Abi laughs. MP4 rendered again
   (`Recordings/`). Not in it: Dan laughing / holding the necklace up (no clips for that). Found: the white-outs, Act I's warm light
   and the silhouette never ran in the exe since Round 4 (MonoBehaviours in wrongly named files); each has its own file now.
1b. See-through: **done, 0.** Kira (both forms) and Abi (both outfits) rebuilt as one closed surface (`closed_mesh.py`: Meshy's
   surface voxelized, gaps sealed, remeshed, Meshy's maps baked on; Kira re-rigged, Abi's Mixamo weights copied, same skeleton).
   Exe: 0 see-through pixels each over 24 views (16 angles + head, ears, tail, paws, fangs) / 20 views (Abi); runs look normal.
   Home Kira's whiskers went (too thin); Kira lock OK. Materials are opaque (no alpha).
2. Trees: **done.** Cause: `WorldArt.Prop` replaced each prop's root rotation (the FBX's 90 deg Blender-to-Unity turn) with the
   yaw. Fixed there; build report `Resources/Data/WorldProps.txt`; exe audit: 142 props upright and on the ground.
3. Clean-up: **done** (list fixed / not): fixed: props lying (pines, standing stones, rock caps); a vent rock 7.9 m in the air;
   bare graybox slabs (snow caps); smooth-sphere boulder (a rock now); rectangular lava (round pools, rim); solid-blob campfire
   (glowing tongues + flame particles); hint text over the hero (now below); a pine blocking the grotto; a rock blocking the
   pillars; a half-buried crystal; Act I white-out/light missing in the exe. Not fixed: big slabs (cave block, shelf, terraces)
   are still boxes (snow-capped); faint lighter bake seams on Abi's skin; checkpoint flags are graybox.
4. Music: **done.** Dan's four tracks, chosen by place: title, Act I (fades from 16.6 s, gone by the white-out), arrival + valley
   (Ice Age, wind under it), vortex (crossfades both ways). Stand-in gone from `make_sounds.py`. Checked playing in each place.
5. Bigger valley: **done** (DEFAULT layout): frozen lake (west basin; slick ice, a skating lane with an island ramp, three holes,
   under-ice swim), glacier climb (18 ice ledges to a 24.7 m summit with a view), hot-spring grotto (east of the stones). Bot:
   landing to story shard 10.5 min (Round 4: 6.6; a player is slower).
6. Later-move places: **done** as data (`WorldData.gates`, 10): 4 double-jump ledges (+1 glacier), 2 boulders (+1 lake),
   the grotto's cracked rock wall (charged punch), the deep lake (deep dive: divers are pushed back up), 2 wall-jump chimneys.
   All visible on the first visit, each says what it waits for; checked closed in the exe.
7. Shards / hearts: **done.** 60 shards, 35 wake the necklace. Rule 2 by data: 43 reachable on the first visit; play-through:
   37 before the ground pound. Hearts: IA-H1 (cave) + IA-H2 in the grotto (charged punch).
8. Violinist: **done.** Meshy model (1.85 m) rigged by Dan in Mixamo; his clips play (violin), bow, nod + the shared idle / walk.
   In the game he idles by the fire, plays through `ViolinistTeaches`, then bows and nods; the bone fiddle and a bow follow his
   hands (placed each frame; the hand bone's scale had made the fiddle huge). Check `violinist`: idle -> play -> bow -> nod.
9. Wolf, critter, mammoth: **done.** Meshy, closed rebuild, Code's quadruped rig + clips (+ stunned), Generic; in the game on
   the placeholders (kept as fallback): walk/run by speed, lunge, hit, stunned. Mammoth skin: distance weights (bone heat failed).
10. Build / check / publish: **done.** Final check `local/round5/final` (play-through + see-through + frame rate: RESULT all passed, 121 ok; 0.000 RMS; bot 10.1 min landing to story shard; 200 fps); 0.5.0 (211 MB) published as `game-500`; a fresh Round 1 starter install updated itself from GitHub to 0.5.0 and started (ready handshake; launcher hidden, never in the foreground). Disk: 3.4 GB -> 3.3 GB.

Also fixed on the way: the campfire's flicker code reset its flames to big blobs every frame (now slim tongues).

Testing incident (fixed, Dan's rule widened): Code rendered `IceAgeArrival` with the Unity editor in batch
mode *with graphics* (`unity_batch.py --gpu`, Play mode + Unity Recorder): the editor (and the licensing client it starts)
took Dan's focus and the cutscene's music came out of his speakers (the recorder only kept audio out of the file). Changed:
every process now starts through `Tools/quiet.py` (a separate, never-shown Windows desktop, so no window can appear or take
focus; hidden; Below Normal; Blender only with `-b`, Unity only with `-batchmode`; a watcher reports any visible window or
foreground grab); the editor mutes itself in batch mode (`Editor/QuietEditor.cs`); `--gpu` is refused; cutscene videos are
rendered from the built exe in batch mode (`Tools/render_cutscene.py`: muted, frames off-screen at a fixed 30 fps, ffmpeg
hidden); the editor menu calls the same script. Found on the way: even a batch-mode exe check's hidden
`Unity.BatchModeWindow` briefly took the foreground (no window shown) before the separate desktop. Check: ActOne and
IceAgeArrival re-rendered this way: measured output 0.000 RMS / peak 0.000 for both, and the watcher (every 0.25 s) saw no
visible window and no foreground grab for the game exe or ffmpeg; the build and the exe checks report the same.

Decisions Code made (Dan can overrule): the skating lane grips across it (you can steer) and slides along it; Kira 20k
triangles (closed); the necklace waits for open sky (unchanged); hearts in the valley = 2 (H1 kept, H2 new).

**For Dan to check:** the new arrival (MP4 and in game); Kira's / Abi's look up close after the rebuild (smoother than Meshy's
fur, whiskers gone); the music in each place (only checked muted); the lake's slipperiness and the skating lane; the glacier
climb and the view; whether 35 shards feels right; the wolves / critters / mammoths moving; the Violinist playing (Mixamo's violin
clip holds his arms low, so the fiddle sits in front of his chest, not under his chin: say if you want another clip).

### Round 6: Ice Age fixes + Ancient Egypt (0.6.0, build 600, 2026-11-10)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad from the title screen): `local/round6/full4` (New game ->
Ice Age -> vortex -> Egypt -> red shard -> vortex, + Egypt frame rate): RESULT all passed (196 ok; bot 10.3 min in Egypt, 45 shards). Targeted: `final_straight`, `final_underice`,
`final_heads`, `final_violinist` all passed; `final_tomb` passed its tomb checks; `perf_deck` (Steam Deck setting, 1280x800)
passed. Audio output 0.000 RMS in every run; window watcher: no window, never in the foreground. Evidence: `Data/screenshots/round6/`.
1. Straight line: **done.** Measured first (`round6/straight/before.txt`): a stick 3 deg off straight turned the run into a 35 deg/s
   circle. Cause: running is camera-relative and the camera's auto-recenter chased every small difference (feedback). Fix: the
   recenter ignores differences under 20 deg. After: 0.00 deg/s for Dan and Abi on the stick (straight, 3 deg, 8 deg tilt) and on W;
   steering still turns the camera.
2. Under the ice: **done.** One short channel (16 m) between two lit 8 m holes, light shafts + lights in every hole, the ice solid
   to the lake bed elsewhere; swimming up into a hole lifts the hero out onto the ice (A in a hole too); camera close and level
   under the ice; breath 16 s (Dan, Abi). Bot: all three through in 6.5-9.4 s (longest 5 s under, breath never under 44%), camera
   never in the ice, a hero left under is brought up a hole after 10.6 s with no heart lost.
3. Hard parts: **done.** Mammoth tusks and sabertooth fangs: own geometry weighted rigidly to the head, plain ivory material;
   wolf claws: own material (texture colour kept, the concept's brown). Close-ups in the exe (`round6/heads/`): clean tusks, no fur.
   The snow critter has no teeth or claws to split.
4. Violinist's instrument: **done.** Bone fiddle + bow (Blender, `instruments.fbx`), held by two-arm IK on his `play` clip
   (fiddle under the chin, bow in the right hand), on his back when walking, in the hand at idle; per world as data
   (`WorldData.violinistInstrument`: Ice Age fiddle, Egypt lyre). Close-ups `round6/violinist/`.
5. Egypt models: **done.** Dan's nine images -> Meshy -> clean-up; Dan rigged Dan, Abi, the Violinist and the mummy in Mixamo
   and downloaded the clips (incl. "Laughing" and the hold-up clip, now in the Ice Age arrival's ending). Code's rig: mummy Kira
   (head measured 14 deg, counter-turned; fur matched, amber eyes), camel, guardian; scarab and the Sphinx as static models.
6. Getting there: **done.** The vortex has four paths (Ice Age, Egypt, Bethlehem closed until Egypt's red shard, Home blocked
   "7 story shards are still missing"). `EgyptArrival` (13.5 s, skippable): they drop onto the plaza among workers, Egyptian
   clothes, mummy Kira, they react, the necklace glows red. The necklace shard takes each world's story-shard colour.
7. Layout: **done** (DEFAULT, bot 10.8 min first visit): town (houses, roofs, outside stairs), market with merchants and guards
   (light stealth: a guard sends you back to the checkpoint), the cat temple, the quarry with hauled stones and a quicksand
   shuttle, the judgmental camel (stares, snorts, turns away; rideable, the whole party seated, across the dunes to the Sphinx),
   the Sphinx with Kira's face (landmark; `SphinxGag` 10.5 s; climbable: paws, back, head), the pyramid scaffolding up to the
   top and the way in, the tomb (dark, necklace light: blade corridor, dart wall, sand-pit hall, sliding blocks, a turning
   bridge room, the mummies' gallery, a hidden cat chamber, a counterweight lift, the burial chamber), the Nile with boats.
   Future-move gates as data (pounce gaps, Muscle Man blocks, cracked wall, deep pool, wall-jump shaft), each says what it waits for.
8. Mummy Kira: **done.** Cat plates open cat doors only for her (temple, hidden chamber); mummies don't wake for her (checked: 3
   asleep for Kira, 2 wake for Dan).
9. Abi's double jump: **done.** The Violinist teaches it on his lyre (`ViolinistTeachesEgypt`); checked on Egypt's obelisk ledge
   (E16) and back in the Ice Age on the ridge ledge (R08).
10. Shards / hearts / story shard: **done.** 60 shards, 35 wake the necklace; rule 2 by data: 50 reachable on arrival (the double
   jump's 2 only after the Violinist); play-through 44. Two hearts (the hidden cat chamber; behind the burial chamber's cracked wall, charged punch later). The red story shard in the
   burial chamber opens the vortex.
11. Enemies: **done.** Mummies (slow, grab), scarab swarms (small, quick, a short chase), guardians (statues that wake and chase).
   No boss.
12. Look: **done.** Golden dunes, deep blue sky, two big pyramids, pastel town with domes and palms, striped market awnings.
   Frame rate: 185 fps at the slowest place on Dan's PC (1080p), 196 fps with the Steam Deck setting (1280x800) (need 60 / 40).
13. Music / sounds: **done.** Dan's `egypt.mp3` plays in Egypt (checked playing, muted); 9 new ElevenLabs effects (sand steps,
   trap click, stone grind, mummy groan, scarab skitter, camel snort, guard halt, sand sink, lyre tune).
14. Build / check / publish: **done.** 0.6.0 (306 MB build, 167 MB download) published as `game-600`; a fresh Round 1 starter install updated
   itself from GitHub to 0.6.0 and started (ready handshake; no window, never in the foreground). Cutscene MP4s rendered from the
   exe: `EgyptArrival`, `SphinxGag`, `IceAgeArrival` (`Recordings/`, 0.000 RMS). Disk: 5.1 GB -> 4.5 GB.

Also fixed on the way: Act I's music came back on for a moment after its fade (the director's time goes back to 0 as it ends);
a fading track could stay stuck at part volume when the track changed mid-fade; in film renders of Egypt's cutscenes the
world's own Sphinx trigger played a second copy (doubles in the video).

Decisions Code made (Dan can overrule): the turning bridge turns in 90 deg steps with pauses (a continuous spin was unplayable);
the camel ride seats the party (no free walking on its back); the counterweight lift only rises with a rider; the Violinist
stands in the middle of the market; Kira lock grey threshold 20% for mummy Kira (bandages cover most of her; measured 27%).

**For Dan to check:** running straight on your pad (and steering); getting out from under the ice; mammoth tusks / sabertooth
fangs up close; the Violinist's fiddle and lyre in his hands; Egypt on a first visit (is 10-15 min right? the guards fair?);
the camel ride and the Sphinx gag (MP4s in `Recordings/`; the gag's double-take shot is still wide, Kira half behind Dan
(two framing tries, your call)); mummy Kira's look; Egypt's song in place. Known: Abi's Egypt sandal
has a thin dark strand between the ankles; a few big shapes (walls, plinths, scaffold planks) are still plain boxes.

### Fix 6.1: universal shards + the Violinist's lyre (0.6.1, build 601, 2026-11-10)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/fix61/full` (New game -> Ice Age -> vortex ->
Egypt -> red shard -> vortex + Egypt frame rate): RESULT all passed. `local/fix61/c4` (lyre + 0.6.0 saves): all passed but one
(below). Audio output 0.000 RMS in every run; window watcher: no window, never in the foreground. Evidence: `Data/screenshots/fix61/`.
1. Universal shards: **done.** One total (`SaveGame.Total`, never spent); a world's necklace wakes when the total reaches its
   threshold (`AreaData.requirement` is now cumulative: Ice Age 35, Egypt 75), checked every frame, so arriving with enough
   already wakes it too. Rule 2 by data (`WorldData.RequirementReport`, `WorldData.StoryOrder`): Ice Age 43 >= 35; Egypt: Ice
   Age 43 + Egypt 50 = 93 on arrival >= 75. HUD: the total, "Next: 75" under it until the necklace wakes ("Follow the necklace"
   after); the pause menu lists "The Ice Age: 47 / 60", "Ancient Egypt: 45 / 60", "Shards in all: 92". Egypt's messages say the
   threshold. `CLAUDE.md` rules 2 and 3 updated (the design doc already said it). Play-through: Egypt's necklace woke at 78 in
   all (46 Ice Age + 32 Egypt). 0.6.0 saves (`Data/fixtures/save_0.6.0_after_egypt.json`, a real one): total 92 = 47 + 45,
   both story shards and the vortex gates kept; a mid-Egypt 0.6.0 save (47 + 30 of Egypt's, asleep) wakes the necklace on
   Continue; 40 + 20 = 60 stays asleep with "Next: 75", one more shard makes 61.
2. The Violinist's lyre: **done, one number short.** His own clip `violinist_lyre` (humanoid, made by `Editor/LyreClip.cs` at
   build: the idle's legs, a gentle sway, head tilted down toward the strings, curled fingers, plucking right fingers), chosen per
   world (`WorldData.violinistPlayClip`; the Ice Age keeps `violinist_play` for the fiddle). The IK holds the lyre upright against
   his left hip, the left arm round the outer frame arm with the hand behind the strings, the right hand in front plucking; palms
   turned by the finger bones. Close-ups front / both sides / 3/4 / player's view, idle and in `ViolinistTeachesEgypt`
   (`fix61/lyre_and_saves/`; Round 6's for comparison in `lyre_round6_before/`): hands on the lyre, no arm through his body or
   the lyre (forearms 19+ cm from his spine). The one miss: at the outer end of the strum his right hand stops 3-5 cm short of
   the planned spot (arm length; still in front of the strings). Two tries (6 cm -> 4.5 cm strum), then stopped per the tuning
   limit. No Mixamo download needed.
3. Build / check / publish: **done.** 0.6.1 published as `game-601` (175 MB download). Disk: 4.7 GB -> 4.5 GB.

Decisions Code made (Dan can overrule): the pause menu lists only worlds reached; earlier worlds count toward rule 2 with their
own first-visit moves plus the new world's (moves learned on the way, like the ground pound, are not counted).

**For Dan to check:** the HUD's total and "Next: 75" in Egypt; the pause menu's per-world counts; your own 0.6.0 save in 0.6.1
(Ice Age 39 + Egypt 47 = 86, both shards found: nothing should change); the Violinist playing the lyre for Abi (does the hold
read right? the strum is narrow).

### Round 7: Bethlehem (0.7.0, build 700, 2026-11-10)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/round7/full` (New game -> Ice Age -> Egypt -> vortex ->
Bethlehem -> golden shard -> vortex -> Egypt's far roof + Bethlehem frame rate): all passed but 3 bot-side misses, fixed and passed in
`local/round7/final` (the Bethlehem route alone, from the vortex: RESULT all passed, 65 ok); `perf_deck` passed. A Round 1 starter
install updated itself from GitHub to 0.7.0 and started (no window, never in the foreground). Audio output 0.000 RMS in every run; window watcher: no window, never in
the foreground. Evidence: `Data/screenshots/round7/`.
1. Models: **done for Code's part; Dan's Mixamo part waiting.** All 12 images -> Meshy -> clean-up (previews `round7/models/`). Code's
   rigs: lamb Kira (head straight, -1 deg; fur matched to Kira's gray; Kira lock OK: 51% gray, 0% white, amber eyes, no ear bones;
   whiskers removed like home Kira), goat, sheep (`round7/rigs/`); the scorpion moves by code like the scarab. **Guard dog: head turned
   51 deg** (limit ~35): asked Dan for a side-profile redo; dogs are placeholders until then. **Mixamo:** upload files for the 7 people
   ready, Dan told what to download (`Docs/MODELS-NEEDED.md` top); **not in by the end of the round**, so not imported
   (the import is wired: `import_characters.py` names + clip map, Humanoid settings, the riders / watchmen switch to the models and clips
   when present). A follow-up fix brings them in.
2. Props: **done.** Blender (`bethlehem_world.py`): the barn (boards, X-braced doors, round gable window, thatch with the crash hole),
   manger, hay bales and stacks, barrels, rail fences, the three gift boxes with bows, the watchman's lantern, houses, the inn, closed
   stalls, the well, olive trees, campfires, rocks, cart, trough, sign. The Violinist's small shepherd's harp (`instruments.py`), held
   and played like the lyre (the lyre's clip). The Wise Men ride Egypt's camel model with plain cream blankets.
3. `BethlehemArrival` (14.5 s, skippable): **done.** The golden vortex opens over the barn, they drop through the roof hole; Abi lands
   in the hay, Dan on the floor; Kira pops out of the hay as a lamb; the manger glows faintly, empty; they stare; the necklace glows
   gold. The hole stays in the roof (it's part of the barn).
4. Layout: **done** (DEFAULT): farmyard (hay loft, bale tower, the big hay stack, fences), the star over the barn with its beam, the
   hills (3 shepherds' camps with fires, 4 flocks, goats' rocks, the outcrop, olive grove, the well, the cave), the village (houses,
   main street, closed market, courtyard, the inn with "NO ROOM" and a voice through the door, rooftops and outside stairs), the road
   with the Wise Men (stop, argue, check the map, a line of their bickering when you're near). Gates as data and visible: 2 pounce gaps,
   the well (deep dive), the cave boulder (Muscle Man), a cracked courtyard wall (charged punch), a wall-jump gap, 2 double-jump ledges.
   Bot: 9.2 min arrival to the necklace (a player is slower).
5. Light stealth: **done.** 6 night watchmen with lantern light cones (one at the inn dozes now and then; one guards the watchmen's
   yard): seen = "HALT! Who goes there? ...Nobody?..." and back to the checkpoint. A dog's bark brings the nearest watchman over.
6. Lamb Kira: **done.** Flock hide (the yard's watchman saw her 0 times walking in with the flock; Dan is caught at the gate); the
   sheep turn and stare at her. Springy hay / wool: up the 4.4 m hay stack (bounce 6.4 m), onto the barn's roof, up through the roof
   hole from the hay under it; others don't bounce.
7. Kira's pounce: **done.** The Violinist teaches it on his harp (`ViolinistTeachesBethlehem`); B on the ground: a short aim, then
   11.7 m. Used on Bethlehem's two pillars (P01 + heart) and the far roof (P03, P04), and on Egypt's far roof (E14 / E15). Egypt's far
   roof was 19 m away (Round 6's note said 9): moved in to 10.5 m (Abi's double jump reaches ~9.8, so only the pounce makes it).
8. Enemies: **done.** Grumpy goats (lower the head 0.8 s, charge, headbutt knock-back), guard dogs (bark, short chase; placeholders),
   scorpions (Meshy model, scuttle and snap). No boss.
9. Shards: **done.** 60 + 2 hearts (the barn's ridge by bounce; the far pillar by pounce). Threshold 112 = 62% of the 180 shards up to
   here; rule 2: 141 available on arrival (Ice Age 43 + Egypt 50 + Bethlehem 48), the pounce's 3 and 9 later-move shards not counted.
10. `WiseMenArrive` (16.5 s, skippable): **done.** The golden shard in the manger; the Wise Men walk in, presents out, the choir; they
   see the shard and three confused travellers, look at each other, walk out; the hay bursts into a golden vortex that pulls the party
   in -> the vortex space (3 of 8; the Medieval Kingdom path opens; the hay's vortex stays as the gate back).
11. Look: **done.** Deep blue night, 900 stars, soft blue moonlight, warm lamps (barn, windows, lanterns, fires). Frame rate:
   155 fps at the slowest place on Dan's PC (1080p), 159 fps with the Steam Deck setting (1280x800) (need 60 / 40).
12. Music / sounds: **done.** Dan's `bethlehem.mp3` in the world (checked playing, muted); 12 ElevenLabs effects (grass steps, hay
   rustle and bounce, sheep, goat, dog, scorpion, lantern creak, roof crash, a knock at the inn, the harp tune, the choir sting).
13. Build / check / publish: **done.** 0.7.0 published as `game-700`. MP4s rendered from the exe: `BethlehemArrival`, `WiseMenArrive`
   (`Recordings/`, 0.000 RMS). Disk: 5.7 GB -> 5.5 GB.

Also fixed on the way: lamb Kira (0.5 m) couldn't pass through vortex doors or world gates (their trigger was a circle 2 m up: her
middle sat under it; now "inside the arch"); the old checks walked the vortex paths at Round 6's angles (five paths now).

Decisions Code made (Dan can overrule): Dan and Abi wear their Egypt clothes in Bethlehem until their Bethlehem models are in; the
harp uses the lyre's playing clip; the pounce is B on the ground (crouch-aim 0.28 s, 17 m/s); Egypt's far roof moved (above); the
Wise Men's riders stay placeholders until a sitting clip is in (a standing model on a camel looked wrong).

**For Dan to check:** the roof-crash arrival and the Wise Men finale (MP4s in `Recordings/`, and in game); lamb Kira's look; the
pounce's feel (distance, the short aim); bouncing on hay (wool pile by the barn's east wall, the stack, under the roof hole); the
watchmen (fair? too many?) and sneaking in with the flock; the night look (dark enough / too dark?); Bethlehem's song in place. Known:
the Wise Men, the watchmen and the dogs are placeholders until your Mixamo files / the dog redo; the sheep's walk shows a leg
poking out of the wool at times; the rock pillars and the cave are plain stone boxes.

### Fix 7.1: Bethlehem's real people + the new guard dog (0.7.1, build 701, 2026-11-10)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/fix71/final` (the Bethlehem route): RESULT all passed
(65 ok). An earlier run (`beth2`) missed the far-pillar pounce once and the bot then got stuck on a fence (4 knock-on misses); the same
build passed all 65 on the next run (`beth3`): bot variance, not the game. Frame rate 108 fps at the slowest place at 1080p (was 155
before the seven real people), 146 fps with the Steam Deck setting (need 60 / 40). Audio 0.000 RMS in every run, the renders and the
build; window watcher: no window, never in the foreground. Evidence: `Data/screenshots/fix71/`.
1. Dan's Mixamo files: **done.** All seven rigged people imported (Dan, Abi, the Violinist in their Bethlehem models; the three Wise
   Men; the watchman), Abi's mesh closed like Egypt's (`close_person.py`, 0 open edges). His eight clips in: "Sitting Idle" (riders),
   "Standing Arguing", "Looking Down" (as the map), "Carrying" (as offering the presents), "Disappointed", "Yelling Out", "Sleeping
   Idle" (the dozing watchman), "Surprised" (`import_characters.py` now takes either name).
2. Guard dog: **done.** New Meshy model from the side-profile image: head turn -1 deg (was +51), rigged by `kira_rig.py` (10 clips);
   barks and chases in the village.
3. In the game: Dan and Abi wear their Bethlehem clothes (necklace on), the Violinist plays the harp in his own model, the watchmen
   carry their lanterns, the Wise Men ride sitting on the camels' saddle blankets (the cream block was only for the placeholders),
   and walk into the barn with their presents in the finale (`CutsceneAuthoring.Version` 17; presents moved down to their hands).
   MP4s rendered again from the exe: `Recordings/BethlehemArrival.mp4`, `WiseMenArrive.mp4`.
4. Publish: **done.** 0.7.1 as `game-701`; the Round 1 starter install in `local/sc` updated itself to it and started.

Decisions Code made: Dan's substitute clips used as named above; the cream camel blanket dropped under the real riders.

**For Dan to check:** the Wise Men on their camels (the Santa-hat one's present floats a little high beside him) and in the finale
(`Recordings/WiseMenArrive.mp4`); Dan and Abi's Bethlehem clothes in game; the watchmen and the new dog; long robes stretch a bit in
the run clip (Mixamo; they only walk and sit).

===========================================================================
Round 8 plan (moved from TODO.md, 2026-11-11)
===========================================================================
Fix 7.2's plan is in `TODO-ARCHIVE.md`. Report per item: done / not done /
deferred, and why. DEFAULT = planning chat's choice, Dan can overrule after
playing. Testing rules as always (`Tools/quiet.py`, muted with the level
reported, never a window in front of Dan); rule 12 (never stuck) applies.

**Inputs, all in (Dan, 2026-11-10/11):** the 10 concept sheets M1–M10 in
`SourceArt/Concepts/` (`dan_medieval`, `abi_medieval`, `kira_dragon`,
`muscleman_medieval`, `violinist_medieval`, `knight`, `archer`, `rat`, `horse`,
`wagon_driver`; prompts and notes in `Docs/MODELS-NEEDED.md` and the draft
below), film references in `SourceArt/Reference/Worlds/04-Medieval/` and
`Characters/*`, Dan's song `Music/medieval.mp3` (2:59).

**Muscle Man, as in the film (DECIDED, Dan 2026-11-11):** he joins only at the
very end. Until then he's a rival who **mocks Dan**; he's won over only when
Dan does a feat of strength he couldn't: pulling the shard from the stone.

1. **Models. ORDER (Dan, 2026-11-11: he wants to do Mixamo first and go to
   bed): do the Mixamo people FIRST, before anything else in the round:**
   Meshy → clean-up → Mixamo upload files for the 7 people only (`dan_medieval`,
   `abi_medieval`, `muscleman_medieval`, `violinist_medieval`, `knight`,
   `archer`, `wagon_driver`), then **stop at once** and tell Dan exactly what to
   upload and download (file, clip search names, settings, where to save).
   Previews can follow later. Everything else (the other models, props, world)
   comes after that stop; carry on with it without waiting for Dan.
   **(Dan, 2026-11-11: the Violinist's cape wouldn't rig in Mixamo.)** Upload file without the cape
   `violinist_medieval_nocape.fbx` (`Tools/Blender/split_cape.py`); after Dan's rig, `Tools/Blender/attach_cape.py
   violinist_medieval` puts the cape back on the spine / shoulder bones (rigid, no stretching) -> `rigged.fbx`.
   Then: Meshy → clean-up → previews for the rest. Notes: Muscle Man grins
   with teeth (check the mouth); dragon Kira is darker than her gray (match it,
   Kira lock check); keep her thin wings as their own two-sided part (don't let
   the closed-mesh rebuild swallow them); the archer's quiver is part of him.
   Mixamo upload files and clip lists for `dan_medieval`, `abi_medieval`,
   `muscleman_medieval`, `violinist_medieval`, `knight`, `archer`,
   `wagon_driver`, reusing the shared clips; Muscle Man also needs his own:
   flex / show off, laugh / point (mocking), heavy lift, push, failed heave,
   fall flat on his back, cheer, and a "lift someone up" clip. **Stop and tell
   Dan exactly what to download** as soon as those files are ready; keep
   building with placeholders; import his clips in this same round. Horse and
   rat: Code's rigs.
2. **Props** (Blender / Meshy text-to-3D, from the film shots): the castle
   (stone walls with crenellations, round towers with conical slate roofs, a
   row of arches, the keep), the courtyard (cobblestones, wall torches,
   wooden benches, flower pots, steps), the round stone pedestal with the
   **purple story shard** stuck in it, the hay wagon, the lute (its own playing
   clip like the lyre), knight's sword and shield, archer's bow, boulders,
   portcullis, braziers.
3. **`MedievalArrival`** (Dan's version, skippable, ~15–18 s): the purple vortex
   opens over the courtyard; the party falls into a horse-drawn hay wagon (film:
   `hay-wagon-landing.png`); they pop up in medieval clothes, Kira as a baby
   dragon; she discovers her fire with a sneeze-puff and the hay catches; they
   all leap out; the wagon, driver aboard and the pony galloping, careens into
   the castle wall and explodes in a comic cartoon blast (`wagon-on-fire.png`);
   the driver staggers out singed with his hat smoking, the pony is fine; the
   party slinks away. Irreverent comedy is fine.
4. **Layout** (DEFAULT), about 10–15 minutes first visit, a torch-lit night:
   - **The courtyard and the stone:** the stone stands there from the start;
     Dan's necklace hums near it but nothing happens until the end.
   - **The walls and towers:** climb, archers on the battlements, tower tops.
   - **The great hall:** a feast table, chandeliers to swing across, banners.
   - **The dungeon:** dark (the necklace's light), rats, cells, keys.
   - **The moat:** water round the castle (a deep-dive gate below).
   - **The tournament field** outside the walls: a comic strength contest where
     Muscle Man shows off (see item 5).
   - **The village and market** below the castle.
   - **Gates for later** as data, visible on the first visit: **Muscle Man's
     boulders here too** (he isn't with you yet), the moat's depths (deep dive),
     wall-jump chimneys. Rule 2 counts none of them for this world's threshold.
5. **Muscle Man the rival (before he joins).** He appears 3–4 times around the
   world (DEFAULT: the courtyard on arrival, the tournament field, the great
   hall, the stone at the end), each a short comic moment, grunts and laughs
   only: he lifts a boulder / a cart / a knight one-handed and flexes; he laughs
   and points at Dan (especially when Dan tries the same thing and fails); at
   the tournament he wins the strength contest Dan can't. He blocks nothing the
   player needs and can't be fought.
6. **Dragon Kira's abilities:** **glide** with her little wings (hold Jump in
   the air: long gaps, slow fall) and **fire puffs** that light torches and
   braziers, burn ropes, hay and brambles, and scare rats; never hurts people.
7. **New move: Dan's charged punch** (hold attack to charge, release: breaks
   cracked walls and knights' shields), taught by the Violinist as a bard with
   his lute. It works everywhere, so the cracked walls in the Ice Age grotto,
   Egypt and Bethlehem open too: check one in an earlier world.
8. **Enemies:** clumsy knights (shield up from the front; the charged punch or
   going round them), nervous archers on the walls (slow, dodgeable arrows),
   dungeon rats. Light combat, no boss (DEFAULT).
9. **Shards (universal rule):** about 60 small shards; the cumulative threshold
   set by rule 2 (reachable on arrival with the moves you have, the charged
   punch only after the Violinist, no Muscle Man). Two hearts. Report the numbers.
10. **The finale (`TheStone`, skippable, the film's scene):** when the necklace
    wakes it leads to the stone in the courtyard. Muscle Man is there first,
    brags, heaves with everything he has, fails and falls flat on his back
    (film: `muscleman-falls-at-stone.png`). Dan steps up, the shard answers his
    necklace and slides out with no effort. Muscle Man stares, then kneels:
    "Master!" The purple vortex opens and **sweeps Muscle Man along with the
    party**, much to Dan's bewilderment → the vortex space (Home: 4 / 8).
11. **Muscle Man the companion (after the stone).** He follows like the others
    (warps in when left behind, never controlled), cheers at pickups, and does
    his jobs at prompts: **moves boulders**, **lifts heavy things** (portcullis,
    carts), **boosts Dan** to high ledges (the film's "lifts Dan to the clock").
    Every boulder gate in every world (Ice Age, Egypt, Bethlehem, Medieval)
    opens with him: check one in an earlier world and one in Medieval. He goes
    through the vortex with the party.
12. **Look:** from Dan's references: warm torch-lit sandstone castle under a
    deep blue night sky, cobblestones, conical towers, arches, flower pots and
    benches; stylized like the characters. Frame rate checked on both settings.
13. **Music and sound:** `Music/medieval.mp3` in the world. ElevenLabs: hay
    rustle, fire whoosh and the wagon blast, armor clank, arrow twang, rat
    squeak, Muscle Man's grunts / laughs / "Master!", stone grind and the shard
    sliding out, the lute tune.
14. Build, check (a full run: … Bethlehem → vortex → Medieval → the stone →
    vortex with Muscle Man; a boulder in an earlier world; a cracked wall in an
    earlier world), publish 0.8.0, update `TODO.md` / `CLAUDE.md`, clean up.

Round 8 draft by the planning chat (superseded, moved from TODO.md, 2026-11-11):
- **(Superseded by the Round 8 above; kept for reference.) Round 8: the Medieval Kingdom (draft by the planning chat, 2026-11-10; written in full after Fix 7.2 and
  once Dan's concept images are in).** Film references in `SourceArt/Reference/Worlds/04-Medieval/` and
  `Characters/*` (added 2026-11-10); image prompts M1–M10 in `Docs/MODELS-NEEDED.md`; music in `Docs/MUSIC.md`.
    **All in (Dan, 2026-11-11), checked by the planning chat:** the 10 concept sheets in
    `SourceArt/Concepts/` (`dan_medieval`, `abi_medieval`, `kira_dragon`, `muscleman_medieval`,
    `violinist_medieval`, `knight`, `archer`, `rat`, `horse` (a round dappled pony: the first prompt was blocked
    for looking like an existing character), `wagon_driver`) and Dan's song `Music/medieval.mp3` (2:59). Notes for
    Code: Muscle Man grins with his teeth showing (fine for a static face; check the mouth after Meshy); dragon Kira
    is a darker charcoal gray than her usual gray: match it (`match_fur.py`); her thin wings may close up in the
    closed-mesh rebuild: keep them as their own thin two-sided part; the archer's quiver is part of his outfit.
  - **Arrival (`MedievalArrival`, Dan's version of the film, 2026-11-10):** the vortex opens over the castle
    courtyard; they fall into a horse-drawn wagon full of hay; Kira pops up as a baby dragon, discovers her fire,
    and a puff sets the hay alight; they all leap out; the wagon (driver still aboard, horse galloping) careens into
    the castle wall and explodes (cartoon: the driver staggers out singed, hat smoking; the horse is fine) as the
    party runs off. Irreverent comedy is fine.
  - **Look:** a torch-lit stone castle at night: cobblestone courtyard, round towers with conical slate roofs,
    crenellated walls, a row of arches, wall torches, wooden benches, flower pots, the keep at the back.
  - **The story shard:** the purple shard stuck in a round stone pedestal in the courtyard (the film's sword in the
    stone). The film gag stays: Muscle Man brags, heaves, fails and falls flat on his back; Dan, whose necklace
    answers the shard, pulls it out with no effort; Muscle Man becomes his devoted fan ("Master!").
  - **Muscle Man joins only at the end, as in the film (DECIDED, Dan 2026-11-11).** Through the world he's a
    rival who **mocks Dan**: he shows up around the castle (the courtyard, the tournament field, the great hall)
    flexing, lifting things Dan can't, laughing at Dan's attempts (short comic moments, no dialogue needed beyond
    grunts and laughs). He's only impressed when Dan does something of strength he can't: the stone. At the end he
    brags, heaves, fails and falls flat on his back; Dan pulls the shard out with no effort; Muscle Man becomes his
    devoted fan ("Master!") and the purple vortex sweeps him along. From then on he's a companion (follower, never
    controlled): moves boulders, lifts portcullises and heavy things, boosts Dan to high ledges, cheers at pickups.
    So **the Medieval world's own boulder gates are for coming back later** (like the earlier worlds' boulders),
    and rule 2 counts none of them for the Medieval threshold.
  - **Kira, baby dragon:** glide with her little wings (long gaps), and fire puffs that light braziers and torches,
    burn ropes, hay and brambles (weak, comic, never a weapon that hurts people).
  - **New move:** the Violinist as a bard with a lute teaches Dan the **charged punch** (breaks cracked walls and
    knights' shields; opens every earlier world's cracked walls).
  - **Places (DEFAULT):** the courtyard and the stone, the castle walls and towers (climb, archers), the great hall,
    a dungeon (dark, rats, the necklace's light), the moat (deep-dive gate), the tournament field outside the walls
    (a comic joust / strength contest), the village and market below the castle.
  - **Enemies:** clumsy knights (shields; the charged punch breaks them), nervous archers on the walls (slow,
    dodgeable arrows), dungeon rats.
  - **Exit:** collecting the purple shard opens the purple vortex, which sweeps Muscle Man along with the party,
    much to Dan's bewilderment.

===========================================================================
Fix 8.1 plan (moved from TODO.md, 2026-11-11)
===========================================================================
Current round — Fix 8.1: Medieval fixes + shard thresholds (target 0.8.1)
Round 8 is delivered (below). Dan played 0.8.0 (2026-11-11). Report per item:
done / not done / deferred, and why. Testing rules as always; rule 12 applies.
For each of items 1–4: find the cause, fix it at the source, and check it in
the built exe from the player's view (screenshot + numbers).

1. **"In the room where you first meet Muscle Man the ground is high, like you
   are wading through water."** The heroes sink into the floor there: the
   floor's collider sits above its visible mesh (or the visible floor is lower
   than the collider / a water or fog volume overlaps it). Feet must sit on the
   visible floor. Then audit **every** walkable surface in the Medieval world
   (and spot-check the other worlds) for feet sunk into or floating above the
   floor: a probe that compares the hero's foot height with the visible
   surface under it, reported per area.
2. **The drawbridge isn't solid: Dan fell right through it.** Give it a real
   collider that moves with it (lowered and raised), and check every moving or
   animated platform in all worlds the same way (walk across each one, every
   hero).
3. **A shard behind the village can't be reached: an invisible wall.** Find
   the invisible collider (probably the world-boundary wall or a leftover
   blocker) and move it outside the shard, or move the shard. Then add to the
   never-stuck / reachability check: **every shard in every world is reachable
   by the bot with the moves it should need** (none behind invisible walls),
   and report any that aren't.
4. **The needed shard count is far too low ("I only needed like 3")** when you
   have collected well in the earlier worlds. The universal total stays (extra
   shards still help, Dan 2026-11-10), but each world must still ask for real
   collecting there. New rule (DEFAULT, Dan can tune): a world's necklace wakes
   when **both** are true:
   - the universal total reaches the world's cumulative threshold, raised to
     about **75%** of all shards in the worlds so far (Ice Age 45, Egypt 90,
     Bethlehem 135, Medieval 180), and
   - **at least 20 shards from this world** have been collected.
   So a player who hunted everything earlier still needs 20 here, and one who
   skipped a lot needs more. HUD: "Next: 180 (this world: 12 / 20)". Rule 2
   (reachable on arrival with the moves held) must still pass with the new
   numbers in every world: report them. Existing saves: no world that's already
   awake relocks. Update `CLAUDE.md` rule 3 and `Docs/Design/design-decisions.md`.
5. Build, check, publish 0.8.1, update `TODO.md` / `CLAUDE.md`, clean up. Don't
   end your turn until 0.8.1 is published (unless truly blocked: say so).

### Fix 7.2: never stuck (the well) + Return to checkpoint (0.7.2, build 702, 2026-11-11)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/fix72/final` (`--only fix72`: New game -> the
never-stuck sweep of the Ice Age, Egypt and Bethlehem -> the well -> the reset): RESULT all passed (76 ok). Audio 0.000 RMS in every
run; window watcher: no window, never in the foreground. The Round 1 starter install in `local/sc` updated itself to 0.7.2 and
started. Evidence: `Data/screenshots/fix72/`.
1. The well: **done.** Two level bugs made it a trap, not just the missing way out: the well's height was taken from a point inside
   the terrain's wide hole (the rim sat ~5 m down in a dry crater), and its stone ring only went 1.75 m down, so a swimmer drifted
   sideways out of the water onto the crater's floor 10 m down. Now: the well stands on the flat ground with a stone apron over the
   hole, the ring is solid to the bottom, the water fills the shaft to the bottom, and a rope hangs from the beam to a bucket on the
   water. In the well, Jump (pressed or held, any depth) or swimming into the wall climbs the rope up and over the rim
   (`WellExit`, `CharacterMotor.ClimbOut(.., straightUp)`), with a hint "In the well! Grab the rope: A climbs out." Checked from the
   player's view for Dan, Abi and Kira, from a jump and from a fall, Jump and wall both: out in about 3 s. Only Abi can jump over
   the 1.8 m rim from the ground (Dan 1.6 m, Kira 1.4 m), so for them the "jump in" test drops them in instead.
2. Never stuck everywhere: **done.** Rule 12 in `CLAUDE.md`. Each world's data lists its low / enclosed spots (`WorldData.lowSpots`:
   Ice Age 12, Egypt 7, Bethlehem 3). The exe check `stuck` drops each hero into each one (with only the moves held, plus what it
   takes to get in) and a pad bot must get out within 20 s; it also fails any water volume that isn't on the list (all 7 are).
   Results: pond, river (up the ramp), the cave pit, the frozen well, the lake's four holes, Kira's crawl chamber, the Nile and its
   deep pool, both quicksands, the tomb's pit, the barn, Bethlehem's well: all out, 0.3-14 s. Closed until a later move (reported,
   not enterable yet): the hot-spring grotto, the grotto and the lake hollow behind boulders, Egypt's quarry and secret room, the
   shepherds' cave. **Found and fixed on the way:** the Ice Age cave's floor was a thin slab over a 4 m hollow; little Kira walked
   off the pit's steps underneath it (now solid to the pit's floor). And Bethlehem's well (item 1).
3. Return to checkpoint: **done.** Pause menu row 4 "Return to checkpoint": a short fade, the party at the last checkpoint, no heart
   lost, shards kept (checked in Bethlehem and in the vortex). The safety net: pushing the stick for 15 s without getting 4 m
   anywhere shows "Stuck? Pause (Menu) > Return to checkpoint" (checked against the barn's wall; it also showed for the bot in the
   old dry well).
4. Build / check / publish: **done.** 0.7.2 published as `game-702`. Disk: 7.4 GB -> 7.1 GB.

Decisions Code made (Dan can overrule): the rope works from any depth (Jump) and on touching the wall (not once the deep dive is
learned, so divers can explore the bottom later); the well's stone apron is plain grey stone (placeholder look); the stuck hint
counts only time spent pushing the stick (standing still never triggers it).

**For Dan to check:** fall into Bethlehem's well and get out with A or by swimming to the wall (does the climb feel right?); the
stone apron round the well (fine, or should it look like ground?); Pause > Return to checkpoint anywhere; the "Stuck?" hint
(15 s too long / too short?). Not re-run this fix: the long Bethlehem / Egypt routes (the changes are the well, the cave floor and
the pause menu; the sweep covered each of them).

===========================================================================
Round 10 plan (as written by the planning chat, 2026-11-11)
===========================================================================
---------------------------------------------------------------------------
Current round — Round 10: the Pirate Seas (target 0.10.0)
Round 9 is delivered (below). Report per item: done / not done / deferred, and why. DEFAULT = planning chat's choice,
Dan can overrule after playing. Testing rules as always (`Tools/quiet.py`, muted with the level reported, never a
window in front of Dan); rule 12 (never stuck) and the Fix 8.1 floor / platform / reach checks apply. Don't end your
turn until 0.10.0 is published unless truly blocked (say so).

**Inputs, all in (Dan, 2026-11-11):** the 9 concept sheets P1–P9 in `SourceArt/Concepts/` (`dan_pirate`,
`abi_pirate`, `kira_otter`, `muscleman_pirate`, `violinist_pirate`, `pirate`, `crab`, `pufferfish`, `fish`),
references in `SourceArt/Reference/Worlds/05-PirateSeas/` and `Characters/*`, his clips (`Clips/04-Medieval_TheStone_05-Pirate_Arrival.mp4`
from ~70 s, `Worlds/05-PirateSeas/shard-rolls-overboard.mp4`) with notes in `Docs/CUTSCENE-FILM-NOTES.md`, and two
songs: `Music/pirate.mp3` (the ship, 2:59) and `Music/underwater.mp3` (under the sea, 3:00). Notes: otter Kira is a
browner gray (match her gray, Kira lock); Muscle Man grins with teeth (fine).

1. **Mixamo first (Dan wants it early):** Meshy → clean-up → Mixamo upload files for `dan_pirate`, `abi_pirate`,
   `muscleman_pirate`, `violinist_pirate`, `pirate`, then **stop at once and tell Dan exactly what to upload and
   download** (shared clips reused; add swim / tread-water clips if any hero lacks them; Muscle Man's struggling swim).
   Then carry on with everything else in the same turn; import Dan's clips when they arrive.
2. **The other models:** otter Kira (Code's rig; swim, dive, belly-slide), crab, pufferfish (puff up / deflate), the
   tropical fish (schools, simple swim cycle). Props: the pirate ship (red-brown hull, turned rail balusters, pink
   sails, rigging, crow's nest, cannons, lanterns, barrels, crates, the hold), the island and cove, corals, the
   shipwreck, the green story shard, the fiddle (its own playing clip).
3. **Two areas, almost two levels (Dan, 2026-11-11):** **the ship** (deck, rigging, crow's nest, the hold, plus the
   tropical island and cove) and **under the sea** (the reef, the shipwreck, underwater caves, the sea floor). Each
   with its own look, enemies, about 30 shards and **its own song** (`pirate.mp3` above water, `underwater.mp3`
   below, crossfading at the surface). One world for the vortex and the story shard.
4. **`PirateArrival`** (from Dan's clip): the ship at **night** with lanterns; a green vortex swirls round it; Dan and
   otter Kira pop out of barrels (Abi too), Muscle Man drops from the sky **through the sails** and crashes flat onto the
   deck, lies there, sits up grinning; Abi laughs; Dan's necklace glows green. Side-by-side sheet against the clip as in
   Round 9. (Gameplay can move on to sunset / day: DEFAULT the ship section at sunset, as the film's deck shots.)
5. **`ShardOverboard`** (from Dan's clip `shard-rolls-overboard.mp4`, the doorway between the two areas): when the
   ship part's necklace moment comes, Muscle Man grabs the green shard, holds it up laughing with joy, it slips from his
   hand, bounces and rolls across the deck, under the rail and over the side; he leaps in after it, Dan and Abi follow,
   otter Kira dives. Before this the water round the ship stays shallow; after it the deep sea opens.
6. **Otter Kira:** fast swimming, deep diving and no breath limit, squeezes through narrow underwater gaps; on land a
   belly-slide across wet decks.
7. **Muscle Man the companion** (his first world as one): lifts cannons, crates and the anchor, boosts Dan up the
   rigging; underwater he swims slowly and struggles to keep up (comic, never blocks the player).
8. **New move: everyone's deep dive** (longer breath, swim deeper), taught by the Violinist as a ship's fiddler. Opens
   the earlier worlds' deep-water gates (the Ice Age lake, Egypt's Nile, Bethlehem's well, Medieval's moat): check one.
9. **Enemies:** goofy pirate deckhands (above), crabs (beach and deck), pufferfish (underwater: puff up and bump). Light
   combat, no boss (DEFAULT). Friendly fish schools everywhere under the sea.
10. **Shards (Fix 8.1 rule):** about 60 (30 + 30); threshold at about 75% of all shards so far **and** at least 20 from
    this world, reachable on arrival with the moves held (the deep dive only after the Violinist). Two hearts. Report
    the numbers.
11. **The finale:** the necklace leads to the green shard on the sea floor among corals (film:
    `green-shard-sea-floor.png`); collecting it opens a swirling underwater vortex that pulls everyone in, Muscle Man
    included → the vortex space (Home: 5 / 8).
12. **Look:** above, a pink-violet sunset over a deep blue sea, red-brown ship, pink sails; below, bright blue water,
    light shafts, colorful corals, cheerful fish. Frame rate checked on both settings (water and fish schools are the
    risk).
13. **Sound:** ElevenLabs: waves, creaking wood, barrel thuds, the splash, bubbles, cannon, crab clicks, pufferfish
    puff, Muscle Man's laughs and "My hero!", the fiddle tune.
14. Build, check (a full run: … Medieval → vortex → Pirate Seas → ship → overboard → under the sea → green shard →
    vortex; plus a deep-water gate in an earlier world), publish 0.10.0, update `TODO.md` / `CLAUDE.md`, clean up.

### Round 8: the Medieval Kingdom (0.8.0, build 800, 2026-11-11)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/round8/final` (`--only medieval`: from the vortex with
three worlds done -> the arrival -> the world -> the stone -> the vortex with Muscle Man -> Bethlehem's boulder and cracked wall -> his
jobs here): RESULT all passed (113 ok); the bot took 5.2 min from the arrival to the necklace (a player is slower). `stuckmed` / `stuck` (rule 12, all four worlds): all passed: every moat side, the deep pit, the dungeon, the hall, and the yard / armory / store behind their opened gates, each hero out in 1-5 s; the Ice Age, Egypt and Bethlehem too, now including the spots behind their boulders and cracked walls (Muscle Man and the charged punch are learned now). Frame rate 157 fps at the slowest place (1080p),
148 (Steam Deck setting) (need 60 / 40). `bethlehem` (regression): 64 ok and one bot-side stealth miss (the yard's watchman saw lamb Kira once in 1,212 hidden frames; that code is untouched; Round 7 saw the same variance). Audio 0.000 RMS in every run, the build and the
renders; window watcher: no window, never in the foreground. Evidence: `Data/screenshots/round8/`.
1. Models: **done.** Dan's seven Mixamo rigs and 26 clips in the same night (the Violinist rigged without his cape: `split_cape.py`,
   then `attach_cape.py` put it back on his spine / shoulder bones, rigid, no stretch); Abi's mesh closed. Code's rigs: dragon Kira
   (fur matched to Kira's gray; her wings kept as their own two-sided sheet on two wing bones; Kira lock OK: 49% gray, 0% white, amber
   eyes, no ear bones; her eyes are a deeper amber, so the lock's eye test takes that for the dragon form), the rat, the pony (head
   turns 4 / 0 / -1 deg). Previews `round8/models`, `round8/rigs`, `round8/mixamo`.
2. Props: **done.** `medieval_world.py`: the castle (curtain walls with walks, four round towers, two with cone roofs, the gatehouse,
   the keep and its spire, the arcade, the hollow great hall with its gallery), the courtyard's cobbles, the stone pedestal and the
   purple shard, the hay wagon and its wreck, torches, braziers, benches, flower pots, banners, chandeliers, the feast table,
   portcullises, the drawbridge, cell bars, keys, sword, shield, bow, arrow, timber houses, stalls, the smithy, the stands, tents,
   the tilt rail, the high striker, trees (74.5k triangles). The lute (`instruments.py`).
3. `MedievalArrival` (17 s, skippable): **done.** The purple vortex over the courtyard, into the hay wagon, Kira's fiery sneeze, the
   runaway wagon into the west wall (a cartoon blast), the driver staggers out singed (his hat smoking), the pony trots off fine, the
   party slinks away. The wreck, the driver (dazed) and the pony stay in the courtyard.
4. Layout: **done** (DEFAULT): the courtyard and the stone, the walls and towers (stairs, the NW / NE tower tops), the great hall
   (gallery, two swinging chandeliers to the east ledge), the dungeon (its own dark space: rats, three locked cells and their keys, the
   brazier room), the moat (its deep pit for later), the drawbridge, the tournament field, the village and market. Gates as data:
   the yard's boulder and the armory's portcullis (Muscle Man), the store room's cracked wall (charged punch), a village chimney (wall
   jump), the stands' flag platform (double jump, or Muscle Man's boost), the moat's deep pit (deep dive).
5. Muscle Man the rival: **done.** Three spots: the courtyard (lifts a boulder one-handed, flexes, points and laughs; laughs again when
   Dan pushes the yard's boulder), the tournament (rings the high striker's bell; Dan's punch gets the puck 35% up, the charged punch
   86%: "So close!"; he laughs), the great hall (lifts a barrel over his head). Grunts and laughs only, no collider, gone once he joins.
6. Dragon Kira: **done.** Glide (hold Jump in the air: 59 m from the 10 m wall; a plain jump goes 7 m). Fire puff (her attack):
   lights braziers (the dungeon's three open its last room; the stands' three open the prize chest), burns the hay heap and the
   brambles, scares rats; never hurts people.
7. The charged punch: **done.** The bard (the Violinist, his lute held across him by IK, the lyre's playing clip) teaches Dan
   (`ViolinistTeachesMedieval`). Hold the attack (the fist glows after 0.55 s), let go: a lunge-punch that breaks cracked walls in every
   world (checked: the store room here, Bethlehem's courtyard) and knocks knights' shields away.
8. Enemies: **done.** Clumsy knights (shield up at the front: a punch clangs off; from behind it lands; the charged punch sends the
   shield flying), nervous archers on the walls (draw 1 s, a slow arcing arrow; they back off when you come close), dungeon rats (a
   nip, then they scurry off; fire sends them squeaking away). No boss.
9. Shards: **done.** 60 + 2 hearts (the brazier room; the east ledge over the hall). Threshold 150 = 63% of the 240 shards up to here;
   rule 2: 191 available on arrival (Ice Age 43 + Egypt 50 + Bethlehem 48 + Medieval 50); the 10 later-move shards not counted.
10. `TheStone` (19 s, skippable): **done.** Muscle Man brags, heaves ("Pull Heavy Object"), falls flat on his back; Dan steps up, the
   shard slides out; Muscle Man kneels: "My hero!"; the purple vortex takes all four -> the vortex space (4 of 8; the Pirate Seas path
   opens, "to be continued"). Only Dan can pull it (anyone else: "It won't budge for Abi... switch to Dan").
11. Muscle Man the companion: **done.** Follows as a third follower (warps like the others), cheers at pickups, and does his jobs at
   X: rolls boulders aside in every world (checked: Bethlehem's cave, the Medieval yard), heaves the armory's portcullis up, boosts Dan
   onto the stands' flag platform. He goes through the vortex with the party and wears his Medieval clothes everywhere.
12. Look: **done.** A torch-lit night: deep blue sky, cool moonlight, warm torches, the stone's purple glow. Frame rate above.
13. Music and sound: **done.** Dan's `medieval.mp3` in the world (checked playing, muted). 23 ElevenLabs effects (fire puff, hay
   fire, the wagon blast, armour, shield block / break, arrow, rat, Muscle Man's grunt / laugh / "My hero!", boulder, the shard sliding
   out, the punch's charge and hit, a wall breaking, wings, crowd, the bell, the pony, cobble steps, the portcullis, the lute tune).
14. Build / check / publish: **done.** 0.8.0 published as `game-800`. MP4s: `Recordings/MedievalArrival.mp4`, `TheStone.mp4`.
   The Round 1 starter install in `local/sc` updated itself from GitHub to 0.8.0 and started. Disk: 9.4 GB -> 8.8 GB.

Also fixed on the way (found by the checks): the moat had no way out once its steps were gone (now two 0.3 m steps on each side's
middle); the dungeon's checkpoint flag stood in the corridor (now no flag); a follower further back in line (Muscle Man) warped every 3 s (the "not caught up" distance now grows with the
place in line); the older checks walked the vortex at fixed angles (now read from the paths: six paths, 24 deg apart).

Decisions Code made (Dan can overrule): the lute is held across his body with the lyre's playing clip (as Bethlehem's harp); "Pull
Heavy Object" is the stone's heave ("Pulling A Rope" spare); the rival's three spots and bits as above; X near a job sets Muscle Man to
it; rats run off after a nip (the bot kept losing all three hearts to them); the dungeon is a separate space down a stair door.

**For Dan to check:** the arrival and the stone (MP4s in `Recordings/`, and in game); listen once to Muscle Man's "My hero!"
(`Assets/Resources/Sounds/mm_master.mp3`: ElevenLabs' sound model, not checked by ear; re-roll with `make_sounds.py --only mm_master
--force` if it isn't a clear word); dragon Kira's glide and fire; the charged punch's feel (0.55 s hold); the rival's bits (funny?
too often?); the knights / archers / rats (fair?); the chandeliers; the night look. Known: dragon Kira's wings look a little faceted when
spread (Meshy's thin sheet); the pony's gallop is the generic quadruped run.

(From TODO.md's "Next rounds", retired at the end of Round 10: both drafts were delivered.)
- **(Now the current round above.) Round 9: the cutscene redo (planned by the planning chat, 2026-11-11; next after Fix 8.1, before the Pirate
  Seas, which becomes Round 10).** Dan's film clips are in `SourceArt/Reference/Clips/` and the planning chat's
  shot-by-shot notes in `Docs/CUTSCENE-FILM-NOTES.md`. Rebuild the eight existing cutscenes (`ActOne`,
  `IceAgeArrival`, `EgyptArrival`, `SphinxGag`, `BethlehemArrival`, `WiseMenArrive`, `MedievalArrival`,
  `TheStone`) to follow each clip's shot order, angles, framing and timing, in the game's style with the game's
  models; the "What the game should change" list in that doc is the checklist. Muscle Man says "My hero!".
  `TheStone` ends in the cobblestone whirlpool. `ActOne` will be about a minute (the film's length): Dan's longer Act I
  song is in (2026-11-11): `Music/act1.mp3`, 60.0 s, replacing the 22 s one; time the scene to it. For each: render the MP4 from the exe and a
  side-by-side contact sheet (film frames above, game frames below, same times) for Dan to compare.
- **(Superseded by the Round 10 above; kept for reference.) Round 10: the Pirate Seas (draft by the planning chat, 2026-11-11; written in full after Fix 8.1 and once Dan's
  concept images are in).** Film references in `SourceArt/Reference/Worlds/05-PirateSeas/` and `Characters/*`
  (added 2026-11-11), including Dan's clip `shard-rolls-overboard.mp4`; image prompts P1–P9 in
  `Docs/MODELS-NEEDED.md`; music in `Docs/MUSIC.md`.
  **All in (Dan, 2026-11-11), checked by the planning chat:** the 9 concept sheets in `SourceArt/Concepts/`
  (`dan_pirate`, `abi_pirate`, `kira_otter`, `muscleman_pirate`, `violinist_pirate`, `pirate`, `crab`,
  `pufferfish`, `fish`) and both songs: `Music/pirate.mp3` (2:59, the ship, crew shouts) and
  `Music/underwater.mp3` (3:00, dreamy, soft choir). Notes for Code: otter Kira is a browner gray than hers: match
  it; Muscle Man grins with teeth again (fine); the fish has an open smile (fine, it isn't rigged for talking).
  - **Two areas, almost two levels (Dan, 2026-11-11):** the Pirate Seas are **the ship** (the deck, rigging, crow's
    nest, the hold, plus the tropical island / cove) and **under the sea** (the reef, the shipwreck, underwater caves,
    the sea floor where the green shard lies). Each has its own look, its own enemies, its own shards (DEFAULT about
    30 each) and **its own song**: `pirate.mp3` on the ship and island, `underwater.mp3` whenever the party is under
    water, crossfading at the surface. One world for the vortex and the story shard; the shard drop (Muscle Man
    loses it overboard) is the doorway from the ship to the deep: before it, the water near the ship stays shallow;
    after it, the deep sea opens. Rule 2 and the per-world minimum count both areas together.
  - **Arrival (from Dan's clip, `Docs/CUTSCENE-FILM-NOTES.md`):** the ship at **night** with lanterns; a green vortex swirls round it; Dan and otter Kira pop out of barrels, Muscle Man drops through the sails and crashes onto the deck, Abi laughs. Earlier draft text, superseded where it differs: a green whirlpool vortex over a pirate ship at sunset; Dan, Abi and Kira land in separate barrels,
    Muscle Man crashes into a crate; they pop out as pirates, Kira as a little gray otter.
  - **Look:** a red-brown wooden ship with turned rail balusters, pink sails, rope rigging, barrels; a pink-violet
    sunset sky over a deep blue sea; underwater: bright blue water with light shafts, cheerful cartoon fish,
    colorful corals, the green shard glowing on the sandy sea floor.
  - **The film's story beat (Dan's version, from his clip):** the green shard is found (DEFAULT: in a treasure chest
    by the rail, as the synopsis says). Muscle Man grabs it, holds it up laughing with joy, it slips from his hand,
    bounces and rolls across the deck, under the rail and over the side into the sea. He leaps in after it, Dan and
    Abi follow, otter Kira dives after them: the big underwater section.
  - **Otter Kira:** fast swimming, deep diving and no breath limit; she squeezes through narrow underwater gaps;
    on land she slides on her belly across wet decks.
  - **New move: everyone's deep dive** (longer breath, swim deeper), taught by the Violinist as a ship's fiddler.
    Opens every earlier world's deep-water gates (the Ice Age lake, Egypt's Nile, Bethlehem's well, Medieval's moat).
  - **Muscle Man is a companion here** (his first world as one): lifts cannons, crates and the anchor, boosts Dan
    up the rigging; underwater he swims slowly and is the comic one struggling to keep up (film).
  - **Places (DEFAULT):** the pirate ship (deck, rigging, crow's nest, the hold), a tropical island with a cove and a
    jungle path, the reef, a sunken shipwreck, underwater caves.
  - **Enemies:** goofy pirate deckhands, crabs, pufferfish underwater (puff up and bump you). No boss (DEFAULT).
  - **Finale:** when the necklace wakes it leads to the green shard on the sea floor (film:
    `green-shard-sea-floor.png`); collecting it makes an underwater swirling vortex that pulls everyone in.

- **Code's notes for Round 9 (the planning chat decides):** Dan's notes on 0.8.0 first; "My hero!" by ear (re-roll if unclear);
  Muscle Man already follows into every world (the Pirate Seas' "drops the shard overboard" can use him); the Pirate Seas path is in
  the vortex ("to be continued"); the deep dive opens the moat's pit (DD1, DD2), Bethlehem's well, Egypt's Nile pool and the Ice Age
  lake; art left over: real meshes for the plain boxes (Bethlehem's rock pillars and cave, Egypt's walls and plinths, Ice Age slabs,
  the Medieval dungeon), dragon Kira's wing membrane (faceted when spread), the pony's own gallop.

===========================================================================
Fix 8.1 delivery (moved from TODO.md, 2026-11-12)
===========================================================================
### Fix 8.1: Medieval fixes + shard thresholds (0.8.1, build 801, 2026-11-11)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): new modes `fix81` (all four worlds, ~45 min), `fix81med`,
`floors`, `platforms`, `reach`, `rule81` (`Checks/ExeCheck.Fix81.cs`: a probe collider is made from every visible mesh and compared
with what the heroes stand on and bump into). `local/round81/final_med` (`fix81med`): all passed; `local/round81/final` (`fix81`, all
worlds): 2 left, both reported below; `stuckmed` (rule 12): all passed; `medieval` (Round 8's play-through, regression): all passed,
112 ok. Audio 0.000 RMS in every run, the builds and Blender; window watcher: no window, never in the foreground. Evidence:
`Data/screenshots/round81/`.
1. Wading in Muscle Man's room: **done.** Cause: the keep's stone plinth (`medieval_world.py`) was one solid block over the whole
   keep, 0.8 m tall, so in the great hall (where the rival Muscle Man lifts his barrel) the floor you saw was its top while you walked
   on the flagstones under it. Now a ring outside the walls only. The audit (every walkable collider top vs the visible surface, per
   area) also found and fixed: the keep door's three steps had no collider; the feast table's collider was commented out (and 11 cm
   too high) and its benches' colliders sat 45 cm off the benches; the flower pots, the arcade deck's end and its edge lip, the hearth.
   Medieval now: 0 sunk points in the courtyard, hall, field, village and dungeon (2 stray points at 11 cm on the walls / moat); feet
   at Muscle Man's spots +-3 cm for all three heroes (`81_feet_*`). Spot-check of the other worlds: no floor anyone walks sinks; listed
   only (props whose look reaches past their collider: Egypt's 30 cm roof parapets, a 4-point patch at a far pyramid's foot).
2. The drawbridge: **done.** Cause: its deck collider line had slipped into a `//` comment in Round 8. Now a child of the drawbridge
   that sits exactly on the seen planks (measured at build time: x -4..4, z -18..-3, top 0.00) and moves with it (raised 80 deg its top
   is at 15 m; lowered again 0). Every moving platform in every world, every hero (`platforms`): the drawbridge, both chandeliers, the
   Ice Age's two mammoths, Egypt's 12 (boats, shuttle stone, hauled stones, carts, the lift, the turning bridge, the tomb's sliding
   blocks; the camel is ridden, checked by `camel`): every hero stood on each, none fell through. Bethlehem has none.
3. The invisible wall by the village: **done.** Cause: the village's back alley (shard V06, behind the brambles) was boxed by three
   hidden 7 m walls with nothing drawn there, and the three bramble bushes (2 m across) had 4.6 m colliders turned with each bush, so
   the gaps between bushes were invisible walls too; the chimney's walls were hidden as well. Now seen stone walls, bushes scaled to
   fill the alley with unturned colliders. The new scan (every hidden solid's open sides: anything drawn there?) also found the yard
   boulder's collider 1.9 m up in the air (the ground was found after its own new collider; a gap under it into the walled yard), the
   round towers' colliders 1.4 m inside the drawn towers (now a 5 m cylinder), the north wall walks without a parapet (feet in the
   merlons) and the hall's north gallery rail with no collider (you walked through it, and onto the east ledge without the
   chandeliers). Medieval: 0 invisible colliders left (the gate arch over the passage is out of reach). Reachability (`reach`): the
   bot walks to every shard from up to three open sides, with the moves taught so far and gates opened as their move would (jumping
   gaps, skipping cutscenes): Ice Age 49, Egypt 48, Bethlehem 51, Medieval 53 walked, **0 not reached**; not walked: later-move shards
   and shards under water (the never-stuck water checks cover those).
4. The new shard rule: **done** (DEFAULT, Dan can tune). The necklace wakes when the total reaches ~75 % of all shards so far
   **and** 20 come from this world (`AreaData.fromHere`). Thresholds: Ice Age **40** (not 45: only 43 of its 60 are reachable on
   arrival, rule 2), Egypt 90, Bethlehem 135, Medieval 180. Rule 2 with both parts: Ice Age 43 >= 40 (43 here), Egypt 93 >= 90 (50 here),
   Bethlehem 141 >= 135 (48 here), Medieval 191 >= 180 (50 here). In the exe: 170 in all + 25 here: asleep ("Next: 180 (this world:
   20 / 20)"); 210 + 12 here: asleep ("Next: 180 (this world: 12 / 20)", `81_hud_next_this_world.png`); the 20th from here wakes it;
   an awake world with 5 from here stays awake. CLAUDE.md rule 3 and `design-decisions.md` updated; hints and messages say both parts.
5. Build / check / publish: **done.** 0.8.1 published as `game-801`; the Round 1 starter install in `local/sc` updated itself from
   GitHub to build 801 and started. Disk: 9.8 GB -> 9.3 GB.

Decisions Code made (Dan can overrule): the Ice Age threshold 40 instead of 45 (rule 2); the bramble bushes drawn 2.2x bigger so the
alley looks closed where it is closed; the hall's north rail now blocks walking onto the east ledge (the chandeliers are the way, as
designed); the old Round 8 check jumps onto the feast table for G01 (it sat inside the table before, with no collider under it).

**For Dan to check:** the great hall (floor, steps, benches, table); the drawbridge both ways; the village alley and its brambles (do
they read as "burn me"?); the round towers and wall walks (no walking into stone); the necklace pace with the new rule (HUD line).
Known, not changed: Egypt's flat-roof parapets have no colliders (feet 30 cm into the lip at a roof's edge); the village houses'
collider tops (7 m) cut through their pitched roofs (only a gliding Kira could get there).

===========================================================================
Fix 10.1 plan (as written by the planning chat, 2026-11-11)
===========================================================================
---------------------------------------------------------------------------
Current round — Fix 10.1: never shard-locked, the deckhands, the arrival, the sails, a cutscene theater (target 0.10.1)
Round 10 is delivered (below). Dan played 0.10.0 (2026-11-11). Report per item: done / not done / deferred, and why.
Testing rules as always; rule 12 and the Fix 8.1 checks apply. Don't end your turn until 0.10.1 is published unless
truly blocked (say so).

1. **Shard-locked in the Pirate Seas (Dan: "I got all I could on this level and still don't have enough, and I can't
   go back to the vortex to get more").** Two faults:
   a. **The threshold is unreachable for a normal player.** 225 = 75% of all 300, but the under-sea half only opens
      after the necklace wakes, so before the wake a player has only the ship's 30 plus whatever they kept from earlier
      worlds; a player who didn't nearly complete every earlier world is stuck. Rule 2 checked "everything reachable",
      not a real player.
   b. **No way back to the vortex** until the story shard is taken, so a short player can't fetch shards elsewhere.
   **New rule (DEFAULT, replaces Fix 8.1's 75% rule; Dan can tune):**
   - **Stepped thresholds:** each world's threshold = the previous world's threshold **+ 25**, and **at least 15**
     from this world. So a player who only ever collected the minimum needs 25 here (always reachable before the
     wake), and one who collected extras still needs 15 here. Recompute every world in order (Ice Age 25; Egypt 50,
     Bethlehem 75, Medieval 100, Pirate 125, …; check Ice Age's first value against its pre-wake shards).
   - **Rule 2, rewritten (the check must prove it):** simulate the **minimum player**: in each world, they collect only
     the threshold's need, using only shards reachable **before that world's necklace wakes** with the moves they hold
     then. Every world must be completable that way. Also simulate the completionist (all reachable) to confirm the
     15-from-this-world floor makes sense. Report both per world.
   - **Always a way out:** from the moment the first vortex has been opened, **Pause → Return to the vortex** is
     available in every world (the party's spot is saved; worlds already unlocked stay open; the current world's path
     stays open too). So a player who's short can always go back to an earlier world to collect more.
   - Existing saves: recompute; nothing already woken relocks.
   Update `CLAUDE.md` rule 3 and `Docs/Design/design-decisions.md`.
2. **The pirate deckhands stand in a T-pose.** Their animator isn't wired (Dan's Mixamo files for `pirate` are in):
   idle, walk / run (chase), trip (the peg-leg gag), hit, recover. Then check **every animated character in every
   world** for a T-pose or bind pose in play and in cutscenes (a check that samples each character's pose and flags
   any still in the rest pose).
3. **`PirateArrival` skips the fall.** Dan: they just climb out of the barrels; it misses that they happen to fall into
   them. Follow the clip from the vortex: the green vortex over the ship, the three tumble out of it, each drops
   **into a barrel** (a shot of each landing: splash / thud, the barrel wobbles), then they pop out; then Muscle Man
   through the sails. Re-render and re-sheet against the clip.
   **Muscle Man didn't look at Dan when he said "My hero!".** Whenever a character speaks or reacts to someone in a
   cutscene, they turn their head (and body if needed) to that person: add look-at (head / eyes / upper body) to the
   cutscene tooling and use it in every scene with a line or reaction (`TheStone`, `PirateArrival`, `SphinxGag`,
   `WiseMenArrive`, the arrivals). Check each with a close-up frame.
4. **The sails often block the view.** The camera must never sit behind a sail, net or rigging: thin cloth and rope
   don't push the camera in; instead, anything between the camera and the player **fades to see-through** (sails,
   nets, ropes, masts, also the Medieval banners and similar). Check from the player's view walking the whole deck
   and rigging.
5. **"How can I watch the other cutscenes?"** Add a **Theater** to the title screen and the pause menu: every
   cutscene the player has seen so far (saved), grouped by world in story order, play any of them (skippable, with
   its music). A New game save starts with none; Dan's current save fills in what he's seen.
6. Build, check (a minimum-player play-through from New game to the Pirate Seas' green shard; Return to the vortex
   from mid-Pirate and back; the T-pose sweep; the arrival sheet; the sails view), publish 0.10.1, update `TODO.md` /
   `CLAUDE.md`, clean up.

### Round 9: the cutscene redo (0.9.0, build 900, 2026-11-11)
Every scene was built from Dan's clip (contact sheets of each clip, `python Tools/film_sheet.py frames <clip>`), rendered from the exe
and compared on a side-by-side sheet (film on top, game below, matching moments: `python Tools/film_sheet.py sheet <Scene>`), four
passes of fixes. Code: `Assets/Editor/CutsceneAuthoring.Film.cs` (`CutsceneAuthoring.Version` 25). Check `local/round9/r9` (new mode
`r9`, ~5 min: New game -> Act I to its end -> each scene through the game's own path, each skipped with a held A): RESULT all passed (49
ok). Audio 0.000 RMS in the check, the builds and all 8 renders x 4 passes; window watcher: no window, never in the foreground (one
build's watcher listed a visible "Task Manager" window by a reused process id: not started by Code, never in the foreground).
1. `ActOne` (60 s, Dan's 60 s `act1.mp3`): **done.** Decorating (Abi left, Dan right of the tree, Kira under it), Kira leaps at the bauble
   in Dan's hand, his fumble, the lunge and gasp, the close-up on the floor, the gold glow, Kira sniffs, Abi scoops her up, light beams
   and blue crystal shards, the rainbow spiral between them, holding hands, the rainbow tunnel, then the Ice Age sky (the room's light
   switches to the world's): a mammoth under the blue vortex, the vortex in the clouds, the three falling past the mountains. Act I's
   song plays to its end; the Ice Age theme starts on the landing (`Game.ActOneMusicFadeAt` 58.4).
2. `IceAgeArrival` (22 s): **done.** Landing white-out; by a campfire the gray house cat glows and is the sabertooth when it fades;
   high shots on Dan and Abi, ground-level on her (snarl, stalk toward the camera), the leap, Dan on his back, Abi kneeling, Kira on his
   chest pawing the blue necklace; Round 5's relief beat (laugh, the shard in his hand) kept.
3. `EgyptArrival` (33 s; the film's 44 s with a shorter empty-dune pause and dig): **done.** The building site (scaffolded pyramid, sledge
   teams, the camel crossing close), the red vortex over a dune north of town, the sand plume, the empty dune, they pop out, look round,
   kneel and dig, mummy Kira pops up, Abi holds her, the necklace red. Play then starts on the plaza as before.
4. `SphinxGag` (17.6 s): **done.** From behind them (Abi left), the push-in to Kira's stone face, held; they turn, react; Dan looks down;
   mummy Kira's innocent close-ups; the long look, red necklace. (The old 16.5 m / 21 m face position was inside its chin: measured anew.)
5. `BethlehemArrival` (21 s): **done.** The Wise Men in the field east of the barn see the star grow into a golden spiral; the golden ring
   over the barn with its beam; a golden flash; top-down crash through the roof; the Wise Men puzzled; inside (warm light): Abi sitting
   on the hay with lamb Kira, Dan flat on the floor in a gold glow.
6. `WiseMenArrive` (17 s): **done.** Silhouettes at the barn, they push the doors open (cutscene doors), Dan and Abi face each other over
   the glowing manger with lamb Kira, the Wise Men stare then sag (Santa hat on the left), turn and leave, **the Santa-hat one tosses his
   present over his shoulder**, the hug, the golden hay tornado takes them.
7. `MedievalArrival` (41 s): **done.** The castle under the purple vortex, the three fall out of it, top-down the pony pulls the wagon
   in and they drop into the hay, it heaves, they pop up (Kira, Dan, Abi), Kira's sneeze sets it alight, out, the runaway wagon from
   behind them, the wall blast and the singed driver (kept), then they walk on with **Kira flying** beside them.
8. `TheStone` (60 s): **done.** Muscle Man heaves, falls back, embarrassed then smug close-ups, points: your turn; Dan steps up (necklace
   glowing), reaches, lifts it out with no effort into his palm; Muscle Man laughs, then **"My hero!"** (new `mm_my_hero.mp3`; `mm_master`
   deleted; no "Master" left in code, sounds or messages); the cobblestones twist up into a column and **a whirlpool** that takes Dan,
   Abi with Kira, and Muscle Man; the blue-white tunnel with all four tumbling. New `stone_whirl.mp3`.
9. Hand-offs: **done** (`r9` above): each scene plays from the game's own path, its music is right (Act I's song, then the world themes;
   no theme under WiseMenArrive / TheStone; the vortex track after them), a held A skips it, control comes back and the stick moves.
10. Build / check / publish: **done.** 0.9.0 published as `game-900`; the Round 1 starter install in `local/sc` updated itself from
    GitHub to build 900 and started (ready signal). Disk: 10.7 GB -> 10.2 GB.

Also found and fixed on the way: the people's Mixamo "fall" carries a downward root motion that a Timeline adds up (a long fall sank
metres; `Kit.Clips` now plays a long fall as restarting 0.6 s pieces); a shot can pan as it moves (`Kit.Shot(..., lookTo)`); film mode
hides the stone's own shard for `TheStone` as the game does.
Decisions Code made (Dan can overrule): lengths as above (Egypt shorter than the clip, Medieval longer for the game's blast and driver);
Egypt's dune and the Wise Men's field are sets out in the world (`Kit.Set`), so the scenes stand where the film's do; outfits from the
concept sheets; Dan's 60 s Act I song was in Downloads ("Shards of Time - Act I cutscene (1).mp3", 59.9 s), not yet in
`Music/act1.mp3` (still the 22 s one): Code copied it there.
Known, not changed: no facial animation (laughing, gaping and smug read through the body clips only); Egypt's workers are the graybox
figures; the Sphinx is the game's own (with its headdress); the vortexes are blob spirals, not painted swirls; the scenes are night where
the world is night (Bethlehem, Medieval) and keep the game's colours rather than the film's warm grade.

**For Dan to check:** the eight sheets `Data/screenshots/round9/{ActOne,IceAgeArrival,EgyptArrival,SphinxGag,BethlehemArrival,
WiseMenArrive,MedievalArrival,TheStone}.jpg` and the eight videos `Recordings/<same names>.mp4` (no sound in the videos); Act I with
your song from New game (does it land on your beats?); listen to `Assets/Resources/Sounds/mm_my_hero.mp3` and `stone_whirl.mp3`
(Code doesn't play sound on your PC).

### Round 10: the Pirate Seas (0.10.0, build 1000, 2026-11-11)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad): `local/round10/final` (new mode `pirate`: from the vortex with four
worlds done -> PirateArrival -> the ship, the rigging, Muscle Man's jobs, the fiddler, the island and cove -> the necklace -> the chest ->
ShardOverboard -> under the sea -> the green shard -> SeaVortex -> the vortex -> back in for the grotto -> the Medieval moat's deep pit):
RESULT all passed (61 ok); the bot took 3.2 min from the arrival to the vortex. `stuckpirate` (rule 12, 13 spots x every hero): all passed.
`fix81pirate`: all passed (25 walkable shards reached, 0 invisible walls, no moving platforms). `seareach` (new): all 30 under-sea shards
swum to. `perfpirate`: 272 fps at the slowest place (the reef, 1080p), 288 (Steam Deck setting) (need 60 / 40). `r9` (regression, the
vortex paths moved): all passed. Audio 0.000 RMS in every run, build and render; window watcher: no window, never in the foreground (one
build listed a visible "Task Manager" by a reused process id: not started by Code, never in the foreground). Evidence: `Data/screenshots/round10/`.
1. Mixamo first: **done.** Meshy -> clean-up -> five upload files; Code stopped and told Dan (5 T-poses + 3 Muscle Man clips; the heroes'
   swim clips were already in). Dan's five rigs arrived the same evening and are in (Abi's mesh closed). **Not in:** his three clips
   (Drowning, Getting Up, Jumping Down): fallbacks Treading Water / Hard Landing / Jump until they are (`Docs/MODELS-NEEDED.md`).
2. The other models: **done.** Otter Kira (Code's rig, 10 clips incl. a belly-slide; fur moved to Kira's gray; Kira lock OK: 0 ear bones,
   55% gray, 0% white, amber eyes 0.03% (small on her long atlas: the lock takes 0.02% for her, as for the dragon)); crab, pufferfish, fish
   as static models moved by code (fish decimated to 1k triangles). Props by script (`Tools/Blender/pirate_world.py`): the ship (red-brown
   hull, turned balusters, pink sails, rigging nets, tops, crow's nest, lanterns, the hold), the island and cove, the sea floor, reef corals,
   kelp, the shipwreck, the sea cave, chest, cannon, anchor, barrels, crates, palms. The fiddle is the Ice Age's (with its playing clip).
3. Two areas: **done.** The ship at sunset (decks, rigging, crow's nest, hold, cabin, locker) + the island and cove with `pirate.mp3`; under
   the sea (reef, wreck, cave, grotto) with `underwater.mp3`, crossfading at the surface (head under 0.6 s / out 1.2 s). One world, one area.
4. `PirateArrival` (34 s): **done.** The film's order: the tunnel, the ship at night, the green vortex, the barrels, Dan and otter Kira out,
   Abi, Muscle Man through the sails, the crash, flat, sits up grinning, Abi laughs, the green shard. Sheet `round10/PirateArrival.jpg`.
5. `ShardOverboard` (17.5 s): **done.** Held up from behind, it slips, bounces, rolls across the deck, under the rail, into the sea; he leaps
   after it, Dan and Abi follow, otter Kira dives. Before it the sea pushes everyone up below 3.5 m; after it the deep sea is open.
   Sheet `round10/ShardOverboard.jpg`. Also `SeaVortex` (9 s, the finale). MP4s in `Recordings/`.
6. Otter Kira: **done.** Swims 1.6x, any depth, no breath limit; squeezes into the cave's grotto (0.95 m gap) and the hold's cargo nook;
   belly-slide (B while running): 6.1 m in the check, full speed on the wet deck.
7. Muscle Man: **done.** Rolls the cabin's cannon aside, lifts the hold's crates and the locker's anchor, boosts Dan / Abi onto the fore
   top; struggles under water (slower, his struggle clip). Jobs now only on your own level (an attack below decks no longer triggers one).
8. The deep dive: **done.** The fiddler on the quarterdeck teaches everyone (breath x3, any depth). Checked in an earlier world: DD1 in the
   Medieval moat's deep pit.
9. Enemies: **done.** Goofy deckhands (chase on their own deck, trip over the peg leg: 1.6 s harmless), crabs (sideways, snap), pufferfish
   (puff up near you, harmless deflated). 80 friendly fish in 8 schools. No boss.
10. Shards: **done.** 60 (30 ship + island, 30 under the sea) + 2 hearts (the cabin, the wreck). Threshold **225** (75% of 300) and 20 from
   here; rule 2: Ice Age 45 + Egypt 52 + Bethlehem 50 + Medieval 54 + Pirate Seas 30 = 231 on arrival (Muscle Man counts now), 30 here.
11. The finale: **done.** The necklace leads to the chest, then down to the green shard among corals; `SeaVortex` takes all four ->
   the vortex (Home: 5 of 8; Victorian Steampunk "to be continued").
12. Look: **done.** Pink-violet sunset, deep blue sea; under water bright blue fog to 75 m, light, colourful corals. Frame rate above.
13. Sound: **done.** 18 new ElevenLabs effects (waves, wood creak, barrel thud, sail rip, crash, chest, shard bounce / roll, splash,
   bubbles, sea vortex, crab, pufferfish, "Arr!", rope, deck steps, cannon roll, the fiddle's shanty); Muscle Man's laugh and "My hero!"
   reused. No music made by Code.
14. Build / check / publish: **done.** 0.10.0 published as `game-1000`; the Round 1 starter install in `local/sc` updated itself from GitHub
   to build 1000 and started (ready signal).

Also found and fixed on the way: Blender's merge left all ship balusters on one spot (stale transforms); hold beams crossed the hatch ramp at
head height; ramps had a crawl space under them (now solid wedges); the back room's lintel was exactly Dan's height; the port rail had no
gap at the rope ladder; the cove's bank was too steep to walk out; the wreck's ribs closed its open deck; a deckhand and the heal heart sat
in the arrival's shots (world enemies are hidden during Pirate cutscenes).
Decisions Code made (Dan can overrule): the vortex now has seven paths 20 deg apart (Victorian Steampunk added); Pirate threshold 225; the
plank goes out through a gap in the rail at deck level; the rigging is climbed by Jump at its net (an automatic climb), the fore top by
Muscle Man's boost; air bubbles from vents give Dan and Abi their breath; the arrival plays at night, play continues at sunset.

**For Dan to check:** the two sheets `Data/screenshots/round10/{PirateArrival,ShardOverboard}.jpg` and the MP4s in `Recordings/`; the ship
by the rope nets, the plank, the crow's nest; otter Kira's belly-slide on the wet deck; the music switching as you dive and surface; the
sounds (Code doesn't play sound on your PC). Known: the first view of the reef / the deck stutters once (250-420 ms, shaders warming up);
the wreck reads as a ribbed box; the island's grass edge is still a little stepped; Muscle Man's three clips (above).
