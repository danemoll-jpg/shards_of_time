# Shards of Time — models needed

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
