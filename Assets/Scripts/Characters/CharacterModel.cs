using UnityEngine;

namespace ShardsOfTime
{
    // A real model for a hero (Round 2: Kira's forms; Round 3: Dan and Abi in both outfits), from Resources/Characters/<model>.fbx and
    // its material Resources/Characters/<model>.mat (made at import by Editor/CharacterImport.cs). Missing = null: the caller keeps the
    // graybox body.
    public static class CharacterModel
    {
        // Blender's front (-Y) arrives as Unity's back (-Z) with the FBX export defaults: turn the model to face +Z
        public static readonly Quaternion BlenderFront = Quaternion.Euler(0, 180, 0);

        public static bool Exists(string model) => !string.IsNullOrEmpty(model) && Resources.Load<GameObject>("Characters/" + model);

        public static GameObject Build(string model, Transform parent, CharacterMotor motor)
        {
            // the character controller rests its skin width above the ground: the feet go down by that much
            var go = Spawn(model, parent, motor ? -motor.Cc.skinWidth : 0f);
            if (!go) return null;
            CreatureAnimator.Attach(go, model, motor);
            return go;
        }

        // Round 5: a creature's model (wolf, snow_critter, mammoth) on its placeholder. The placeholder's shapes move into a hidden
        // "Graybox" child (kept: the fallback when the model is missing); `keep` names children that stay (the mammoth's Back collider,
        // its renderer hidden). Returns the model's animator, or null (placeholder shown).
        public static CreatureAnimator Creature(Transform t, string model, float walk, float run, params string[] keep)
        {
            if (!Exists(model)) return null;
            var gray = new GameObject("Graybox").transform; gray.SetParent(t, false);
            var kids = new System.Collections.Generic.List<Transform>(); foreach (Transform c in t) if (c != gray) kids.Add(c);
            foreach (var c in kids)
            {
                if (System.Array.IndexOf(keep, c.name) >= 0) { foreach (var r in c.GetComponentsInChildren<Renderer>()) r.enabled = false; continue; }
                if (c.GetComponent<ParticleSystem>() || c.GetComponent<Light>()) continue;
                c.SetParent(gray, true);
            }
            gray.gameObject.SetActive(false);
            var go = Spawn(model, t);
            return CreatureAnimator.Attach(go, model, null, walk, run);
        }

        // the model alone (no clips playing): the game's heroes (Build) and the cutscene actors (Editor/CutsceneAuthoring.cs)
        public static GameObject Spawn(string model, Transform parent, float footOffset = 0f)
        {
            if (string.IsNullOrEmpty(model)) return null;
            var prefab = Resources.Load<GameObject>("Characters/" + model);
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Model " + model;
            go.transform.localPosition = new Vector3(0, footOffset, 0);
            var anim = go.GetComponentInChildren<Animator>();
            go.transform.localRotation = anim && anim.avatar && anim.avatar.isHuman ? HumanFront(go, anim) : BlenderFront;
            var mat = Resources.Load<Material>("Characters/" + model);
            var hard = HardMat(model);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (mat) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = i > 0 && hard ? hard : mat; r.sharedMaterials = ms; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;
            }
            return go;
        }

        // Round 6, item 3: tusks, fangs and claws are submesh 1 with a plain material of their own (Characters/<model>_hard.mat, made by
        // Editor/CharacterImport.cs from <model>_hard.txt)
        public static Material HardMat(string model) => Resources.Load<Material>("Characters/" + model + "_hard");

        // A humanoid faces +Z whatever its file did (Mixamo's own FBX faces +Z, a Blender re-export may not): its right leg on its right.
        static Quaternion HumanFront(GameObject go, Animator anim)
        {
            var l = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var r = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (!l || !r) return Quaternion.identity;
            go.transform.localRotation = Quaternion.identity;
            var right = go.transform.InverseTransformDirection(r.position - l.position); right.y = 0;
            if (right.sqrMagnitude < 1e-6f) return Quaternion.identity;
            var fwd = Vector3.Cross(right.normalized, Vector3.up);
            return Quaternion.Inverse(Quaternion.LookRotation(fwd, Vector3.up));
        }
    }
}
