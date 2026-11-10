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
