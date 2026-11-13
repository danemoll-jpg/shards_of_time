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
Current round — Round 12: Tomorrowland (target 0.12.0), then Round 13: the Moon (target 0.13.0) in the same turn
Round 11 and Fixes 11.1 / 11.2 are delivered (below). Do Round 12, publish 0.12.0, then go straight on to Round 13 and
publish 0.13.0. Report per item: done / not done / deferred, and why. DEFAULT = planning chat's choice, Dan can overrule
after playing. Testing rules as always (`Tools/quiet.py`, muted with the level reported, never a window in front of
Dan); rule 12 (never stuck), the Fix 8.1 floor / platform / reach checks, the Fix 10.1 minimum-player check and the
T-pose sweep apply to both worlds. Don't end your turn until 0.13.0 is published unless truly blocked (say so).

**Dan's decisions (2026-11-12, `Docs/Design/design-decisions.md`):** Tomorrowland and the Moon are each a full level
("pad this out"). **Tomorrowland → the Moon is the rocket, as in the film:** Tomorrowland's game-only story shard is
**neon pink**; finding it powers the rocket; the launch plays and the rocket flies **straight to the Moon** (no vortex
in between). Both worlds still get vortex paths for travelling back (the Moon's opens on landing). Music for both is
space-age pop with vocal "booms" and "wahs" (Dan's Suno songs, below).

**Inputs, all in (Dan, 2026-11-12):** 16 concept sheets in `SourceArt/Concepts/` and **16 textured Tripo GLBs in
`SourceArt/Tripo/`** (made by hand, ~1.9M triangles each: decimate as in Fix 11.1). Tomorrowland: `dan_tomorrowland`,
`abi_tomorrowland`, `muscleman_tomorrowland`, `kira_robot`, `violinist_tomorrowland`, `robot_greeter`, `cleanbot`,
`flying_car`, `rocket`. The Moon: `dan_moon`, `abi_moon`, `muscleman_moon`, `kira_moon`, `violinist_moon`,
`lunar_rover`, `moon_critter`. Film art in `SourceArt/Reference/Worlds/07-Tomorrowland/`, `08-Moon/` and
`Characters/*/…-tomorrowland-film.png`, `…-space-film.png`, `Kira/kira-robot-film.png`, `kira-space-film.png`; Dan's
clip `Clips/07-Tomorrowland_RocketLaunch.mp4` (6 s) with notes in `Docs/CUTSCENE-FILM-NOTES.md`. Songs
`Music/tomorrowland.mp3` (3:00) and `Music/moon.mp3` (2:59). Notes on the models (`Docs/MODELS-NEEDED.md`):
**the flying car's clear canopy came out as a solid half-dome over one seat: cut it off and add a see-through glass
canopy on the chrome arch** (say so if it can't be cut cleanly); the Moon sheets have **no helmets on purpose**: add
each bubble helmet as its own see-through model (Dan's, Abi's, Muscle Man's round bubbles; Kira's silver-rimmed one;
the Violinist's on his brass ring); the greeter's propeller and the cleanbot's brush were drawn spinning (animate
them; replace them if they came out as solid discs). Muscle Man grins with teeth (fine).

### Round 12: Tomorrowland

0. **Carried from Fix 11.3 (Dan, 2026-11-12;** `Data/screenshots/dan/egypt-abi-legs-missing-boulder.png`):
   a) something dark and spiky between Egyptian Abi's legs (and a pale woven shape at her feet): find it, fix it, then
   check every skirt / dress / long coat from low front and side angles (Abi's new flared silver dress and Dan's
   steampunk coat too); b) "Muscle Man rolls the boulder away" shown where there's no boulder (Egypt, a walled sand
   room): find why, fix it, and add the check that every Muscle Man job prompt appears only with its object visible in
   front of the player and disappears for good once done (also after a reload), in every world.
1. **Mixamo first, for both worlds at once (Dan wants it early and only once):** decimate / clean up all 16, then the
   Mixamo upload files for the eight people: `dan_tomorrowland`, `abi_tomorrowland`, `muscleman_tomorrowland`,
   `violinist_tomorrowland`, `dan_moon`, `abi_moon`, `muscleman_moon`, `violinist_moon`; **stop at once and tell Dan
   exactly what to upload and download** (shared clips reused; ask only for what's missing, e.g. a floaty low-gravity
   jump if nothing shared fits, Muscle Man lifting a car). Then carry on with everything else; import the rigs when
   they arrive. Code's rigs: robot Kira (wheels on her feet; no ear bones; Kira lock), space Kira (Round 13), the
   greeter robot (one wheel, waving arms, spinning propeller), the cleanbot.
