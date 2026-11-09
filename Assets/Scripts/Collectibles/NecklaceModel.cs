using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, item 8: Dan's necklace as its own model (Resources/Characters/necklace.fbx from Tools/Blender/necklace.py: Cord, Bail,
    // Shard; origin = the neck's centre at the collar). The socket: a Mixamo skeleton's chest bone ("...Spine2") when Dan has his real
    // model (Round 3: both outfits use the same skeleton, so the same offset); on the graybox capsule the cord is widened to wrap the
    // capsule and the shard keeps its real size. The shard glows and pulses with the necklace logic (Necklace.Pulse).
    public static class NecklaceModel
    {
        public static readonly Color Dim = new Color(.25f, .55f, .75f), Bright = new Color(.6f, .97f, 1f);
        // Mixamo Spine2 -> collar (metres, bone space); a first guess for Round 3 to tune on the real model
        public static readonly Vector3 Spine2Offset = new Vector3(0, .17f, .02f);

        public static Transform Attach(Transform body, CharacterStats s)
        {
            var prefab = Resources.Load<GameObject>("Characters/necklace");
            if (!prefab) return null;
            var socket = Find(body, "Spine2");
            var go = Object.Instantiate(prefab, socket ? socket : body, false); go.name = "Necklace";
            var shard = go.transform.Find("Shard"); var bail = go.transform.Find("Bail");
            var glow = Placeholders.Mat(Bright, "Glow"); var cord = Placeholders.Mat(new Color(.25f, .15f, .08f)); var gold = Placeholders.Mat(new Color(.8f, .65f, .35f));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = r.name.StartsWith("Shard") ? new Material(glow) : r.name.StartsWith("Bail") ? gold : cord;
            if (socket) { go.transform.localPosition = Spine2Offset; go.transform.localRotation = CharacterModel.BlenderFront; return shard; }
            // the capsule (Placeholders.HeroBody): the collar where the head meets the torso; the capsule is ~0.16 m round there and
            // ~0.3 m round the chest, so the cord is stretched to lie on it
            float H = s.height, r0 = s.radius;
            go.transform.localPosition = new Vector3(0, H * .79f, 0);
            go.transform.localRotation = CharacterModel.BlenderFront;
            go.transform.localScale = new Vector3(2.0f, 1.0f, 3.0f);
            foreach (var t in new[] { bail, shard })
            {
                if (!t) continue;
                t.SetParent(body, true); t.localScale = Vector3.one; t.localRotation = Quaternion.identity;
                var p = t.localPosition; p.z = Mathf.Max(p.z, r0 * .86f); t.localPosition = p;   // on the capsule's surface
            }
            if (shard) shard.SetParent(go.transform.parent, true);
            return shard;
        }

        // pulse: 0..1 (Necklace.Pulse); lit: a shard to lead to
        public static void Pulse(Transform shard, float pulse, bool lit)
        {
            if (!shard) return;
            shard.localScale = Vector3.one * (1f + pulse * .45f);
            var r = shard.GetComponent<Renderer>();
            if (r) r.sharedMaterial.color = Color.Lerp(Dim, Bright, lit ? .45f + pulse * .55f : .15f);
        }

        static Transform Find(Transform t, string suffix)
        {
            if (t.name.EndsWith(suffix)) return t;
            foreach (Transform c in t) { var f = Find(c, suffix); if (f) return f; }
            return null;
        }
    }
}
