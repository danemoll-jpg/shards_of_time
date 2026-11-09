using UnityEditor;

namespace ShardsOfTime
{
    // Round 4: import settings for the Blender-made world art in Assets/Resources/World (Tools/Blender/iceage_world.py, home_room.py):
    // no materials (World/WorldArt.cs colours the parts by name), no animation, no cameras or lights, meshes readable (the terrain's
    // mesh colliders are made at run time), Blender's axes baked so a file's x / z read as the game's.
    public sealed class WorldImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Resources/World/")) return;
            var m = (ModelImporter)assetImporter;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.animationType = ModelImporterAnimationType.None; m.importAnimation = false;
            m.importCameras = false; m.importLights = false;
            m.isReadable = true;
            m.bakeAxisConversion = true;
            m.importNormals = ModelImporterNormals.Import;
            m.meshCompression = ModelImporterMeshCompression.Off;
            m.indexFormat = ModelImporterIndexFormat.Auto;
        }
    }
}
