# Cutscenes: making them, and rendering them to video

Every cutscene in Shards of Time is made in Unity with **Timeline** and **Cinemachine**, using the game's own characters,
and plays live in the game (CLAUDE.md rule 8). The same cutscene renders to an MP4 file from the editor. This page is
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
Round 1's actors are the graybox bodies (`Placeholders.HeroBody`). When the real models arrive, swap each actor's body
for the model. The tracks don't change.

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

The two Round 1 cutscenes were made by code (`Assets/Editor/CutsceneAuthoring.cs`) only to have them on day one. From
now on they're ordinary assets: edit them by hand. **Shards of Time > Cutscenes > Rebuild Graybox Cutscenes** would
overwrite hand edits (it asks first).

## Rendering a cutscene to an MP4

**Shards of Time > Render Cutscene to Video**, then click **Render <Name>**.

The editor enters Play mode in the game's scene. The game builds the playground and places everyone. It plays the
cutscene with no title screen, no saving and no skipping, and **Unity Recorder** records the main camera. Play mode
ends by itself when the cutscene does. The file is `Recordings/<Name>.mp4` (1920 x 1080, 30 fps, H.264, no audio;
`Recordings/` is git-ignored). **Open the Recordings folder** is in the same window.

From a command line, with no window (close the editor first; `--gpu` keeps the graphics card the recorder needs):

```
python Tools/unity_batch.py render --gpu -executeMethod ShardsOfTime.CutsceneRender.RenderFromCommandLine -cutscene NecklaceWakes
```

Settings live in `Assets/Editor/CutsceneRender.cs` (`Width`, `Height`, `Fps`). Sound: none for now. When the game has
music and voices, turn on the recorder's audio (`AudioInputSettings.PreserveAudio`) and render unmuted on purpose.

## Using this for a film

The film setup is the same thing without the game:
- one prefab per **scene** (set, actors, shot cameras, director);
- one Timeline per scene, its **Shots** track as the edit;
- to render, an empty scene with a **Main Camera** that has a **CinemachineBrain**, the scene prefab dropped in, and
  the Shots track bound to that brain. Then **Window > General > Recorder > Recorder Window**, add a **Movie** recorder
  (Targeted camera: MainCamera, MP4) and set **Recording Mode: Timeline**, or copy `CutsceneRender.cs`'s
  `StartRecorder` into your film project's own menu.
- Longer films: one Timeline per scene, then a master Timeline with **Control Tracks** that play the scene Timelines
  one after another.
