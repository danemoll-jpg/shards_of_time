using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, items 8 and 9: everything the player picks up by touching it. Small shards (saved by id, the never-spent total), the
    // story shard, heal hearts, heart upgrades (max +1, saved), ability pickups (saved). Collected by the player only, by distance.
    public sealed class Pickup : MonoBehaviour
    {
        public enum Kind { Shard, StoryShard, Heal, HeartUpgrade, Ability }
        public static readonly List<Pickup> All = new List<Pickup>();
        public Kind kind; public string id; public string area; public float radius = 1.0f;
        public Color color = Color.cyan;
        Vector3 basePos; float spin;

        void OnEnable() { All.Add(this); basePos = transform.position; spin = Random.value * 360f; }
        void OnDisable() => All.Remove(this);

        void Update()
        {
            spin += Time.deltaTime * (kind == Kind.StoryShard ? 60f : 120f);
            transform.SetPositionAndRotation(basePos + Vector3.up * Mathf.Sin(Time.time * 2f + spin * .01f) * .12f, Quaternion.Euler(0, spin, 0));
            if (!Game.Playing || !Party.I) return;
            var c = Party.I.Player.Motor.Center;
            float reach = radius + Party.I.Player.Stats.radius;
            // Round 8: the stone's shard answers only Dan's necklace (the film); anyone else gets a line from StoneHum
            if (kind == Kind.StoryShard && Game.InMedieval && Party.I.Player.Stats.displayName != "Dan") return;
            if ((c - transform.position).sqrMagnitude < reach * reach || (Party.I.Player.transform.position - transform.position).sqrMagnitude < reach * reach) Collect();
        }

        void Collect()
        {
            var s = SaveGame.Current;
            switch (kind)
            {
                case Kind.Shard:
                    var w = s.World(Game.World.worldId);
                    if (!w.shards.Contains(id)) w.shards.Add(id);
                    Sfx.Play("shard", .5f, 1f + (s.Total % 5) * .06f);
                    PlayLog.Line($"shard {id} ({s.Total} total)");
                    break;
                case Kind.StoryShard:
                    s.World(Game.World.worldId).storyShard = true;
                    Sfx.Play("story", .7f);
                    PlayLog.Line($"story shard of area {area}");
                    break;
                case Kind.Heal: Health.Heal(1); break;
                case Kind.HeartUpgrade: Hud.Message("Heart upgrade! One more heart."); Health.AddHeartUpgrade(id); Sfx.Play("story", .5f, 1.3f); break;
                case Kind.Ability:
                    if (!s.abilities.Contains(id)) s.abilities.Add(id);
                    Party.I.ApplyAbilities();
                    Sfx.Play("story", .6f, 1.2f);
                    PlayLog.Line($"ability {id} unlocked");
                    SaveGame.Save("ability");
                    break;
            }
            Placeholders.Puff(transform.position, color);
            gameObject.SetActive(false);
            Game.I.OnCollected(this);
        }

        public static Pickup Make(Kind kind, string id, string area, Vector3 pos, Transform parent, Color color)
        {
            var go = Placeholders.PickupVisual(kind, color, id);
            go.transform.SetParent(parent, false); go.transform.position = pos;
            var p = go.AddComponent<Pickup>(); p.kind = kind; p.id = id; p.area = area; p.color = color;
            if (kind == Kind.StoryShard) p.radius = 1.2f;
            return p;
        }
        public static Pickup HealHeart(Vector3 pos, Transform parent) => Make(Kind.Heal, "heal", "", pos, parent, new Color(1f, .3f, .4f));
    }
}
