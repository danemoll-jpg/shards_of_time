using System.Collections;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 4, item 6: the Violinist teaches moves (DECIDED, Dan 2026-11-09). He stands somewhere in each world; when the hero the move is
    // for reaches him, he plays a short tune (the cutscene ViolinistTeaches: no dialogue, he never explains himself) and that hero
    // learns the move (saved). Anyone else gets a hint that he is looking past them. Placeholder figure (Ice Age look: fur cloak, silver
    // beard) with the Blender bone fiddle until his model exists (Docs/MODELS-NEEDED.md #10).
    public sealed class Violinist : MonoBehaviour
    {
        public static Violinist I { get; private set; }
        public string ability = Abilities.GroundPound, learner = "Dan", moveName = "Ground Pound";
        public float radius = 2.6f;
        public bool Playing { get; private set; }
        public int Taught { get; private set; }
        Transform bowArm, bow, head; float hintAt = -10f; ParticleSystem notes;

        void OnEnable() => I = this;
        void OnDisable() { if (I == this) I = null; }

        public static Violinist Make(Transform parent, Vector3 at, float yaw)
        {
            var go = new GameObject("Violinist"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var t = go.transform; var v = go.AddComponent<Violinist>();
            var hide = Placeholders.Mat(new Color(.72f, .58f, .4f)); var cloak = Placeholders.Mat(new Color(.86f, .86f, .88f)); var skin = Placeholders.Mat(new Color(.93f, .78f, .66f));
            var silver = Placeholders.Mat(new Color(.82f, .83f, .86f)); var wrap = Placeholders.Mat(new Color(.55f, .5f, .46f));
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, .95f, 0), new Vector3(.5f, .62f, .38f), hide, name: "Tunic");
            Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, -.05f), new Vector3(.78f, .42f, .55f), cloak, name: "Cloak");
            Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(0, 1.15f, -.16f), new Vector3(.62f, .45f, .25f), cloak, name: "Cloak back");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(s * .12f, .3f, 0), new Vector3(.16f, .32f, .16f), wrap, name: "Leg");
            v.head = Placeholders.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.72f, 0), new Vector3(.3f, .34f, .3f), skin, name: "Head").transform;
            Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(0, .32f, -.12f), new Vector3(1.05f, .55f, 1.0f), silver, name: "Hair");
            Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(0, -.42f, .25f), new Vector3(.7f, .6f, .55f), silver, name: "Beard");
            foreach (float s in new[] { -1f, 1f }) Placeholders.Prim(PrimitiveType.Sphere, v.head, new Vector3(s * .2f, .06f, .44f), new Vector3(.12f, .1f, .06f), Placeholders.Mat(new Color(.35f, .6f, .9f)), name: "Eye");
            // the fiddle under his chin (left arm), the bow in his right hand
            var leftArm = Placeholders.Prim(PrimitiveType.Capsule, t, new Vector3(-.28f, 1.35f, .2f), new Vector3(.12f, .3f, .12f), cloak, name: "Left arm").transform;
            leftArm.localRotation = Quaternion.Euler(70, 25, 0);
            var fiddle = WorldArt.Prop("iceage_props", "Fiddle", t, Vector3.zero);
            if (fiddle) { fiddle.transform.SetParent(t, false); fiddle.transform.localPosition = new Vector3(-.12f, 1.48f, .25f); fiddle.transform.localRotation = Quaternion.Euler(-80, 0, 30) * Quaternion.Euler(0, 0, 0); var bowPart = fiddle.transform.Find("FiddleBow__Bark"); if (bowPart) bowPart.gameObject.SetActive(false); }
            var armPivot = new GameObject("Bow arm").transform; armPivot.SetParent(t, false); armPivot.localPosition = new Vector3(.27f, 1.45f, 0);
            Placeholders.Prim(PrimitiveType.Capsule, armPivot, new Vector3(0, -.25f, .1f), new Vector3(.12f, .28f, .12f), cloak, name: "Right arm").transform.localRotation = Quaternion.Euler(30, 0, 0);
            var bowGo = Placeholders.Prim(PrimitiveType.Cylinder, armPivot, new Vector3(-.2f, -.4f, .3f), new Vector3(.02f, .35f, .02f), Placeholders.Mat(new Color(.42f, .28f, .18f)), name: "Bow");
            bowGo.transform.localRotation = Quaternion.Euler(0, 0, 70);
            v.bowArm = armPivot; v.bow = bowGo.transform;
            v.notes = MakeNotes(t);
            return v;
        }

        static ParticleSystem MakeNotes(Transform t)
        {
            var go = new GameObject("Notes"); go.transform.SetParent(t, false); go.transform.localPosition = new Vector3(0, 1.8f, .3f);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 2.2f; main.startSpeed = .8f; main.startSize = .18f; main.startColor = new Color(.7f, .9f, 1f, 1f); main.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 0;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35; sh.radius = .1f; sh.rotation = new Vector3(-90, 0, 0);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Resources.Load<Material>("Materials/Particle");
            return ps;
        }

        void Update()
        {
            // the tune: the bow arm saws, notes drift up (during his cutscene, and a slow idle sway otherwise)
            bool play = Playing || Cutscenes.Playing == "ViolinistTeaches";
            var em = notes.emission; em.rateOverTime = play ? 6f : 0f;
            if (bowArm) bowArm.localRotation = Quaternion.Euler(0, play ? Mathf.Sin(Time.time * 7f) * 25f : 0, play ? 0 : 10f);
            if (head) head.localRotation = Quaternion.Euler(play ? 8f : 0, 0, play ? Mathf.Sin(Time.time * 1.5f) * 8f : Mathf.Sin(Time.time * .7f) * 3f);
            if (!Game.Playing || !Party.I || Game.I.Busy) return;
            var p = Party.I.Player; var d = p.transform.position - transform.position; d.y = 0;
            if (d.magnitude > radius) return;
            if (SaveGame.Current.Has(ability)) return;
            if (p.Stats.displayName == learner) { Game.I.StartCoroutine(Teach(p)); return; }
            if (Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message($"The musician smiles, but he is looking past you, at {learner}.", 4f); }
        }

        IEnumerator Teach(Hero hero)
        {
            Game.I.SetBusy(true); Playing = true;
            PlayLog.Line($"the Violinist plays for {hero.Stats.displayName}");
            Sfx.Play("violinist_tune", .8f);
            var face = Quaternion.LookRotation(Vector3.ProjectOnPlane(hero.transform.position - transform.position, Vector3.up).normalized);
            transform.rotation = face;
            yield return Cutscenes.Play("ViolinistTeaches", transform.position, face);
            Playing = false;
            if (!SaveGame.Current.abilities.Contains(ability)) SaveGame.Current.abilities.Add(ability);
            Party.I.ApplyAbilities(); Taught++;
            Sfx.Play("story", .6f, 1.2f);
            Hud.Message($"{hero.Stats.displayName} learned the {moveName}! Jump, then press {Controls.Btn(Controls.Act.Dive)}. It breaks cracked ice and stuns enemies.", 7f);
            PlayLog.Line($"ability {ability} learned from the Violinist");
            SaveGame.Save("learned " + ability);
            Game.I.SetBusy(false);
        }
    }
}
