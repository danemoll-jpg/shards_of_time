using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 8: the Medieval Kingdom's world pieces (World/Medieval.cs places them).

    // Something dragon Kira's fire puff lights or burns (Hero.FirePuff). Saved by id.
    //  Torch / Brazier: lit (its flame child and a warm light come on); a FireGroup counts them.
    //  Rope: burns through; its `drops` object falls to `dropTo` (a plank bridge, a crate) and stays there.
    //  Hay / Bramble: flares up and is gone (what was behind it is open).
    public sealed class FireTarget : MonoBehaviour
    {
        public enum Kind { Torch, Brazier, Rope, Hay, Bramble }
        public static readonly List<FireTarget> All = new List<FireTarget>();
        public Kind kind; public string id;
        public GameObject flame; public Light lamp;
        public Transform drops; public Vector3 dropTo; public Quaternion dropRot = Quaternion.identity;
        public FireGroup group;
        public bool Lit { get; private set; }
        public static int Burns { get; private set; }
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        bool Saved => !string.IsNullOrEmpty(id) && SaveGame.Current.World(Game.World.worldId).opened.Contains(id);

        void Start()
        {
            if (Saved) Apply(true);
            else { if (flame) flame.SetActive(false); if (lamp) lamp.enabled = false; }
        }

        public void Burn(Hero by)
        {
            if (Lit) return;
            Burns++;
            PlayLog.Line($"fire puff: {kind} {id} by {by.Stats.displayName}");
            if (!string.IsNullOrEmpty(id)) { var prog = SaveGame.Current.World(Game.World.worldId); if (!prog.opened.Contains(id)) prog.opened.Add(id); }
            if (kind == Kind.Hay || kind == Kind.Bramble) { Sfx.Play("hay_fire", .6f); StartCoroutine(Flare()); }
            else if (kind == Kind.Rope) { Sfx.Play("hay_fire", .5f, 1.3f); StartCoroutine(Fall()); }
            else { Sfx.Play("fire_puff", .5f, .8f); Apply(false); }
            SaveGame.Save("burned " + id);
        }

        void Apply(bool loading)
        {
            Lit = true;
            if (flame) flame.SetActive(true); if (lamp) lamp.enabled = true;
            if (kind == Kind.Hay || kind == Kind.Bramble) gameObject.SetActive(false);
            if (kind == Kind.Rope) { if (drops) drops.SetPositionAndRotation(dropTo, dropRot); gameObject.SetActive(false); }
            if (group) group.Check(loading);
        }

        IEnumerator Flare()
        {
            for (int i = 0; i < 8; i++) { Placeholders.Puff(transform.position + Random.insideUnitSphere * 1f + Vector3.up * .8f, Color.Lerp(new Color(1f, .8f, .3f), new Color(1f, .3f, .1f), Random.value)); yield return new WaitForSeconds(.15f); }
            Apply(false);
        }

        IEnumerator Fall()
        {
            Lit = true;
            if (drops)
            {
                Vector3 p0 = drops.position; var r0 = drops.rotation;
                for (float t = 0; t < 1f; t += Time.deltaTime / .7f) { float e = t * t; drops.SetPositionAndRotation(Vector3.Lerp(p0, dropTo, e), Quaternion.Slerp(r0, dropRot, e)); yield return null; }
                Placeholders.Puff(dropTo, new Color(.7f, .6f, .45f)); Sfx.Play("boulder_roll", .4f, 1.4f);
            }
            Apply(false);
        }
    }

    // a set of torches / braziers: all lit -> `onAllLit` (a gate swings open, a chest opens); saved through the targets themselves
    public sealed class FireGroup : MonoBehaviour
    {
        public readonly List<FireTarget> targets = new List<FireTarget>();
        public System.Action<bool> onAllLit;   // loading: true when restored from the save (no fanfare)
        public string hint;
        public bool Done { get; private set; }
        public void Check(bool loading)
        {
            if (Done) return;
            int lit = 0; foreach (var t in targets) if (t.Lit) lit++;
            if (lit < targets.Count) { if (!loading && hint != null) Hud.Message($"{lit} of {targets.Count} {hint}", 2.5f); return; }
            Done = true; PlayLog.Line($"fire group {name}: all {targets.Count} lit");
            onAllLit?.Invoke(loading);
        }
    }

    // something that reacts to a punch (Hero.TryAttack / ChargedPunch): the high striker's pad
    public abstract class PunchTarget : MonoBehaviour { public abstract void Punched(Hero by, bool charged); }

    // The tournament's test of strength (a high striker): strike the pad, the puck shoots up the post; ring the bell at the top to win.
    // Muscle Man rings it every time; Dan's punch only gets the puck a third of the way (the charged punch: so close!), and the rival
    // laughs. Nothing hangs on it (comedy; rule 2).
    public sealed class HighStriker : PunchTarget
    {
        public Transform puck; public float top = 5.4f, bottom = .5f;
        public RivalMuscleMan rival;
        public int Tries { get; private set; }
        public float LastHeight { get; private set; }
        float busyUntil;
        public override void Punched(Hero by, bool charged)
        {
            if (!by.IsPlayer || Time.time < busyUntil && !charged) return;
            if (charged) StopAllCoroutines();   // the charged punch comes right after the plain one: it counts
            Tries++;
            float share = by.IsMuscleMan ? 1f : charged ? .86f : by.Stats.displayName == "Dan" ? .35f : by.Stats.displayName == "Abi" ? .3f : .12f;
            StartCoroutine(Shoot(share, by));
        }
        public IEnumerator Shoot(float share, Hero by)
        {
            busyUntil = Time.time + 2.5f; LastHeight = share;
            Sfx.Play("punch_big", .5f, 1.2f);
            float h = Mathf.Lerp(bottom, top, share);
            for (float t = 0; t < 1f; t += Time.deltaTime / .45f) { if (puck) puck.localPosition = new Vector3(puck.localPosition.x, Mathf.Lerp(bottom, h, 1 - (1 - t) * (1 - t)), puck.localPosition.z); yield return null; }
            if (share >= 1f) { Sfx.Play("bell_ring", .7f); Sfx.Play("crowd_cheer", .5f); }
            yield return new WaitForSeconds(.3f);
            for (float t = 0; t < 1f; t += Time.deltaTime / .6f) { if (puck) puck.localPosition = new Vector3(puck.localPosition.x, Mathf.Lerp(h, bottom, t * t), puck.localPosition.z); yield return null; }
            if (by && !by.IsMuscleMan)
            {
                PlayLog.Line($"the high striker: {by.Stats.displayName} {(share >= .8f ? "nearly" : "barely")} ({share:0.00})");
                Hud.Message(share >= .8f ? "So close! The bell doesn't ring." : "The puck barely gets going.", 3f);
                if (rival && rival.isActiveAndEnabled) { yield return new WaitForSeconds(.2f); rival.Mock(by); }
            }
        }
    }

    // Muscle Man before he joins (item 5): a rival who shows off and mocks Dan. One of these per spot (the courtyard, the tournament
    // field, the great hall); the first time the party comes near he does his bit (lift something with one hand, flex, point and
    // laugh), and he laughs again when Dan tries something like it and fails. Grunts and laughs only. He blocks nothing and can't be
    // fought (no collider). Gone once he has joined.
    public sealed class RivalMuscleMan : MonoBehaviour
    {
        public static readonly List<RivalMuscleMan> All = new List<RivalMuscleMan>();
        public string spot; public float radius = 11f;
        public Transform lifted; public Vector3 liftedHome; public string bit;
        public HighStriker striker;
        public CreatureAnimator Anim { get; private set; }
        public int Shows { get; private set; }
        public int Mocks { get; private set; }
        public bool Busy { get; private set; }
        public static int TotalShows, TotalMocks;
        float lastShow = -100f, mockAt = -10f; string loop = "idle";

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static RivalMuscleMan Make(Transform parent, string spot, Vector3 at, float yaw, string bit)
        {
            var go = new GameObject("Muscle Man (rival, " + spot + ")"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var r = go.AddComponent<RivalMuscleMan>(); r.spot = spot; r.bit = bit;
            if (CharacterModel.Exists("muscleman_medieval"))
            {
                var m = CharacterModel.Spawn("muscleman_medieval", go.transform);
                r.Anim = CreatureAnimator.Attach(m, "muscleman_medieval", null);
                r.Anim.Drive = () => (r.loop, 0f);
            }
            else
            {
                Placeholders.Prim(PrimitiveType.Capsule, go.transform, new Vector3(0, 1f, 0), new Vector3(1f, 1f, .7f), Placeholders.Mat(new Color(.2f, .25f, .4f)), name: "Body");
                Placeholders.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.85f, 0), new Vector3(.4f, .42f, .4f), Placeholders.Mat(new Color(.9f, .7f, .55f)), name: "Head");
            }
            go.SetActive(!SaveGame.Current.Has(Abilities.MuscleMan));
            return r;
        }

        void Start() { Boulder.Tried += OnTried; }
        void OnDestroy() { Boulder.Tried -= OnTried; }
        void OnTried(Boulder b, Hero h) { if (h && Vector3.Distance(b.transform.position, transform.position) < 16f) Mock(h); }

        void Update()
        {
            if (!Game.Playing || !Party.I || Busy || Game.I.Busy) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < radius && Time.time > lastShow + 45f) StartCoroutine(Show());
            else if (d.magnitude < radius * 1.5f && d.sqrMagnitude > .01f)   // he keeps an eye on Dan
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 60f * Time.deltaTime);
        }

        void Play(string clip) { if (Anim && Anim.Has(clip)) Anim.Trigger(clip); }

        IEnumerator Show()
        {
            Busy = true; lastShow = Time.time; Shows++; TotalShows++;
            PlayLog.Line($"the rival Muscle Man shows off ({spot}: {bit})");
            var first = Shows == 1;
            if (first) Hud.Message(spot == "courtyard" ? "A huge strongman lounges by the courtyard wall, admiring himself..." : spot == "field" ? "The strongman again, at the test of strength. The crowd adores him." : "The strongman, at the feast. Of course.", 4f);
            if (striker)
            {
                // the tournament: he strikes the pad, the bell rings, he flexes for the crowd
                Play("punch"); yield return new WaitForSeconds(.35f);
                yield return striker.Shoot(1f, null);
                Play("mm_flex"); Sfx.Play("mm_grunt", .6f); yield return new WaitForSeconds(2.2f);
                if (first) Hud.Message($"Ding! Your turn, Dan... ({Controls.Btn(Controls.Act.Attack)} at the pad)", 4f);
            }
            else if (lifted)
            {
                // he lifts the thing over his head with one hand, holds it, sets it down
                Play("mm_lift"); Sfx.Play("mm_grunt", .7f);
                var up = transform.position + transform.forward * .4f + Vector3.up * 2.6f;
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.1f) { lifted.position = Vector3.Lerp(liftedHome, up, Mathf.SmoothStep(0, 1, t)); yield return null; }
                yield return new WaitForSeconds(1.4f);
                for (float t = 0; t < 1f; t += Time.deltaTime / .8f) { lifted.position = Vector3.Lerp(up, liftedHome, t * t); yield return null; }
                Sfx.Play("boulder_roll", .35f, 1.6f);
                Play("mm_flex"); yield return new WaitForSeconds(2.2f);
            }
            else { Play("mm_flex"); Sfx.Play("mm_grunt", .6f); yield return new WaitForSeconds(2.2f); }
            yield return MockNow(Party.I.Player);
            Busy = false;
        }

        public void Mock(Hero h) { if (!Busy && Time.time > mockAt + 2.5f && isActiveAndEnabled) StartCoroutine(MockAndFree(h)); }
        IEnumerator MockAndFree(Hero h) { Busy = true; yield return MockNow(h); Busy = false; }
        IEnumerator MockNow(Hero h)
        {
            mockAt = Time.time; Mocks++; TotalMocks++;
            var d = h.transform.position - transform.position; d.y = 0;
            if (d.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(d);
            Play("mm_taunt"); Sfx.Play("mm_laugh", .7f);
            PlayLog.Line($"the rival Muscle Man laughs at {h.Stats.displayName} ({spot})");
            if (Mocks == 1 && spot != "field") Hud.Message("He points at Dan and laughs. Loudly.", 3f);
            yield return new WaitForSeconds(2.4f);
        }
    }

    // the dungeon: a key opens its cell (touch it); saved
    public sealed class CellDoor : MonoBehaviour
    {
        public string id; public float hintAt = -10;
        public bool Open => SaveGame.Current.World(Game.World.worldId).opened.Contains(id);
        void Start() { if (Open) gameObject.SetActive(false); }
        public void Unlock()
        {
            var prog = SaveGame.Current.World(Game.World.worldId); if (!prog.opened.Contains(id)) prog.opened.Add(id);
            Sfx.Play("portcullis", .45f, 1.4f); Placeholders.Puff(transform.position + Vector3.up, new Color(.5f, .5f, .55f));
            PlayLog.Line($"cell {id} unlocked"); SaveGame.Save("cell " + id);
            gameObject.SetActive(false);
        }
        void Update()
        {
            if (!Game.Playing || !Party.I || Open) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < 2.2f && Time.time > hintAt + 6f) { hintAt = Time.time; Hud.Message("A locked cell. Its key must be somewhere down here.", 3f); }
        }
    }
    public sealed class CellKey : MonoBehaviour
    {
        public CellDoor door; public static int Found;
        void Start() { if (door && door.Open) gameObject.SetActive(false); }
        void Update()
        {
            transform.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            if (!Game.Playing || !Party.I || !door) return;
            if (Vector3.Distance(Party.I.Player.Motor.Center, transform.position) < 1.1f)
            {
                Found++; Sfx.Play("shard", .6f, 1.3f); Hud.Message("A big iron key! It opens one of the cells.", 3f);
                door.Unlock(); gameObject.SetActive(false);
            }
        }
    }

    // near the stone before the end: Dan's necklace hums (a rumble and a glow), nothing more
    public sealed class StoneHum : MonoBehaviour
    {
        public static int Hums;
        float at = -20;
        void Update()
        {
            if (!Game.Playing || !Party.I || Game.I.Busy) return;
            var prog = SaveGame.Current.World(Game.World.worldId); if (prog.storyShard) return;
            var d = Party.I.Player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < 4f && Time.time > at + 12f)
            {
                at = Time.time; Hums++; Sfx.Play("necklace", .35f, .8f);
                var who = Party.I.Player.Stats.displayName;
                Hud.Message(prog.necklaceAwake ? (who == "Dan" ? "The necklace blazes. The purple shard is singing to it..." : $"It won't budge for {who}. The shard is singing to Dan's necklace: switch to Dan.") :
                    $"A purple shard stuck fast in a stone. Dan's necklace hums near it... but nothing happens. ({SaveGame.Current.Total} of {Game.World.areas[0].requirement} shards)", 4.5f);
            }
        }
    }
}
