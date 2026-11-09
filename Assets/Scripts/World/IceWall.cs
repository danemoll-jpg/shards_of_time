using System.Collections.Generic;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, item 7: a wall of ice that only a hero with the ClawIce ability breaks (Kira's sabertooth form: her attack, next to it).
    // A broken wall is remembered in the save (WorldProgress.opened). Anyone else hitting it gets a hint.
    public sealed class IceWall : MonoBehaviour
    {
        public static readonly List<IceWall> All = new List<IceWall>();
        public string id;
        public bool Broken { get; private set; }
        float hintAt = -10f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Claw(Hero by)
        {
            if (Broken) return;
            if (!by.HasAbility(Abilities.ClawIce))
            {
                if (Time.time > hintAt + 3f) { hintAt = Time.time; Hud.Message("Too hard! Only sabertooth Kira's claws can break this ice.", 3f); }
                return;
            }
            Broken = true;
            var prog = SaveGame.Current.World(Game.World.worldId);
            if (!prog.opened.Contains(id)) prog.opened.Add(id);
            Sfx.Play("attack", .7f, .6f);
            Placeholders.Puff(transform.position, new Color(.75f, .92f, 1f));
            PlayLog.Line($"ice wall {id} broken by {by.Stats.displayName} ({by.Form?.displayName})");
            gameObject.SetActive(false);
            SaveGame.Save("ice wall");
        }
    }
}
