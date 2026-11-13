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
Current round — Fix 13.2: Dan's play-test of Tomorrowland and the Moon (target 0.13.2)
Rounds 12 / 13 and Fix 13.1 are delivered (below); their plan is in `TODO-ARCHIVE.md`. Report per item: done / not done /
deferred, and why. Testing rules as always (`Tools/quiet.py`, muted with the level reported, never a window in front of
Dan). Don't end your turn until 0.14.0 is published unless truly blocked (say so). Then Round 14 (below) in the same turn: the
ending and the credits. After that, the big clean-up round.

1. **A yellow rectangle stays behind when Muscle Man moves a parked flying car** (both cars in Tomorrowland). Whatever
   it is (the hatch marker, a shadow / decal, the job's highlight, a collider made visible), it must move with the car,
   go once the car is moved, or show the uncovered hatch properly. Then check every Muscle Man job in all worlds for the
   same leftover (boulders, crates, the anchor, the airlock door, the moon boulders, the press).
2. **Signs can be read from behind and through walls.** Sign text (and any other world text: labels, neon words, shop
   names, prompts placed in the world) must be hidden by walls like any other object and show only on its front face
   (blank or mirrored from behind). Check every world's signs, not only Tomorrowland's.
3. **When Muscle Man moves something it looks like he is swimming.** Find which clip his jobs play (lift, push, carry,
   roll) and why it reads as swimming (a swim / Treading Water clip mapped by mistake, or the wrong frames of a lift
   clip); give every job a proper clip from his folders and the shared ones (Lifting Object, Pushing, Carrying, Pull
   Heavy Object, the Overhead Squat hold). Check each job in every world on screen.
4. **The Moon's light puzzle is redone (Dan: "it sux").** Instead of the stick, the panel's lights **are the
   controller's face buttons**: four lights shaped and coloured like the pad's buttons (Xbox: A green, B red, X blue,
   Y yellow; PlayStation: cross, circle, square, triangle in their colours, matching the pad in use; on the keyboard the
   four keys). **Each light shows its letter / symbol big and clear** (A, B, X, Y; the PlayStation shapes; the key
   names): the colours only help, the letters are what Dan reads. The pattern flashes the buttons; Abi answers by pressing the same buttons; each press
   lights that button on the panel, plays its tone and Abi's press. Keep three rounds (4, 6, 8); a wrong press just
   replays the pattern. Easy to read: big lights, a clear pause between flashes, the current step shown.
5. **Nobody runs in place.** During the puzzle (and any time the party stands still: waits, talking, prompts, menus)
   the followers and the player stand in their idle, never play run / walk while not moving. Find the cause (followers'
   speed not reaching zero, an animator parameter not reset, the input lock keeping the run state) and fix it
   everywhere; add a check that no character plays a walk / run clip while its speed stays under a small threshold for
   more than half a second, in every world.
6. Build, check (new checks for 1, 2 and 5 in every world; the puzzle played through with the pad and the keyboard;
   `tpose`), publish 0.13.2, update `TODO.md` / `CLAUDE.md`.

### Round 14: the ending (Acts X and XI) and the credits (target 0.14.0; straight on after 0.13.2, same turn)

Dan (2026-11-13): no new art is needed: both places (the Medieval courtyard with the stone, the home living room) and
everyone's end-of-film outfits (Muscle Man's Medieval clothes from when we first meet him; Dan, Abi and Kira in their
Act I clothes and Kira's own body) are already in the game. The home path in the vortex opens after `WhiteShard`.

1. **Through the Home path: `Farewell` (Act X, first half; DEFAULT from the film synopsis, short):** the restored
   shards open portals to the eras around the vortex; Muscle Man spots the purple one to his medieval home, hugs Dan
   (or tries: his air hug is fine), still sure Dan is the greatest hero who ever lived, and steps through.
2. **`TheKing` (Act X, Dan's description):** the Medieval courtyard; Muscle Man, back in his first Medieval clothes,
   falls out of the purple portal flat on the ground right in front of the sword in the stone; he stands up, takes
   the sword and pulls it out easily; the crowd roars and chants "King! King! King!" (ElevenLabs crowd chant).
   Music: `medieval.mp3`.
3. **`HomeAgain` (Act XI, Dan's description):** literally **Act I played backwards, music included**: `ActOne`
   reversed (picture and `act1.mp3` reversed) from the warp tunnel back to just before Kira knocks the ornament off
   the tree, so the ornament is whole on the tree again. Cut to Dan and Abi waking up on the couch (Kira with them):
   it seems it was all a dream. They hear a violin, go to the window and see the Violinist outside playing (DEFAULT his
   look from his first concept sheet): it wasn't a dream. They turn around just in time to see Kira leaping at the
   ornament; "NO, KIRA, NO!" (canon line, Dan's voice via ElevenLabs; DEFAULT, drop it if Dan prefers the silent cut)
   and cut to black on her mid-leap. Same outfits and set as Act I.
4. **The credits** (after `HomeAgain`, skippable): a gentle scroll over stills from each world, music `title.mp3`
   (DEFAULT; Dan may make a credits song later). Text (Dan can edit): "Shards of Time", "Based on the film *The Shards of
   Time* by Dan Moll", then design / story by Dan Moll; built with Claude Code; music by Dan Moll (Suno); characters
   from Dan's concept art (Meshy, Tripo), animation (Mixamo), sounds (ElevenLabs); made with Unity.
5. **After the credits: free roam** (design decision): back in the vortex with every path open, **Muscle Man rejoins
   the party** for the secrets that need him; the save remembers the ending is seen. The Theater gets the four new
   scenes.
6. Build, check (a full run … the Moon → WhiteShard → vortex → Home → Farewell → TheKing → HomeAgain → credits →
   free roam with Muscle Man; `tpose` over the new scenes; render the new MP4s from the exe), publish 0.14.0, update
   `TODO.md` / `CLAUDE.md`.

Later: **a big clean-up round after the Moon** (Dan, 2026-11-12), especially the environments (all eight worlds: plain
walls and floors like the Egypt room in Dan's screenshot, rough edges, empty corners). Note anything found on the way
there rather than fixing it piecemeal, unless it blocks play.

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
### Fix 13.1: Abi's Button Pushing and Muscle Man's air hug (0.13.1, build 1301, 2026-11-13)
1. Abi at the rover panel: **done.** Dan's `Button Pushing.fbx` was in `SourceArt/Mixamo/` itself, not `abi_moon/` (Code only looked
   there in Round 13). Imported as `Clips/button` (frames 25-58: her hand at the panel, 1.1 s), played once on each press. Round 13's
   stand-in never played at all: `offer` was not a one-shot, so `Trigger("offer")` was skipped. Check `moon`: "Abi presses the light with
   Dan's Button Pushing clip (playing: button)", passed (`local/fix131/moon`, shot `652b_abi_presses`: from behind, her arm is hidden).
2. Muscle Man's air hug (the greeter gag): **done.** The same skipped `offer`: he cheered and stepped in but never hugged the air. `offer`
   is a one-shot now, cut to 1.8 s (`CreatureAnimator.Trigger(clip, maxSeconds)`). Check `tomorrowland`: "he hugs the air (playing: offer)".
3. Checks muted (0.000 RMS), no windows. Published `game-1301`.

For Dan to check: Abi at the rover's panel; Muscle Man's hug in the avenue.

### Round 13: the Moon (0.13.0, build 1300, 2026-11-13)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run, build and render; no window ever visible or in
front): `local/round13/{moon,tomorrowland,stuck,fix81moon,perfmoon,perfdeck,prompts,tpose,minplayer}`; logs and sheets in `Data/screenshots/round13/`.
1. Models: **done.** Space Kira (Code's rig, 23 bones, no ear bones; Kira lock OK: grey 43%, white 0.4%, amber 0.05%); the moon critter (static,
   wobbling eye stalks), the lunar rover (static, its panel's lights blink), the bubble helmets (`Characters/Helmet.cs`: see-through, on the head
   bone of every Moon character in play and in every Moon cutscene: round for Dan / Abi / Muscle Man, the Violinist's on a brass ring, Kira's small and
   silver-rimmed); Dan's four Moon rigs. The Moon built in code (`World/Moon.cs`: a 600 m cratered heightfield, Earth, stars), the landed rocket (the
   Tomorrowland model), the moon base, the white story shard; the Violinist's chrome violin again.
2. `MoonLanding` (17 s): **done.** RocketLaunch flies straight on (no vortex): the descent on its flame, the dust, helmets click on in the cabin, the
   hatch, the four bound down in slow motion, Earth behind them, Muscle Man's big slow jump and laugh. The Moon's path in the vortex opens on landing.
3. Low gravity: **done.** One `LowGravityVolume` (gravity x0.35, take-off x0.8): Dan's jump 2.9 m and 1.5 s (on Earth 1.6 m, 0.6 s); shard reach and the
   stuck check re-run with it.
4. `KiraFloats` + space Kira's float: **done.** The gag plays once at the ridge's foot; hold Jump in the air: up at 2 m/s for 2.8 s (`CharacterMotor`
   `Float`): she reaches the 7.6 m float pillars; Abi's double jump tops out at 3.8 m there.
5. Layout: **done.** The landing site, the crater field (glowing bouncy rims, the deep crater, cave A), the moon base (airlock, hub, garden, lab,
   corridors; solid domes), cave B, the mesa, the ridge and the rover's plateau. 60 shards, 2 hearts.
6. The Violinist: **done.** In his cream spacesuit on a rock by the base; a slow floaty tune (no move left), its hint at the light puzzle.
7. Muscle Man: **done.** Carries the jammed airlock door aside, lifts the moon boulders off both caves, boosts onto the mesa; big floaty jumps.
8. Enemies: **done.** Moon critters hop at you (a bump: knocked back, no heart lost); bopped, they curl up and roll away. No boss.
9. Shards: **done.** 60 + 2 hearts; threshold 200 and 15 from here; minimum player: arrives with 175, all 60 reachable before the wake.
10. The finale: **done.** The necklace leads to the rover; Dan / Muscle Man / Kira puzzle over the panel ("Switch to Abi"); Abi: three rounds (4, 6, 8
   lights) on the stick, a wrong press plays the pattern again (Fix 13.1: Dan's Button Pushing on each press); `WhiteShard`: the shard
   answers, all eight shards light round Dan's necklace, light fills the Moon -> the vortex with Home open ("to be continued": Acts X / XI next).
11. Look and frame rate: **done.** Stark white sun, deep blue starry sky, Earth; 216 fps at the slowest place at 1080p (need 60), Steam Deck below.
12. Sound: **done.** In Round 12's batch: dust steps, helmet clicks, breathing, radio, the landing, the airlock, critter boings and rolls, four panel
   tones and a soft wrong buzz, the shard's chime, the WhiteShard swell, the floaty violin tune, the Moon's hush. No music made by Code.
13. Build / checks / publish: **done.** `moon`, `tomorrowland` (the launch now goes on to the Moon), `stuck` (all eight worlds), `fix81moon`, `perfmoon` (+
   Steam Deck), `prompts` (eight worlds), `tpose` (201 characters), `minplayer` (New game to the white shard). Published `game-1300`.

Decisions Code made (Dan can overrule): the puzzle's lights are answered with the stick (up, right, down, left = left to right); the patterns are the
same every time; low gravity everywhere on the Moon (inside the base too); the critters never cost a heart.
**For Dan to check:** MoonLanding and WhiteShard (MP4s in `Recordings/`); the light puzzle as Abi; Kira floating up a pillar; the helmets (sizes are
guesses on the new rigs); the base through the airlock.

### Round 12: Tomorrowland (0.12.0, build 1200, 2026-11-12)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run, build and render; no window ever visible or in
front): `local/round12/{tomorrowland2,stuck2,prompts2,fix81tl,perftl,perfdeck,tpose,minplayer}`; logs and sheets in `Data/screenshots/round12/checks/`.
0. Carried from Fix 11.3: **done.** a) Egyptian Abi: the closed-mesh rebuild (Round 5) had fused her calves, thighs and the two ankle wings below the
   hem; the walk stretched that into the dark spiky sheet. `close_person.py` now pushes the legs apart before the voxel solid and back after, cuts
   faces joining the ankles, gives everything below the knees to one leg (`SPREAD`): 0 open edges, nothing between the legs in idle / walk / run /
   jump (`Tools/Blender/skirt_check.py`, low front / side / back; `round12/skirts/`). Every skirt, dress and long coat checked the same way: the
   Bethlehem Violinist's and the three Wise Men's robes swung out with their forearms: re-weighted (`Tools/Blender/fix_robe_weights.py`, cloth more
   than 12 cm from the arm bones off them). Dan's steampunk coat, every Abi, Abi's new silver dress: fine. b) The prompt measured from the 12 m block's
   centre (8.5 m) and never looked at the screen: now the player must be within 2.6 m of the object's own surface, it must be on screen with a clear
   line from the camera, not behind the player (a boost spot: its ledge in front); the prompt goes the moment that stops and never shows for a done
   job. The quarry's block was sandstone like the walls and only covered 12 of its 22 m doorway: now grey granite with rope bands, the doorway
   closed. New exe check `prompts`: every job in every world (in front: shows; turned away, 6 m off: none; done: none; after a real reload: none;
   Dan's quarry spot): 160 passed.
1. Mixamo first: **done.** All 16 Tripo GLBs decimated (people 25k), textures out, cleaned up; the eight upload files; Dan's eight rigs imported
   (both Abis closed, 0 open edges). Mixamo had no Hug and no Button Pushing: Muscle Man's hug = his cheer, a step in, `offer` (he hugs the air); Abi
   at the rover's panel plays `offer` (Round 13). Robot Kira and space Kira: Code's rigs, 23 bones, no ear bones; **Kira lock OK** (robot: grey 41%,
   white 0.2%, amber 0.06%; space: grey 43%, white 0.4%, amber 0.05%).
2. Models and props: **done.** The rocket, the flying car (tinted; Tripo's solid canopy cut off, a see-through glass bubble on a chrome rim and arch:
   it cut cleanly), the greeter and the cleanbot as static models with moving parts (`Tools/Blender/static_parts.py`: the greeter's propeller, arms
   and wheel, the cleanbot's top). The city built in code (`World/Tomorrowland.cs`, `ProcMesh.cs`: ring tops, domes); the neon pink shard; the chrome
   electric violin (`instruments.py` ChromeViolin, teal light strips).
3. Layout: **done.** The neon avenue (shops, roofs at 7, walkways, the escalator, the wall-jump gap), the sky (the Ring Tower's sky deck by the
   always-running car, the Orbit Tower by the car robot Kira calls, the gantries' lift), the square (fountain, garden dome, the Atomic Diner, the
   air-dash pads), the Skyline Tower, the Rocket Terminal and its golden launch hall. 60 shards, 2 hearts.
4. `TomorrowlandArrival` (16 s): **done.** The teal vortex, the tumble, the shimmer into the retro-future outfits (robot Kira), a flying car over
   Muscle Man, the greeter's line, the necklace pink. `Recordings/TomorrowlandArrival.mp4`, frames `round12/TomorrowlandArrival_frames.jpg`.
5. The fuel socket: **done.** "Fuel cell missing!" at the socket; the necklace wakes, the Skyline Tower's glass lift lights up, the pink shard on its
   roof; carried to the socket.
6. Robot Kira: **done.** Plug-in terminals: the escalator (runs down 9 m/s until she turns it: up 2.6), the diner's door, the Orbit car, the gantry's
   service lift, the cleanbots' friendly switch; the wheel dash (13.5 m/s) up the boost ramp onto the billboard (8.6 m).
7. Abi's air dash: **done.** The Violinist teaches it; Dive in the air, 13.5 m/s; the far pad 12.5 m off (X01, X02) and an earlier world's gap:
   Steampunk's north street, two lamp platforms 12.5 m apart (new shard S31; `Steampunk.asset` remade).
8. Muscle Man: **done.** The hug gag (above); lifts the parked car off the maintenance hatch (the pit's ramp out), pushes the vending machine, bends
   the chrome bars, boosts onto the west roofs.
9. Enemies: **done.** Cleanbots vacuum loose shards and cough them back out when bopped (then don't vacuum for 6 s); hover-car lane (a bump, no heart);
   the malfunctioning walkway. No boss.
10. Shards: **done.** 60 + 2 hearts; threshold 175 and 15 from here. Minimum player: arrives with 150, 57 of 60 reachable before the wake; `minplayer`
   from New game: asleep at 174, wakes at 175, the pink shard, the launch. Completionist fine.
11. `RocketLaunch` (15 s, Dan's clip): **done.** Dan places the shard; the four small before the rocket (the clip's framing); boarding; the hall
   stays warm and closed, the doors glow; ignition, the dome halves part, it rises into the sky. Sheet `round12/RocketLaunch.jpg`. With Round 13 in,
   it flies straight on to the Moon (0.12.0 alone: the vortex, the Moon to be continued; nine paths).
12. Look and frame rate: **done.** 120 fps at the slowest place at 1080p (need 60), 83 on the Steam Deck setting (need 40); a first-view hitch (shaders).
13. Sound: **done.** 47 ElevenLabs effects (both rounds' at once): cars, robot beeps and both greeter lines, vacuum and cough, walkways, neon city loop,
   plug-in, wheels, air dash, the launch (countdown, ignition, roar), the chrome violin; the Moon's too. No music made by Code.
14. Build / checks / publish: **done.** `tomorrowland` 93 passed (incl. the Steampunk air-dash gap), `stuck` (all seven worlds, 199), `fix81tl`,
   `perftl` (+ Steam Deck), `prompts` (160), `tpose` (180 characters), `minplayer`: all passed. Published `game-1200`.

Decisions Code made (Dan can overrule): the city built in code (no Blender world this time); robot Kira's terminals are glowing pads (the gear-socket
pattern); the cleanbots stop vacuuming for 6 s after a cough; the roof gaps covered so nobody drops behind Muscle Man's gates; the diner's and the moon
base's domes solid (no jumping in over the walls).
**For Dan to check:** the avenue at dusk and the escalator; robot Kira's terminals and the boost ramp; Abi's air dash to the far pad; the hug gag; the
two MP4s; Egyptian Abi from low angles.

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
