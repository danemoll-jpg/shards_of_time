using System.Linq;
using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, item 7: one of Kira's forms as data (Assets/Resources/Data/KiraForms/<id>.asset, made by Editor/DataAssets.cs from
    // Defaults() if missing). Each world area names Kira's form (AreaData.kiraForm); arriving there swaps her model and her abilities.
    // Every form model keeps the Kira lock (folded ears, solid grey fur, golden-amber eyes): Editor/CharacterImport.cs checks it at build.
    [CreateAssetMenu(menuName = "Shards of Time/Kira Form")]
    public sealed class KiraForm : ScriptableObject
    {
        public string id = "Home";
        public string displayName = "Kira";
        [Tooltip("Resources/Characters/<model>.fbx (rigged by Tools/Blender/kira_rig.py); missing = the graybox body")]
        public string model = "kira";
        [Tooltip("Ability ids this form brings (Abilities.*)")] public string[] abilities = new string[0];
        [TextArea] public string note;

        public bool Has(string ability) => abilities != null && abilities.Contains(ability);

        public const string Home = "Home", Sabertooth = "Sabertooth", Mummy = "Mummy", Lamb = "Lamb", Dragon = "Dragon", Otter = "Otter", Mechanical = "Mechanical", Robot = "Robot", Space = "Space";

        public static KiraForm Load(string id)
        {
            if (string.IsNullOrEmpty(id)) id = Home;
            return Resources.Load<KiraForm>("Data/KiraForms/" + id) ?? Defaults(id);
        }

        public static KiraForm Defaults(string id)
        {
            var f = CreateInstance<KiraForm>(); f.name = id; f.id = id;
            switch (id)
            {
                case Sabertooth:
                    f.displayName = "Sabertooth Kira"; f.model = "kira_sabertooth"; f.abilities = new[] { Abilities.ClawIce };
                    f.note = "Ice Age: a saber-toothed kitten. Her claws break ice walls.";
                    break;
                case Mummy:   // Round 6
                    f.displayName = "Mummy Kira"; f.model = "kira_mummy"; f.abilities = new[] { Abilities.MummyHush };
                    f.note = "Ancient Egypt: a little mummy cat (folded ears showing through the bandages). Mummies don't wake for her; cat-statue doors open for her (any form).";
                    break;
                case Lamb:   // Round 7
                    f.displayName = "Lamb Kira"; f.model = "kira_lamb"; f.abilities = new[] { Abilities.FlockHide, Abilities.WoolBounce };
                    f.note = "Bethlehem: a round gray woolly lamb with her own cat face (folded ears, amber eyes). In a flock nobody notices her; she bounces high off hay and wool.";
                    break;
                case Dragon:   // Round 8
                    f.displayName = "Dragon Kira"; f.model = "kira_dragon"; f.abilities = new[] { Abilities.Glide, Abilities.FirePuff };
                    f.note = "The Medieval Kingdom: a tiny gray baby dragon with her own cat face (folded ears, amber eyes), little wings and nub horns. She glides (hold Jump in the air) and puffs fire (her attack): torches, braziers, ropes, hay and brambles; rats run from it.";
                    break;
                case Otter:   // Round 10
                    f.displayName = "Otter Kira"; f.model = "kira_otter"; f.abilities = new[] { Abilities.OtterSwim, Abilities.BellySlide };
                    f.note = "The Pirate Seas: a little gray river otter with her own cat face (folded ears, amber eyes). She swims fast, dives as deep as she likes with no breath limit, squeezes through narrow gaps under water, and belly-slides across wet decks (Dive while running).";
                    break;
                case Mechanical:   // Round 11
                    f.displayName = "Mechanical Kira"; f.model = "kira_mechanical"; f.abilities = new[] { Abilities.WindUp, Abilities.MagnetPaws };
                    f.note = "Victorian Steampunk: a little clockwork cat of gunmetal and brass with her own face (folded ears, amber eyes). Standing on a gear socket she winds up machines (lifts, catwalks, gear doors, the turntable); her magnet paws carry her up iron pillars marked with rivets (Jump at one).";
                    break;
                case Robot:   // Round 12
                    f.displayName = "Robot Kira"; f.model = "kira_robot"; f.abilities = new[] { Abilities.PlugIn, Abilities.WheelDash };
                    f.note = "Tomorrowland: a shiny gunmetal robot cat with her own face (folded ears, amber eyes) and little wheels on her feet. On a glowing terminal pad she plugs in and takes over machines (walkways, flying cars, doors, cleanbots); Dive on the ground rolls her fast on her wheels, and boost ramps launch her.";
                    break;
                case Space:   // Round 13
                    f.displayName = "Space Kira"; f.model = "kira_moon"; f.abilities = new[] { Abilities.Float };
                    f.note = "The Moon: Kira in a little silver spacesuit and bubble helmet (her own gray face, folded ears, amber eyes). Hold Jump in the air and she floats slowly upward for a moment, paddling her paws: high ledges.";
                    break;
                default:
                    f.displayName = "Kira"; f.model = "kira"; f.abilities = new string[0];
                    f.note = "At home: the Scottish Fold. The base every form is built from.";
                    break;
            }
            return f;
        }
    }
}
