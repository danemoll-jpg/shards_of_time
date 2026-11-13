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
Current round — none. Fix 13.2 and Round 14 are delivered (below); their plan is in `TODO-ARCHIVE.md`.
---------------------------------------------------------------------------
Next (the planning chat writes it): **the big clean-up round after the Moon** (Dan, 2026-11-12), especially the environments (all
eight worlds: plain walls and floors like the Egypt room in Dan's screenshot, rough edges, empty corners). Notes Code made on the way
(not fixed piecemeal): the Medieval world looks like night in TheKing (its own look; the film's courtyard is by day); the credits' stills
are cropped check screenshots (Dan may want proper key art); the living room's couch is a box couch; Dan and Abi "asleep" sit with their
eyes open (no Mixamo sleeping-sitting clip in the set: `Sitting Idle` only).

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
### Round 14: the ending (Acts X and XI) and the credits (0.14.0, build 1400, 2026-11-13)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run, build and render; no window ever visible or in
front): `local/round14/{ending,tpose,minplayer}`; shots `Data/screenshots/round14/` (70x_*.jpg, HomeAgain_frames.jpg); MP4s in `Recordings/`.
1. `Farewell` (20.5 s, the vortex): **done.** The eight portals open round the platform in the story shards' colours; Muscle Man spots the purple
   one and points, hugs Dan (his air hug, "My hero!"), walks to it, waves, steps through; Dan, Abi and Kira wave him off. Everyone in their Act I
   clothes (no Moon helmets: the first shots had them, fixed). Dan's vortex song.
2. `TheKing` (20 s, the Medieval courtyard; the world is built for it, its enemies and the rival hidden): **done.** He shoots out of a purple
   portal, lands and topples flat on his back in front of the stone, gets up, sees the sword (new: a sword in the stone), pulls it out with no
   effort (the sword parented to his right hand), turns to the crowd (knights, archers, the wagon driver, the bard) who roar "King! King!
   King!" (ElevenLabs chant). Dan's medieval song.
3. `HomeAgain`: **done.** Act I played backwards (the director driven by hand from 49 s, the end of the warp tunnel, back to 9.8 s, just before
   Kira's leap) with Dan's act1.mp3 reversed (`Music/act1_reversed.mp3`, ffmpeg); then the living room: Dan and Abi on the couch with Kira,
   awake with a start, the ornament whole on the tree, a laugh; a violin: at the window the Violinist (his Ice Age look, the real one with his
   fiddle by IK) plays in the snow outside; they smile; they turn just as Kira leaps at the ornament: "No, Kira, no!" (DEFAULT: an ElevenLabs
   voice, not Dan's own), cut to black mid-leap. Same outfits and set as Act I (the window's backdrop replaced by a snowy garden).
4. The credits: **done.** 62 s, skippable (hold Skip): a scroll over stills from all eight worlds, Dan's title song (or `credits.mp3` if he
   makes one: Suno prompt in `Docs/MUSIC.md`). Text in `Assets/Resources/Data/Credits.txt` (Dan can edit it): the title, "Based on the film The
   Shards of Time by Dan Moll", design / story / music by Dan Moll, Meshy / Tripo / Mixamo / ElevenLabs, built with Claude Code, made with Unity,
   the cast, the launcher's licences, "And Kira... no!".
5. Free roam: **done.** Back in the vortex of the world the party left, every path open, Muscle Man in the party again (into the Medieval
   Kingdom with him), `SaveGame.endingSeen` (survives a reload). The Theater's new group "The ending": Farewell, TheKing, HomeAgain, the credits.
   The Home path plays the whole ending again any time.
6. Build / checks / publish: **done.** `ending` (28 passed), `tpose` (221 characters, the three new scenes), `minplayer` (New game through all
   eight worlds, the white shard, the Home path, the ending, the credits, free roam: passed), `moon`; MP4s rendered from the exe
   (`Recordings/Farewell.mp4`, `TheKing.mp4`, `HomeAgain.mp4` 63 s, `Credits.mp4`). Published `game-1400`.

Decisions Code made (Dan can overrule): the staging of all three scenes (no film clip); TheKing's crowd is the kingdom's own people; the sword
is a simple new prop; Muscle Man turns to the crowd after pulling it; the HomeAgain Violinist stands in the window's left pane; each ending
scene starts under black; Act I backwards plays at normal speed (39 s); the credits' stills are cropped from earlier check shots.
**For Dan to check:** the four MP4s in `Recordings/` (above all HomeAgain: the backwards part and the music); the Home path in the game (a save
with all eight story shards); `Credits.txt` (names, wording); whether you'd like your own "No, Kira, no!" or the silent cut.

### Fix 13.2: Dan's play-test of Tomorrowland and the Moon (0.13.2, build 1302, 2026-11-13)
Checks (built exe, batch mode, muted, never-shown desktop, virtual pad; audio 0.000 RMS in every run and build; no window ever visible or in
front): `local/fix132/{fix132,jobs3,moon,tpose}`; shots `Data/screenshots/fix132/`.
1. The yellow rectangle: **done.** It was the parked car's hatch cover (`Hatch`, a neon-yellow plate over the pit), left floating over the open
   pit when Muscle Man lifted the car (you fell through it). `Boulder.goneWhenMoved / shownWhenMoved`: the cover goes with the job, the opened
   pit shows a neon rim. The car pads (solid yellow plates, read as leftovers when their car was away) are dark pads with a neon edge. Every
   job in every world checked: nothing left inside the object's old footprint (new check in `fix132`, before / after shots `510_* / 511_*`).
2. Signs through walls: **done.** World text used the font's own material (GUI/Text Shader: drawn over everything, both sides). New
   `Shaders/WorldText` (depth tested, nothing drawn from behind) for every TextMesh: shop signs, NO ROOM, the vortex labels, the playground's
   signs, the ornament's initials in cutscenes (`UiKit.WorldText`). Pixel test in `fix132`: a sign from the front shows (166-5016 px), from
   behind 0 px, from its front with a wall between 0 px (Tomorrowland, Bethlehem, the vortex).
3. Muscle Man "swimming": **done.** A job teleported him beside the object, which left his motor "in the air" while nothing stepped it: he
   played his fall loop, arms flailing like a swimmer (the job clip was a one-shot cancelled at once; the bend job's loop was skipped). Now
   each job holds its own clip the whole time (`CreatureAnimator.Hold`): boulders rolled = Pushing, lifted (crates, cannon, anchor, car, moon
   boulders, the airlock door) = Lifting Object, portcullis = the Overhead Squat's drive up, bars / pipes = Pull Heavy Object, boost = Throw;
   a landing can't cut it. Found on the way: in tight places (the ship's hold, a factory lane) he stood between the player and the object, so
   the close camera hid him as a too-near follower: he now stands beside the object in view (`MuscleJob.Beside`) and is never hidden while
   working. Check: every job in all eight worlds plays its clip and he is in view (fix132: 277 passed).
4. The light puzzle: **done.** The rover's panel is an upright board with the pad's four face buttons in its diamond (Y top, X left, B right,
   A bottom), coloured like the pad, each with its letter big on it; PlayStation pads: the cross / circle / square / triangle shapes; keyboard:
   I J K L in the same diamond (new "Panel" action map). The pattern flashes one button at a time (0.55 s on, 0.35 s off, 1 s lead-in), a row
   of dots shows the steps, "Round n of 3: watch... / your turn"; Abi answers on the same buttons (each lights, its tone, her Button Pushing);
   three rounds (4, 6, 8); a wrong press replays the pattern; push the stick to step back. Check `moon`: round 1 on the keyboard, rounds 2-3
   on the pad, the labels switch with the device. The first board showed the diamond mirrored and Abi's helmet hid the A: fixed (bigger,
   higher), shots `652f / 652k / 652p`.
