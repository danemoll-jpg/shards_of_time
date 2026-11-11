# Cutscenes: making them, and rendering them to video

Every cutscene in Shards of Time is made in Unity with **Timeline** and **Cinemachine**, using the game's own characters,
and plays live in the game (CLAUDE.md rule 8). The same cutscene renders to an MP4 file (from the built game, Round 5). This page is
written so the same setup can be copied into a film project: nothing in it depends on gameplay.

## What a cutscene is

A cutscene is two assets:

| Asset | Where | What's in it |
|---|---|---|
| The **Timeline** | `Assets/Cutscenes/<Name>.playable` | the tracks: a **Shots** track (Cinemachine: which camera is live when, and the cuts or blends between them), one **Animation** track per actor or moving thing, **Activation** tracks for things that appear and disappear |
| The **scene prefab** | `Assets/Resources/Cutscenes/<Name>.prefab` | a root with a **PlayableDirector** (playing the Timeline), the **actors**, one **Cinemachine camera per shot**, and props / effects. The director's bindings tie the tracks to these objects |

Everything in the prefab is placed **relative to its root**. The game puts the root at an **anchor** when it plays the
cutscene. That's why the same cutscene works wherever it happens:
- `NecklaceWakes`: the root is where the player stands, facing the story shard, so the light beam points the right way.
- `PortalOpens`: the root is the portal; its +Z faces the heroes.

The game's code only does this (`Assets/Scripts/Cutscenes/Cutscenes.cs`, `Cutscenes.Play(name, position, rotation)`):
1. puts the prefab at the anchor;
2. binds the Shots track to the game's camera brain;
3. hides the playing party and freezes everyone;
4. plays the director;
5. gives control back when it ends, or when the player **holds A / Space for 1 second** to skip. The game camera comes
   back behind the player.

The actors are the prefab's own copies of the heroes, so a cutscene doesn't depend on where the followers happen to be.
Since Round 3 the actors are the real models (Dan and Abi in their home clothes, Kira the home cat; the graybox bodies
only when a model is missing). Each actor is a holder (Animator, moves and turns the `Body` child) with the model under
`Body`; the model's own Animator plays its clips on a **clip track** ("Dan clips": idle, look, cheer from
`Resources/Characters/Clips/`; Kira's from `kira.fbx`). Dan's necklace sits on his chest bone and has its own small
Animator, so the shard's glow keys stay out of the rig's. To act a moment: drop another clip on the actor's clip track
(clips that overlap blend), or key the holder's `Body` to move the whole actor.

## Making a new cutscene

1. **Start from a copy.** In the Project window, duplicate `Assets/Resources/Cutscenes/NecklaceWakes.prefab` and
   `Assets/Cutscenes/NecklaceWakes.playable`. Rename both to the new name, then select the new prefab and set its
   **PlayableDirector > Playable** to the new `.playable`.
   (Or start empty: make an empty GameObject, add **PlayableDirector**, click **Create** in the Timeline window and save
   the `.playable` under `Assets/Cutscenes/`.)
2. **Open it.** Double-click the prefab (prefab mode), select the root, and open **Window > Sequencing > Timeline**.
3. **Actors.** Put each actor under the root. Give each one a holder object with an **Animator** (no controller needed)
   and put the body inside it. Animate the **body (child)**, not the holder. The holder stays where you placed it, and the
   clips only move things relative to it.
4. **Animate.** In the Timeline window, **Add > Animation Track** and drag the actor's holder onto it. Press the red
   record button on the track and move or rotate things in the Scene view at different times. Keys go into the clip.
   (Real characters: drag Mixamo clips straight onto the track instead.)
5. **Shots.** For each shot add a camera: **GameObject > Cinemachine > Cinemachine Camera** under the root. Place it and
   aim it in the Scene view (**GameObject > Align With View** is handy), and set its **Lens > Field Of View**. Set its
   **Priority to -1**, so it never takes over gameplay by itself.
   - A moving shot: put the camera under a holder with an Animator and animate the camera like an actor (a dolly), or
     give it a **Position Composer / Rotation Composer** and a **Tracking Target** to follow an actor.
6. **Cut between shots.** Add a **Cinemachine Track** (named "Shots"; leave its binding empty, the game binds the
   brain). Right-click it, **Add Cinemachine Shot**, and drag the shot's camera into the clip's **Virtual Camera** field.
   Clips back to back = cuts; overlap two clips = a blend over the overlap.
