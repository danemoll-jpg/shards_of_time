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
Current round — Round 5: Ice Age polish, a bigger valley, creature models (target 0.5.0)
Round 4's plan is in `TODO-ARCHIVE.md`. Dan played 0.4.0 (2026-11-09): "for a
start not too bad", clean-up needed, and the notes below. Report per item:
done / not done / deferred, and why. DEFAULT = planning chat's choice, Dan can
overrule after playing.

**Dan's notes on 0.4.0**
1. **Rewrite `IceAgeArrival` (Dan's story beat).** They land in the snow. Dan
   gets up and sees what looks like a **sabertooth about to attack him**: shoot
   Kira from a low angle in silhouette / shadow against the bright sky so she
   reads as big and dangerous, a growl, fangs. Dan scrambles backward and
   **falls flat on his back in fright**. Kira leaps onto his chest; close-up:
   he sees the folded ears and amber eyes and realizes it's **Kira**: big
   relief (he laughs, slumps back). She paws at his chest, **pointing out the
   necklace** he didn't know he had; he holds it up, it pulses blue. Abi
   laughs. Comedy is situational, no dialogue needed. Keep it about the same
   length (13–16 s) and skippable; render the MP4 again.
1b. **Kira still has see-through spots (Dan, 2026-11-09, after 0.4.0).**
   Round 4's reversed-twin faces cut it down but didn't finish it (sabertooth
   still 168 px in the exe). Get to **zero** see-through pixels, both forms,
   any angle, in the exe. If the Meshy mesh itself is open or non-manifold,
   don't patch it again: rebuild a closed mesh in Blender (voxel remesh or
   shrink-wrap to a solid body), re-bake the texture onto it, re-skin to the
   same rig, keep the Kira lock checks. Also check the material isn't using
   alpha / transparency anywhere. Check: turntable at 16 angles + close-ups of
   ears, tail, paws and fangs, and a player's-view run, with the measured
   see-through count (must be 0). Same check for Abi's hair (32 / 30 px left).
