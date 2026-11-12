# Shards of Time — models needed

## Round 11 (2026-11-12): Mixamo for Victorian Steampunk — what Dan uploads and downloads

Dan, Abi and Muscle Man are Meshy models; the Violinist and the factory worker are Dan's Tripo models (`SourceArt/Tripo/`, decimated by
`Tools/Blender/decimate_glb.py`). Fronts: `Data/screenshots/round11/models/`. **Upload these five:**

| Model | Upload this (mixamo.com > Upload Character) | Height |
|---|---|---|
| Dan, steampunk | `SourceArt/Mixamo/dan_steampunk/upload/dan_steampunk.fbx` | 1.8 m |
| Abi, steampunk | `SourceArt/Mixamo/abi_steampunk/upload/abi_steampunk.fbx` | 1.65 m |
| Muscle Man, steampunk | `SourceArt/Mixamo/muscleman_steampunk/upload/muscleman_steampunk.fbx` | 2.0 m |
| The Violinist, street musician (Dan's Tripo model, decimated to 25k) | `SourceArt/Mixamo/violinist_steampunk/upload/violinist_steampunk.fbx` | 1.85 m |
| Factory worker (Dan's Tripo model, decimated to 15k; enemy) | `SourceArt/Mixamo/factory_worker/upload/factory_worker.fbx` | 1.8 m |

For **each**:
1. **Upload**, then the auto-rigger: markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**.
   (Dan's long coat: put the knee markers on the knees, not the coat's hem.)
2. **Download the rigged character** with no animation picked: **FBX for Unity (.fbx)**, **With Skin**, **T-pose**.
   Save as `SourceArt/Mixamo/<model>/T-Pose.fbx` (next to `upload`, not inside it).

Everything else is reused (the shared clips in `SourceArt/Mixamo/` play on every person; the workers slip with Fall Flat and get up
with Hard Landing like the deckhands). **Muscle Man needs two new clips** for the press and the clock (his folder has Lifting Object,
Throw, Carrying, Pushing and Pull Heavy Object, but nothing held up over the head): **FBX for Unity, 30 fps, Keyframe Reduction none,
Without Skin, In Place ticked where offered**, Muscle Man on screen. If a search name isn't there, take the closest one that fits "For"
and save it under the first column's name anyway.

| Save as | Mixamo search | Folder | For |
|---|---|---|---|
| `Overhead Hold.fbx` | **Overhead** (else **Lift** / **Hold Up**): the one where he holds both arms straight up over his head, as if holding up something heavy, and stays there | `SourceArt/Mixamo/muscleman_steampunk/` | holding the dome press up over Dan, Abi and Kira; lifting Dan up to the clock face |
| `Heavy Throw.fbx` | **Throw** (else **Toss**): a big two-handed throw from over the head | `SourceArt/Mixamo/muscleman_steampunk/` | heaving the press off so it smashes out through the wall |

**Still open from Round 10** (same settings, folder `SourceArt/Mixamo/muscleman_pirate/`): `Drowning.fbx` (search **Drowning**, else
the most flailing **Swim**), `Getting Up.fbx` (**Getting Up**, else **Stand Up**), `Jumping Down.fbx` (**Jumping Down**, else **Jump**).

That's **5 T-poses** (Dan, Abi, Muscle Man, the Violinist, the factory worker) + 2 Muscle Man clips (+3 from Round 10). Tell Code when they're in.

**Fix 11.2 (2026-11-12): all five rigs are in.** Heavy Throw and Drowning weren't on Mixamo: Muscle Man's `Overhead Squat` is both his hold and his heave (Dan's idea); his struggle stays Treading Water. Getting Up and Jumping Down are in.

**Fix 11.1 (2026-11-12): all seven Tripo models are in.** The Violinist and the worker are the uploads above. Code rigged mechanical Kira (23 bones, no ear bones) and the clockwork horse (Code's four-legged rig, 9 clips each); the street clock, the carriage and the press are static models (`Tools/Blender/static_export.py`). Previews: `Data/screenshots/round11/models/`, rigs `round11/rig/`.

## Round 11 (2026-11-12): Victorian Steampunk — images needed from Dan (prompts by the planning chat)

Film art: `SourceArt/Reference/Characters/*/…-steampunk-film.png`, `Kira/kira-mechanical-film.png`,
`Violinist/violinist-steampunk-violin-film.png`, `Others/steampunk-lineup-greenscreen.png`, the world in
`Worlds/06-Steampunk/` (factory: bolt bucket, conveyor, dome press, press frame with workers, broken wall; street:
cobbles with rail tracks, brick and cream buildings, gear doors and the gear gate, copper clockwork horses, brass steam
carriage, teal lamps). Concept sheets override film stills (Abi keeps her brown hair and green eyes; the Violinist stays
the older gray-haired man; Kira's ears stay folded even where a film frame shows them up). Same rules and negative
prompt as before. People: front view, A-pose, empty hands. Animals: strict side profile. Save in `SourceArt/Concepts/`.

### S1. Dan, steampunk — `dan_steampunk.png`
> Pixar-style 3D character render of Dan, the same young man as in his other character sheets (short swept-back
> auburn-brown hair, friendly face). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open
> and not touching the body, feet shoulder-width apart, mouth closed with a gentle smile, looking straight ahead.
> Steampunk clothes: a long open reddish-brown leather coat reaching below the knees, decorated with small copper and
> brass gears, sleeves rolled up to the forearms with buckled straps; a dark charcoal shirt with a faint gear pattern;
> a brown leather belt with a brass buckle; dark trousers; laced brown leather boots with brass trim. No necklace or
> pendant. Plain light gray background, soft even light, no shadow, whole body in frame.

### S2. Abi, steampunk — `abi_steampunk.png`
> Pixar-style 3D character render of Abi, the same young woman as in her other character sheets (long wavy brown hair,
> green eyes, warm smile). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not
> touching the body, feet shoulder-width apart, mouth closed with a confident smile, hair falling behind her shoulders.
> A steampunk adventurer outfit: brass aviator goggles pushed up on her head, small earrings, a laced brown leather
> corset top with thin straps, leather arm bands, a layered jagged brown skirt with a belt decorated with small copper
> gears, black fingerless gloves, tall brown leather boots with brass buckles and gear details. Plain light gray
> background, soft even light, no shadow, whole body in frame.

### S3. Muscle Man, steampunk — `muscleman_steampunk.png`
> Pixar-style 3D character render of Muscle Man, the same huge, extremely muscular, cheerful man as in his other
> character sheets (brown hair swept back into a small ponytail, a short brown beard). Full body, front view, A-pose:
> arms about 45 degrees out from the body, hands open and not touching the body, feet shoulder-width apart, mouth
> closed in a big grin. A steampunk strongman-worker outfit: a riveted dark leather sleeveless vest decorated with
> copper gears, riveted copper shoulder armor, brown leather straps over the shoulders, a wide belt with a big copper
> gear buckle, worn dark brown trousers with patched knees, tall cuffed brown boots. Plain light gray background, soft
> even light, no shadow, whole body in frame.

### S4. Kira, mechanical cat — `kira_mechanical.png`
> Pixar-style 3D render of Kira, a tiny Scottish Fold kitten transformed into a cute clockwork mechanical cat made of
> polished gunmetal-gray metal. Full body, strict side profile: the whole body and the head seen exactly from the side,
> head level and in line with the body, nose pointing at the left edge of the image, only one eye visible, not looking
> at the camera. Standing on four slim jointed metal legs with round metal paws, a sleek segmented metal body with small
> brass rivets and a few little brass gears at the hip, a long curved metal tail. A round cat face with a small nose
> and whiskers, folded-down metal ears that lie flat and forward, never raised. Every surface is gunmetal gray or brass,
> with absolutely no white anywhere. Golden-amber eyes. Mouth closed. Plain light gray background, soft even light, no
> shadow, whole body in frame with every paw and the tail visible.

### S5. The Violinist, steampunk — `violinist_steampunk.png`
> Pixar-style 3D character render of the Violinist, the same older gray-haired, gray-bearded man as in his other
> character sheets (same face and hair). Full body, front view, A-pose: arms about 45 degrees out from the body, hands
> open and not touching the body, feet shoulder-width apart, mouth closed with a calm knowing smile. A steampunk street
> musician outfit: brass aviator goggles pushed up on his forehead, a loose cream shirt with rolled sleeves, a worn
> dark brown vest with brass buttons, a colorful patchwork sash at the waist under a leather belt, faded brown and cream
> striped trousers, a beaded bracelet, tall brown boots. Holding nothing (no instrument). Plain light gray background,
> soft even light, no shadow, whole body in frame.

### S6. Factory worker (enemy) — `factory_worker.png`
> Pixar-style 3D character render of a goofy cartoon factory worker for a family video game, not scary: big and burly
> with a round belly, a brown leather cap with a little brass knob on top, a sleeveless gray work vest, brown leather
> straps crossed over the chest, one dented metal shoulder pad, a wide belt, baggy brown trousers, heavy boots, a
> bushy mustache. Full body, front view, A-pose: arms about 45 degrees out, hands open and empty, feet apart, mouth
> closed, a grumpy frown. Plain light gray background, soft even light, no shadow, whole body in frame.

### S7. Clockwork horse — `clockwork_horse.png`
> Pixar-style 3D render of a friendly cartoon clockwork horse made of polished copper plates and brass gears, with
> visible rivets, a small brass chimney on its back puffing a little steam, a short dark mane and tail, and a leather
> harness. Strict side profile: the whole body and the head seen exactly from the side, head level and in line with
> the neck, only one eye visible, not looking at the camera, standing still on four straight legs. Plain light gray
> background, soft even light, no shadow, whole body in frame with all four hooves visible.

### S8. Brass steam carriage — `steam_carriage.png`
> 3D render of a cartoon Victorian steampunk carriage made of shiny brass and gold, with large spoked wheels, big glass
> windows, a rounded roof with a small copper chimney, rivets and gear details, no horse attached, no people. Three-
> quarter view from the front left, the whole carriage in frame. Plain light gray background, soft even light, no
> shadow.

### S9. The great street clock — `street_clock.png`
> 3D render of a tall cartoon Victorian steampunk street clock: a dark green cast-iron column with ornate bands, topped
> by a big round four-sided brass clock head with a cream clock face, black hands and Roman numerals, visible brass
> gears turning behind glass, teal glass lamps at the corners, and an empty round gear-shaped socket in the middle of
> the face. Front view, standing upright, the whole clock from base to top in frame. Plain light gray background, soft
> even light, no shadow.

### S10. The steam press — `steam_press.png`
> 3D render of a huge cartoon steampunk factory press machine: a giant rounded brass dome on top with big bolts,
> heavy dark iron rings and a wide flat press plate underneath, on two thick iron pillars with chains and pipes, a
> small brass control panel with dials and red buttons beside it. Front view, the whole machine in frame. Plain light
> gray background, soft even light, no shadow.

Built by Code, no image: the factory hall (conveyor belts with blue rollers, the bolt bucket, gear walls, chains, pipes,
steam vents, control panels, the wall the press breaks through), the city streets (cobbles, rail tracks, brick and
cream buildings, gear doors and the gear gate, teal lamp posts, barrels), the teal gear story shard, the steampunk
violin (brass gears set into the body).
Mixamo after Meshy: `dan_steampunk`, `abi_steampunk`, `muscleman_steampunk`, `violinist_steampunk`,
`factory_worker`. Mechanical Kira and the clockwork horse are Code's rigs (no Mixamo).

## Round 10 (2026-11-11): Mixamo for the Pirate Seas — rigs DONE (same night); 3 Muscle Man clips still open

**In:** all five T-poses (Dan 41 bones, Abi 65, Muscle Man 41, the Violinist 65, the deckhand 49); renders `Data/screenshots/round10/mixamo/`;
Abi's mesh closed (`close_person.py`, 0 open edges). **Not found:** `Drowning.fbx`, `Getting Up.fbx`, `Jumping Down.fbx` in
`SourceArt/Mixamo/muscleman_pirate/`. Until they are in, Muscle Man uses Treading Water (his struggle), Hard Landing (getting up after the
crash) and Jump (the leap overboard). Drop them in that folder and run `python Tools/import_characters.py --only muscleman_pirate`, then a
build (the cutscenes pick them up when rebuilt: delete `Assets/Resources/Cutscenes/{PirateArrival,ShardOverboard,SeaVortex}.prefab`).

What was asked (kept for the record):

All 9 Pirate Seas images are Meshy models (fronts: `Data/screenshots/round10/models/`). The five people go through Mixamo;
otter Kira, the crab, the pufferfish and the fish are Code's rigs (no Mixamo).

| Model | Upload this (mixamo.com > Upload Character) | Height |
|---|---|---|
| Dan, pirate | `SourceArt/Mixamo/dan_pirate/upload/dan_pirate.fbx` | 1.8 m |
| Abi, pirate | `SourceArt/Mixamo/abi_pirate/upload/abi_pirate.fbx` | 1.65 m |
| Muscle Man, pirate | `SourceArt/Mixamo/muscleman_pirate/upload/muscleman_pirate.fbx` | 2.0 m |
| The Violinist, ship's fiddler | `SourceArt/Mixamo/violinist_pirate/upload/violinist_pirate.fbx` | 1.85 m |
| Pirate deckhand | `SourceArt/Mixamo/pirate/upload/pirate.fbx` (one peg leg: put both knee markers where the knees are) | 1.65 m |

For **each of the five**:
1. **Upload**, then the auto-rigger: markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**.
2. **Download the rigged character** with no animation picked: **FBX for Unity (.fbx)**, **With Skin**, **T-pose**.
   Save as `SourceArt/Mixamo/<model>/T-Pose.fbx` (next to `upload`, not inside it).

Swimming: **nothing new to download for the heroes.** Your shared clips already have `Swimming.fbx`, `Treading Water.fbx` and
`Swimming To Edge.fbx` (they play on every person, the pirates too). Only Muscle Man gets three new clips of his own:
**FBX for Unity, 30 fps, Keyframe Reduction none, Without Skin, In Place ticked where offered**, Muscle Man on screen. If a search
name isn't there, take the closest one that fits "For" and save it under the first column's name anyway.

| Save as | Mixamo search | Folder | For |
|---|---|---|---|
| `Drowning.fbx` | **Drowning** (if missing: search **Swim** and take the most flailing, struggling one) | `SourceArt/Mixamo/muscleman_pirate/` | his struggling swim under the sea (he can't keep up) |
| `Getting Up.fbx` | **Getting Up** (if missing: **Stand Up**) | `SourceArt/Mixamo/muscleman_pirate/` | the arrival: he lies flat on the deck after crashing through the sails, then sits up grinning |
| `Jumping Down.fbx` | **Jumping Down** (if missing: **Jump**) | `SourceArt/Mixamo/muscleman_pirate/` | the shard overboard: he leaps off the deck after it |

That's 5 T-poses + 3 clips. Tell Code when they're in; Code imports them and keeps going.

## Round 9 (2026-11-11): the Pirate Seas — images needed from Dan (prompts by the planning chat)

Film art: `SourceArt/Reference/Characters/*/…-pirate-film.*`, `Kira/kira-otter-film.jpg`,
`Violinist/violinist-pirate-fiddle-film.png`, the world in `Worlds/05-PirateSeas/` (incl. the clip
`shard-rolls-overboard.mp4`). Same rules and negative prompt as before. People: front view, A-pose, empty hands.
Animals: strict side profile. Save in `SourceArt/Concepts/`.

### P1. Dan, pirate — `dan_pirate.png`
> Pixar-style 3D character render of Dan, the same young man as in his other character sheets (short swept-back
> auburn-brown hair, friendly face). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open
> and not touching the body, feet shoulder-width apart, mouth closed with a gentle smile, looking straight ahead. Pirate
> clothes: a dark red bandana tied over his hair, a brown and cream striped shirt with sleeves rolled to the elbows, a
> brown leather vest with brass buttons, a leather strap across the chest with a buckle, a red-brown neckerchief, a wide
> leather belt with a big buckle, dark green knee-length shorts, brown leather shoes. No necklace or pendant. Plain
> light gray background, soft even light, no shadow, whole body in frame.

### P2. Abi, pirate — `abi_pirate.png`
> Pixar-style 3D character render of Abi, the same young woman as in her other character sheets (long wavy brown hair,
> green eyes, warm smile). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not
> touching the body, feet shoulder-width apart, mouth closed with a confident smile, hair falling behind her shoulders.
> A daring pirate outfit: a dark purple bandana tied over her hair, small gold hoop earrings, a black laced corset top
> with short ruffled cap sleeves, a deep burgundy sash knotted at the hip with long hanging ends, black shorts, black
> fishnet stockings, fingerless black gloves, tall black cuffed boots. Plain light gray background, soft even light, no
> shadow, whole body in frame.

### P3. Kira, otter — `kira_otter.png`
> Pixar-style 3D render of Kira, a tiny gray Scottish Fold kitten transformed into a cute little river otter. Full body,
> strict side profile: the whole body and the head seen exactly from the side, nose pointing at the left edge of the
> image, only one eye visible, not looking at the camera. Standing on four short legs with webbed paws, a long sleek
> otter body and a thick tapering otter tail. A round cat face with a small nose and whiskers, folded-down ears that
> lie flat and forward, never raised. Sleek solid gray fur everywhere with absolutely no white fur or markings.
> Golden-amber eyes. Mouth closed. Plain light gray background, soft even light, no shadow, whole body in frame with
> every paw and the tail visible.

### P4. Muscle Man, pirate — `muscleman_pirate.png`
> Pixar-style 3D character render of Muscle Man, the same huge, extremely muscular, cheerful man as in his Medieval
> character sheet (brown hair swept back into a small ponytail, a short brown beard). Full body, front view, A-pose:
> arms about 45 degrees out from the body, hands open and not touching the body, feet shoulder-width apart, mouth
> closed in a big grin. A pirate brute's outfit: a weathered dark brown sleeveless leather vest laced up the front over
> an open cream linen shirt, a thick coil of rope wrapped around his waist as a belt over a leather belt, dark brown
> leather bracers, worn dark brown trousers with patches and a torn knee, tall cuffed boots. Plain light gray
> background, soft even light, no shadow, whole body in frame.

### P5. The Violinist, pirate fiddler — `violinist_pirate.png`
> Pixar-style 3D character render of the Violinist, the same older gray-haired, gray-bearded man as in his other
> character sheets (same face and hair). Full body, front view, A-pose: arms about 45 degrees out from the body, hands
> open and not touching the body, feet shoulder-width apart, mouth closed with a calm knowing smile. A ship's fiddler
> outfit: a loose cream linen shirt with rolled sleeves, a worn brown leather vest, a colorful patchwork sash at the
> waist, faded black and cream striped trousers with a patch on one knee, a beaded bracelet, tall cuffed brown boots.
> Holding nothing (no instrument). Plain light gray background, soft even light, no shadow, whole body in frame.

### P6. Pirate deckhand (enemy) — `pirate.png`
> Pixar-style 3D character render of a goofy cartoon pirate deckhand for a family video game, not scary: round belly,
> a red and white striped shirt, a black eyepatch, a scruffy ginger beard, a blue bandana, a wide belt, baggy trousers,
> bare feet with one wooden peg leg. Full body, front view, A-pose: arms about 45 degrees out, hands open and empty,
> feet apart, mouth closed, a cheeky grin. Plain light gray background, soft even light, no shadow, whole body in frame.

### P7. Crab (enemy) — `crab.png`
> Pixar-style 3D render of a cute grumpy cartoon crab for a family video game, not scary: a round red-orange shell, big
> eyes on stalks, one claw much bigger than the other, little pointy legs. Front view, standing on its legs, claws held
> up. Plain light gray background, soft even light, no shadow, whole body in frame.

### P8. Pufferfish (underwater enemy) — `pufferfish.png`
> Pixar-style 3D render of a cute cartoon pufferfish for a family video game, not scary: puffed up into a round spiky
> ball, yellow with brown spots, big worried eyes, tiny fins and tail. Strict side profile, only one eye visible. Plain
> light gray background, soft even light, no shadow, whole body in frame.

### P9. Tropical fish (friendly, schools) — `fish.png`
> Pixar-style 3D render of a cute cartoon tropical fish like in a family animated film: a round body, orange with bright
> red and yellow stripes, a pale belly, big friendly eyes, a happy smile, flowing fins. Strict side profile, only one
> eye visible. Plain light gray background, soft even light, no shadow, whole body in frame.

Built by Code, no image: the pirate ship (deck, masts, sails, rigging, crow's nest, cannons, barrels, crates, the
treasure chest), the island, the reef, corals, the shipwreck, the green story shard, the fiddle.
Mixamo after Meshy: `dan_pirate`, `abi_pirate`, `muscleman_pirate`, `violinist_pirate`, `pirate` (all with the shared
clips, plus swimming for the four heroes and Muscle Man).


## Round 8 (2026-11-11): Mixamo for the Medieval Kingdom — DONE (same night)

**In:** all seven rigs and every clip (Dan's picks: "Mutant Flexing Muscles" = flex, "Pointing" = taunt, "Lifting Object" = lift,
"Pull Heavy Object" = the stone heave ("Pulling A Rope" spare), "Throw" = boost, "Standing Aim Idle 01" = aim, "Drunk Idle" = the
driver dazed; "Sword And Shield Idle (1)" is a second idle, unused). The Violinist rigged without his cape; `attach_cape.py` put it
back (`rigged.fbx`). Abi's mesh closed (`close_person.py`, 0 open edges). All seven valid Humanoid avatars in Unity; clip renders in
`Data/screenshots/round8/mixamo/`. Skeletons differ in fingers (65 / 49 / 41 / 33 bones): fine, the Violinist has the index and middle
fingers his lute hold uses.

What was asked (kept for the record):

All 10 Medieval images are Meshy models (fronts: `Data/screenshots/round8/models/`). The seven people go through Mixamo:

| Model | Upload this (mixamo.com > Upload Character) | Height |
|---|---|---|
| Dan, Medieval | `SourceArt/Mixamo/dan_medieval/upload/dan_medieval.fbx` | 1.8 m |
| Abi, Medieval | `SourceArt/Mixamo/abi_medieval/upload/abi_medieval.fbx` | 1.65 m |
| Muscle Man | `SourceArt/Mixamo/muscleman_medieval/upload/muscleman_medieval.fbx` | 2.0 m |
| The Violinist, bard | **`SourceArt/Mixamo/violinist_medieval/upload/violinist_medieval_nocape.fbx`** (no cape: the long cape wouldn't rig cleanly; Code puts it back) | 1.85 m |
| Knight | `SourceArt/Mixamo/knight/upload/knight.fbx` | 1.6 m |
| Archer | `SourceArt/Mixamo/archer/upload/archer.fbx` | 1.8 m |
| Wagon driver | `SourceArt/Mixamo/wagon_driver/upload/wagon_driver.fbx` | 1.65 m |

For **each of the seven**:
1. **Upload**, then the auto-rigger: markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**.
   (Dan's and the Violinist's cloaks hang to the shins: put the knee markers where the knees are under the cloak.)
2. **Download the rigged character** with no animation picked: **FBX for Unity (.fbx)**, **With Skin**, **T-pose**.
   Save as `SourceArt/Mixamo/<model>/T-Pose.fbx` (next to `upload`, not inside it).
   **The Violinist:** upload the no-cape file (Dan, 2026-11-11: the cape wouldn't rig). The cape was split off by
   `Tools/Blender/split_cape.py` (renders `Data/screenshots/round8/cape/`); after your `T-Pose.fbx` is in, Code runs
   `Tools/Blender/attach_cape.py violinist_medieval`: the cape goes back on the spine and shoulder bones as a rigid piece (no arm
   or leg bones, no cloth, so it never stretches), inside faces added, one mesh -> `rigged.fbx`, which the import prefers.

Then the **clips**: each **FBX for Unity, 30 fps, Keyframe Reduction none, Without Skin, In Place ticked where offered**. Any of
the seven can be on screen while you download (Unity retargets). Save each with the file name in the first column, in the folder
given. If a search name isn't there, take the closest one that fits "For" and save it under the first column's name anyway.

| Save as | Mixamo search | Folder | For |
|---|---|---|---|
| `Flexing.fbx` | **Flexing** (if missing: **Victory Idle**) | `SourceArt/Mixamo/muscleman_medieval/` | Muscle Man shows off |
| `Taunt.fbx` | **Taunt** (pointing and laughing; else **Pointing**) | `SourceArt/Mixamo/muscleman_medieval/` | mocking Dan |
| `Lifting.fbx` | **Lifting** (a heavy thing up over the head; else **Picking Up**) | `SourceArt/Mixamo/muscleman_medieval/` | lifts a boulder / cart / knight; portcullis |
| `Pushing.fbx` | **Pushing** (copy the one already in `SourceArt/Mixamo/` if you like: same clip) | `SourceArt/Mixamo/muscleman_medieval/` | moving boulders |
| `Pulling.fbx` | **Pulling** (straining, both hands; else **Tug Of War**) | `SourceArt/Mixamo/muscleman_medieval/` | the failed heave at the stone |
| `Falling Back Death.fbx` | **Falling Back Death** (falls flat on his back) | `SourceArt/Mixamo/muscleman_medieval/` | falls flat at the stone |
| `Victory.fbx` | **Victory** | `SourceArt/Mixamo/muscleman_medieval/` | his big cheer |
| `Toss.fbx` | **Toss** (two hands, upward; else **Throwing**) | `SourceArt/Mixamo/muscleman_medieval/` | boosting Dan up to a ledge |
| `Kneeling.fbx` | **Kneeling** (going down on one knee; else **Kneel**) | `SourceArt/Mixamo/muscleman_medieval/` | "Master!" |
| `Sword And Shield Idle.fbx` | **Sword And Shield Idle** | `SourceArt/Mixamo/knight/` | knight standing guard |
| `Sword And Shield Walk.fbx` | **Sword And Shield Walk** (In Place) | `SourceArt/Mixamo/knight/` | knight patrol |
| `Sword And Shield Slash.fbx` | **Sword And Shield Slash** | `SourceArt/Mixamo/knight/` | knight's swing |
| `Sword And Shield Block.fbx` | **Sword And Shield Block** | `SourceArt/Mixamo/knight/` | shield up |
| `Standing Draw Arrow.fbx` | **Standing Draw Arrow** | `SourceArt/Mixamo/archer/` | archer reloads |
| `Standing Aim Overdraw.fbx` | **Standing Aim Overdraw** (else **Standing Aim Idle**) | `SourceArt/Mixamo/archer/` | aiming |
| `Standing Aim Recoil.fbx` | **Standing Aim Recoil** | `SourceArt/Mixamo/archer/` | the shot |
| `Nervously Look Around.fbx` | **Nervously Look Around** | `SourceArt/Mixamo/archer/` | the nervous archer |
| `Dizzy Idle.fbx` | **Dizzy Idle** (else **Drunk Idle** / **Stunned**) | `SourceArt/Mixamo/wagon_driver/` | the singed driver after the blast |
| `Injured Walk.fbx` | **Injured Walk** (else **Drunk Walk**) | `SourceArt/Mixamo/wagon_driver/` | he staggers out |

**Nothing else:** walking, running, idle, jumping, punching, laughing, cheering, sitting (the driver on the wagon), surprised (Dan
at the stone) are the shared clips already in `SourceArt/Mixamo/`. The Violinist plays his lute with a clip made by code (like the
lyre). Sword, shield, bow and lute are separate props. Abi's long hair: if Mixamo ties it to her arms, Code fixes it. Until the
files are in, the world uses placeholders; when they are, tell Code (or Code picks them up later this round).

## Round 8 (2026-11-10): the Medieval Kingdom — images needed from Dan (prompts by the planning chat)

Film art: `SourceArt/Reference/Characters/*/…-medieval-film.png`, `MuscleMan/muscleman-medieval-film.png`,
`Kira/kira-dragon-film.png`, `Violinist/bard-*.png`, and the world in `Worlds/04-Medieval/`. Same rules and negative
prompt as before. People: front view, A-pose, empty hands. Animals: strict side profile. Save in `SourceArt/Concepts/`.

### M1. Dan, Medieval — `dan_medieval.png`
> Pixar-style 3D character render of Dan, the same young man as in his other character sheets (short swept-back
> auburn-brown hair, friendly face). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open
> and not touching the body, feet shoulder-width apart, mouth closed with a gentle smile, looking straight ahead.
> Medieval adventurer's clothes: a short-sleeved brown leather tunic with frayed edges, a dusty pink cloth sash tied at
> the waist with hanging ends, a charcoal-black hooded cloak over the shoulders, gray-brown trousers, brown open-toed
> leather boot sandals. No necklace or pendant. Plain light gray background, soft even light, no shadow, whole body in
> frame.

### M2. Abi, Medieval — `abi_medieval.png`
> Pixar-style 3D character render of Abi, the same young woman as in her other character sheets (long wavy brown hair,
> green eyes, warm smile). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not
> touching the body, feet shoulder-width apart, mouth closed with a gentle smile, hair falling behind her shoulders.
> Medieval gown: a sky-blue fitted dress to mid-calf with long bell sleeves, gold trim along the square neckline, down
> the front and around the sleeves and hem, a brown leather belt with a buckle, a thin gold headband, tall brown leather
> boots. Plain light gray background, soft even light, no shadow, whole body in frame.

### M3. Kira, baby dragon — `kira_dragon.png`
> Pixar-style 3D render of Kira, a tiny gray Scottish Fold kitten transformed into a cute baby dragon. Full body, strict
> side profile: the whole body and the head seen exactly from the side, nose pointing at the left edge of the image,
> only one eye visible, not looking at the camera. Standing on all four legs. A round cat face with a small nose and
> whiskers, folded-down ears that lie flat and forward, never raised, two tiny nub horns, small gray bat-like wings
> folded half open on her back, a row of little soft spikes down the back and a short dragon tail. Covered in soft
> solid gray scales, with a lighter gray scaled belly; absolutely no white anywhere. Golden-amber eyes. Mouth closed.
> Plain light gray background, soft even light, no shadow, whole body in frame with every paw and the tail visible.

### M4. Muscle Man, Medieval — `muscleman_medieval.png` (the new companion)
> Pixar-style 3D character render of Muscle Man, a huge, extremely muscular, cheerful medieval strongman: a big grin,
> a square jaw with a short brown beard, brown hair swept back into a small ponytail, a broad chest and enormous arms.
> Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not touching the body, feet
> shoulder-width apart, mouth closed in a big confident smile. A dark navy padded vest over a gray linen shirt, dark
> steel shoulder plates, two brown leather straps crossing his chest with silver buckles, a brown leather belt, dark
> steel bracers on his forearms, tan trousers, brown leather boots. Plain light gray background, soft even light, no
> shadow, whole body in frame.

### M5. The Violinist, Medieval bard — `violinist_medieval.png`
> Pixar-style 3D character render of the Violinist, the same older gray-haired, gray-bearded man as in his other
> character sheets (same face and hair). Full body, front view, A-pose: arms about 45 degrees out from the body, hands
> open and not touching the body, feet shoulder-width apart, mouth closed with a calm knowing smile. A medieval bard's
> outfit: a hooded brown traveling cloak, a blue-gray tunic with an embroidered hem over white sleeves, dark gray
> leggings, soft brown boots with fur cuffs. Holding nothing (no instrument). Plain light gray background, soft even
> light, no shadow, whole body in frame.

### M6. Knight (enemy) — `knight.png`
> Pixar-style 3D character render of a clumsy cartoon knight for a family video game, not scary: short and stocky in
> slightly too-big shiny armor, a round helmet with the visor up showing a small grumpy face and a big mustache, a red
> and gold tabard over the armor. Full body, front view, A-pose: arms about 45 degrees out, hands open and empty, feet
> apart, mouth closed. Plain light gray background, soft even light, no shadow, whole body in frame. (Sword and shield
> are separate props.)

### M7. Archer (enemy) — `archer.png`
> Pixar-style 3D character render of a skinny, nervous cartoon castle archer for a family video game, not scary: a
> leather cap with a feather, a green tunic, a brown leather jerkin, a quiver strap across the chest, brown boots, a
> long thin nose and a worried look. Full body, front view, A-pose: arms about 45 degrees out, hands open and empty,
> feet apart, mouth closed. Plain light gray background, soft even light, no shadow, whole body in frame. (The bow is a
> separate prop.)

### M8. Rat (dungeon enemy) — `rat.png`
> Pixar-style 3D render of a chubby cheeky cartoon rat for a family video game, not scary: gray-brown fur, big round
> ears, big cartoon eyes, a pink nose and a long pink tail. Strict side profile: the whole body and the head seen
> exactly from the side, nose pointing at the left edge of the image, only one eye visible. Standing on all four legs.
> Plain light gray background, soft even light, no shadow, whole body in frame.

### M9. Wagon horse — `horse.png`
> Pixar-style 3D render of a sturdy, goofy cartoon farm horse: chestnut brown with a cream blaze down the face, a shaggy
> mane, fluffy feathered hooves, a simple leather harness, a slightly dopey expression. Strict side profile: the whole
> body and the head seen exactly from the side, nose pointing at the left edge of the image, only one eye visible.
> Standing on all four legs. Plain light gray background, soft even light, no shadow, whole body in frame.

### M10. Wagon driver — `wagon_driver.png`
> Pixar-style 3D character render of a sleepy old cartoon peasant wagon driver: a round red nose, bushy white eyebrows,
> a floppy brown felt hat, a patched brown tunic and a rope belt, baggy trousers, worn boots. Full body, front view,
> A-pose: arms about 45 degrees out, hands open and empty, feet apart, mouth closed. Plain light gray background, soft
> even light, no shadow, whole body in frame.

Built by Code, no image: the castle, courtyard, the stone pedestal with the purple shard, the hay wagon, torches,
benches, flower pots, the lute, sword, shield, bow, boulders.
Mixamo after Meshy: `dan_medieval`, `abi_medieval`, `muscleman_medieval` (plus his own clips: lift, push, throw,
flex, cheer, fall on his back), `violinist_medieval`, `knight`, `archer`, `wagon_driver`.


## Round 7 (2026-11-10): Mixamo for Bethlehem — DONE (Fix 7.1)

**Fix 7.1:** all in. Dan's seven rigged people and eight clips are imported; "Carrying" plays as "offer", "Looking Down" as "map",
"Standing Arguing" as "argue", "Yelling Out" as "yell" (`import_characters.py EXTRA_CLIPS` takes either name). The side-profile
guard dog became a new Meshy model (head turn -1 deg, was +51; old image `guard_dog_v1.png`, old model `SourceArt/Meshy/guard_dog_v1`)
rigged by `kira_rig.py`. Nothing else is waiting on Dan for Bethlehem.

What was asked (kept for the record):

All 12 Bethlehem images became Meshy models (previews `Data/screenshots/round7/models/`). Seven are people and go through
Mixamo, the same steps as Egypt. The upload files are ready:

| Model | Upload this (mixamo.com > Upload Character) | Height |
|---|---|---|
| Dan, Bethlehem | `SourceArt/Mixamo/dan_bethlehem/upload/dan_bethlehem.fbx` | 1.8 m |
| Abi, Bethlehem | `SourceArt/Mixamo/abi_bethlehem/upload/abi_bethlehem.fbx` | 1.65 m |
| The Violinist, Bethlehem | `SourceArt/Mixamo/violinist_bethlehem/upload/violinist_bethlehem.fbx` | 1.85 m |
| Wise Man in the Santa hat | `SourceArt/Mixamo/wiseman_santa/upload/wiseman_santa.fbx` | 1.8 m |
| Wise Man, young king | `SourceArt/Mixamo/wiseman_crown/upload/wiseman_crown.fbx` | 1.85 m |
| Wise Man, old king | `SourceArt/Mixamo/wiseman_old/upload/wiseman_old.fbx` | 1.75 m |
| Night watchman | `SourceArt/Mixamo/watchman/upload/watchman.fbx` | 1.75 m |

For **each of the seven**:
1. **Upload**, then the auto-rigger: markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**. Next, Next.
   (The Wise Men's and the Violinist's robes reach the ankles: put the knee markers where the knees would be under the robe.)
2. **Download the rigged character** with no animation picked: Format **FBX for Unity (.fbx)**, **With Skin**, **T-pose**.
   Save it next to `upload` (not inside it): `SourceArt/Mixamo/<model>/T-Pose.fbx`.

Then the **clips** (each: FBX for Unity, 30 fps, Keyframe Reduction none, **Without Skin**, **In Place** ticked where offered).
Any one of the seven can be on screen while you download a clip (Unity retargets it to everyone). Save each under the name in
the first column:

| Save as | Mixamo search | Where to save | For |
|---|---|---|---|
| `Sitting Idle.fbx` | **Sitting Idle** | `SourceArt/Mixamo/wisemen/` | the Wise Men sitting on their camels |
| `Arguing.fbx` | **Arguing** | `SourceArt/Mixamo/wisemen/` | they stop on the road and argue about the way |
| `Looking At Map.fbx` | **Reading** (a book or paper held in both hands; if there's nothing like it: **Looking Down**) | `SourceArt/Mixamo/wisemen/` | checking the map |
| `Offering.fbx` | **Holding Idle** / **Carrying** (both hands held out in front, as if holding a box) | `SourceArt/Mixamo/wisemen/` | gifts held out in the barn (the finale) |
| `Disappointed.fbx` | **Disappointed** (if missing: **Sad Idle**) | `SourceArt/Mixamo/wisemen/` | they look at each other and leave |
| `Yelling.fbx` | **Yelling** | `SourceArt/Mixamo/watchman/` | the watchman's "Halt!" |
| `Sleeping Idle.fbx` | **Sleeping Idle** (standing, nodding off; if only lying ones: **Bored**) | `SourceArt/Mixamo/watchman/` | a watchman dozing at his post |
| `Surprised.fbx` | **Surprised** (if missing: **Shocked** or **Reaction**) | `SourceArt/Mixamo/` (the shared people clips) | Dan and Abi seeing lamb Kira; Dan hitting the floor |

**Nothing else to download:** walking, running, idle, jumping and the rest are the shared clips already in `SourceArt/Mixamo/`;
the watchman walks his rounds with them; the Violinist plays his harp with a clip made by code (like the lyre). The Wise Men's
gift boxes and the watchman's lantern are separate props held by code. Abi's long hair: if Mixamo ties it to her arms, Code fixes it.
When the files are there, tell Code: they go into the game this same round. Until then Bethlehem shows Dan and Abi in their Egypt
models, the Violinist in his Egypt model with the harp, and placeholder Wise Men and watchmen.

**The guard dog needs a new image (item 1's check):** its head is turned about 51 deg toward the camera (the limit is ~35; its nose
sits 7 cm off the body's middle line). The goat, the sheep and lamb Kira are straight. Please make `guard_dog.png` again (keep the old
one as `guard_dog_v1.png`), same prompt (B11) with the profile rule the goat and sheep redo used:
> ... Strict side profile: the whole dog seen exactly from the side, the head in profile too, its nose pointing at the left edge of
> the image, only one eye visible, not looking at the camera. ...

Until the new image is in, guard dogs are placeholders.

## Round 7 (2026-11-10): Bethlehem — images needed from Dan (prompts by the planning chat)

Film art: `SourceArt/Reference/Characters/*/…-bethlehem-film.png`, `kira-lamb-film.png`, `Others/wise-men-*.png`,
and the world in `Worlds/03-Bethlehem/`. Same rules and negative prompt as Egypt (above). People: front view, A-pose,
empty hands (the Wise Men's gift boxes and the Violinist's instrument are separate props). Save in `SourceArt/Concepts/`.

### B1. Dan, Bethlehem — `dan_bethlehem.png`
> Pixar-style 3D character render of Dan, the same young man as in his other character sheets (short swept-back
> auburn-brown hair, friendly face). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open
> and not touching the body, feet shoulder-width apart, mouth closed with a gentle smile, looking straight ahead.
> Simple ancient shepherd's clothes: a rough short-sleeved brown homespun tunic to the knees with frayed edges and a
> wide neckline, a thick cream cloth sash tied at the waist with hanging ends, black flip-flop sandals. No necklace or
> pendant. Plain light gray background, soft even light, no shadow, whole body in frame.

### B2. Abi, Bethlehem — `abi_bethlehem.png`
> Pixar-style 3D character render of Abi, the same young woman as in her other character sheets (long wavy brown hair,
> green eyes, warm smile). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not
> touching the body, feet shoulder-width apart, mouth closed with a gentle smile. Simple ancient village clothes: a long
> beige linen dress with wide sleeves to the ankles, a soft cream head scarf draped over her hair and around her
> shoulders with her hair showing at the front, a tan cloth belt, brown leather sandals. Plain light gray background,
> soft even light, no shadow, whole body in frame.

### B3. Kira, lamb — `kira_lamb.png`
> Pixar-style 3D render of Kira, a tiny gray Scottish Fold kitten transformed into a cute little lamb. Full body, side
> view, standing on all four legs, head facing straight ahead in line with her body (not turned to the camera). A round
> fluffy body of curly solid gray wool, short gray legs with little cat paws, a smooth round gray cat face with a small
> nose and whiskers, soft folded-down ears that lie flat and forward, never raised. Solid gray everywhere with
> absolutely no white wool, fur or markings. Golden-amber eyes. Mouth closed. Plain light gray background, soft even
> light, no shadow, whole body in frame with every paw and the little tail visible.

### B4. Wise Man 1 (the one in the Santa hat) — `wiseman_santa.png`
> Pixar-style 3D character render of a funny cartoon Wise Man from a nativity play: big round nose, a thick black
> beard, wearing a red Santa hat with a white pom-pom, a long maroon-brown robe with gold trim, a purple-blue scarf
> wrapped round his neck, a gold sash, brown slippers. Full body, front view, A-pose: arms about 45 degrees out, hands
> open and empty, feet apart, mouth closed, cheerful. Plain light gray background, soft even light, no shadow, whole
> body in frame.

### B5. Wise Man 2 (the young king) — `wiseman_crown.png`
> Pixar-style 3D character render of a funny cartoon Wise Man from a nativity play: big round nose, long brown hair and
> a full brown beard, a gold crown with little round points, a long cream robe with gold trim, a deep red mantle over
> the shoulders, brown slippers. Full body, front view, A-pose: arms about 45 degrees out, hands open and empty, feet
> apart, mouth closed, earnest and a little wide-eyed. Plain light gray background, soft even light, no shadow, whole
> body in frame.

### B6. Wise Man 3 (the old king) — `wiseman_old.png`
> Pixar-style 3D character render of a funny cartoon Wise Man from a nativity play: an old man with a big round nose, a
> long fluffy white beard and a white headcloth under a gold crown, a long olive-green robe with gold embroidered trim,
> a gold shoulder cape, a red sash, brown slippers. Full body, front view, A-pose: arms about 45 degrees out, hands open
> and empty, feet apart, mouth closed, kindly. Plain light gray background, soft even light, no shadow, whole body in
> frame.

### B7. The Violinist, Bethlehem — `violinist_bethlehem.png`
> Pixar-style 3D character render of the Violinist, the same older gray-haired, gray-bearded man as in his other
> character sheets (same face and hair). Full body, front view, A-pose: arms about 45 degrees out from the body, hands
> open and not touching the body, feet shoulder-width apart, mouth closed with a calm knowing smile. A shepherd's
> outfit: a long sand-colored wool robe, a striped brown and cream cloak over the shoulders, a rope belt, a cloth
> headband, leather sandals. Holding nothing (no instrument). Plain light gray background, soft even light, no shadow,
> whole body in frame.

### B8. Night watchman (stealth guard) — `watchman.png`
> Pixar-style 3D character render of a sleepy, grumpy cartoon night watchman from an ancient village, not scary: round
> belly, droopy mustache, heavy-lidded eyes, a simple leather helmet, a tunic with a short cloak, sandals. Full body,
> front view, A-pose: arms about 45 degrees out, hands open and empty, feet apart, mouth closed. Plain light gray
> background, soft even light, no shadow, whole body in frame. (His lantern is a separate prop.)

### B9. Grumpy goat (enemy) — `goat.png`
> Pixar-style 3D render of a grumpy cartoon billy goat for a family video game, not scary: shaggy brown and cream coat,
> curled horns, a scruffy beard, narrowed annoyed eyes, head lowered as if about to charge. Side view, standing on all
> four legs, head facing straight ahead, tail visible. Plain light gray background, soft even light, no shadow, whole
> body in frame.

### B10. Sheep (the flock) — `sheep.png`
> Pixar-style 3D render of a cute fluffy cartoon sheep: a round body of creamy white curly wool, a black face and black
> legs, sleepy friendly eyes, small floppy ears. Side view, standing on all four legs, head facing straight ahead.
> Plain light gray background, soft even light, no shadow, whole body in frame.

### B11. Guard dog (enemy) — `guard_dog.png`
> Pixar-style 3D render of a scruffy cartoon village guard dog for a family video game, not scary: lanky, sandy brown
> fur, one ear up and one ear down, a big nose, a suspicious squint, a leather collar. Side view, standing on all four
> legs, head facing straight ahead, tail visible. Plain light gray background, soft even light, no shadow, whole body
> in frame.

### B12. Scorpion (enemy) — `scorpion.png`
> Pixar-style 3D render of a small cute cartoon scorpion for a family video game, not scary: glossy amber-orange shell,
> big round cartoon eyes, little pincers held up, a curled tail with a rounded tip. Side view, standing on its legs.
> Plain light gray background, soft even light, no shadow, whole body in frame.

Reused, no new image: the camels (the Wise Men ride Egypt's camel model with plainer blankets), the barn, hay bales,
manger, fences and gift boxes (built by Code from the film shots: Blender / Meshy text-to-3D).
Mixamo after Meshy: `dan_bethlehem`, `abi_bethlehem`, the three Wise Men, `violinist_bethlehem`, `watchman`.

## Round 6 (2026-11-10): Mixamo for Egypt — for Dan, now

Your nine Egypt images became Meshy models (all usable; previews `Data/screenshots/round6/models/`). Four of them are
people-shaped and go through Mixamo, the same steps as the Violinist in Round 5. The upload files are ready:

| Model | Upload this (mixamo.com > Upload Character) | Height |
|---|---|---|
| Dan, Egypt | `SourceArt/Mixamo/dan_egypt/upload/dan_egypt.fbx` | 1.8 m |
| Abi, Egypt | `SourceArt/Mixamo/abi_egypt/upload/abi_egypt.fbx` | 1.65 m |
| The Violinist, Egypt | `SourceArt/Mixamo/violinist_egypt/upload/violinist_egypt.fbx` | 1.85 m |
| Mummy (enemy) | `SourceArt/Mixamo/mummy/upload/mummy.fbx` | 1.75 m |

For **each of the four**:
1. **Upload**, then the auto-rigger: markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**. Next, Next.
2. **Download the rigged character** with no animation picked: Format **FBX for Unity (.fbx)**, **With Skin**, **T-pose**.
   Save it in that model's folder, next to `upload` (not inside it): `SourceArt/Mixamo/<model>/T-Pose.fbx`.

Then the **clips** (each: FBX for Unity, 30 fps, Keyframe Reduction none, **Without Skin**, **In Place** ticked where
offered). Save each under the name in the first column (Mixamo's own name, as you did before):

| Save as | Mixamo search | Where to save | For |
|---|---|---|---|
| `Laughing.fbx` | **Laughing** | `SourceArt/Mixamo/` (the shared people clips) | Dan's relieved laugh in the Ice Age arrival (Round 5 left it out) |
| `HoldUp.fbx` | **Holding up** / **Showing** (a one-hand raise to look at something; if nothing fits: **Looking At Item**) | `SourceArt/Mixamo/` | Dan holding up the necklace in the arrival |
| `Zombie Idle.fbx` | **Zombie Idle** | `SourceArt/Mixamo/mummy/` | the mummy asleep on its feet |
| `Zombie Walk.fbx` | **Zombie Walk** | `SourceArt/Mixamo/mummy/` | the shamble |
| `Zombie Attack.fbx` | **Zombie Attack** | `SourceArt/Mixamo/mummy/` | the grab |
| `Zombie Scream.fbx` | **Zombie Scream** | `SourceArt/Mixamo/mummy/` | waking up |
| `Zombie Reaction Hit.fbx` | **Zombie Reaction Hit** | `SourceArt/Mixamo/mummy/` | hit |

The shared clips (`Laughing`, `HoldUp`) can be downloaded with any of the four on screen (Unity retargets them to everyone).
**Nothing else to download:** Dan and Abi use the shared clip set already in `SourceArt/Mixamo/`; the Violinist plays
his lyre with his Round 5 clips (play / bow / nod, retargeted) and his arms are placed on the lyre by code.
Abi's long hair: if Mixamo ties it to her arms again, Code fixes it (as in Round 2). When the files are there, tell Code:
they go into the game this same round. Until then Egypt shows Dan and Abi in their home models, the Violinist in his Ice
Age model with the lyre, and placeholder mummies.

Not Mixamo (Code's rigs, done this round): mummy Kira, the camel and the temple guardian (four-legged rig in Blender); the
scarab (moved by code); the Sphinx (a statue).

## Round 6 (2026-11-09): Ancient Egypt — images needed from Dan (prompts by the planning chat)

Dan's film art for Egypt is in `SourceArt/Reference/` (characters: `Characters/*/…-egypt-film.png`,
`kira-mummy-film.png`, `Others/camel-judgmental-film.png`, `Violinist/violinist-egypt-lyre-film.png`; the world:
`Worlds/02-Egypt/`). The stills are the look; Meshy still needs clean sheets, so these prompts ask for them. Give the
matching still to the image tool as a reference where it takes one. **The concept sheets are the official design**:
Dan's and Abi's faces and hair follow their existing sheets (`SourceArt/Concepts/dan_iceage.png`, `abi_iceage.png`),
the Violinist's follows `violinist_iceage.png`. Save each as the file named in its heading, in `SourceArt/Concepts/`.

Every image: at least 1024 px tall (animals and the Sphinx: 1536 px wide), the whole body in frame (feet, paws, tail),
plain light background, soft even light, no ground shadow, mouth closed, no glow, no text, one view per image.

Negative prompt, if the tool has one: `cropped, cut off feet, cut off paws, cut off tail, multiple views, necklace,
pendant, held object, instrument, prop, weapon, glowing, open mouth, teeth showing, text, logo, watermark, background
scenery, ground shadow, motion blur, depth of field, photorealistic, scary, horror` (leave out "prop" for the camel,
whose saddle is part of it).

### E1. Dan, Egypt — `dan_egypt.png`
> Pixar-style 3D character render of Dan, the same young man as in his other character sheets (short swept-back
> auburn-brown hair, friendly face). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open
> and not touching the body, feet shoulder-width apart, mouth closed with a gentle smile, looking straight ahead.
> Ancient Egyptian outfit: short-sleeved white linen tunic to the knees, a royal-blue sash from the left shoulder across
> the body to the hip, a broad gold and blue striped collar over the shoulders, a gold belt with a long striped gold
> apron panel hanging at the front, wide gold cuffs on both wrists, black flip-flop sandals. No necklace or pendant.
> Plain light gray background, soft even light, no shadow, whole body in frame.

### E2. Abi, Egypt — `abi_egypt.png`
> Pixar-style 3D character render of Abi, the same young woman as in her other character sheets (long wavy brown hair,
> green eyes, warm smile). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not
> touching the body, feet shoulder-width apart, mouth closed with a gentle smile, hair falling behind her shoulders.
> Ancient Egyptian outfit: white sleeveless knee-length dress, the lower skirt covered in shimmering rose-gold sequins
> with an uneven hem, a thin gold cord belt crossing diagonally, a broad collar of blue beads trimmed with gold, a thin
> gold headband, gold cuffs on the upper arms and wrists, sandals with small white feathered wings at the ankles.
> Plain light gray background, soft even light, no shadow, whole body in frame.

### E3. Kira, mummy cat — `kira_mummy.png`
> Pixar-style 3D render of Kira, a tiny gray Scottish Fold kitten dressed up as a cute little mummy. Full body, side
> view, standing on all four legs, head facing straight ahead in line with her body (not turned to the camera), tail up
> and away from the body. Loose cream linen bandages wrapped around her body, legs, tail and the top of her head, with
> solid gray fur showing between the wraps; her face is uncovered. Folded-down Scottish Fold ears that lie flat and
> forward, never raised, poking out above the head bandage. Solid gray fur with absolutely no white fur or markings.
> Golden-amber eyes. Mouth closed. Plain light gray background, soft even light, no shadow, whole body in frame,
> every paw and the tail visible.

### E4. The judgmental camel — `camel.png`
> Pixar-style 3D render of a cartoon camel with a big personality, side view, standing on all four legs, head facing
> straight ahead, tail down. Sandy tan fur, one hump, a long curved neck, heavy expressive eyebrows and half-closed,
> deeply unimpressed eyes with a smug little smirk, mouth closed. A colorful woven Egyptian saddle blanket in blue,
> turquoise, gold and red with tassels, and a small saddle on the hump. Funny and lovable, not realistic. Plain light
> gray background, soft even light, no shadow, whole body in frame with all four hooves and the tail visible.

### E5. The Violinist, Egypt — `violinist_egypt.png`
> Pixar-style 3D character render of the Violinist, the same young man as in his Ice Age character sheet (same face and
> hair). Full body, front view, A-pose: arms about 45 degrees out from the body, hands open and not touching the body,
> feet shoulder-width apart, mouth closed with a calm knowing smile. Ancient Egyptian musician's outfit: white linen
> kilt with a striped blue and gold front panel, a broad blue and gold striped collar, gold bands on the upper arms,
> wrists and ankles, simple leather sandals. Holding nothing (no instrument). Plain light gray background, soft even
> light, no shadow, whole body in frame.

### E6. Mummy (enemy) — `mummy.png`
> Pixar-style 3D render of a cute, clumsy cartoon mummy for a family video game, not scary. Short and round, wrapped in
> loose cream bandages with a few loose ends, one big curious eye peeking out between the wraps, a sleepy grumpy look.
> Full body, front view, A-pose: arms about 45 degrees out, hands open, feet apart. Plain light gray background, soft
> even light, no shadow, whole body in frame.

### E7. Scarab (enemy) — `scarab.png`
> Pixar-style 3D render of a small cute cartoon scarab beetle for a family video game, not scary. Side view, standing
> on its six legs. A round glossy shell in deep blue-green with gold edges, big shiny cartoon eyes, little mandibles,
> a mischievous look. Plain light gray background, soft even light, no shadow, whole body in frame.

### E8. Temple guardian (enemy) — `guardian.png`
> Pixar-style 3D render of an ancient Egyptian temple guardian statue that has come to life: a sandstone cat, sleek
> and tall, with a blue and gold striped collar and gold earrings, carved stone texture with small cracks, stern
> painted eyes. Side view, standing on all four legs, head facing straight ahead, tail down and away from the body.
> Stylized and family friendly, not scary. Plain light gray background, soft even light, no shadow, whole body in frame.

### E9. The Great Sphinx with Kira's face — `sphinx_kira.png` (the big gag: a landmark, not animated)
> Pixar-style 3D render of the Great Sphinx of Egypt, but its face is Kira's: a big round gray Scottish Fold cat face
> carved in the stone, folded-down ears that lie flat and forward under the striped royal headdress, huge golden-amber
> eyes, a small pink nose, whiskers, a calm slightly puzzled expression, mouth closed. Weathered sandstone lion body
> lying down with both front paws stretched forward, on a low stepped stone base. Three-quarter front view, the whole
> statue and base in frame. Plain light gray background, soft even light, no shadow.

When the images are saved: Code checks them (same list as always), then Meshy → clean-up → Mixamo files for
`dan_egypt`, `abi_egypt`, `violinist_egypt` and `mummy` → stops for Dan's Mixamo. Kira, the camel, the scarab and the
guardian are rigged by Code (four-legged / six-legged). The Sphinx is a static landmark (no rig).


## Round 5 (2026-11-09): the Violinist in Mixamo — for Dan, now

His Meshy model is cleaned up (1.85 m, 20.8k triangles, A-pose, facing front; previews
`Data/screenshots/round5/models/sheet_violinist_iceage.jpg`). Same steps as Dan and Abi in Round 2:

1. **Upload** at mixamo.com (Upload Character): `SourceArt/Mixamo/violinist_iceage/upload/violinist_iceage.fbx`
2. **Auto-rigger:** markers on chin, wrists, elbows, knees, groin. **Skeleton LOD: Standard Skeleton (65)**. Next, Next.
3. **Rigged character:** download it first with no animation picked: Format **FBX for Unity (.fbx)**, **With Skin**,
   **T-pose**. Save as `SourceArt/Mixamo/violinist_iceage/rigged.fbx`.
4. **Clips:** each one FBX for Unity, 30 fps, Keyframe Reduction none, **Without Skin**, **In Place** ticked where offered.
   Save next to the `upload` folder (not inside it), renamed to the file name below:

| File | Mixamo search | For |
|---|---|---|
| `play.fbx` | **Playing Violin** (if nothing: search "violin"; if still nothing, take **Idle** again and say so: Code animates the bow arm) | playing the tune that teaches a move |
| `bow.fbx` | **Quick Formal Bow** (or **Bowing**) | a small bow at the end of the tune |
| `nod.fbx` | **Head Nod Yes** | a knowing nod to the hero who learned the move |

**Idle and walk: reuse the shared clips** already in `SourceArt/Mixamo/` (`Idle.fbx`, `Walking.fbx`; Humanoid retarget, as for Dan and Abi). Dan doesn't download them again (Dan, 2026-11-09).

When the files are there, tell Code: they go into the game this same round (Round 5).


## Round 4 (2026-11-09): Ice Age creatures and the Violinist: images needed from Dan

**Ready for Dan.** Four concept images, made like the six character sheets (same rules as "What makes an image usable"
below): at least 1024 px tall, the whole body in frame (every paw, foot and tail), plain light background, soft even
light, no ground shadow, no props, mouth closed, no glow, no text, one view per image. **Animals: side view**, standing on
all fours, tail away from the body; a second image from the three-quarter front (`<name>_front.png`) helps Meshy a lot.
**The Violinist: front view, A-pose** (he goes through Mixamo like Dan and Abi). Save each as the file named in its heading.
The game uses stand-in shapes for all four until the models arrive (next round: Meshy, clean-up, rigs, as before).

Negative prompt, if your tool has one: `cropped, cut off feet, cut off paws, cut off tail, multiple views, held object,
prop, weapon, instrument, saddle, rider, glowing, open mouth, teeth showing, text, logo, watermark, background scenery,
snow on the ground, ground shadow, motion blur, depth of field, photorealistic, scary, horror`.

They must look like they live in the same film as Kira and the heroes: stylized, rounded, colourful, Pixar-like, never
realistic or scary. The valley's palette (Dan's reference `SourceArt/Reference/Worlds/01-IceAge/iceage-valley-volcanoes.jpg`):
snow white and pale blue, warm brown rock, orange lava glow.

### 7. Wolf — `SourceArt/Concepts/wolf.png` (+ `wolf_front.png`)

> Full-body character concept of a prehistoric Ice Age wolf in the style of a Pixar animated film, side view, standing on
> all four legs, tail held low and away from the body, whole animal in frame from the tip of the nose to the tip of the
> tail and the bottoms of all four paws, with space around it. A lean, shaggy wolf with thick blue-grey and charcoal fur,
> a pale cream belly and muzzle, a bushy tail, big expressive yellow eyes with a sly, mischievous look, ears up and
> alert, long legs with big paws. Cartoon proportions: a slightly oversized head and paws, rounded shapes. Mouth closed.
> Menacing in a funny way, not scary. No collar, nothing glowing. Stylized, colourful, Pixar-like 3D render, soft even
> studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** the Ice Age's main enemy. Wolves come in twos or threes, circle the player at a distance, then lunge one
at a time (light combat: a punch, kick or claw swipe chases one off). About **0.9 m** at the shoulder, **1.6 m** nose to
tail, so it reads as a threat next to Dan (1.8 m) and huge next to Kira (0.5 m). Its fur must not look like Kira's solid
grey (blue-grey with a cream belly). Not Mixamo: a four-legged rig by Code in Blender (as Kira's). Animations: idle,
walk, trot (circling), run, lunge (attack), hurt, stunned (Dan's ground pound), flee.

### 8. Snow critter — `SourceArt/Concepts/snow_critter.png` (+ `snow_critter_front.png`)

> Full-body character concept of a small round Ice Age snow critter in the style of a Pixar animated film, side view,
> standing on its two short stubby legs, whole creature in frame from the top of its head to its feet and the tip of its
> little tail, with space around it. Its body is almost a ball of thick fluffy white-and-pale-blue fur, about the size of
> a beach ball, with a grumpy little face: two small dark eyes under heavy furry eyebrows set in a permanent frown, a
> small pink nose, a short flat mouth, closed. Two tiny round ears, two short stubby legs with big flat feet, a short
> fluffy tail. A tuft of frosty fur sticks up on top of its head. Cute but cross, like a pom-pom with a bad attitude.
> Nothing glowing. Stylized, colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no
> shadows on the ground, no text.

**Design note:** a new creature for the game (not in the film): the simple patrolling enemy. It waddles back and forth
on a path, bumps into you if you're in the way, and pops in a puff of snow when hit. About **0.6 m** tall, so Kira looks
it in the eye. Animations (Code's Blender rig: two legs, but not a person, so not Mixamo): idle (huffing), waddle, bump
(attack), hurt, poof (defeated), stunned.

### 9. Mammoth — `SourceArt/Concepts/mammoth.png` (+ `mammoth_front.png`)

> Full-body character concept of a woolly mammoth in the style of a Pixar animated film, side view, standing on all four
> legs, trunk hanging relaxed in a gentle curl, whole animal in frame from the tip of the trunk to the tail and the
> bottoms of all four feet, with space around it. Huge and gentle: long shaggy warm reddish-brown fur hanging in thick
> locks, a darker brown face, small kind dark eyes, a calm sleepy expression, big curved cream tusks curling up and
> inward, small rounded ears. Important: a **broad, flat, level back** with the fur lying flat on top, like a big woolly
> table, so characters can stand on it. Thick round legs. Mouth closed. No saddle, no rider, nothing on its back,
> nothing glowing. Stylized, colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no
> shadows on the ground, no text.

**Design note:** the mammoths on the ridge are friendly: a slow herd that walks a fixed route, and the heroes ride on
their backs (moving platforms) to reach high shelves. About **3.5 m** at the shoulder, **6 m** long; the flat back about
**2 m x 3.5 m**. In the film they walk through the blowing snow in the background. Four-legged rig by Code in Blender.
Animations: idle (trunk sway, ear flap), walk (slow and steady: the back must stay level enough to stand on), trumpet.

### 10. The Violinist, Ice Age — `SourceArt/Concepts/violinist_iceage.png`

No film picture of him is in `SourceArt/Reference/Characters/Violinist/` yet: if you have one, please save it there and
give it to your image tool (his face should stay the same in every era).

> Full-body character concept of a mysterious travelling musician in the style of a Pixar animated film, standing in an
> A-pose: arms held about 45 degrees out from his sides, hands open and relaxed, not touching his body, feet
> shoulder-width apart, front view, whole body in frame from the top of his head to his feet with space around him. A
> tall, slim older man with a kind, knowing face, a neat silver-grey beard and swept-back silver hair, bright pale-blue
> eyes, a faint calm smile with his mouth closed. Ice Age clothes: a short hooded cloak of shaggy white-and-grey fur
> thrown back from his head, ending at his hips, a simple tan hide tunic tied with a leather cord, fur-wrapped lower legs
> and feet. Something about him doesn't quite belong in the Stone Age: his clothes are a little too neat. Nothing in his
> hands (his bone fiddle is its own model), no jewellery, nothing glowing. Stylized, colourful, Pixar-like 3D render,
> soft even studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** he appears in every world and teaches a move: he plays a short tune, and the hero who reached him learns
it (in the Ice Age: Dan's ground pound). He never speaks or explains himself. The cloak ends at the hips and stays away
from the arms, so Mixamo's rig doesn't tie it to them. Height **1.85 m** (Round 5: Mixamo steps at the top of this page). Mixamo: Humanoid. Clips: **Idle** (`idle.fbx`);
for playing, search **"Playing Violin"** (`play.fbx`); if Mixamo has nothing by that name, download **Idle** only and tell
Code, who will animate the bow arm in Blender. His **bone fiddle** (a hollow bone body, gut strings, a curved stick bow)
is made by Code in Blender, like Dan's necklace: no image needed.

### Props (no images needed)

Rocks, cliffs, ice, snow drifts, bones, pine trees, standing stones, the cave mouth, the volcanoes and the **dark silver
glass bauble** (the ornament that shatters in Act I) are made by Code: terrain and ice by Blender script, props as simple
stylized shapes. Meshy's text-to-3D stays an option for props later if they need more detail.

---

## Mixamo checklist for Dan (Round 2, 2026-11-09)

Four uploads, one per person. For each one:

1. **Upload** at mixamo.com (Upload Character): `SourceArt/Mixamo/<model>/upload/<model>.fbx`
   - `dan_home`, `dan_iceage`, `abi_home`, `abi_iceage`
2. **Auto-rigger:** the model should show facing you. Place the markers: chin, wrists, elbows, knees, groin.
   **Skeleton LOD: Standard Skeleton (65)** (fingers included). Next, then Next again.
3. **Download the clips** in the table at the bottom of this page ("Dan and Abi (Mixamo, step 4)"):
   - `dan_iceage`, `abi_iceage`: all their rows (`rigged` + 14 clips each: Dan has `push`, Abi has `double_jump`).
   - `dan_home`, `abi_home`: the first ten rows only (`rigged` ... `cheer`).
   Search the name, click it, tick **In Place** if the clip shows it, then **Download**.
4. **Download settings:** Format **FBX for Unity (.fbx)**, Frames per Second **30**, Keyframe Reduction **none**.
   Skin: first download the rigged character itself, **With Skin**, in its **T-pose** (no animation picked) as
   `rigged.fbx`. Then every clip **Without Skin**.
5. **Save as** `SourceArt/Mixamo/<model>/<file>`, for example `SourceArt/Mixamo/dan_iceage/run.fbx`
   (next to the `upload` folder, not inside it). Rename Mixamo's download to the file name in the table.

If a search finds nothing, take the closest clip and keep the table's file name. Then tell Code; Round 3 imports them.

**Status after Round 2 (2026-11-09): Mixamo is done for all four.** What Dan downloaded, and what Code did with it:
- Rigged T-poses (With Skin): `SourceArt/Mixamo/dan_home/T-Pose (2).fbx`, `dan_iceage/T-Pose (3).fbx`,
  `abi_home/T-Pose.fbx`, `abi_iceage/T-Pose (1).fbx`. Skeletons differ in finger detail (Dan 41 bones, Abi home 41,
  Abi Ice Age 33); fine for Unity's Humanoid rig.
- **One clip set for all four** (27 files in `SourceArt/Mixamo/`, Without Skin): Unity's Humanoid retargeting plays the
  same clips on every person. Round 3 maps them to the table's names (Fast Run -> run, Box Jump / Jump -> jump_up, ...).
- **Abi's hair:** Mixamo tied her long back hair to her arm bones (it stretched out with the arms).
  `Tools/Blender/fix_hair_weights.py` gives the hair its own weights (head / neck / spine) and cuts the few faces that joined
  hair to arm: `abi_iceage/rigged_hairfix.fbx` and `abi_home/rigged_hairfix.fbx` are the files Round 3 imports.
- **abi_home was remade** in Meshy (the first one's hair was fused to the sweater, which no weights could fix); the first
  version is kept on disk in `SourceArt/Meshy/abi_home_v1/` and `SourceArt/Mixamo/abi_home/v1/` (not in git).
- **Kira** (both forms): rigged and animated by script (`Tools/Blender/kira_rig.py`; Meshy's rigging is humanoid only):
  23 bones, **no ear bones**, nine clips, three LODs. In the game since 0.2.0.

**Round 3 (2026-11-09): all four people are in the game** (Humanoid, Unity mesh LODs), with the shared clip set mapped as
`idle` Idle, `walk` Walking, `run_slow` Slow Run, `run` Fast Run, `jump` Jump (from take-off), `fall` Falling, `land` Falling
To Landing, `land_hard` Hard Landing, `swim` Swimming, `tread` Treading Water, `push` Push, `punch` Punching, `kick` Mma Kick,
`hurt` Hit On Side Of Body (first 0.6 s), `cheer` Cheering (1), `look` Looking Around. Not used yet: Box Jump, Running Jump,
Jumping, Landing, Cheering, Fall Flat, Falling Down, Hit On Side Of Head, Hit To The Legs, Pushing, Swimming To Edge.

**Round 1, item 12: the first six character models.** Dan, Abi and Kira at home, and in the Ice Age.
**Step 2 is done:** Dan saved six clean full-body sheets (1024 x 1536 or larger: A-pose, white background, no
necklace, every paw and the tail in frame). They are now the concept files in `SourceArt/Concepts/`
(`dan_home`, `dan_iceage`, `abi_home`, `abi_iceage`, `kira`, `kira_sabertooth`). The originals stay in
`SourceArt/Reference/Characters/`. **Next is step 3 (Meshy), in Round 2.** No Meshy calls were made in Round 1, as asked.
The prompts below are kept for the next outfits and forms.

The pipeline:
1. Code writes the prompts (this page). **Done.**
2. **Dan has the concept images made** and saves them under `SourceArt/Concepts/`.
3. Code makes the models with the Meshy API (`python Tools/meshy_generate.py`), cleans them up in Blender (metres, feet at
   0, facing +Z, LODs), renders previews and **stops to show Dan**.
4. Dan rigs the four people in Mixamo and downloads the clips listed below. Kira can't go through Mixamo: it only rigs
   two-legged humanoids. Code first checks whether the Meshy API can rig and animate a four-legged animal. If it can't,
   Code builds Kira's rig and her clips in Blender by script. Her folded ears get no ear bones, so nothing can raise them.
5. Code imports everything (Humanoid rig for Dan and Abi, Generic for Kira), wires up the animations and replaces the
   graybox capsules. The capsules stay as a fallback.

## Your references (what exists, what doesn't)

| Model | Reference in `SourceArt/Reference/` | Notes |
|---|---|---|
| Dan, Ice Age | `Characters/Dan/dan-iceage-film.png` (349 x 634) | Good front view. It has the necklace, which the concept leaves out, and an open smile. |
| Abi, Ice Age | `Characters/Abi/abi-iceage-film.png` (169 x 367) | Good front view, but very small. |
| Kira, Ice Age sabertooth | `Characters/Kira/kira-iceage-sabertooth-film.png` (475 x 382) | Three-quarter front view. Paws and tail tip are cut off. |
| Dan, home clothes | Dan's full-body Christmas pyjama image (shared in chat, 2026-11-09) | Navy tree sweater, reindeer-pattern trousers. Save as `Characters/Dan/dan-home-film.png`. |
| Abi, home clothes | Abi's full-body Christmas pyjama image (shared in chat) | Green Santa sweater and trousers, fluffy white slippers. Save as `Characters/Abi/abi-home-film.png`. |
| Kira, normal Scottish Fold | Kira full-body images (shared in chat) | Three-quarter front, all four paws and the tail in frame. Save as `Characters/Kira/kira-home-film.png`. |
| Dan / Abi, Ice Age full body | the full-body caveman images (shared in chat) | **Barefoot** (Dan with fur ankle wraps), where the first stills had fur boots. The prompts now follow the full-body images. |

**Code can't save images from the chat.** Please drop them into the folders above so they sit beside the others.

**Settled by Dan's sheets:** Abi has long wavy brown hair and green eyes. Ice Age outfits are barefoot (Dan with fur
ankle wraps). **Settled (Dan, 2026-11-09):** the sheets Dan made from these prompts are the official designs and
override the earlier film stills, including Abi's home outfit (green Santa pyjamas). No red-pyjama model is planned.

The film stills set the style: Pixar-like, warm, a plain green background. They can't go to Meshy as they are. Meshy
needs a clean full-body sheet: at least 1024 px tall, nothing cut off, a neutral pose, mouth closed and no glow. So
each prompt below asks for a new image in that pose, **using the film still as the look to match**. If your image tool
takes a reference image, give it the matching still.

## What makes an image usable (I check these when you've saved them)

- **Full body in frame**, feet (and paws, tail) included, some space round it, nothing cropped.
- **At least 1024 px tall.**
- **People:** front view, **A-pose** (arms about 45 degrees out from the body, hands open and not touching the body or
  the legs, feet shoulder-width apart). Mouth **closed**, relaxed neutral face, eyes open, looking straight ahead.
- **Kira:** side view, standing on all four legs with the tail up and away from the body. A second image from the front
  or three-quarter front helps Meshy a lot.
- **Plain light background** (white, light grey or the film's flat green), soft even light, no shadows on the ground.
- No props, no necklace (Dan's necklace is its own model and glows in the game), no glow, no text, no second view in
  the same image.

If your tool has a negative prompt: `cropped, cut off feet, cut off paws, cropped tail, multiple views, necklace,
pendant, jewelry, held object, prop, glowing, open mouth, teeth showing, text, logo, watermark, background scenery,
ground shadow, motion blur, depth of field, photorealistic`.

**Kira lock (every Kira image, every form):** Scottish Fold. **Folded ears that lie flat and forward, never raised or
pointed.** **Solid grey fur with no white anywhere** (no white chest, paws, muzzle or chin). **Golden-amber eyes.** It's
written into both Kira prompts. Please check it on the images before saving them.

---

### 1. Dan, home clothes — `SourceArt/Concepts/dan_home.png`

> Full-body character concept of Dan from the reference image, a friendly man in his thirties in the style of a Pixar
> animated film, standing in an A-pose: arms held about 45 degrees out from his sides, hands open and relaxed, not
> touching his body, feet shoulder-width apart, front view, whole body in frame from the top of his hair to the soles of
> his slippers with space around him. Short light-brown hair swept up and back, warm brown eyes, light tan skin, mouth
> closed, gentle friendly expression. He wears Christmas pyjamas: a navy-blue knitted sweater with red collar and cuffs, a
> big decorated green Christmas tree with a gold star on the chest, white snowflakes; navy pyjama trousers with red, white
> and green bands of reindeer, trees and snowflakes; dark slippers. No necklace, nothing in his hands. Stylized,
> colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** the opening and closing cutscenes (the living room, the couch) and the post-credits free roam. The same
face, hair and build as his Ice Age model (`dan_iceage.png`), so make both from the same reference if your tool allows.
Mixamo: Humanoid. In-game height **1.8 m**.

### 2. Dan, Ice Age caveman — `SourceArt/Concepts/dan_iceage.png`

Reference: `SourceArt/Reference/Characters/Dan/dan-iceage-film.png`. Match the face, hair and outfit.

> Full-body character concept of Dan from the reference image, in the style of a Pixar animated film, standing in an
> A-pose: arms held about 45 degrees out from his sides, hands open and relaxed, not touching his body, feet
> shoulder-width apart, front view, whole body in frame from the top of his hair to the soles of his bare feet with space
> around him. Short light-brown hair swept up and back, warm brown eyes, light tan skin, athletic build, mouth closed,
> relaxed friendly expression. He wears a caveman outfit: a sleeveless shaggy fur tunic in tan and brown with dark spots,
> ending in a ragged hem above the knee; a twisted brown leather belt with a knot at the front; bare feet with small
> fur wraps round the ankles; bare arms. No necklace, no pendant, nothing glowing, nothing in his hands. Stylized, colourful, Pixar-like
> 3D render, soft even studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** Dan's model for the whole Ice Age world. His necklace is a separate model the game puts on his chest
(its shard glows and pulses near shards), so the concept must not have it. The fur hem and boots are short and stiff
enough for the Mixamo skeleton, so nothing hangs loose. Mixamo: Humanoid. Height **1.8 m**.

### 3. Abi, home clothes — `SourceArt/Concepts/abi_home.png`

> Full-body character concept of Abi from the reference image, a warm, clever woman in her thirties in the style of a
> Pixar animated film, standing in an A-pose: arms held about 45 degrees out from her sides, hands open and relaxed, not
> touching her body, feet shoulder-width apart, front view, whole body in frame from the top of her hair to the soles of
> her slippers with space around her. Long wavy brown hair falling behind her shoulders (not over her arms), big
> green eyes, light skin, mouth closed, gentle smile. She wears Christmas pyjamas: a green sweater with a big Santa Claus
> on the chest, red collar and cuffs and white snowflakes; green pyjama trousers printed with little Santas, candy canes
> and snowflakes, red cuffs at the ankles; fluffy white slippers. No jewellery, nothing in her hands. Stylized, colourful,
> Pixar-like 3D render, soft even studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** the living-room cutscenes and the post-credits free roam. Same face and hair as `abi_iceage.png`. Her
hair goes behind her shoulders so Meshy doesn't fuse it to her arms. Mixamo: Humanoid. Height **1.65 m**.

### 4. Abi, Ice Age — `SourceArt/Concepts/abi_iceage.png`

Reference: `SourceArt/Reference/Characters/Abi/abi-iceage-film.png`. Match the face, hair and outfit.

> Full-body character concept of Abi from the reference image, in the style of a Pixar animated film, standing in an
> A-pose: arms held about 45 degrees out from her sides, hands open and relaxed, not touching her body, feet
> shoulder-width apart, front view, whole body in frame from the top of her hair to the soles of her bare feet with space
> around her. Long wavy brown hair falling behind her shoulders (not over her arms), big green eyes, light olive
> skin, slim and athletic, mouth closed, relaxed friendly expression. She wears an Ice Age outfit: a one-shoulder
> leopard-spotted fur dress in golden tan with dark brown spots, a pale fringed hem just above the knee, a thin brown
> leather belt at the waist, bare feet. Nothing in her hands, nothing glowing.
> Stylized, colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no shadows on the ground,
> no text.

**Design note:** Abi's model for the whole Ice Age world. She is the fast, agile hero (higher jump, the double jump), so
the outfit stays short and close to the body. Mixamo: Humanoid. Height **1.65 m**.

### 5. Kira, Scottish Fold — `SourceArt/Concepts/kira.png` (+ `kira_front.png` if you can)

> Full-body character concept of Kira, a small adult Scottish Fold cat, in the style of a Pixar animated film, side
> view, standing on all four legs, tail raised in a gentle curve away from her body, whole cat in frame from the tip of
> her nose to the tip of her tail and the bottoms of all four paws, with space around her. Scottish Fold: small ears
> folded flat and forward against the head, never raised, never pointed. Solid soft grey fur everywhere with absolutely
> no white: grey chest, grey paws, grey muzzle, grey chin. Big round golden-amber eyes, a small dusty-pink nose, a round
> chubby face, mouth closed, a curious, mischievous expression. Nothing in her mouth or paws, no collar. Stylized,
> colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no shadows on the ground, no text.

For `kira_front.png`: the same prompt, but **"three-quarter front view"** instead of "side view".

**Design note:** Kira at home (the cutscenes) and the base every one of her forms is built from. In the game she's
about **0.5 m** tall at the head and must read on screen next to Dan and Abi. **Kira lock:** folded ears, no white
anywhere, golden-amber eyes. Not Mixamo: a four-legged rig by Meshy or by Code in Blender. **Generic** rig.

### 6. Kira, Ice Age sabertooth kitten — `SourceArt/Concepts/kira_sabertooth.png` (+ `kira_sabertooth_front.png` if you can)

Reference: `SourceArt/Reference/Characters/Kira/kira-iceage-sabertooth-film.png`. Match her face and fangs.

> Full-body character concept of Kira from the reference image as a tiny saber-toothed kitten, in the style of a Pixar
> animated film, side view, standing on all four legs, tail raised in a gentle curve away from her body, whole cat in
> frame from the tip of her nose to the tip of her tail and the bottoms of all four paws, with space around her. The same
> Scottish Fold cat: small ears folded flat and forward against the head, never raised, never pointed. Solid soft grey
> fur everywhere with absolutely no white: grey chest, grey paws, grey muzzle, grey chin. Big round golden-amber eyes, a
> small dusty-pink nose. Two long, curved, cream-coloured sabre fangs hanging down past her chin from a closed mouth
> (adorable, not scary), a slightly sturdier, fluffier prehistoric build. No collar, nothing glowing. Stylized,
> colourful, Pixar-like 3D render, soft even studio lighting, plain light background, no shadows on the ground, no text.

**Design note:** Kira's Ice Age form for the whole first world. The fangs are the only change from her home model, so
the two should look like the same cat. **Kira lock** as above. Generic rig. About **0.5 m** at the head.

---

## Animations

### Dan and Abi (Mixamo, step 4)

You rig each person once in Mixamo: upload the FBX Code gives you after step 3, auto-rig it, then download the clips
below. First the rigged model itself, **With Skin** in its T-pose (`rigged.fbx`); then every clip **Without Skin**. Format FBX
for Unity, 30 fps, **In Place** ticked where Mixamo offers it. Save them as `SourceArt/Mixamo/<model>/<file>` (model
= `dan_home`, `dan_iceage`, `abi_home`, `abi_iceage`). Mixamo's names change now and then: if a search finds nothing,
take the closest match and keep my file name.

The home outfits only appear in cutscenes and the free roam, so they need fewer clips: the first ten rows.

| File | Mixamo search | For |
|---|---|---|
| `rigged.fbx` | (no animation: the T-pose) | the rigged model, **With Skin** |
| `idle.fbx` | Breathing Idle | standing still |
| `idle_look.fbx` | Looking Around | idle variation after a few seconds |
| `walk.fbx` | Walking | slow stick |
| `run.fbx` | Running | full stick |
| `jump_up.fbx` | Jumping Up | take-off and rising |
| `fall.fbx` | Falling Idle | in the air, coming down |
| `land.fbx` | Falling To Landing | landing |
| `hit.fbx` | Hit Reaction | losing a heart |
| `cheer.fbx` | Cheering | the story shard, cutscenes |
| `swim.fbx` | Swimming | swimming at the surface and diving |
| `tread.fbx` | Treading Water | still at the surface |
| `hard_land.fbx` | Hard Landing | a long fall (and Dan's backward fall in the Ice Age) |
| `attack.fbx` | Dan: **Punching**. Abi: **Mma Kick** | the basic attack (Dan's punch, Abi's kick) |
| `push.fbx` | **Dan only:** Pushing | pushing heavy blocks |
| `double_jump.fbx` | **Abi only:** Front Flip | her double jump |

### Kira (Code, step 4: no Mixamo)

Code makes these on Kira's four-legged rig, either through Meshy's animation API if it handles quadrupeds or by script
in Blender: **idle, walk, run, jump, fall, land, swim, attack (claw swipe), hurt.** The same set goes on every Kira
form. The ears have no bones.
