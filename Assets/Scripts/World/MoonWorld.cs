using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 13: the Moon's pieces (World/Moon.cs places them).

    // ---- a bouncy crater rim: anyone landing on it is thrown up high and floaty (Hero.OnMotorEvent "land")
    public sealed class MoonBounce : MonoBehaviour
    {
        public float height = 7f;
        public static int Bounces { get; private set; }
        public void Bounce(Hero h)
        {
            h.Motor.Launch(height); Bounces++;
            if (h.IsPlayer) { Sfx.Play("critter_boing", .55f, .7f); PlayLog.Line($"{h.Stats.displayName} bounces off a crater rim ({height} m)"); }
            Placeholders.Puff(h.transform.position + Vector3.up * .1f, new Color(.8f, .8f, .85f));
        }
    }

    // ---- KiraFloats (the film's gag): once, on the way to the rover
    public sealed class FloatsZone : MonoBehaviour
    {
        public float radius = 7f;
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy || !Game.InMoon) return;
            var pr = SaveGame.Current.World(Game.World.worldId); if (pr.opened.Contains(Moon.FloatsFlag)) { enabled = false; return; }
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < radius && Party.I.Player.Motor.Grounded) Game.I.StartCoroutine(Game.I.KiraFloatsScene(transform.position));
        }
    }

    // ---- the rover's control panel: the light puzzle (the film: only Abi solves it). With the necklace awake, Abi at the panel: the white
    // shard's light flashes a pattern on the four big lights; she repeats it with the stick (up, right, down, left = the lights left to right);
    // three rounds of 4, 6, 8 lights; a wrong press just plays the pattern again. Dan or Muscle Man puzzle over it ("Switch to Abi").
    public sealed class RoverPanel : MonoBehaviour
    {
        public readonly Renderer[] lights = new Renderer[4]; public readonly Material[] on = new Material[4], off = new Material[4];
        public static readonly int[] Lengths = { 4, 6, 8 };
        public bool Active { get; private set; }
        public bool Showing { get; private set; }
        public int Round { get; private set; }
        public int Wrongs { get; private set; }
        public int[] Pattern { get; private set; }
        public int Step { get; private set; }
        public static int Solved { get; private set; }
        float hintAt = -10; bool released = true;

        void Update()
        {
            if (!Game.InMoon || !Party.I) return;
            var pr = SaveGame.Current.World(Game.World.worldId);
            var p = Party.I.Player; var d = p.transform.position - transform.position; d.y = 0;
            bool near = d.magnitude < 2.2f && Mathf.Abs(p.transform.position.y - transform.position.y) < 1.5f;
            if (Active)
            {
                if (!near || p.Stats.displayName != "Abi") { Stop("stepped away"); return; }
                if (Showing) return;
                // the stick: one flick = one light (up 0, right 1, down 2, left 3: the lights left to right)
                var m = Controls.Move.ReadValue<Vector2>();
                if (m.magnitude < .35f) { released = true; return; }
                if (!released || m.magnitude < .7f) return;
                released = false;
                int pick = Mathf.Abs(m.y) > Mathf.Abs(m.x) ? (m.y > 0 ? 0 : 2) : (m.x > 0 ? 1 : 3);
                Press(pick);
                return;
            }
            if (!near || !Game.Playing || Game.I.Busy || !pr.necklaceAwake || pr.storyShard) return;
            if (p.Stats.displayName != "Abi")
            {
                if (Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message(p.IsMuscleMan || p.Stats.displayName == "Dan" ? $"{p.Stats.displayName} stares at the blinking lights and scratches his head... Switch to Abi!" : "Kira paws at the lights... nothing. Switch to Abi!", 4f); }
                return;
            }
            Begin();
        }

        void Begin()
        {
            Active = true; Round = 0; Wrongs = 0; Party.I.Frozen = true;
            PlayLog.Line("the rover's light puzzle starts (Abi)");
            Hud.Message($"The white shard's light flashes a pattern. Repeat it with the stick: up, right, down, left = the four lights. ({Controls.Btn(Controls.Act.Jump)}: step back)", 6f);
            NewRound();
        }
        void Stop(string why) { Active = false; Showing = false; if (Party.I) Party.I.Frozen = false; foreach (var i in new[] { 0, 1, 2, 3 }) Light(i, false); PlayLog.Line($"the light puzzle stops ({why})"); }
        void LateUpdate() { if (Active && !Showing && Controls.Jump.WasPressedThisFrame()) Stop("Jump"); }

        void NewRound()
        {
            var rnd = new System.Random(7 + Round * 31);   // the same patterns every time (the Violinist's tune hints at the rhythm)
            Pattern = new int[Lengths[Round]]; for (int i = 0; i < Pattern.Length; i++) Pattern[i] = rnd.Next(4);
            Step = 0; StartCoroutine(Show());
        }
        IEnumerator Show()
        {
            Showing = true; yield return new WaitForSeconds(.8f);
            foreach (var k in Pattern) { Light(k, true); Sfx.Play("rover_tone_" + (k + 1), .7f); yield return new WaitForSeconds(.42f); Light(k, false); yield return new WaitForSeconds(.18f); }
            Showing = false; released = false;
            PlayLog.Line($"the rover shows round {Round + 1}: {string.Join(" ", Pattern)}");
        }
        void Light(int i, bool lit) { if (lights[i]) lights[i].sharedMaterial = lit ? on[i] : off[i]; }

        void Press(int k)
        {
            StartCoroutine(Blink(k));
            var abi = Party.I ? Party.I.Player : null;   // Dan, 2026-11-12: no Button Pushing on Mixamo: her offer clip (both hands reach forward) on each press
            if (abi && abi.Anim && abi.Anim.Has("offer")) abi.Anim.Trigger("offer");
            if (k != Pattern[Step]) { Wrongs++; Sfx.Play("rover_wrong", .6f); PlayLog.Line($"wrong light ({k}, wanted {Pattern[Step]}): the pattern again"); Step = 0; StartCoroutine(Show()); return; }
            Sfx.Play("rover_tone_" + (k + 1), .7f); Step++;
            if (Step < Pattern.Length) return;
            Round++;
            if (Round < Lengths.Length) { Hud.Message($"Round {Round + 1} of 3...", 2f); NewRound(); return; }
            Solved++; Active = false; Party.I.Frozen = false;
            PlayLog.Line($"the light puzzle is solved ({Wrongs} wrong presses)");
            Game.I.StartCoroutine(Game.I.WhiteShardFinale());
        }
        IEnumerator Blink(int k) { Light(k, true); yield return new WaitForSeconds(.25f); if (!Showing) Light(k, false); }
    }
}
