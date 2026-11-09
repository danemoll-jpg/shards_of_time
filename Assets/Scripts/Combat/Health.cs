using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 8 (rule 5): one heart pool shared by all heroes. 3 hearts at the start, up to 10 with hidden heart upgrades (saved).
    // A hit takes hearts and gives a short invincibility; out of hearts, or falling out of the world, puts the party back at the last
    // checkpoint (out of hearts: full hearts again; a fall costs one heart).
    public static class Health
    {
        public const int StartHearts = 3, MaxHeartsCap = 10;
        public const float InvincibleSeconds = 1.3f;
        public static int Hearts { get; private set; } = StartHearts;
        public static int Max => SaveGame.Current.maxHearts;
        public static float InvincibleUntil { get; private set; }
        public static bool Invincible => Time.time < InvincibleUntil;
        public static int HitsTaken, Respawns, Ignored;
        public static string LastRespawn = "";

        public static void Refill() => Hearts = Max;

        public static bool Damage(int n, Vector3 knockFrom, string cause)
        {
            if (!Game.Playing || Party.I == null) return false;
            if (Invincible) { Ignored++; return false; }
            Hearts = Mathf.Max(0, Hearts - n); HitsTaken++;
            InvincibleUntil = Time.time + InvincibleSeconds;
            PlayLog.Line($"hurt by {cause}: {Hearts}/{Max} hearts");
            Sfx.Play("hurt", .6f);
            Hud.Flash();
            var p = Party.I.Player;
            if (knockFrom != Vector3.zero)
            {
                var away = p.transform.position - knockFrom; away.y = 0;
                if (away.sqrMagnitude < .01f) away = -p.transform.forward;
                p.Motor.Knockback(away.normalized * 6f + Vector3.up * 6f);
            }
            if (Hearts <= 0) Respawn("out of hearts", true);
            return true;
        }

        public static void Heal(int n) { Hearts = Mathf.Min(Max, Hearts + n); Sfx.Play("heal", .5f); PlayLog.Line($"healed: {Hearts}/{Max} hearts"); }

        public static void AddHeartUpgrade(string id)
        {
            var s = SaveGame.Current;
            if (s.heartUpgrades.Contains(id)) return;
            s.heartUpgrades.Add(id);
            s.maxHearts = Mathf.Min(MaxHeartsCap, s.maxHearts + 1);
            Hearts = s.maxHearts;
            PlayLog.Line($"heart upgrade {id}: max {s.maxHearts}");
            SaveGame.Save("heart upgrade");
        }

        public static void FellOut()
        {
            if (!Game.Playing) return;
            Hearts = Mathf.Max(0, Hearts - 1);
            PlayLog.Line($"fell out of the world: {Hearts}/{Max} hearts");
            Respawn("fell out of the world", Hearts <= 0);
        }

        static void Respawn(string why, bool refill)
        {
            Respawns++; LastRespawn = why;
            if (refill) Hearts = Max;
            InvincibleUntil = Time.time + InvincibleSeconds;
            Checkpoint.RespawnParty();
            Hud.Message(refill ? "Out of hearts! Back to the last checkpoint." : "Back to the last checkpoint.");
            PlayLog.Line($"respawn at checkpoint {SaveGame.Current.checkpoint} ({why}){(refill ? ", hearts refilled" : "")}");
        }
    }
}
