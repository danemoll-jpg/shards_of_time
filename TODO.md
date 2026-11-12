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
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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


---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Code's notes for Round 11 (the planning chat decides):** Dan's notes on 0.10.0 first; Muscle Man's three Pirate clips (Drowning,
  Getting Up, Jumping Down: drop them in `SourceArt/Mixamo/muscleman_pirate/`, see `Docs/MODELS-NEEDED.md`); the vortex's Victorian
  Steampunk path is in ("to be continued" until now: open after the green shard); its move roadmap entry is Abi's wall jump (the Medieval
  village chimney and the Ice Age gates wait for it); the deep dive now opens every earlier world's deep water (checked: the Medieval moat's
  pit); art left over: the wreck's look (a ribbed box), the island's grass edge, real meshes for the plain boxes in earlier worlds, dragon
  Kira's wing membrane, the pony's own gallop.
- Memory Fragments (Dan's time-echo visions when touching temporal artifacts)
  and the Violinist's per-world appearances. Deferred, not decided against.

---------------------------------------------------------------------------
Technical notes
---------------------------------------------------------------------------
- Open design question: Tomorrowland has no story shard in the film; default is
  that the rocket plays that role (shard total fuels the launch). Confirm with
  Dan before building Tomorrowland.
