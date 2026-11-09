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
