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
Current round — Fix 7.2: never stuck (the well) + a reset option (target 0.7.2)
Round 7 and Fix 7.1 are delivered (below). Round 8 (the Medieval Kingdom) is
next; the planning chat writes it once Dan's film references are in. Report
per item: done / not done / deferred, and why.

1. **"If you get stuck in the well you can't get out" (Dan, 2026-11-10).** The
   well is a deep-dive gate (divers are pushed back up), but a hero who falls
   in can't climb out. Fix it: a rope and bucket (or rungs) inside to climb up,
   and swimming up to the rim climbs out automatically (like the ice holes in
   Round 6). Check from the player's view, each hero, both from a fall and a jump.
2. **Never stuck anywhere (standing rule, add to `CLAUDE.md`).** Every pit,
   well, pool, hole, crawl space and enclosed area in every world must have a
   way out with the moves the player has at that point, or not be enterable
   without them. Add a check that runs for every world: drop each hero into
   every water volume and every low enclosed spot (the level data lists them)
   and prove the bot gets out within ~20 s; report any it can't, then fix them.
   Run it on the Ice Age, Egypt and Bethlehem now.
3. **A reset button (Dan, 2026-11-10).** Pause menu: **"Return to checkpoint"**
   (back to the last checkpoint with the party, no heart lost, shards kept),
   plus a safety net: if the player hasn't been able to move far for ~15 s in a
   spot with no exit (no progress, stuck against geometry), show a small hint
   "Stuck? Pause → Return to checkpoint". Works in every world and in the vortex.
4. Build, check, publish 0.7.2, update `TODO.md` / `CLAUDE.md`, clean up.
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Code's notes for Round 8 (the planning chat decides):** Dan's notes on 0.7.0 first; Bethlehem's
  Mixamo people (the Wise Men sitting on their camels, the watchmen) and the guard dog once Dan's files
  are in; art left over: real meshes for the plain boxes (Bethlehem's rock pillars and cave, Egypt's walls
  and plinths, Ice Age slabs), Abi's Egypt sandal strand, the Sphinx gag's double-take shot.
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
