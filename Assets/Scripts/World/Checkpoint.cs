using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, items 8 and 10: a checkpoint flag. Touching it makes it the respawn point and saves the game.
    public sealed class Checkpoint : MonoBehaviour
    {
        public static readonly Dictionary<string, Checkpoint> All = new Dictionary<string, Checkpoint>();
        public string id; public float yaw;
        public bool silent;   // Round 4: an invisible checkpoint (the vortex space): no message when touched
        Renderer flag;
        public bool Active => SaveGame.Current.checkpoint == id;

        void OnEnable() { if (!string.IsNullOrEmpty(id)) All[id] = this; flag = transform.Find("Flag")?.GetComponent<Renderer>(); }
        void OnDisable() { if (All.TryGetValue(id, out var c) && c == this) All.Remove(id); }

        void Update()
        {
            if (flag) flag.material.color = Active ? new Color(.3f, 1f, .5f) : new Color(.9f, .85f, .3f);
            if (!Game.Playing || !Party.I || Active) return;
            var p = Party.I.Player.transform.position;
            if (new Vector2(p.x - transform.position.x, p.z - transform.position.z).sqrMagnitude < 1.6f * 1.6f && Mathf.Abs(p.y - transform.position.y) < 2.5f)
            {
                SaveGame.Current.checkpoint = id;
                Health.Refill();
                if (!silent) { Sfx.Play("checkpoint", .5f); Hud.Message("Checkpoint"); }
                SaveGame.Save("checkpoint " + id);
            }
        }

        public static Checkpoint Current => All.TryGetValue(SaveGame.Current.checkpoint ?? "", out var c) ? c : null;

        public static void RespawnParty()
        {
            var c = Current;
            if (c) Party.I.PlaceAll(c.transform.position + Vector3.up * .1f + Quaternion.Euler(0, c.yaw, 0) * Vector3.forward * 3f, Quaternion.Euler(0, c.yaw, 0));
            else { var a = Game.World.areas[0]; Party.I.PlaceAll(a.spawn, Quaternion.Euler(0, a.spawnYaw, 0)); }
            CameraRig.I?.SnapBehind();
        }
    }

    // Round 4: the vortex gate's glow, on while it is open
    public sealed class PortalLight : MonoBehaviour
    {
        public GameObject lightObject; Portal p;
        void Start() { p = GetComponent<Portal>(); }
        void Update() { if (lightObject && p) lightObject.SetActive(p.Open); }
    }

    // Round 1, item 9: a portal ring. Closed until its id is in the save's opened portals; walking in takes the party to the other
    // portal (saved there).
    public sealed class Portal : MonoBehaviour
    {
        public string id; public string toCheckpoint; public bool startsOpen;
        public int Uses { get; private set; }
        Transform disc; float cool;
        public bool Open => startsOpen || SaveGame.Current.portals.Contains(id);

        void Start() { disc = transform.Find("Disc"); }
        void Update()
        {
            if (disc) { disc.gameObject.SetActive(Open); disc.Rotate(0, 0, 90 * Time.deltaTime, Space.Self); }
            cool -= Time.deltaTime;
            if (!Open || cool > 0 || !Game.Playing || !Party.I) return;
            var p = Party.I.Player.Motor.Center;
            var local = transform.InverseTransformPoint(p);
            if (Mathf.Abs(local.z) < .6f && Mathf.Abs(local.x) < 1.7f && local.y > -.3f && local.y < 3.7f) Use();   // inside the arch (Round 7: little Kira's middle is only 0.25 m up)
        }

        public void Use()
        {
            if (!string.IsNullOrEmpty(toCheckpoint) && toCheckpoint == Vortex.Checkpoint) { Game.I.EnterVortex(this); return; }
            if (!Checkpoint.All.TryGetValue(toCheckpoint, out var to)) return;
            Uses++; cool = 2f;
            if (!SaveGame.Current.portals.Contains(id)) SaveGame.Current.portals.Add(id);
            SaveGame.Current.checkpoint = toCheckpoint;
            foreach (var other in FindObjectsByType<Portal>()) other.cool = 2f;
            Checkpoint.RespawnParty();
            Sfx.Play("portal", .6f);
            PlayLog.Line($"portal {id} -> {toCheckpoint}");
            SaveGame.Save("portal " + id);
            Game.I.OnAreaChanged();
        }
    }
}
