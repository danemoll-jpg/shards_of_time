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

**Delivered as 0.11.0 (build 1100), 2026-11-12: report under "Last deliveries".**

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
### Fix 11.2: Dan's Steampunk rigs and Muscle Man's clips (0.11.2, build 1102, 2026-11-12)
1. The five Mixamo rigs: **done.** `dan_steampunk`, `abi_steampunk` (closed by `close_person.py`: 0 open edges), `muscleman_steampunk`,
   `violinist_steampunk`, `factory_worker` replace their stand-ins (player's view: `local/fix112/steampunk/440_street_musician.png` Dan, Abi,
   Muscle Man and the Violinist in their Steampunk looks; `413` a worker on the avenue).
2. Muscle Man's hold / throw: **done, one clip for both (Dan's idea).** Mixamo had no Heavy Throw; Dan's `Overhead Squat` is trimmed into
   `mm_hold` (frames 1-15: the dip under the weight, held) and `mm_throw` (15-57: the drive back up that heaves the press off). Shots
   `412d_he_holds_it`, `412e_heave`.
3. Round 10's leftovers: `Getting Up` (mm_getup) and `Jumping Down` (mm_leap) **in**; **Drowning / a flailing swim: not on Mixamo**, his
   struggle stays Treading Water (not needed again).
4. Checks (exe, muted 0.000 RMS, no windows): `steampunk` passed, `tpose` passed (all worlds and cutscenes, Steampunk 8 characters).
   Cutscenes rebuilt (`CutsceneAuthoring.Version` 31). Published `game-1102`; `local/sc` updated itself to 1102 and reported ready.

For Dan to check: MuscleManHero (the dip under the press, the heave); the Violinist holding his steam violin on the new rig.

