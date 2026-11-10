using UnityEngine;

namespace ShardsOfTime
{
    // Round 1, item 6: a character's numbers as data (Assets/Resources/Data/Characters/<Name>.asset; made by Editor/DataAssets.cs
    // with these defaults if missing, then edited there). 1 unit = 1 m.
    [CreateAssetMenu(menuName = "Shards of Time/Character Stats")]
    public sealed class CharacterStats : ScriptableObject
    {
        public string displayName = "Dan";
        public Color color = new Color(.25f, .5f, .9f);
        [Header("Body")]
        public float height = 1.8f;
        public float radius = .32f;
        [Header("Run and jump")]
        public float runSpeed = 7f;
        public float acceleration = 45f;
        [Tooltip("Share of the ground acceleration in the air")] public float airControl = .5f;
        public float jumpHeight = 1.6f;
        public float gravity = 30f;
        [Tooltip("Velocity kept when jump is released early (variable height)")] public float jumpCut = .45f;
        [Header("Swim")]
        public float swimSpeed = 4f;
        [Tooltip("Swimmers never run out of breath")] public bool swimmer;
        public float breathSeconds = 10f;
        [Header("Attack")]
        public string attackName = "Punch";
        public int attackDamage = 2;
        public float attackRange = 1.1f;
        public float attackRadius = .7f;
        public float attackCooldown = .4f;
        [Header("Moves")]
        public bool canPushHeavy;
        [Tooltip("Ability id this character can unlock (e.g. DoubleJump); empty = none")] public string lockedAbility = "";
        public float doubleJumpHeight = 1.5f;

        public static CharacterStats Defaults(string name)
        {
            var s = CreateInstance<CharacterStats>(); s.name = name; s.displayName = name;
            switch (name)
            {
                case "Dan":
                    s.color = new Color(.22f, .48f, .92f); s.height = 1.8f; s.radius = .32f; s.runSpeed = 7f; s.acceleration = 45f; s.airControl = .5f;
                    s.jumpHeight = 1.6f; s.swimSpeed = 3.6f; s.swimmer = false; s.breathSeconds = 16f;
                    s.attackName = "Punch"; s.attackDamage = 2; s.attackRange = 1.0f; s.attackRadius = .7f; s.attackCooldown = .45f; s.canPushHeavy = true;
                    s.lockedAbility = Abilities.GroundPound;
                    break;
                case "Abi":
                    s.color = new Color(.93f, .36f, .62f); s.height = 1.65f; s.radius = .3f; s.runSpeed = 8.2f; s.acceleration = 55f; s.airControl = .85f;
                    s.jumpHeight = 2.1f; s.swimSpeed = 4f; s.swimmer = false; s.breathSeconds = 16f;
                    s.attackName = "Kick"; s.attackDamage = 1; s.attackRange = 1.25f; s.attackRadius = .75f; s.attackCooldown = .35f;
                    s.lockedAbility = Abilities.DoubleJump; s.doubleJumpHeight = 1.6f;
                    break;
                case "Kira":
                    s.color = new Color(.5f, .52f, .55f); s.height = .5f; s.radius = .22f; s.runSpeed = 7.6f; s.acceleration = 60f; s.airControl = .7f;
                    s.jumpHeight = 1.4f; s.swimSpeed = 6f; s.swimmer = true; s.breathSeconds = 0f;
                    s.attackName = "Claws"; s.attackDamage = 1; s.attackRange = .7f; s.attackRadius = .6f; s.attackCooldown = .25f;
                    break;
            }
            return s;
        }
    }

    public static class Abilities
    {
        public const string DoubleJump = "DoubleJump";   // Abi, unlocked in Area B
        public const string MuscleMan = "MuscleMan";   // the companion from the Medieval Kingdom: moves boulders (not yet)
        public const string ClawIce = "ClawIce";       // Kira's sabertooth form (Ice Age): her claws break ice walls (Round 2)
        public const string GroundPound = "GroundPound"; // Dan, taught by the Violinist in the Ice Age (Round 4): slam down, break cracked ice, stun enemies
        // Round 5: later moves (the move roadmap in TODO.md); the Ice Age's gates wait for them (WorldData.gates), they come in their worlds
        public const string ChargedPunch = "ChargedPunch";   // Dan, Medieval Kingdom: breaks cracked rock walls
        public const string DeepDive = "DeepDive";           // everyone, Pirate Seas: a longer breath, deep water
        public const string WallJump = "WallJump";           // Abi, Steampunk: up narrow chimneys
        // Round 6: Egypt
        public const string Pounce = "Pounce";               // Kira, Bethlehem: a long pounce over wide gaps (Egypt's rooftop gaps wait for it)
        public const string MummyHush = "MummyHush";         // mummy Kira (Egypt): mummies don't wake for her
    }
}
