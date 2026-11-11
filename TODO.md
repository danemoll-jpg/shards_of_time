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
Current round — Fix 8.1: Medieval fixes + shard thresholds (target 0.8.1)
Round 8 is delivered (below). Dan played 0.8.0 (2026-11-11). Report per item:
done / not done / deferred, and why. Testing rules as always; rule 12 applies.
For each of items 1–4: find the cause, fix it at the source, and check it in
the built exe from the player's view (screenshot + numbers).

1. **"In the room where you first meet Muscle Man the ground is high, like you
   are wading through water."** The heroes sink into the floor there: the
   floor's collider sits above its visible mesh (or the visible floor is lower
   than the collider / a water or fog volume overlaps it). Feet must sit on the
   visible floor. Then audit **every** walkable surface in the Medieval world
   (and spot-check the other worlds) for feet sunk into or floating above the
   floor: a probe that compares the hero's foot height with the visible
   surface under it, reported per area.
2. **The drawbridge isn't solid: Dan fell right through it.** Give it a real
   collider that moves with it (lowered and raised), and check every moving or
   animated platform in all worlds the same way (walk across each one, every
   hero).
3. **A shard behind the village can't be reached: an invisible wall.** Find
   the invisible collider (probably the world-boundary wall or a leftover
   blocker) and move it outside the shard, or move the shard. Then add to the
   never-stuck / reachability check: **every shard in every world is reachable
   by the bot with the moves it should need** (none behind invisible walls),
   and report any that aren't.
4. **The needed shard count is far too low ("I only needed like 3")** when you
   have collected well in the earlier worlds. The universal total stays (extra
   shards still help, Dan 2026-11-10), but each world must still ask for real
   collecting there. New rule (DEFAULT, Dan can tune): a world's necklace wakes
   when **both** are true:
   - the universal total reaches the world's cumulative threshold, raised to
     about **75%** of all shards in the worlds so far (Ice Age 45, Egypt 90,
     Bethlehem 135, Medieval 180), and
   - **at least 20 shards from this world** have been collected.
   So a player who hunted everything earlier still needs 20 here, and one who
   skipped a lot needs more. HUD: "Next: 180 (this world: 12 / 20)". Rule 2
   (reachable on arrival with the moves held) must still pass with the new
   numbers in every world: report them. Existing saves: no world that's already
   awake relocks. Update `CLAUDE.md` rule 3 and `Docs/Design/design-decisions.md`.
5. Build, check, publish 0.8.1, update `TODO.md` / `CLAUDE.md`, clean up. Don't
   end your turn until 0.8.1 is published (unless truly blocked: say so).
---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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
   shard slides out; Muscle Man kneels: "Master!"; the purple vortex takes all four -> the vortex space (4 of 8; the Pirate Seas path
   opens, "to be continued"). Only Dan can pull it (anyone else: "It won't budge for Abi... switch to Dan").
11. Muscle Man the companion: **done.** Follows as a third follower (warps like the others), cheers at pickups, and does his jobs at
   X: rolls boulders aside in every world (checked: Bethlehem's cave, the Medieval yard), heaves the armory's portcullis up, boosts Dan
   onto the stands' flag platform. He goes through the vortex with the party and wears his Medieval clothes everywhere.
12. Look: **done.** A torch-lit night: deep blue sky, cool moonlight, warm torches, the stone's purple glow. Frame rate above.
13. Music and sound: **done.** Dan's `medieval.mp3` in the world (checked playing, muted). 23 ElevenLabs effects (fire puff, hay
   fire, the wagon blast, armour, shield block / break, arrow, rat, Muscle Man's grunt / laugh / "Master!", boulder, the shard sliding
   out, the punch's charge and hit, a wall breaking, wings, crowd, the bell, the pony, cobble steps, the portcullis, the lute tune).
14. Build / check / publish: **done.** 0.8.0 published as `game-800`. MP4s: `Recordings/MedievalArrival.mp4`, `TheStone.mp4`.
   The Round 1 starter install in `local/sc` updated itself from GitHub to 0.8.0 and started. Disk: 9.4 GB -> 8.8 GB.

Also fixed on the way (found by the checks): the moat had no way out once its steps were gone (now two 0.3 m steps on each side's
middle); the dungeon's checkpoint flag stood in the corridor (now no flag); a follower further back in line (Muscle Man) warped every 3 s (the "not caught up" distance now grows with the
place in line); the older checks walked the vortex at fixed angles (now read from the paths: six paths, 24 deg apart).

Decisions Code made (Dan can overrule): the lute is held across his body with the lyre's playing clip (as Bethlehem's harp); "Pull
Heavy Object" is the stone's heave ("Pulling A Rope" spare); the rival's three spots and bits as above; X near a job sets Muscle Man to
it; rats run off after a nip (the bot kept losing all three hearts to them); the dungeon is a separate space down a stair door.

**For Dan to check:** the arrival and the stone (MP4s in `Recordings/`, and in game); listen once to Muscle Man's "Master!"
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

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Code's notes for Round 9 (the planning chat decides):** Dan's notes on 0.8.0 first; "Master!" by ear (re-roll if unclear);
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
