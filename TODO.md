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
Current round — Round 4: the Ice Age, first real world (target 0.4.0)
Fix 3.1's plan is in `TODO-ARCHIVE.md`. Dan played 0.3.1 (2026-11-09): new
sabertooth Kira "looks good". Report per item: done / not done / deferred, and
why. Defaults the planning chat chose are marked DEFAULT: Dan can overrule
them after playing.

**Carried-over fixes (Dan, 2026-11-09)**
1. **Kira has see-through holes** from some angles (sabertooth form; check home
   Kira too). Find the cause per hole (back-face culling on thin or one-sided
   geometry, flipped normals, real gaps in the Meshy mesh) and fix it at the
   source: recalculate normals, close gaps in Blender, two-sided material only
   where the geometry is genuinely thin. Check: a turntable of each form at
   8 angles plus close-ups, no background showing through.
2. **Abi's hair has see-through holes,** worst in her home outfit. Same
   approach (hair is often one-sided cards from Meshy: two-sided hair
   material, normals fixed, gaps closed). Check: turntable at 8 angles, both
   outfits, plus the player's-view camera while she runs.

**The Ice Age**
3. **Game start = the film's opening.** New game plays Act I as an in-engine
   cutscene (Christmas evening, Kira knocks the ornament off the tree, it
   shatters, the vortex pulls them in), then the Ice Age arrival (they land in
   the snow in caveman outfits; Dan falls backward; sabertooth Kira jumps on his
   chest and paws at the necklace, which starts pulsing blue), then play
   starts. Both skippable. Living room set from Dan's references in
   `SourceArt/Reference/Worlds/00-Home/` (added 2026-11-09): purple-magenta
   walls in warm golden light, red velvet curtains tied back, a tall window with
   snow outside and a garland of lights on the sill, a cream couch with red and
   lime cushions, a lit wreath around a framed picture, a decorated tree with
   red / purple / silver baubles, warm fairy lights and a red heart ornament
   with initials. **The ornament that shatters is the dark silver glass bauble
   Dan is holding** in `home-decorating-tree-ornament.png`: model it as its own
   prop (it reappears whole in Act XI). Stylized to match the characters, not
   photo-real. Our characters wear their concept-sheet outfits, not the film's. The Round 1 graybox
   playground stays reachable only from a debug option (test area).
4. **Level layout** (DEFAULT, from the film + concept): one connected frozen
   valley with distant volcanic mountains, about 5–8 minutes to cross, in zones:
   - **Landing snowfield:** start; teaches run / jump / switch / swim (a
     half-frozen pond) with a few easy shards.
   - **Mammoth ridge:** a slow herd of mammoths you can ride on the back of
     (moving platforms) to reach high shelves.
   - **Ice bridges and a frozen river:** jumping, a cracked bridge that breaks.
   - **Ice caves:** Kira-only crawl tunnels and ice walls only sabertooth Kira
     can claw through; a dark section lit by the necklace's glow.
   - **Volcano vents** at the valley edge: warm steam that lifts the player
     (a vertical route).
   - **Future-ability gates**, visible but closed: a high ledge for Abi's
     double jump, a big boulder for Muscle Man.
