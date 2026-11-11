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
Current round — Round 9: the cutscene redo (target 0.9.0)
Fix 8.1 is delivered (below). Pirate Seas is Round 10 (drafted under "Next rounds"). Report per item: done / not
done / deferred, and why. Testing rules as always (`Tools/quiet.py`, muted with the level reported, never a window in
front of Dan; cutscene videos rendered from the exe). Don't end your turn until 0.9.0 is published unless truly
blocked (say so).

**Inputs, all in (Dan, 2026-11-11):** his film clips in `SourceArt/Reference/Clips/` (the clip is canon for its scene),
the planning chat's shot-by-shot notes in `Docs/CUTSCENE-FILM-NOTES.md`, and his longer Act I song `Music/act1.mp3`
(60.0 s). Game style and game models; outfits from the concept sheets (not the clips); the game's added gags stay
(e.g. the wagon hitting the wall and the singed driver).

For each scene: extract the clip's frames, follow its shot order, camera angles, framing and timing, then render the
MP4 from the exe and make a **side-by-side sheet** (film frames on top, game frames below, at matching moments) in
`Data/screenshots/round9/<scene>.jpg`. Look at each sheet and fix what doesn't match before moving on.

1. **`ActOne`** (~60 s, timed to `act1.mp3`): decorating the tree together, Kira swats, Dan fumbles the bauble, it
   drops; the glow, the cracks and light beams, Kira sniffs, Abi scoops her up; the crystal-shard burst and the rainbow
   spiral; sucked in holding hands; then the fall out of the blue vortex over the Ice Age's mountains and volcanoes.
   The Ice Age theme starts on the landing.
2. **`IceAgeArrival`**: Kira (still the normal cat) glows and **transforms into the sabertooth on screen** by a
   campfire; high-angle shots on Dan and Abi, ground-level shots on Kira; the stalk, the pounce, Dan flat on his back,
   Kira on his chest pawing the blue-glowing necklace. Keep Round 5's relief beat.
3. **`EgyptArrival`**: the establishing shot (scaffolded pyramid, workers with stone sledges, the judgmental camel
   crossing frame, the town); the red vortex over the dunes; the sand plume and the comic pause on the empty dune;
   Dan and Abi pop out of the sand; they dig frantically for Kira; mummy Kira pops up, Abi hugs her.
4. **`SphinxGag`**: from behind them, the Sphinx far off; slow push-in to Kira's stone face, held; their reaction; Dan
   looks down at mummy Kira; her innocent close-ups; the long look between them. Comic timing over speed.
5. **`BethlehemArrival`**: the Wise Men walking to the barn see the star grow into a golden spiral and flatten into
   the ring over the barn; the top-down crash through the roof; the Wise Men's puzzled close-up; inside, Abi on the hay
   bales with lamb Kira, Dan flat on the floor with the gold glow.
6. **`WiseMenArrive`**: silhouettes walking to the barn, the doors pushed open, the three heroes at the glowing
   manger, the Wise Men's disappointed close-up, they walk out and **the Santa-hat one tosses his present over his
   shoulder**, the hug, the golden hay tornado.
7. **`MedievalArrival`**: the purple vortex over the castle at night, the fall, the top-down drop into the hay
   wagon, popping up, Kira's sneeze-fire, jumping out, the burning wagon careening off (then the wall crash and the
   singed driver), **Kira flying** beside them as they walk on.
8. **`TheStone`**: Muscle Man heaves and fails, points at the shard to challenge Dan; Dan lifts it out with no
   effort; Muscle Man laughs, then gapes: **"My hero!"** (not "Master!": fix every line, sound and subtitle);
   **the cobblestones swirl up into a whirlpool** that swallows everyone, Muscle Man too, into the vortex tunnel.
9. **Check the hand-offs:** every scene still hands control back cleanly, is skippable, and the music cues line up
   (Act I song, then the world themes; the vortex track only in the vortex space).
10. Build, check, publish 0.9.0, update `TODO.md` / `CLAUDE.md`, clean up. In "For Dan to check", list the eight
    sheets and the eight MP4s.
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **(Now the current round above.) Round 9: the cutscene redo (planned by the planning chat, 2026-11-11; next after Fix 8.1, before the Pirate
  Seas, which becomes Round 10).** Dan's film clips are in `SourceArt/Reference/Clips/` and the planning chat's
  shot-by-shot notes in `Docs/CUTSCENE-FILM-NOTES.md`. Rebuild the eight existing cutscenes (`ActOne`,
  `IceAgeArrival`, `EgyptArrival`, `SphinxGag`, `BethlehemArrival`, `WiseMenArrive`, `MedievalArrival`,
  `TheStone`) to follow each clip's shot order, angles, framing and timing, in the game's style with the game's
  models; the "What the game should change" list in that doc is the checklist. Muscle Man says "My hero!".
  `TheStone` ends in the cobblestone whirlpool. `ActOne` will be about a minute (the film's length): Dan's longer Act I
  song is in (2026-11-11): `Music/act1.mp3`, 60.0 s, replacing the 22 s one; time the scene to it. For each: render the MP4 from the exe and a
  side-by-side contact sheet (film frames above, game frames below, same times) for Dan to compare.
- **Round 10: the Pirate Seas (draft by the planning chat, 2026-11-11; written in full after Fix 8.1 and once Dan's
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
- Memory Fragments (Dan's time-echo visions when touching temporal artifacts)
  and the Violinist's per-world appearances. Deferred, not decided against.

---------------------------------------------------------------------------
Technical notes
---------------------------------------------------------------------------
- Open design question: Tomorrowland has no story shard in the film; default is
  that the rocket plays that role (shard total fuels the launch). Confirm with
  Dan before building Tomorrowland.