2. **The trees are lying down** ("Why were all the trees down?"). Find the
   cause (likely the pine prop's up-axis from Blender, not its placement) and
   fix it at the source. Then check **every** Ice Age prop is upright and
   sitting on the ground (pines, rocks, bones, crystals, stones), with a shot.
3. **Clean-up pass.** Before adding anything: go through Round 4's own exe
   screenshots and new ones from the player's view across the whole valley,
   and list every visual problem found (things lying wrong, floating or sunk
   props, clipping, seams, stretched textures, holes, popping). Fix them, and
   put the list (fixed / not fixed) in the report. Dan will add his own list
   after playing.

**Music (standing rule in `CLAUDE.md`: level songs are Dan's)**
4. Remove Code's Ice Age stand-in (`Music/iceage.mp3`, and from
   `make_sounds.py`). Play Dan's files when they exist, silence until then:
   - `Music/title.mp3`: **title screen (new, Dan 2026-11-09)**: its own main
     theme, different from Act I and the Ice Age. Prompt in `Docs/MUSIC.md`.
   - `Music/act1.mp3`: during `ActOne`, fade out at the white-out.
   - `Music/iceage.mp3`: starts with `IceAgeArrival`, then loops in the world.
   - `Music/vortex.mp3`: **inside the vortex space (new, Dan 2026-11-09)**,
     loops; crossfade in when the party enters, back to the world's theme
     when they leave.
   The Violinist's tune and the sound effects stay (ElevenLabs).
   **Dan's four tracks are in (2026-11-09):** `Music/title.mp3` (3:00),
   `act1.mp3` (22.4 s), `iceage.mp3` (3:00, replaces Code's stand-in: don't
   regenerate it; remove the iceage entry from `make_sounds.py`), `vortex.mp3`
   (3:00). Act I's track runs 3.4 s past the 19 s scene: fade it out over the
   white-out (or hold the white-out a moment longer if it ends on a nicer
   note). Check all four load and play in the right places (muted run, level
   reported); Dan judges the sound.

**A bigger Ice Age with places for later (Dan, 2026-11-09)**
5. **Grow the valley** (DEFAULT): about 10–15 minutes for a first visit (now
   6.6). Keep what's there and add three zones:
   - **Frozen lake:** wide ice with skating slides, holes in the ice, and a deep
     part under the ice for later (see the gates below).
   - **Glacier climb:** a tall wall of ice and ledges up to a summit with a view
     over the whole valley and the volcanoes.
   - **Hot-spring grotto:** a warm hidden cave by the volcano side, behind a
     cracked rock wall.
6. **Places that open with later moves.** Every one is visible on the first
   visit so the player knows to come back, and holds shards, a heart upgrade
   or both. Use the move roadmap below (DEFAULT, see open question):
   - double-jump ledges (Abi, Egypt): keep the two, add one;
   - boulders (Muscle Man, Medieval): keep the one, add one;
   - cracked rock walls (Dan's charged punch, Medieval): the grotto entrance;
   - the deep lake under the ice (deep dive, Pirate Seas);
   - wall-jump chimneys in the glacier (Abi's wall jump, Steampunk).
   Build the gates now as data (which move opens each); the moves themselves
   come in their worlds.
7. **Shards and hearts:** grow to about 60 small shards (DEFAULT) with the
   requirement about 35; Rule 2 must still hold (35 reachable with the moves
   you have on the first visit: no double jump, no Muscle Man, and the ground
   pound only after the Violinist). Hide two heart upgrades in the valley, at
   least one behind a later-move gate.

**Creature models**
8. **Meshy + Mixamo for the Violinist first, then stop for Dan.** Dan's concept
   `SourceArt/Concepts/violinist_iceage.png` → Meshy → clean-up → Mixamo upload
   FBX + clip list (idle, play-instrument idle, walk, a small bow/nod). Stop and
   tell Dan as soon as his file is ready (Dan does Mixamo the same day).
   **When Dan's clips arrive, import them in this same round** (don't wait for
   a later round).
9. **Wolf, snow critter, mammoth:** Meshy from Dan's concepts, Blender rig and
   clips by script (four-legged, like Kira): wolf (idle, walk, run, circle,
   lunge, hit, stunned), critter (idle, waddle, hit, stunned), mammoth (walk,
   idle; ride collider stays flat). Replace the placeholders; keep them as
   fallback.
10. Build, check (a full run from New game: Act I, the new arrival, the whole
    valley, story shard, vortex), publish 0.5.0, update `TODO.md` / `CLAUDE.md`,
    clean up.

**Move roadmap (DEFAULT, open for Dan; moves are kept across worlds, so later
moves reopen earlier worlds):**
| World | The Violinist teaches | Also |
|---|---|---|
| Ice Age | Dan: ground pound | |
| Egypt | Abi: double jump | |
| Bethlehem | Kira: pounce (long leap) | |
| Medieval | Dan: charged punch | Muscle Man joins (boulders, lifts Dan to high ledges) |
| Pirate Seas | everyone: deep dive (longer breath) | |
| Steampunk | Abi: wall jump | |
| Tomorrowland | Abi: air dash | |
| Moon | none (the finale) | |
---------------------------------------------------------------------------

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
### Round 4: the Ice Age, the first real world (0.4.0, build 400, 2026-11-09)
Final check `local/round4/final2` (built exe, batch mode, muted, no window, virtual pad from the title screen; Ice Age
play-through + items 1-2 + frame rate): RESULT all checks passed (96 ok); audio output 0.000 RMS. Evidence:
`Data/screenshots/round4/` (`game/` exe shots from the player's view + turntable sheets, `cutscenes/` frames of the MP4s,
`holes/` Blender before/after, `exe_check_final.txt`). The plan as written is in `TODO-ARCHIVE.md`.
1. Kira's holes: **done.** Cause: single-sheet fur tufts and whiskers seen from behind (culled). Fix at the source: every face
   a camera can see from behind gets a reversed twin (`Tools/Blender/mesh_fix.py`, in `kira_rig.py`). See-through pixels over
   14 Blender views: home 298 -> 0, sabertooth 2,290 -> 499; in the exe (12 views, two-sided vs. normal): home 0, sabertooth 168 (worst view 44).
2. Abi's hair: **done.** Same cause (one-sided strands) plus the strip Round 2's hair fix cut between hair and shoulders. Same
   fix in `fix_hair_weights.py`. Blender: home 12,882 -> 63, Ice Age 9,469 -> 322; exe: 32 and 30 (worst view 19). Turntables at 8
   angles + head close-ups, and running shots in the player's view: `game/r4_turntable_*.png`, `game/r4_run_*`. Dan unchanged (0 before).
3. Opening: **done.** New game plays `ActOne` (19 s, the living room from Dan's references, Kira bats the dark silver bauble off
   the tree, it shatters, the vortex pulls them in) then `IceAgeArrival` (13.5 s: they land in the snow in caveman clothes, Dan falls
   flat on his back, sabertooth Kira jumps on his chest and paws at the necklace, which pulses blue). Both skippable (checked).
   Both rendered to MP4 (`Recordings/`). The graybox playground: Settings > Test area (debug), its own save file.
4. Layout: **done** as the DEFAULT: one valley (~500 m), snowfield + pond, mammoth ridge (two mammoths walk a loop; ride one up to a
   high shelf), gorge with a cracked bridge (planks drop), ice pillars and a river with a ramp out, ice caves (dark; Kira's crawl
   tunnel; an ice wall for sabertooth Kira; cracked ice over a pit), three steam-vent terraces, standing stones; future gates: two
   double-jump ledges, a boulder (Muscle Man). The bot crossed all of it with the pad: 6.6 min from landing to the story shard, 36 shards (target 5-8 min).
5. Shards / story shard / vortex: **done.** 40 shards, 25 needed (DEFAULT). Rule 2: 33 of 40 reachable without double jump, Muscle Man
   or the ground pound (data); the play-through had 27 before Dan learned the ground pound. Necklace wakes (cutscene, now Ice Age
   actors) -> blue story shard on the altar -> `VortexOpens` -> the vortex space: Ice Age (back), Egypt ("to be continued", saved),
   Home (blocked: "7 story shards are still missing"). Replaces the portal (the test area keeps its own).
6. Ground pound + Violinist: **done.** Dan: jump, then B / Ctrl: breaks cracked ice (2 places) and stuns enemies. The Violinist
   (placeholder figure with a Blender bone fiddle, by a campfire at the cave) plays (`ViolinistTeaches`) when Dan reaches him; to
   anyone else he "is looking past you, at Dan". Added to item 9's prompts.
7. Enemies: **done.** Wolves (spot you, circle at 5.5 m, one lunges at a time) and snow critters (patrol), placeholder shapes;
   the ground pound stuns them. No boss.
8. Look: **done.** Blender terrain (snow / faceted rock), two distant volcanoes with lava and ash plumes, fog to 1.7 km, blowing snow,
   pines, rocks, bones, ice crystals, standing stones. Frame rate with the view drawn every frame: 262 fps at 1080p on Dan's GTX 1660
   Ti (need 60); Steam Deck setting at 1280x800 on the same PC: 271 fps (need 40). The Deck itself was not measured (no Deck here).
9. Creature prompts: **done**, then Dan made the images the same day: `SourceArt/Concepts/wolf.png`, `snow_critter.png`,
   `mammoth.png`, `violinist_iceage.png` (renamed from Dan's files; checked: full body, plain background, >= 1024 px, no props).
   Models: **not started** (as planned: next round). The mammoth's back came out rounded, not flat: fine (the game rides a flat collider).
10. Music and sound: **done with one stand-in.** ElevenLabs: footsteps on snow, ice cracking, shard, necklace, wolf growl, ground
    pound, the Violinist's tune, wind loop, and the Ice Age theme (its music API worked: 90 s). The theme is meant to be Suno: Code
    can't call Suno, so the prompt is in `Docs/MUSIC.md` for Dan; his file replaces the stand-in. Checked: clips loaded and playing,
    1,022 effects fired; nothing audible was played on this PC (every run muted, 0.000 output).
11. Build / check / publish: **done.** 0.4.0 (179.5 MB), `game-400` published; a fresh Round 1 starter install from GitHub updated
    itself to 0.4.0 and started (ready handshake; muted test mode). Disk: 3.2 GB -> 2.5 GB.

Decisions Code made (Dan can overrule): the high shelf is one long shelf 1 m over a mammoth's back (three shelves 1.3 m up were too
hard to reach); ice pillars 2.1 m apart; the necklace wakes only once you're out of a cave (its cutscene needs open sky); a fall off
the vortex platform costs a heart like any fall (rails added); invisible walls round the valley; the music volume is a new setting.

**For Dan to check:** New game on a pad: the Act I cutscene (does the room feel like your film?) and the arrival (Dan's fall, Kira
on his chest); riding a mammoth up to the shelf; the cracked bridge; the dark cave with the necklace's light; the Violinist and the
ground pound; the steam vents; the vortex and "to be continued". Whether wolves are too easy / too hard. Kira's fur and Abi's hair
from behind and close up. The stand-in music and the sounds (they've only been checked as files, never heard). The volcanoes read
as distant brown cones (lava barely visible through the fog): say if you want them closer or brighter.

### Fix 3.1: sabertooth Kira's face (0.3.1, build 301, 2026-11-09)
Check `local/round3/fix31_play` (built exe, batch mode, muted, no window, virtual
pad from the title screen, `--only play`): RESULT all checks passed; audio output
0.000 RMS. Evidence: `Data/screenshots/round3/fix3.1/` (`kira_sabertooth_face_sheet.png`
= the 9 close-ups, clip frames, `12d`-`12g` exe shots, `exe_check_play.txt`).
1. New concept: **done** (checked again by Code before use: head in profile in line
   with the body, ears folded, mouth closed, both fangs straight down, all paws and
   tail in frame, amber eyes, no white: passes).
2. Meshy + clean-up + rig: **done.** New Meshy model (30 credits), 14.7k tris, 0.5 m;
   it came out facing front, so the old 90 deg turn in `meshy_cleanup.py` is gone.
   Fur matched to home Kira by `Tools/match_fur.py` (fur average rgb 115,100,97 ->
   118,111,109; home Kira 117,110,109; eyes / nose / fangs left alone). Rig: head
   measured 6 deg off straight (old model: 83 deg); **no counter-turn** for the
   sabertooth. Home Kira keeps hers (her head is 34 deg turned; she isn't re-rigged).
   Kira lock at build: OK (no ear bones, 0% white, amber eyes).
3. Face previews: **done.** Front / side / 3/4 of the head in idle, run and attack
   (Blender) and in the exe (player's view `12d`, close-ups `12e`-`12g`): both eyes
   look the same way in every shot, face not stretched, fangs straight down from
   under the upper lip. Her pupils sit slightly outward, mirrored (Meshy's eyes, not
   the rig). Old model kept: `SourceArt/Rigged/kira_sabertooth_v1/model.fbx` (in git)
   and `SourceArt/Meshy/kira_sabertooth_v1/` (local only, git-ignored).
4. Build / check / publish: **done.** 0.3.1 (166 MB), `game-301` published; a fresh
   Round 1 starter install from GitHub updated itself to 0.3.1 (ready handshake;
   game hidden, batch mode, master volume 0). Disk: 2.7 GB -> 2.3 GB.

Decisions Code made (Dan can overrule): the counter-turn is now per model
(`COUNTER_TURN` in `kira_rig.py`, home Kira only); the fur match also moves the
fur's brightness spread to home Kira's.

**For Dan to check:** sabertooth Kira in Area B on a pad, face toward the camera
(idle, running, clawing the ice); whether her gray now reads as the same cat as
home Kira.

### Round 3: Dan and Abi in the game, animated (0.3.0, build 300, 2026-11-09)
Final check `local/round3/final` (built exe, batch mode, muted, no window,
virtual pad from the title screen; play + Round 3 models + measure + perf):
RESULT all checks passed (171 ok); audio output 0.000 RMS. Evidence:
`Data/screenshots/round3/` (`game/` 39 exe shots from the player's view +
necklace close-ups, `cutscenes/` frames of the two MP4s, `exe_check_final.txt`).
1. Import: **done.** Four models Humanoid (Mixamo T-poses for Dan, `rigged_hairfix.fbx`
   for Abi), 20-21k triangles, LODs by Unity's own mesh LODs (made from the rigged
   mesh, so skin weights kept; 10-11 levels). One Mixamo clip set for all four (16
   clips, trimmed in `CharacterImport.PeopleClips`): idle, walk / slow run / run
   blended by speed, jump, fall, land, hard landing (falls of 3.3 m+), swim + tread,
   push, punch (Dan), kick (Abi), hit, cheer (shard pickups; everyone on the story
   shard), look (cutscenes). Punch / cheer / hit go on the upper body while running.
   Check: a planted foot slides 0.3-0.7 m/s while running (4-9% of run speed; limit
   12%) after the clip speeds were tuned once from the check (x1.09).
2. Capsules replaced: **done.** Dan and Abi as player and followers; outfit per
   area like Kira's form (`AreaData.outfit`: A home, B Ice Age); capsules kept as
   fallback. Check: outfits swap both ways at the portal; feet on the ground
   (lowest toe -2.5 to -1.3 cm), every model faces its way (0.96-1.00).
3. Necklace: **done.** Fitted per outfit from the model's own chest (neck bone,
   clothes under the shard, neck width) on `Spine2`, 3 cm off the clothes. Check:
   shard outside the clothes while running (home: 1.5 cm in front at the
   deepest; Ice Age: touches the fur, 0.2 cm in at the deepest, limit 0.5 cm); close-ups in `game/r3_dan_*_necklace_closeup.png`.
4. Cutscenes: **done.** NecklaceWakes and PortalOpens rebuilt with Dan, Abi (home
   clothes) and Kira as actors with clip tracks (idle / look / cheer; Kira idle /
   jump); both MP4s rendered again (`Recordings/`, 1920x1080, 30 fps).
5. Checked as Dan sees it: **done.** New exe check `--only models`: per person and
   outfit idle / run front / run side / jump / attack / swim / tread / push / hard
   landing / cheer, followers, switching. Found and fixed: foot sliding, Abi's run
   speed, the necklace sinking into the sweater mid-stride, Dan's head in the push
   block (the model steps back 25 cm while pushing), the cutscene glow covering
   Dan's face. Left as is: followers can overlap each other in the pool (followers
   never collide, by design); Abi's long hair looks stringy close up (Meshy mesh).
6. Build / check / publish: **done.** 0.3.0 (166 MB), `game-300` published; a
   Round 1 starter install updated itself from GitHub to 0.3.0 (ready handshake).

Decisions Code made (Dan can overrule): Unity mesh LODs instead of Blender LODs;
heroes always animate (no off-screen culling: 3 characters, cheap); hard landing
from 14 m/s; cheer on every shard pickup (upper body if running); the model lifts
0.28 x height when swimming at the surface; Abi's double jump replays the jump clip.

**For Dan to check:** Dan and Abi running, jumping, swimming, punching / kicking on
a pad (both outfits: Area A and B); the necklace on both outfits; the two
cutscenes in play and the new MP4s; whether the cheer on every shard is too much.

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Suggested for Round 5 (Code's notes, the planning chat decides):** the four
  creature models from Dan's images (Meshy; Code's Blender rigs for wolf,
  critter and mammoth; Mixamo for the Violinist) replacing the placeholders;
  Dan's Suno Ice Age theme; then Egypt (mummy Kira, the judgmental camel).
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