5. **Shards, story shard and the vortex** (numbers DEFAULT): 40 small shards
   in the Ice Age, 25 needed. At 25 the necklace wakes (Round 1 cutscene, now
   with the real characters) and leads to the **blue story shard**; collecting
   it **opens a vortex** (blue, the shard's color). **Vortex = travel between
   worlds (DECIDED, Dan 2026-11-09):** every world's story shard opens a vortex;
   stepping into it shows the paths: every world unlocked so far, the next
   world, and **Home**. Home is visible but **blocked until all shards are
   rebuilt** (shows how many story shards are still missing). Build the vortex
   space and its path choice now: paths = Ice Age, Egypt ("to be continued"
   this round, saves), Home (blocked, 1 / 8). The vortex replaces Round 1's
   portal. Rule-2 check: 25 reachable without double jump or
   Muscle Man, proved by data + a play-through.
6. **One new move this round** (DEFAULT): Dan's **ground pound**, learned in
   the Ice Age (breaks cracked ice floors to reach hidden shards, stuns
   enemies). **The Violinist teaches moves (DECIDED, Dan 2026-11-09):** he
   appears somewhere in each world (in the Ice Age with a bone flute or a
   simple prehistoric fiddle, DEFAULT), plays a short tune when a character
   reaches him, and that character learns the move (short in-engine moment,
   no dialogue: he never explains himself). Placeholder figure until his model
   exists; add the Violinist (Ice Age look) to item 9's prompts.
7. **Enemies:** wolves (circle, then lunge) and snow critters (simple
   patrollers). Placeholder shapes until their models exist (item 9). Light
   combat, per the concept. No boss this world (DEFAULT).
8. **Look:** stylized and colorful, matching the characters. Dan's reference
   `SourceArt/Reference/Worlds/01-IceAge/iceage-valley-volcanoes.jpg` sets the
   layout and palette: a wide snowy valley between warm brown rock cliffs,
   **two erupting volcanoes** with glowing lava and ash plumes at the far end,
   mammoths in the blowing snow, a rock cave mouth lit by firelight, standing
   stones and stone ruins in the snow. The reference is semi-realistic; make it
   stylized like the characters. Soft blue-white light, fog for depth. Terrain and ice by Blender script; rocks, bones and trees as
   simple stylized props (Meshy text-to-3D allowed for props). Must hold 60 fps
   at 1080p on Dan's PC and 40+ on the Steam Deck: measure both settings.
9. **Creature models: prompts only, then stop for Dan.** Add concept prompts to
   `Docs/MODELS-NEEDED.md` for: wolf, snow critter (design it: small, round,
   cute-grumpy), mammoth (rideable, big flat back), plus any prop Meshy can't
   do from text. Same rules as before (full body, side view for animals, plain
   background). Tell Dan when they're ready; he makes the images, and the
   models come in the next round. Keep building with placeholders meanwhile.
10. **Music and sound:** an Ice Age theme with Suno (whimsical, adventurous;
    loops cleanly), ambience (wind), and ElevenLabs effects for footsteps on
    snow, ice cracking, shard pickup, necklace pulse, wolf growl. Sound checks
    as short as possible, then mute again.
11. Build, check (as Dan plays it: a full run from New game to the Egypt
    portal), publish 0.4.0, update `TODO.md` / `CLAUDE.md`, clean up.

**Decided this round (Dan, 2026-11-09):** the Violinist teaches moves; the
vortex is the travel hub (path home blocked until all shards are rebuilt).

---------------------------------------------------------------------------

---------------------------------------------------------------------------
Last deliveries
---------------------------------------------------------------------------
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

### Round 2: first character models + Kira forms (0.2.0, build 200, 2026-11-09)
Final check `local/round2/final` (built exe, batch mode, muted, no window,
virtual pad from the title screen, play + measure + perf): RESULT all checks
passed; audio output 0.000 RMS. Evidence: `Data/screenshots/round2/` (`models/`
previews, `mixamo/` rig tests, `kira_rig/` clip frames, `game/` exe shots).
1. Meshy: **done.** All six (30 credits each) + a second `abi_home` (+30, see 6).
2. Blender clean-up: **done.** Metres, feet at 0, facing front: Dan 1.8 m, Abi
   1.65 m, Kira 0.5 m; ~20.7k tris (people), ~15k (Kira). Kira's ears came out
   folded in both forms. LODs: Kira 100/50/20 %; people's LODs in Round 3 from
   the rigged file (keeps the skin weights).
3. Previews: **done.** No fused limbs or missing feet/paws. Faint smear on
   dan_home's trouser (3/4 view). Both Kira heads are turned (as the concept
   sheets): her rig counter-turns them (max 45 deg; sabertooth stays ~38 deg off).
4. Mixamo files + checklist: **done.** Dan rigged all four the same night.
   Abi's hair was tied to her arms: `Tools/Blender/fix_hair_weights.py` fixes it
   (`rigged_hairfix.fbx`); abi_home remade in Meshy, re-rigged by Dan.
5. Kira's rig: **done, Blender path** (Meshy's rigging is humanoid only):
   `Tools/Blender/kira_rig.py`, 23 bones, no ear bones, 9 clips, bone-heat skin.
6. Stop for Dan: **done** (stopped after the four people; then fixed Abi first).
7. Kira forms: **done.** `KiraForm` data (Home, Sabertooth), `AreaData.kiraForm`;
   Area A home cat, Area B sabertooth with `ClawIce`: ice cave + shard B06. Kira
   lock checked at build on every form model (no ear bones, grey, no white,
   amber eyes): both OK. Check: Dan's punch doesn't break the ice, Kira's does.
8. Necklace: **done.** `Tools/Blender/necklace.py` (cord, bail, shard); socket =
   Mixamo `Spine2` (offset to tune in Round 3), on the capsule meanwhile; pulses.
9. Kira in the game: **done.** Real model + clips (run/idle follow the motor),
   form swaps per area; capsule kept as fallback. Dan / Abi stay capsules.
10. Build / check / publish: **done.** 0.2.0 (123 MB), `game-200` published; a
   Round 1 starter install updated itself from GitHub to 0.2.0 (ready handshake).

Decisions Code made (Dan can overrule): one Mixamo clip set for everyone;
Meshy's metallic map dropped (fur came out speckled); the sabertooth's head
counter-turn capped at 45 deg; cutscene actors are still Round 1's graybox ones.

**For Dan to check:** Kira's look and moves in play (both forms; does the
sabertooth's turned head bother you?); the ice cave in Area B; the necklace on
Dan's capsule; the hair fix renders in `Data/screenshots/round2/mixamo/`.
Delete two test folders Code couldn't: `C:\Data` (empty), `C:\sot200` (launcher test install).

---------------------------------------------------------------------------
Next rounds (do not start yet)
---------------------------------------------------------------------------
- **Then:** Ice Age as the first real world (the original concept's first
  milestone), with its film moments as in-engine cutscenes; Act I cutscene.
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
