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
Current round — none. Fix 6.1 is delivered (below); Round 7 (Bethlehem) is
drafted under "Next rounds". Fix 6.1's plan is in `TODO-ARCHIVE.md`.
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Round 7: Bethlehem (draft by the planning chat, 2026-11-10; written in full
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
  Image prompts B1–B12 in `Docs/MODELS-NEEDED.md`, music in `Docs/MUSIC.md`.
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