### Fix 11.1: Dan's seven Tripo models in the game (0.11.1, build 1101, 2026-11-12)
1. Import: **done.** All seven from `SourceArt/Tripo/` (~1.9M triangles each) decimated (`Tools/Blender/decimate_glb.py`: people 25k / 15k,
   animals 15k, clock 12k, carriage 15k, press 20k), textures pulled out of the GLBs (`Tools/glb_textures.py`), cleaned up (`meshy_cleanup.py`;
   Kira and the horse turned 90 deg: Tripo's side profiles face -X).
2. Mechanical Kira: **done.** Code's rig (23 bones, no ear bones, 9 clips), now her model in the game. **Kira lock OK:** ear bones 0, grey 35%,
   white 0.0%, amber eyes 0.31%. (Tripo's plates were a rusty brown-grey, 4% grey: moved to home Kira's grey with `match_fur.py`; the lock takes
   25% grey for her, as 20% for the mummy: brass gears and rivets are her design.) Clockwork horse: Code's rig, in the square.
3. Static models: **done.** Dan's clock is the great clock (the teal gear moved into its face at 5.85 m), his carriage stands on the turntable
   and parked in the yard and the streets, his press as two smaller presses in the factory (box colliders). The rideable carriages on the rail
   loop and the press that breaks apart in `MuscleManHero` stay the Blender ones (Tripo's are one solid mesh each, with a domed roof).
4. Mixamo: **done.** The Violinist's and the factory worker's upload files are in `Docs/MODELS-NEEDED.md` with Dan, Abi and Muscle Man (5).
5. Checks / publish: **done.** On the 0.11.1 exe: `steampunk`, `fix81steam` (the small presses and parked carriages are solid as drawn: their
   own meshes), `stucksteam`, `tpose` (156 characters, none in its rest pose) all passed, audio 0.000 RMS, no window. Published `game-1101`; the
   starter install in `local/sc` updated itself to build 1101 and started. Disk 13.3 -> 12.9 GB. (One build stopped half-way when Unity's licence
   client dropped its connection: built again.)
**For Dan to check:** mechanical Kira in the factory (wind a socket, climb a riveted pillar), the clockwork horses in the square, the great
clock's teal gear; the five Mixamo uploads in `Docs/MODELS-NEEDED.md`.

### Round 11: Victorian Steampunk (0.11.0, build 1100, 2026-11-12)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run, build and render; no window ever visible or
in front): `local/round11/{steampunkF,theater,stucksteam,fix81steam2,perfsteam,perfdeck,minplayer2,tposeF}`. Evidence: `Data/screenshots/round11/`.
0. Theater bug: **done.** The cause: after a scene from the title the world was rebuilt while the game still counted as "playing", so a necklace
   ready to wake started its story on the title screen and held Busy for ever (every later pick did nothing; Continue then played the wake twice).
   Now the title comes back before the rebuild, and a wake never waits on the title. `theater`: three scenes in a row from the title (on a save
   whose necklace is ready but asleep, the bug's state), Continue (the wake once), two from the pause menu: all played.
1. Mixamo first: **partly.** Meshy ran out of credits after three models: Dan, Abi, Muscle Man cleaned up, upload files ready, Dan told (plus Muscle
   Man's two new clips and the three still open from Round 10; `Docs/MODELS-NEEDED.md`). **Not done:** the rigs (waiting on Dan); the Violinist's
   and the worker's upload files (Meshy 402; Tripo tried: `Tools/tripo_generate.py` works but the API account's balance is 0). After the
   release Dan made the Violinist on the Tripo website: decimated to 25k (`Tools/Blender/decimate_glb.py`), his upload file is ready too.
2. The other models: **deferred (no credits).** Stand-ins: home Kira for mechanical Kira, the Medieval pony tinted copper for the clockwork horse,
   the deckhand model for the workers, the pirate fiddler for the Violinist; the carriage, the clock, the press are Blender props. **Done:** every
   prop by script (`Tools/Blender/steampunk_world.py`: the factory hall, belt, stampers, press frame / dome / wreck, bolt bucket, chain line, gear
   doors and the great gear gate, sockets, riveted pillars, houses, lamps, the clock with its gear socket, the teal gear shard, carriages, the
   turntable) and the steampunk violin (`instruments.py` SteamViolin).
3. Two areas: **done.** The factory at night (blue-violet through the skylights, brass lamps) and the streets by day (golden morning), 30 + 30
   shards, Dan's `steampunk.mp3`, the ambience switches factory / city. The hole the press makes joins them.
4. `SteampunkArrival` (23 s): **done.** The gear and the teal streak, the flat teal spiral, over the roof, the gear wall, the chain line, the
   bucket of bolts carried along, tipped onto the belt. Sheet `round11/SteampunkArrival.jpg`, `Recordings/SteampunkArrival.mp4`.
5. Belt run + `MuscleManHero` (14 s): **done.** Carried toward the press at 2.4 m/s between screens, stampers (a bonk pushes you on, never back)
   and steam jets (a hop), the gate opens at 30 s: the bot took 33 s, no heart lost. The scene: flat on the belt, the dome comes down, he holds it
   up (red alarms, cables, sparks), heaves it out through the wall. Sheet `round11/MuscleManHero.jpg`.
6. Mechanical Kira: **done.** Wind-up from brass sockets: the lift to the crane deck, the gantry's bridge, the boiler room's gear door, the great
   gear gate, the carriage turntable; magnet paws up riveted pillars (two in the factory, two in the streets; Dan can't).
7. Abi's wall jump: **done.** The Violinist (street musician, brass violin) teaches it; the bot wall-jumped up the narrow alley (11 jumps) and the
   factory's brick shaft. Earlier gate checked: the Medieval village chimney (its ledge had been lost in a comment since Fix 8.1: J01 lay on the
   ground; now on a ledge, reached by 4 wall jumps).
8. Muscle Man: **done.** Bends the cage's bars and the alley's pipes, lifts the machine and the crates, holds the big stamper up, boosts onto a
   roof; the horse gag (steam in his face) plays once.
9. Enemies: **done.** Goofy workers (chase on their floor, slip on oil, reel from steam); stampers, steam jets, carriages (a bump; their roofs
   ride: the moving-platform check passed). Horses friendly. No boss.
10. Shards: **done.** 60 + 2 hearts; threshold 150 and 15 from here. Minimum player: arrives with 125, 58 of 60 reachable before the wake (needs
   25); `minplayer` from New game: asleep at 149, wakes at 150. Completionist fine.
11. The finale: **done.** The necklace leads to the great clock; `ClockShard` (Kira spots it, Muscle Man lifts Dan, he takes it), the teal vortex
   -> the vortex (Home 6 of 8; Tomorrowland "to be continued"; eight paths 18 deg apart).
12. Look and frame rate: **done.** 184 fps at the slowest place at 1080p (need 60), 206 on the Steam Deck setting (need 40). Known: a first-view
   hitch of 250-360 ms (shaders warming up).
13. Sound: **done.** 26 ElevenLabs effects (factory and city ambience, chain, bucket, belt, stampers, press thump / groan, alarm, cables, wall
   smash, steam, gears, clock chime, horse hooves / puff, carriage, workers, Kira's metal steps / wind-up / magnet, bars, the violin tune, the
   teal vortex). No music made by Code.
14. Build / checks / publish: **done.** `steampunk`, `theater`, `stucksteam` (rule 12), `fix81steam`, `perfsteam`, `minplayer`, `tpose` (156
   characters, none in its rest pose) passed; 0.11.0 published as `game-1100`. The Round 1 starter install in `local/sc` updated itself from GitHub to build
   1100 and started (ready signal). Disk: 13.0 GB -> 12.5 GB.

Decisions Code made (Dan can overrule): stampers on the belt bonk you forward (the round's "nothing sends you back"); the belt's gate opens at
30 s; the clock's shard is taken by walking to the clock's foot once the necklace is awake; the necklace-wake scene and the finale are in the
square; a carriage turntable that spins once wound (a toy, a shard on it).
**For Dan to check:** the belt run and the press from the vortex path; the two sheets and MP4s; the streets by day; Abi's wall jump up the narrow
alley; the Theater several times in a row. **Waiting on Dan:** the three rigs + Muscle Man's clips (Mixamo), Meshy or Tripo API credits for the
other seven models.

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
