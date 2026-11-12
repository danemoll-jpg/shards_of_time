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
                    s.lockedAbility = Abilities.Pounce;   // Round 7: taught by Bethlehem's Violinist
                    break;
                case "MuscleMan":   // Round 8: the companion (a follower, never controlled): big, strong, a little slower
                    s.color = new Color(.25f, .3f, .45f); s.height = 2.0f; s.radius = .42f; s.runSpeed = 7.2f; s.acceleration = 40f; s.airControl = .5f;
                    s.jumpHeight = 1.5f; s.swimSpeed = 3.6f; s.swimmer = true; s.breathSeconds = 0f;
                    s.attackName = "Punch"; s.attackDamage = 3; s.attackRange = 1.1f; s.attackRadius = .8f; s.attackCooldown = .6f; s.canPushHeavy = true;
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
        // Round 7: Bethlehem
        public const string FlockHide = "FlockHide";         // lamb Kira: in a flock of sheep, watchmen, dogs and goats don't see her
        public const string WoolBounce = "WoolBounce";       // lamb Kira: a high springy bounce off hay bales and wool piles
        // Round 8: the Medieval Kingdom
        public const string Glide = "Glide";                 // dragon Kira: hold Jump in the air, her little wings slow the fall (long gaps)
        public const string FirePuff = "FirePuff";           // dragon Kira: her attack is a puff of fire (lights torches and braziers, burns ropes, hay, brambles; scares rats; never hurts people)
        // Round 10: the Pirate Seas
        public const string OtterSwim = "OtterSwim";         // otter Kira: fast swimming, any depth, no breath limit (the narrow gaps under water)
        public const string BellySlide = "BellySlide";       // otter Kira: Dive while running = a long belly-slide (far on wet decks)
        public const string Overboard = "Overboard";
        // Round 11: Victorian Steampunk
        public const string WindUp = "WindUp";               // mechanical Kira: standing on a gear socket she winds up a machine (lifts, catwalks, gear doors, the turntable)
        public const string MagnetPaws = "MagnetPaws";       // mechanical Kira: Jump at an iron pillar with a rivet strip = up it to the top
        public const string PressDone = "PressDone";         // not a move: the press is gone (after MuscleManHero); rule 2 counts it on arrival (it comes before play)         // not a move: the deep sea is open (after ShardOverboard); rule 2 never counts it on arrival
        // the charged punch (Dan, Medieval: hold attack, release) is ChargedPunch above; MuscleMan = he has joined (after the stone)

        // Round 8: every move a hero can learn from the Violinist (Dan learns two)
        public static bool Learns(string hero, string id) =>
            id == DeepDive || (hero == "Dan" ? id == GroundPound || id == ChargedPunch : hero == "Abi" ? id == DoubleJump || id == WallJump : hero == "Kira" && id == Pounce);   // Round 11: Abi's wall jump   // Round 10: everyone learns the deep dive
    }
}