2. **The other models and props:** the rocket (one model, reused on the Moon), the flying car (several colours by
   tinting), the greeter, the cleanbot. By script: the city of tomorrow (the pink neon avenue, chrome towers with
   glowing ring tops, bubble domes, brick shopfronts with teal glass, neon signs and arrows, lamp posts, moving
   walkways, hover-car lanes and landing pads, the "Rocket Terminal" with its golden domed launch hall), the neon pink
   story shard, and the Violinist's instrument (DEFAULT a sleek chrome electric violin with teal light strips, its own
   playing clip).
3. **Layout (DEFAULT):** **the neon avenue** (street level, shops, the square), **the sky** (tower tops and landing
   pads, reached by flying cars on fixed routes as moving platforms: Fix 8.1 moving-platform check; lifts and moving
   walkways), and **the Rocket Terminal** (the launch hall and its gantries). About 60 shards, two hearts.
4. **`TomorrowlandArrival`** (new, the film has none): the teal vortex from Steampunk drops all four onto the neon
   avenue at dusk; a shimmer and their outfits change to the retro-future ones, Kira's body turns to the robot;
   Muscle Man gapes at a flying car going over, delighted; the **greeter robot** rolls up: "Your rocket is ready for
   departure!" and points to the Rocket Terminal sign; Dan's necklace glows pink.
5. **The rocket needs its pink shard (the world's goal):** in the launch hall the rocket's fuel socket is empty
   (the greeter: "Fuel cell missing!"). Reaching the threshold wakes the necklace, which leads to the pink shard
   (DEFAULT on top of the tallest ring tower).
6. **Robot Kira (DEFAULT):** a) **plug-in**: she plugs into terminals and takes control of machines: reverses moving
   walkways, calls a flying car, opens doors, switches a cleanbot to "friendly" for a while; b) **wheel dash**: she
   rolls fast on smooth floors and up boost ramps. Both open shards.
7. **New move: Abi's air dash**, taught by the Violinist (teal jacket, the chrome violin) in a corner of the square.
   One quick dash in the air (any direction on the stick); opens the earlier worlds' air-dash gates (check one).
8. **Muscle Man:** delighted by everything (comic: he tries to hug the greeter robot; it beeps in alarm). Jobs: lifts a
   parked flying car off a hatch, pushes a vending machine, bends chrome bars, boosts Dan.
9. **Enemies:** goofy **cleanbots** that chase and vacuum up loose shards (bop one and it coughs them back out);
   hazards: hover-car lanes (a bump, not a kill), malfunctioning walkways. No boss (DEFAULT).
10. **Shards (Fix 10.1 rule):** about 60 + 2 hearts; threshold **175** (Steampunk 150 + 25), at least 15 from here;
   minimum-player simulation and `minplayer` extended. Report the numbers.
