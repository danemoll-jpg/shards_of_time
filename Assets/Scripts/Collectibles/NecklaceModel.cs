using UnityEngine;

namespace ShardsOfTime
{
    // Dan's necklace as its own model (Resources/Characters/necklace.fbx from Tools/Blender/necklace.py: Cord, Bail, Shard; origin = the
    // neck's centre at the collar, the shard 0.17 m below and 0.105 m in front of it). The shard glows and pulses with the necklace logic.
    //  - On Dan's real model (Round 3, both outfits): fitted to that model's own chest in its bind pose (the neck bone, the front of the
    //    clothes under the shard, the width of the neck), then parented to the Mixamo chest bone ("...Spine2"), so it moves with him.
    //  - On the graybox capsule (the fallback): the cord is widened to wrap the capsule.
    public static class NecklaceModel
    {
        public static readonly Color Dim = new Color(.25f, .55f, .75f), Bright = new Color(.6f, .97f, 1f);
        const float LoopX = .086f, ShardFront = .105f, Drop = .17f, Clear = .03f;   // necklace.py's sizes (m); Clear: shard off the cloth
        public static string LastFit { get; private set; } = "";

        public static Transform Attach(Transform body, CharacterStats s, GameObject model = null)
        {
            var prefab = Resources.Load<GameObject>("Characters/necklace");
            if (!prefab) return null;
            var anim = model ? model.GetComponentInChildren<Animator>() : null;
            var socket = model ? Find(model.transform, "Spine2") : null;
            var go = Object.Instantiate(prefab, body, false); go.name = "Necklace";
            var shard = go.transform.Find("Shard"); var bail = go.transform.Find("Bail");
            var glow = Placeholders.Mat(Bright, "Glow"); var cord = Placeholders.Mat(new Color(.25f, .15f, .08f)); var gold = Placeholders.Mat(new Color(.8f, .65f, .35f));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = r.name.StartsWith("Shard") ? new Material(glow) : r.name.StartsWith("Bail") ? gold : cord;
            if (socket && anim && anim.avatar && anim.avatar.isHuman) { Fit(go.transform, body, model, anim, socket); return shard; }
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

        // Fit on a humanoid model still in its bind pose (call before its clips play): measured in the body's space (+Z = his front)
        static void Fit(Transform necklace, Transform body, GameObject model, Animator anim, Transform socket)
        {
            var neckBone = anim.GetBoneTransform(HumanBodyBones.Neck);
            Vector3 neck = body.InverseTransformPoint(neckBone ? neckBone.position : socket.position + body.up * .17f);
            float front = neck.z + .06f, halfWidth = LoopX - .012f; int chest = 0;
            var baked = new Mesh();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.BakeMesh(baked, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (var v in baked.vertices)
                {
                    var p = body.InverseTransformPoint(m.MultiplyPoint3x4(v));
                    float dx = Mathf.Abs(p.x - neck.x), dy = p.y - neck.y;
                    if (dx < .045f && dy < -Drop + .03f && dy > -Drop - .03f && p.z > neck.z) { front = Mathf.Max(front, p.z); chest++; }    // the chest under the shard
                    if (dy > .02f && dy < .06f && Mathf.Abs(p.z - neck.z) < .05f && dx < .12f) halfWidth = Mathf.Max(halfWidth, dx);       // the neck just above the collar
                }
            }
            if (Application.isPlaying) Object.Destroy(baked); else Object.DestroyImmediate(baked);
            float sx = Mathf.Clamp((halfWidth + .012f) / LoopX, .9f, 1.6f);
            // the shard sits Clear in front of the clothes; the loop wide enough for the neck
            var local = new Vector3(neck.x, neck.y, front + Clear - ShardFront * sx);
            necklace.SetPositionAndRotation(body.TransformPoint(local), body.rotation * CharacterModel.BlenderFront);
            necklace.SetParent(socket, true);
            var ls = socket.lossyScale; necklace.localScale = new Vector3(sx / ls.x, 1f / ls.y, sx / ls.z);
            LastFit = $"necklace on {model.name}: socket {socket.name}, collar {neck.y:0.00} m up, chest front {front - neck.z:0.000} m ahead of the neck ({chest} skin points), " +
                      $"neck half-width {halfWidth:0.000} m -> loop x{sx:0.00}, shard {Clear * 100:0.0} cm off the clothes";
        }

        // pulse: 0..1 (Necklace.Pulse); lit: a shard to lead to
        // Round 4: in dark places (the ice cave) the shard gives a light: the party sees by it
        public static void Light(Transform shard, bool on)
        {
            if (!shard) return;
            var l = shard.Find("Necklace light");
            if (!l)
            {
                if (!on) return;
                l = new GameObject("Necklace light").transform; l.SetParent(shard, false); l.localPosition = Vector3.zero;
                var lt = l.gameObject.AddComponent<UnityEngine.Light>(); lt.type = LightType.Point; lt.color = new Color(.45f, .8f, 1f);
                lt.range = 13f; lt.intensity = 3.2f; lt.shadows = LightShadows.None;
            }
            if (l.gameObject.activeSelf != on) l.gameObject.SetActive(on);
            if (on) l.GetComponent<UnityEngine.Light>().intensity = 2.8f + Necklace.Pulse * 1.2f;
        }

        public static void Pulse(Transform shard, float pulse, bool lit)
        {
            if (!shard) return;
            shard.localScale = Vector3.one * (1f + pulse * .45f);
            var r = shard.GetComponent<Renderer>();
            if (r) r.sharedMaterial.color = Color.Lerp(Dim, Bright, lit ? .45f + pulse * .55f : .15f);
        }

        // the pieces of a necklace attached to this body (capsule or model), removed before a new one goes on
        public static void Remove(Transform body)
        {
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                var c = body.GetChild(i);
                if (c.name == "Necklace" || c.name == "Bail" || c.name == "Shard") { c.SetParent(null); if (Application.isPlaying) Object.Destroy(c.gameObject); else Object.DestroyImmediate(c.gameObject); }
            }
        }

        public static Transform Find(Transform t, string suffix)
        {
            if (t.name.EndsWith(suffix)) return t;
            foreach (Transform c in t) { var f = Find(c, suffix); if (f) return f; }
            return null;
        }
    }
}
