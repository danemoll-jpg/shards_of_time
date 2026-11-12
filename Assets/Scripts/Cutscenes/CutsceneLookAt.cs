using UnityEngine;

namespace ShardsOfTime
{
    // Fix 10.1, item 3: a cutscene actor turns its head (and some of its neck and chest) to whoever it speaks or reacts to. Sits on the
    // actor's Body; the Timeline keys `weight` (0 = the clip's own pose, 1 = looking) and `which` (the index into `targets`, held steps).
    // Applied after the clips (LateUpdate), so any clip keeps playing underneath. People: the humanoid head / neck / chest bones; Kira and
    // other rigs: the bones named head / neck. Author with CutsceneAuthoring's Look(k, actor, target, t0, t1).
    public sealed class CutsceneLookAt : MonoBehaviour
    {
        public Transform[] targets = new Transform[0];
        public float weight;
        public float which;
        public float maxAngle = 70f;
        Transform head, neck, chest; Vector3 headFwd; bool ready;
        public float LastAngle { get; private set; }   // the head's angle to its target after the turn (checks)

        public static Transform HeadOf(Transform t)
        {
            if (!t) return null;
            var a = t.GetComponentInChildren<Animator>();
            if (a && a.avatar && a.avatar.isHuman) { var h = a.GetBoneTransform(HumanBodyBones.Head); if (h) return h; }
            foreach (var c in t.GetComponentsInChildren<Transform>()) if (c.name.ToLowerInvariant().EndsWith("head") && c.GetComponent<Renderer>() == null) return c;
            return null;
        }

        void Setup()
        {
            ready = true;
            var a = GetComponentInChildren<Animator>();
            if (a && a.avatar && a.avatar.isHuman)
            {
                head = a.GetBoneTransform(HumanBodyBones.Head); neck = a.GetBoneTransform(HumanBodyBones.Neck);
                chest = a.GetBoneTransform(HumanBodyBones.UpperChest) ?? a.GetBoneTransform(HumanBodyBones.Chest);
            }
            else
            {
                foreach (var c in GetComponentsInChildren<Transform>())
                {
                    string n = c.name.ToLowerInvariant();
                    if (!head && n.EndsWith("head") && !c.GetComponent<Renderer>()) head = c;
                    else if (!neck && n.EndsWith("neck") && !c.GetComponent<Renderer>()) neck = c;
                }
            }
            if (!head) return;
            // the head's own axis that faces where the body faces, from the mesh's bind pose (the first frame of a scene may be a fall or a
            // crawl: measuring then gave a wrong axis and broken necks)
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int i = System.Array.IndexOf(smr.bones, head); var bp = smr.sharedMesh ? smr.sharedMesh.bindposes : null;
                if (i < 0 || bp == null || i >= bp.Length) continue;
                var bindHead = bp[i].inverse.rotation;                                    // the head in the mesh's space at bind time
                var fwdMesh = Quaternion.Inverse(smr.transform.rotation) * transform.forward;   // the body's facing in the mesh's space
                headFwd = Quaternion.Inverse(bindHead) * fwdMesh; return;
            }
            head = null;   // no bind pose to measure from: no look
        }

        void LateUpdate()
        {
            if (!ready) Setup();
            if (!head || weight <= .001f || targets.Length == 0) return;
            var tg = targets[Mathf.Clamp(Mathf.RoundToInt(which), 0, targets.Length - 1)]; if (!tg) return;
            var th = HeadOf(tg); Vector3 aim = th ? th.position : tg.position + Vector3.up * 1.4f;
            // up or down at most 30 deg (a head looking 11 m up at the Sphinx tips back, it doesn't fold)
            var to = aim - head.position; var flat = new Vector3(to.x, 0, to.z); float fl = Mathf.Max(.01f, flat.magnitude);
            float pitch = Mathf.Clamp(Mathf.Atan2(to.y, fl) * Mathf.Rad2Deg, -30f, 30f);
            aim = head.position + flat.normalized * fl + Vector3.up * Mathf.Tan(pitch * Mathf.Deg2Rad) * fl;
            float w = Mathf.Clamp01(weight);
            // chest 25 %, neck 30 %, the head the rest, each measured again after the one before
            foreach (var (b, share) in new[] { (chest, .25f), (neck, .4f), (head, 1f) })
            {
                if (!b) continue;
                var cur = head.rotation * headFwd; var want = aim - head.position; if (want.sqrMagnitude < 1e-4f) return;
                var turn = Quaternion.FromToRotation(cur, want.normalized);
                turn.ToAngleAxis(out float ang, out var axis); if (ang > 180f) ang -= 360f;
                ang = Mathf.Clamp(ang, -maxAngle, maxAngle) * share * w;
                b.rotation = Quaternion.AngleAxis(ang, axis) * b.rotation;
            }
            LastAngle = Vector3.Angle(head.rotation * headFwd, aim - head.position);
        }
    }
}
