using System;
using System.Collections;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ShardsOfTime
{
    // Round 1, item 11 (rule 8): in-engine cutscenes. A cutscene is a prefab in Resources/Cutscenes/<Name>.prefab: a PlayableDirector with
    // a Timeline asset (Assets/Cutscenes/<Name>.playable), its own actors (the graybox heroes, later the real models), Cinemachine
    // cameras for the shots, effects. The game places it at an anchor (its root = the anchor), hides the party, plays it through the
    // camera brain, and gives control back cleanly at the end or when skipped (hold Skip for SkipSeconds). The same prefab renders to an
    // MP4 from the editor (Shards of Time > Render Cutscene to Video, Editor/CutsceneRender.cs). How to author one: Docs/CUTSCENES.md.
    public static class Cutscenes
    {
        public const float SkipSeconds = 1f;
        public static string Playing { get; private set; }
        public static float SkipHeld01 { get; private set; }
        public static bool LastSkipped { get; private set; }
        public static float LastDuration { get; private set; }
        public static int Played { get; private set; }
        public static PlayableDirector Director { get; private set; }
        public static bool AllowSkip = true;       // the film render turns it off
        public static event Action<string> Started, Finished;

        public static GameObject Load(string name) => Resources.Load<GameObject>("Cutscenes/" + name);

        public static IEnumerator Play(string name, Vector3 at, Quaternion facing)
        {
            var prefab = Load(name);
            if (!prefab) { Debug.LogWarning("[Cutscene] missing " + name); yield break; }
            var inst = UnityEngine.Object.Instantiate(prefab, at, facing);
            if (Game.InMoon) foreach (var an in inst.GetComponentsInChildren<Animator>(true))   // Round 13: every Moon actor in its bubble helmet
                if (an.transform.parent && an.transform.parent.name == "Body") Helmet.Attach(an.gameObject, Helmet.KindFor(an.gameObject.name.ToLowerInvariant().Replace("model ", "")));
            inst.name = "Cutscene " + name;
            var dir = inst.GetComponent<PlayableDirector>();
            Director = dir; Playing = name; SkipHeld01 = 0; LastSkipped = false; Played++;
            dir.timeUpdateMode = DirectorUpdateMode.GameTime; dir.extrapolationMode = DirectorWrapMode.None; dir.playOnAwake = false;
            // the camera track drives the game's camera brain
            if (dir.playableAsset is TimelineAsset tl)
                foreach (var t in tl.GetOutputTracks().OfType<CinemachineTrack>()) dir.SetGenericBinding(t, CameraRig.I.Brain);
            var party = Party.I;
            if (party) { party.Frozen = true; party.SetVisible(false); }
            var mood = inst.GetComponent<CutsceneMood>();   // Round 4: the cutscene's own light (the living room)
            if (mood) Look.Mood(mood);
            PlayLog.Line($"cutscene {name} starts ({dir.duration:0.0} s)");
            float start = Time.time;
            if (!SaveGame.Disabled && !SaveGame.Current.seen.Contains(name)) SaveGame.Current.seen.Add(name);   // Fix 10.1: the Theater
            Started?.Invoke(name);
            dir.Play();
            bool skipArmed = !Controls.Skip.IsPressed();   // a held button from before doesn't count
            while (dir.state == PlayState.Playing && dir.time < dir.duration - .001)
            {
                if (!Controls.Skip.IsPressed()) skipArmed = true;
                if (AllowSkip && skipArmed && Controls.Skip.IsPressed()) SkipHeld01 = Mathf.MoveTowards(SkipHeld01, 1f, Time.unscaledDeltaTime / SkipSeconds);
                else SkipHeld01 = Mathf.MoveTowards(SkipHeld01, 0f, Time.unscaledDeltaTime * 3f);
                if (SkipHeld01 >= 1f) { LastSkipped = true; break; }
                yield return null;
            }
            LastDuration = Time.time - start;
            dir.time = dir.duration; dir.Evaluate(); dir.Stop();
            UnityEngine.Object.Destroy(inst);
            if (mood) Look.Mood(null);
            if (party) { party.SetVisible(true); party.Frozen = false; }
            CameraRig.I?.SnapBehind();
            Director = null; Playing = null; SkipHeld01 = 0;
            PlayLog.Line($"cutscene {name} {(LastSkipped ? "skipped" : "ended")} after {LastDuration:0.0} s");
            Finished?.Invoke(name);
        }
    }
}