11. **The finale: `RocketLaunch`** (from Dan's clip): Dan places the pink shard in the socket; the launch hall, the four
   small in front of the huge silver rocket; they board; the rocket alone, the doors glow warmer, the engines ignite;
   the shot from below as it rises into a starry dusk sky. It flies **straight to the Moon** (Round 13 picks up there).
   Tomorrowland's path appears in the vortex (nine paths). Until Round 13 is in, the launch ends in the vortex with the
   Moon "to be continued".
12. **Look:** pink-lavender dusk, neon glow, chrome, teal glass; frame rate on both settings (neon, glass, many
   moving cars are the risk).
13. **Sound:** ElevenLabs: hover-car whooshes, robot beeps and the greeter's line ("Your rocket is ready for
   departure!"), "Fuel cell missing!", the cleanbot's vacuum and cough, moving walkways, neon hum, Kira's plug-in and
   wheel whirr, the air dash, the launch (countdown beeps, rumble, roar), the chrome violin tune. No music made by Code.
14. Build, check (a full run … Steampunk → vortex → Tomorrowland → arrival → the pink shard → the launch; plus an
   air-dash gate in an earlier world; the new prompt check (0b) in every world; rule 12 here), publish 0.12.0.

### Round 13: the Moon (straight on, same turn)

1. **Space Kira** (Code's rig; Kira lock: gray fur face, folded ears, amber eyes, no white) and the moon critter
   (Code's rig: bounce, wobble eye stalks), the lunar rover (static; its control-panel lights animated), the bubble
   helmets (see-through, on every Moon character in and out of cutscenes), Dan's / Abi's / Muscle Man's / the
   Violinist's Moon rigs from item 1. By script: the Moon (gray dust, craters, rocks, ridges, Earth huge in a deep blue
   sky, stars), the landed rocket (the Tomorrowland model), a small moon base (domes, tubes, an airlock), the white
   story shard, the Violinist's instrument here (DEFAULT the chrome violin again).
2. **`MoonLanding`** (new, continues `RocketLaunch`): the rocket descends on its flame and touches down in a puff of
   dust; inside, helmets click on; the hatch opens; the four bounce down the ladder in slow motion; Earth rises huge
   behind them; Muscle Man does a big slow jump and laughs. The Moon's path appears in the vortex.
3. **Low gravity for everyone** (higher, slower, floatier jumps; a soft landing; dust puffs). Re-tune shard reach and
   the stuck check for it.
4. **`KiraFloats`** (the film's sweet gag, short, skippable, on the way to the rover): Kira takes a little leap, keeps
   floating up paddling her paws; Abi catches her by the ankle and pulls her back down. Space Kira's ability (DEFAULT):
   **float**: hold Jump in the air to drift upward slowly (limited, a little steering): reaches high ledges.
5. **Layout (DEFAULT):** the landing site, the crater field (bouncy crater rims, a deep crater), the moon base
   (inside the domes: airlock, corridors, a garden dome), the ridge with the rover. About 60 shards, two hearts.
6. **The Violinist** (cream suit, on a rock by the base): plays a slow floaty tune (DEFAULT no new move: every move is
   learned; his tune quietly hints at the rhythm of the rover's light puzzle).
7. **Muscle Man:** loves the low gravity (huge jumps, comic); jobs: lifts moon boulders, carries the airlock door,
   boosts.
8. **Enemies:** goofy **moon critters** that bounce at you (a bump, harmless after a bop: they roll away like little
   moons). No boss (DEFAULT).
9. **Shards (Fix 10.1 rule):** about 60 + 2 hearts; threshold **200** (175 + 25), at least 15 from here.
10. **The finale (film): the light puzzle.** The necklace leads to the white shard on the rover; it won't come loose;
   its light flashes a pattern matching the rover's panel. **Only Abi can solve it** (as Dan / Muscle Man: they puzzle
   over it: "Switch to Abi"): a Simon-says memory puzzle on the panel (DEFAULT three rounds, 4 → 6 → 8 lights, a wrong
   press just replays the pattern). Then `WhiteShard` (new): the shard responds, all the shards in Dan's necklace light
   up together, brilliant light fills the Moon: the ornament is ready to be restored → the vortex with **the Home path
   open**. If Acts X / XI aren't in yet, Home shows "to be continued" (DEFAULT: Acts X / XI and the credits are the
   next round, once Dan's film art for them is in).
11. **Look:** stark white sun, black shadows, deep blue sky with stars, Earth glowing; frame rate on both settings.
12. **Sound:** ElevenLabs: muffled footsteps in dust, helmet clicks and breathing, radio beeps, the landing, the
   airlock hiss, critter boings, the rover panel's tones (one per light), the shard's chime, the WhiteShard swell, the
   violin tune. No music made by Code.
13. Build, check (a full run … Tomorrowland → launch → landing → the Moon → the rover puzzle → WhiteShard → vortex;
   rule 12 with low gravity; T-pose sweep incl. the eight new people), publish 0.13.0, update `TODO.md` / `CLAUDE.md`,
   clean up.

Later: **a big clean-up round after the Moon** (Dan, 2026-11-12), especially the environments (all eight worlds: plain
walls and floors like the Egypt room in Dan's screenshot, rough edges, empty corners). Note anything found on the way
there rather than fixing it piecemeal, unless it blocks play.

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
