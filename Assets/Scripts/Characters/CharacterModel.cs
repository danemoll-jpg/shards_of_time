using UnityEngine;

namespace ShardsOfTime
{
    // Round 2, item 9: a real model for a hero, from Resources/Characters/<model>.fbx (with its LODs) and its material
    // Resources/Characters/<model>.mat (made at import by Editor/CharacterImport.cs). Missing = null: the caller keeps the graybox body.
    public static class CharacterModel
    {
        // Blender's front (-Y) arrives as Unity's back (-Z) with the FBX export defaults: turn the model to face +Z
        public static readonly Quaternion BlenderFront = Quaternion.Euler(0, 180, 0);

        public static GameObject Build(string model, Transform parent, CharacterMotor motor)
        {
            if (string.IsNullOrEmpty(model)) return null;
            var prefab = Resources.Load<GameObject>("Characters/" + model);
            if (!prefab) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = "Model " + model;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = BlenderFront;
            var mat = Resources.Load<Material>("Characters/" + model);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (mat) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = false;
            }
            CreatureAnimator.Attach(go, model, motor);
            return go;
        }
    }
}
