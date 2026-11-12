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
Current round — Round 11: Victorian Steampunk (target 0.11.0)
Fix 10.1 is delivered (below). Report per item: done / not done / deferred, and why. DEFAULT = planning chat's choice,
Dan can overrule after playing. Testing rules as always (`Tools/quiet.py`, muted with the level reported, never a
window in front of Dan); rule 12 (never stuck), the Fix 8.1 floor / platform / reach checks, the Fix 10.1 minimum-player
check and T-pose sweep apply. Don't end your turn until 0.11.0 is published unless truly blocked (say so).

**Dan's decisions (2026-11-12):** Steampunk and Tomorrowland are **two separate worlds, each ending in a vortex** (the
film's "reality changes" is dropped; `Docs/Design/design-decisions.md`). The cutscenes stay as they are for now (no
quality pass, no film clips in game); focus on the game.

**Inputs, all in (Dan, 2026-11-12):** the 10 concept sheets S1–S10 in `SourceArt/Concepts/` (`dan_steampunk`,
`abi_steampunk`, `muscleman_steampunk`, `kira_mechanical`, `violinist_steampunk`, `factory_worker`, `clockwork_horse`,
`steam_carriage`, `street_clock`, `steam_press`), film art in `SourceArt/Reference/Worlds/06-Steampunk/` and
`Characters/*/…steampunk…`, `Kira/kira-mechanical-film.png`, two clips `Clips/06-Steampunk_Arrival.mp4` (25 s) and
`Clips/06-Steampunk_MuscleManHero.mp4` (12 s) with notes in `Docs/CUTSCENE-FILM-NOTES.md`, and the song
`Music/steampunk.mp3` (2:59). Notes on the sheets: Abi has a thin necklace and fishnet stockings (fine, keep them; it's
not Dan's shard necklace); Muscle Man grins with teeth (fine, as before); the street clock has an empty gear socket in
its face for the teal shard; mechanical Kira passes the lock (folded ears, gunmetal, amber eyes, no white).

**Round 11 progress (resume note, Code, 2026-11-12):** done and checked in the exe: item 0 (Theater: `theater` passed), the world
(factory + streets), belt run + press, wind-up / magnet paws / wall jump, jobs, horse gag, carriages, clock finale, vortex 8 paths
(`steampunk` all passed, `stucksteam` all passed), the Medieval chimney ledge (J01 was on the ground). Mixamo: 3 upload files ready,
Meshy out of credits for the other 7. Still to do: `fix81steam`, `perfsteam` (+ Steam Deck), rebuild with the cutscene fixes (press scene
hides the world's press; Dan/Abi lie flat), render + side-by-side sheets, `minplayer`, `tpose`, publish 0.11.0, TODO/CLAUDE report.

0. **The Theater bug first (Dan):** the Theater plays one scene, then refuses to play another until the game is
   restarted. Fix it (state left over after the first scene?) and add to the `theater` check: play three scenes in a row
   from the title and two from the pause menu, each one actually playing.
1. **Mixamo first (Dan wants it early):** Meshy → clean-up (closed meshes) → Mixamo upload files for `dan_steampunk`,
   `abi_steampunk`, `muscleman_steampunk`, `violinist_steampunk`, `factory_worker`, then **stop at once and tell Dan
   exactly what to upload and download** (shared clips reused; Muscle Man needs holding something heavy overhead and a
   big throw for the press, and lifting someone up for the clock; ask only for what isn't already in his folders).
   Then carry on with everything else in the same turn; import Dan's rigs when they arrive. (Still open from Round 10:
   Muscle Man's Drowning / Getting Up / Jumping Down: remind Dan in the same list.)
2. **The other models:** mechanical Kira (Code's rig, no ear bones, Kira lock check), the clockwork horse (Code's
   rig: walk, idle, head toss, steam puff), the steam carriage, the street clock, the steam press (top plate moves; can
   break apart for the scene). Props by script: the factory hall (conveyors with blue rollers, the bolt bucket on its
   chain line, gear walls, chains, pipes, steam vents, small stampers, control panels, catwalks, the wall the press
   breaks through), the city (cobbles with rail tracks, red brick and cream buildings, gear doors and the great gear
   gate, teal lamp posts, chimneys, rooftops, barrels), the teal gear story shard, the steampunk violin (gears set in,
   its own playing clip).
3. **Two areas (DEFAULT):** **the factory at night** (blue-violet haze, warm brass lamps, red alarm light while the
   press runs) and **the streets of steampunk London by day** (warm golden morning, red brick, teal lamps, steam),
   about 30 shards each, the same song (`steampunk.mp3`). One world for the vortex and the story shard. The hole the
   press smashes is the way from the factory to the streets (and back).
4. **`SteampunkArrival`** (from Dan's clip): night over the factory city, a giant brass gear puffing steam, a teal
   streak winding into a flat teal spiral vortex; push in over the domes; inside, the four drop onto the chain line and
   land in the copper bolt bucket, which is carried along and tips them onto a blue-roller conveyor. Side-by-side sheet
   against the clip as in Round 9.
5. **The belt run, then `MuscleManHero`** (from Dan's second clip): a short playable stretch on the moving conveyor
   (30–40 s: run against the belt, hop small stampers and steam jets; it can't be escaped, it carries you to the great
   press; no damage that sends you back, falling off just puts you back on). Then the scene: Dan, Abi and Kira flat on
   the belt, covering their heads, the dome press coming down; Muscle Man stands under it and holds it up, red alarms,
   snapping cables, sparks; he heaves it off and it smashes out through the wall and window. This is his hero moment
   (he saves Dan this time). Then the factory is free to explore (the press gone, steam everywhere), and the streets lie
   through the hole.
6. **Mechanical Kira (DEFAULT):** a) **wind-up**: she stands on gear sockets (brass plates) and powers machines: lifts,
   moving catwalks, gear doors, a carriage turntable; b) **magnet paws**: she walks up iron beams and pillars marked
   with rivet strips. Both open shards in both areas.
7. **New move: Abi's wall jump**, taught by the Violinist as a street musician with the steampunk violin (on a corner
   of the main square). Jump into a wall, jump again off it; chimneys and narrow alleys here. Opens the earlier worlds'
   wall-jump gates (the Medieval village chimney and any others): check one.
8. **Muscle Man the companion:** jobs here: bends bars and pipes aside, lifts heavy machinery and crates, holds a
   stamper up while the others pass, boosts Dan. Comic beat in the streets (DEFAULT): he tries to pet a clockwork horse
   and it puffs steam in his face.
9. **Enemies:** goofy factory workers (chase on their own floor; slip on oil patches or get a face of steam: harmless
   for a moment). Hazards: stampers, steam jets, moving carriages on the rails (a bump, not a kill; their roofs are
   moving platforms: the Fix 8.1 moving-platform check applies). Clockwork horses are friendly. No boss (DEFAULT).
10. **Shards (Fix 10.1 rule):** about 60 (30 + 30) + 2 hearts; threshold **150** (Pirate Seas 125 + 25) and at least
   15 from here; the minimum-player simulation and the `minplayer` check extended to this world. Report the numbers.
11. **The finale (film):** the necklace wakes and leads to the great street clock in the main square; the teal gear
   shard sits in its face, too high to reach; `ClockShard` (new, skippable): Kira spots it, Muscle Man proudly lifts Dan
   up, Dan takes it; **then the teal vortex opens (the game's change)** and takes all four → the vortex space
   (Home: 6 of 8; the Tomorrowland path "to be continued").
12. **Look:** as in 3. Frame rate checked on both settings (steam particles and the factory's many moving parts are
   the risk).
13. **Sound:** ElevenLabs: factory clanks, the chain line, the bolt bucket's rattle and tip, conveyor rollers, the
   press's thump and groan, alarms, cables snapping, the wall smash, steam hiss, gears ticking, the clock's chime,
   clockwork horse hooves and puffs, carriage rolling, workers' grumbles, Kira's metal steps and wind-up whirr, the
   violin tune; Muscle Man's grunts and laughs reused. No music made by Code.
14. Build, check (a full run: … Pirate Seas → vortex → Steampunk → arrival → belt run → the press → the factory → the
   streets → the necklace → the clock → vortex; plus a wall-jump gate in an earlier world; rule 12 stuck check here;
   T-pose sweep incl. the new people), publish 0.11.0, update `TODO.md` / `CLAUDE.md`, clean up.


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
