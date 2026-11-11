using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 5, items 5-6: the bigger Ice Age's world pieces. The gates the later moves open are data (WorldData.gates); these are
    // what the player meets on the first visit, so each one is visible and says what it waits for.

    // Slick ice (the frozen lake): little grip, so the heroes slide; `push` (m/s2, world) drives a skating slide lane.
    public sealed class SlickIce : BoxVolume
    {
        public static readonly List<SlickIce> All = new List<SlickIce>();
        public Vector3 push; public float grip = .18f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static SlickIce At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
    }

    // Deep water (the lake under the ice): without the deep dive (a longer breath: Pirate Seas) a diver is pushed back up.
    public sealed class DeepWater : BoxVolume
    {
        public static readonly List<DeepWater> All = new List<DeepWater>();
        float hintAt = -10f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public static DeepWater At(Vector3 p) { foreach (var v in All) if (v.Contains(p)) return v; return null; }
        public void Hint() { if (Time.time > hintAt + 5f) { hintAt = Time.time; Hud.Message("Too deep! Nobody can hold their breath that long... yet.", 4f); } }
    }

    // A cracked rock wall (the hot-spring grotto): only Dan's charged punch (the Medieval Kingdom) breaks it. Saved when broken.
    public sealed class RockWall : MonoBehaviour
    {
        public static readonly List<RockWall> All = new List<RockWall>();
        public string id;
        float hintAt = -10f;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Punch(Hero by, bool charged)
        {
            if (!charged)
            {
                if (Time.time > hintAt + 3f)
                {
                    hintAt = Time.time;
                    Hud.Message(by.HasAbility(Abilities.ChargedPunch) ? $"Cracked! Hold {Controls.Btn(Controls.Act.Attack)} to charge Dan's punch, then let go." : "Cracked rock. A much stronger punch could break it... (later)", 4f);
                }
                return;
            }
            var prog = SaveGame.Current.World(Game.World.worldId);
            if (!prog.opened.Contains(id)) prog.opened.Add(id);
            Placeholders.Puff(transform.position, new Color(.55f, .45f, .4f)); Sfx.Play("wall_break", .7f);
            PlayLog.Line($"rock wall {id} broken by {by.Stats.displayName}");
            gameObject.SetActive(false);
            SaveGame.Save("rock wall");
        }
    }
}
