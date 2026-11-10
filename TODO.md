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
Last deliveries
---------------------------------------------------------------------------
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Suggested for Round 5 (Code's notes, the planning chat decides):** the four
  creature models from Dan's images (Meshy; Code's Blender rigs for wolf,
  critter and mammoth; Mixamo for the Violinist) replacing the placeholders;
  Dan's Suno Ice Age theme; then Egypt (mummy Kira, the judgmental camel).
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