5. Nobody runs in place: **done.** The animator followed the motor's last velocity, which a frozen or held party keeps: everyone ran on the
   spot during the puzzle. Now the clips follow how fast the body really moves, and a motor nobody steps stands. Check: in every world the
   party frozen with speed left in their motors, and input held off with the stick pushed: all idle; and every exe check now counts any
   character on a walk / run clip for more than 0.5 s without moving: 0 in every run.
6. Build / checks / publish: **done.** `fix132`, `moon`, `tpose` (221 characters, the new ending scenes included) passed. Published
   `game-1302` (the ending's code was in but switched off: `Game.EndingReal`).

**For Dan to check:** the light puzzle on your pad (and on a PlayStation pad if you have one); Muscle Man's jobs in the hold and the factory;
the shop signs from the roofs; the car on the hatch.

### Fix 13.1: Abi's Button Pushing and Muscle Man's air hug (0.13.1, build 1301, 2026-11-13)
1. Abi at the rover panel: **done.** Dan's `Button Pushing.fbx` was in `SourceArt/Mixamo/` itself, not `abi_moon/` (Code only looked
   there in Round 13). Imported as `Clips/button` (frames 25-58: her hand at the panel, 1.1 s), played once on each press. Round 13's
   stand-in never played at all: `offer` was not a one-shot, so `Trigger("offer")` was skipped. Check `moon`: "Abi presses the light with
   Dan's Button Pushing clip (playing: button)", passed (`local/fix131/moon`, shot `652b_abi_presses`: from behind, her arm is hidden).
2. Muscle Man's air hug (the greeter gag): **done.** The same skipped `offer`: he cheered and stepped in but never hugged the air. `offer`
   is a one-shot now, cut to 1.8 s (`CreatureAnimator.Trigger(clip, maxSeconds)`). Check `tomorrowland`: "he hugs the air (playing: offer)".
3. Checks muted (0.000 RMS), no windows. Published `game-1301`.

For Dan to check: Abi at the rover's panel; Muscle Man's hug in the avenue.

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
