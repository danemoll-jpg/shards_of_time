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
Current round — Round 8: the Medieval Kingdom (target 0.8.0)
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
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
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