7. **Appear / disappear.** **Add > Activation Track**, bind the object, and the clip is when it's visible.
8. **Length.** Timeline asset > **Duration Mode: Fixed Length** and the length in seconds. The game hands control back
   at the end.
9. **Play it in the game.** Call `StartCoroutine(Cutscenes.Play("<Name>", anchorPosition, anchorRotation))` from the
   story moment's code (see `Game.WakeNecklace` / `Game.OpenPortal` in `Assets/Scripts/Core/Game.cs`). Test it with
   **Render Cutscene to Video** (below) or in Play mode.

The two first cutscenes were made by code (`Assets/Editor/CutsceneAuthoring.cs`) only to have them on day one (and
remade once in Round 3 with the real models). From now on they're ordinary assets: edit them by hand.
**Shards of Time > Cutscenes > Rebuild Cutscenes** would overwrite hand edits (it asks first).

## Rendering a cutscene to an MP4

**Shards of Time > Render Cutscene to Video**, then click **Render <Name>** (it uses the last build: build first if the
cutscene changed). Or, with no editor open:

```
python Tools/render_cutscene.py ActOne IceAgeArrival
```

**Round 5: videos are rendered from the built game, never by the editor's Play mode** (that run played the music on Dan's
speakers and took his focus). `Tools/render_cutscene.py` starts `Builds/ShardsOfTime/ShardsOfTime.exe -batchmode -mute
-exeCheck <dir> -exeCheckOnly film -film <Name>` through `Tools/quiet.py` (a desktop of its own that is never shown, so
nothing can appear or take focus; Below Normal). The game plays the cutscene at a fixed 30 frames a second
(`Time.captureFramerate`), draws every frame off-screen and saves it (`Assets/Scripts/Checks/ExeCheck.Film.cs`); white-outs
are kept, the skip prompt is not drawn. ffmpeg (hidden too) encodes `Recordings/<Name>.mp4` (1920 x 1080, 30 fps, H.264,
no audio). The script prints the measured audio output (must be 0.000) and what the window watcher saw (must be "none
visible, never in the foreground"); a still every 1.5 s stays in `local/film/<Name>/stills/`.

Sound: none in the video. When it should have music and voices, that is a separate, deliberate step Dan asks for.

## Using this for a film

The film setup is the same thing without the game:
- one prefab per **scene** (set, actors, shot cameras, director);
- one Timeline per scene, its **Shots** track as the edit;
- to render, an empty scene with a **Main Camera** that has a **CinemachineBrain**, the scene prefab dropped in, and
  the Shots track bound to that brain. Render it from a build the same way as the game does (`ExeCheck.Film.cs`: fixed
  30 fps, frames off-screen, ffmpeg), or for hand use in the editor Unity Recorder's Movie recorder (Recording Mode:
  Timeline) works too, but never on Dan's PC by script.
- Longer films: one Timeline per scene, then a master Timeline with **Control Tracks** that play the scene Timelines
  one after another.

## Round 4: the cutscenes in the game now

