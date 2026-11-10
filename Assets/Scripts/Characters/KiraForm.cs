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

        public const string Home = "Home", Sabertooth = "Sabertooth", Mummy = "Mummy", Lamb = "Lamb";

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
                default:
                    f.displayName = "Kira"; f.model = "kira"; f.abilities = new string[0];
                    f.note = "At home: the Scottish Fold. The base every form is built from.";
                    break;
            }
            return f;
        }
    }
}
