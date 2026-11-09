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