| Name | Where (its root) | Length | What |
|---|---|---|---|
| `ActOne` | `Game.HomeAnchor` (far from every world) | 19 s | Act I: the living room (`home_room.fbx`), Kira bats the dark silver bauble off the tree, it shatters, a vortex pulls them in, white-out |
| `IceAgeArrival` | the Ice Age landing spot | 13.5 s | they land in the snow; Dan's backward fall (`fall_back` = Mixamo "Falling Down"); sabertooth Kira on his chest paws the necklace, which pulses blue; Abi laughs |
| `NecklaceWakes` | where the player stands (out of caves) | 6.5 s | the 25th Ice Age shard: the necklace wakes (Ice Age outfits, sabertooth Kira) |
| `ViolinistTeaches` | the Violinist, +z toward Dan | 6.5 s | he plays (the world's own figure), Dan glows, learns the ground pound |
| `VortexOpens` | the vortex gate | 6.5 s | the blue story shard flies into the ring, the vortex opens |
| `PortalOpens` | the test area's portal | 6 s | Round 1's (the debug test area only) |

New-game order: `ActOne` -> `IceAgeArrival` -> play. All are skippable (hold A / Space).

**Rebuilt automatically when the code changes:** each prefab carries a `CutsceneVersion`; `CutsceneAuthoring.Version` is
bumped when a cutscene's authoring code changes, and the next build remakes the older ones. **To keep a hand-edited
cutscene**, set its `CutsceneVersion` to 999 in the Inspector.

**A cutscene's own light:** a `CutsceneMood` on the prefab's root (`ActOne`: warm interior, no fog, no falling snow) is applied
while it plays and undone after. **White-outs:** a `ScreenTint` whose amount is its object's scale x, keyed like anything else.
**Sets:** a Blender file in `Assets/Resources/World/` can be dropped in and unpacked (`ActOne` does this with the room; the
ornament is moved under its own holder so it can be keyed).

Render any of them to MP4 the same way: `python Tools/render_cutscene.py ActOne`.

## Round 6: Egypt, and the story cutscenes per world

| Name | Where (its root) | Length | What |
|---|---|---|---|
| `EgyptArrival` | Egypt's plaza (`Egypt.Start`), +z north to the pyramids | 13.5 s | the vortex drops them among the pyramids and busy workers; Egyptian clothes; Kira is a little mummy cat (she trots a circle); Dan and Abi react; the necklace's shard glows red |
| `SphinxGag` | `Egypt.GagSpot`, +z toward the Sphinx | 10.5 s | they walk up; the camera tilts up to its face (Kira's); Dan's double-take between the Sphinx and mummy Kira (sitting in the same pose); Abi turns away, unimpressed. Uses the world's Sphinx (not in the prefab). Plays once (`EG-sphinx-gag` in the save) |
| `NecklaceWakesEgypt` | where the player stands | 6.5 s | as `NecklaceWakes`, Egypt's cast, red |
| `VortexOpensEgypt` | Egypt's gate (in the burial chamber) | 6.5 s | as `VortexOpens`, the red story shard |
| `ViolinistTeachesEgypt` | the Violinist (market) | 6.5 s | Abi learns the double jump (he plays his lyre) |

The story cutscenes are made per world from one function each (`NecklaceWakesIn`, `VortexOpensIn`, `ViolinistTeachesIn`)
with a cast (`CutsceneAuthoring.Cast`: the models; a model not in yet falls back to the home models) and the story shard's
colour (`NecklaceModel.Tint`). `Game` picks the world's version. **After new models arrive** (e.g. Dan's Egypt Mixamo
files), bump `CutsceneAuthoring.Version` so the next build remakes them with the real models.

`IceAgeArrival` gets 4 s more at its end (Dan on his feet laughing, then holding the shard up in his hand) once the shared
clips `laugh` and `hold_up` are in `Resources/Characters/Clips/` (Dan's Mixamo list, Round 6); until then it is as in Round 5.

Render: `python Tools/render_cutscene.py EgyptArrival SphinxGag` (Round 5's way: the built exe, muted, off-screen).

## Round 8: the Medieval Kingdom

| Name | Root | Length | What happens |
|---|---|---|---|
| `MedievalArrival` | the courtyard at world (0, floor, 14), +z north | 17 s | the purple vortex opens over the courtyard; the party drops into a horse-drawn hay wagon and pops up in medieval clothes, Kira a baby dragon; her sneeze is a puff of fire and the hay catches; they leap out; the pony bolts, the wagon (its sleepy driver aboard) careens into the west wall: a cartoon blast; the driver staggers out singed, hat smoking; the pony trots off fine; the party slinks away. The world's wreck, driver and pony (`Medieval.WreckSite`) stay hidden until it ends |
| `TheStone` | the stone at world (0, floor, 34), +z north (the keep) | 19 s | Muscle Man is there first: flexes, laughs at Dan, grips the shard and heaves (his "Pull Heavy Object"), falls flat on his back; Dan steps up, the shard answers his necklace and slides out; Muscle Man kneels ("Master!"); the purple vortex pulls all four in |
| `NecklaceWakesMedieval` | where the player stands | 6.5 s | as `NecklaceWakes`, the Medieval cast, purple |
| `ViolinistTeachesMedieval` | the Violinist (the courtyard, by the benches) | 6.5 s | Dan learns the charged punch (he plays his lute) |

`VortexRings(..., colors:)` takes a colour set (`PurpleRings`). Muscle Man's actor plays his own Mixamo clips (`mm_*`).
Render: `python Tools/render_cutscene.py MedievalArrival TheStone`.
