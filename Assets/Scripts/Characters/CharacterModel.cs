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
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (mat) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;
            }
            return go;
        }

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
