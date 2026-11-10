using System.Collections;
using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 5: Dan's music in the right places, the props upright, the bigger valley.
    public sealed partial class ExeCheck
    {
        // the track this place should play is the one playing (Dan's file, loaded, its source running; the check run is muted)
        void MusicCheck(string where, string track)
        {
            var srcs = Game.I.GetComponents<AudioSource>();
            var playing = srcs.FirstOrDefault(x => x.isPlaying && x.clip && x.clip.name == track);
            Ok(Music.Track == track && playing, $"music in {where}: {track}" + (playing ? $" ({playing.clip.length:0.0} s, at {playing.time:0.0} s)" : $" NOT playing (track {Music.Track ?? "none"}; sources: {string.Join(", ", srcs.Where(x => x.isPlaying && x.clip).Select(x => x.clip.name))})"));
        }

        // ---------------------------------------------------------------- item 3: a look round the whole valley (player's view) + the props audit
        static readonly (string n, float x, float z, float yaw)[] TourStops =
        {
            ("start", -5, 6, 0), ("pond", 6, 38, 60), ("snowfield_west", -30, 60, 80), ("ridge_foot", 30, 100, 10), ("ridge_shelf", 57, 180, 200),
            ("valley_middle", 0, 180, 0), ("bridge_south", -10, 228, 0), ("pillars", 20, 236, 0), ("north_bank", 0, 275, 180),
            ("cave_mouth", -12, 330, 270), ("cave_hall", -30, 330, 0), ("west_side", -45, 420, 45), ("vents", 34, 400, 30),
            ("terrace_top", 60, 420, 250), ("stones", 0, 456, 0), ("stones_back", 0, 500, 180), ("grotto", -36, 484, 270), ("east_wall", 50, 300, 300),
        };

        IEnumerator Tour()
        {
            yield return EnsurePlaying(false);
            Head("Item 3: a look round the valley from the player's view");
            foreach (var (n, x, z, yaw) in TourStops)
            {
                yield return TpSettle(IceAge.OnGround(x, z, n.StartsWith("cave") ? 5f : 80f) + Vector3.up * .1f, yaw);
                yield return Wait(.8f);
                yield return Shot($"t_{n}");
            }
            yield return PropAudit();
        }

        // every Blender prop in the valley: upright (its up = the world's up), its bottom on the ground (not floating, not sunk)
        IEnumerator PropAudit()
        {
            Head("Items 2-3: every Ice Age prop upright and on the ground");
            yield return null;
            var names = new[] { "Pine", "Stone", "Rock1", "Rock2", "Rock3", "Bones", "IceCrystal" };
            var props = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => names.Contains(t.name) && t.parent && t.parent.name == "World Ice Age").ToList();
            int bad = 0; var kinds = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var t in props)
            {
                kinds[t.name] = kinds.TryGetValue(t.name, out var c) ? c + 1 : 1;
                var rends = t.GetComponentsInChildren<Renderer>(); if (rends.Length == 0) continue;
                var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                var own = t.GetComponentsInChildren<Collider>();
                float G(float x, float z)
                {
                    var hits = Physics.RaycastAll(new Vector3(x, b.max.y + .5f, z), Vector3.down, 200f, ~((1 << Hero.PlayerLayer) | (1 << Hero.FollowerLayer) | (1 << Hero.EnemyLayer) | (1 << 4) | (1 << 2)), QueryTriggerInteraction.Ignore);
                    var h = hits.Where(hh => !own.Contains(hh.collider)).OrderBy(hh => hh.distance).FirstOrDefault();
                    return h.collider ? h.point.y : float.NaN;
                }
                bool tall = t.name == "Pine" || t.name == "Stone";
                bool upright = !tall || b.size.y > Mathf.Max(b.size.x, b.size.z);
                float gc = G(b.center.x, b.center.z);
                float lift = b.min.y - gc;                       // > 0: floating
                float sunk = gc - b.min.y;                       // how deep its bottom is under the ground at its centre
                float maxSunk = t.name == "Bones" ? .9f : t.name == "IceCrystal" ? .3f * b.size.y : t.name.StartsWith("Rock") ? .6f * b.size.y : .5f;   // crystal clusters lean: their lowest tip goes in
                bool ok = upright && !float.IsNaN(gc) && lift < .12f && sunk < maxSunk;
                if (!ok) { bad++; Log($"    BAD {t.name} at {Fmt(t.position)}: size {Fmt(b.size)}, {(upright ? "upright" : "LYING DOWN")}, bottom {(lift >= 0 ? $"{lift:0.00} m over" : $"{sunk:0.00} m under")} the ground at its centre"); }
            }
            Ok(bad == 0 && props.Count > 100, $"{props.Count} props ({string.Join(", ", kinds.Select(k => $"{k.Value} {k.Key}"))}): {(bad == 0 ? "all upright and on the ground" : bad + " wrong (listed above)")}");
        }
    }
}
