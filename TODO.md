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
Current round: none. Fix 10.1 is delivered (below); the next round is for the planning chat to write.
Fix 10.1's plan is in `TODO-ARCHIVE.md`.

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
### Fix 10.1: never shard-locked, the deckhands, the arrival, the sails, a theater (0.10.1, build 1001, 2026-11-12)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run, build and render; no window ever
visible or in front): `local/fix101/{minplayer,tpose,sails,theater}`, all passed. Evidence: `Data/screenshots/fix101/`.
1. Never shard-locked: **done.** a. Stepped thresholds: Ice Age 25, Egypt 50, Bethlehem 75, Medieval 100, Pirate Seas 125 (each +25),
   and 15 from each world (defaults and the five world assets). Rule 2's report now simulates both players: the minimum player
   (arrives with the previous threshold, only shards reachable before this necklace wakes) needs 25 here and has 43 / 50 / 48 / 50 / 30;
   the completionist is fine everywhere. New check `minplayer`: New game -> every world taking exactly 25 pre-wake shards -> the green
   shard: in every world the necklace still sleeps at 24 and wakes at 25 (125 in the Pirate Seas); arrivals skipped with the pad.
   b. Pause > **Return to the vortex** (once the Ice Age's vortex is open, every world): checked mid-Pirate with the pad: in the vortex
   with 110 shards, earlier paths open, back by the Pirate Seas path to the very spot (0.0 m), checkpoint kept. Saves: thresholds come
   from the world data, so existing saves just see the new numbers; nothing awake relocks. `CLAUDE.md` rule 2 / 3 and the design doc updated.
2. The deckhands: **done.** The cause: the `pirate` model imported as a Generic rig (its name was missing from the importer's list of
   people), so no human clip could play. Now Humanoid with the shared clips: idle, walk / run (chase), cheer ("Arr!"), punch, hit (hurt),
   the trip = Fall Flat (mm_fall) then picking himself up (Hard Landing), 2.9 s harmless; stunned = dazed. Dan's `pirate` folder had only
   the T-pose rig, so no per-deckhand clips were needed. **T-pose sweep** (new check `tpose`): every skinned character in all five worlds
   and every cutscene at five moments, against its bind pose: 135 characters; it found one (sabertooth Kira between her snarls in
   IceAgeArrival: a one-shot shorter than its slot left the rig in its bind pose). Fixed in the cutscene kit for every scene (a one-shot
   now holds its last frame); the sweep then passed.
3. PirateArrival and look-at: **done.** The fall from the clip (87-89 s): out of the green vortex, tumbling past the sails, an overhead
   of the three upright barrels, then a close shot of each landing (feet first, thud, splash, the barrel wobbles); at the cut the barrels
   lie on their sides and they crawl out as before. Dan, Abi and Kira now turn to Muscle Man after his crash (the clip's 97-102 s); he
   looks at Dan as Abi laughs; Dan glances at Abi. Look-at (head, neck, chest, after the clips: `CutsceneLookAt`) is in TheStone
   (Muscle Man at Dan for "your turn" and "My hero!", Dan at him), PirateArrival, SphinxGag (up at the Sphinx, then Dan and Kira's look),
   WiseMenArrive (the Wise Men stare at Dan), and the Ice Age, Egypt, Bethlehem and Medieval arrivals. Re-rendered from the exe; sheet
   `fix101/PirateArrival.jpg`, close-ups `fix101/looks.jpg`. (The first look sheet showed bent necks: the head's forward was measured
   on a falling first frame; now taken from the bind pose, tilt capped at 30 deg; re-rendered.)
4. The sails: **done.** Sails, ropes, nets, masts, yards and flags (and the Medieval banners, bunting, pennants) fade to see-through when
   they are between the camera and the player, and masts no longer pull the camera in (thin cloth never did: it has no collider). The
   merged meshes are split per sail / rope at load, so only the one in the way fades. Check `sails`: the deck walked three ways plus the
   main top, crow's nest, fore top and quarterdeck: parts faded where in the way; nothing thin pulled the camera in (only the hull's
   walls and the bulkhead did, as before).
5. The Theater: **done.** Title screen and pause menu: the scenes seen so far, by world in story order, any of them played (skippable,
   with its music), then back where you were. A New game starts with none; an old save fills in what its progress shows (checked with
   the 0.6.0 save: 10 scenes). Checked from the pause menu mid-Pirate (IceAgeArrival, back on deck 0.0 m away) and from the title.
6. Build, check, publish: **done.** 0.10.1 published as `game-1001`. The Round 1 starter install in `local/sc` updated itself from GitHub to build 1001 and
   started (ready signal).

Decisions Code made (Dan can overrule): Return to the vortex appears once the Ice Age's vortex is open; coming back by the same world's
path puts you where you left (another world's path doesn't); the Theater lists the 23 story scenes (not PortalOpens, the test area's);
it rebuilds the world after a scene (enemies reset). The deckhand's trip got longer (1.6 -> 2.9 s) so the fall and get-up both play.

**For Dan to check:** the Pirate Seas from your save (the HUD now says "Next: 125 (this world: n / 15)"; you should wake the necklace
at once if you have 15 from the ship); Pause > Return to the vortex and back; the deckhands chasing and tripping; walking the deck and
climbing the rigging (sails fading); `Recordings/PirateArrival.mp4` and the two sheets; Pause > Theater. Known: transparent sails can
sort oddly against each other for a frame; the look-at is procedural (heads turn, eyes don't move separately).


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



---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Code's notes for Round 11 (the planning chat decides):** Dan's notes on 0.10.0 and 0.10.1 first; Muscle Man's three Pirate clips (Drowning,
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
