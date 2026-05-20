using UnityEngine;
using UnityEditor;

/// <summary>
/// One-shot tool: Tools > Biomes > Remap Nature Materials.
/// Connects BirchTree_1, Rock_1, and Plant_1 FBX importers to the
/// hand-authored URP materials in Assets/Art/Nature/.../Materials/.
/// Run once — afterwards the menu item can be ignored.
/// </summary>
public static class NatureMaterialRemapper
{
    [MenuItem("Tools/Biomes/Remap Nature Materials")]
    static void Remap()
    {
        const string matBase = "Assets/Art/Nature/Ultimate Stylized Nature - May 2022/Materials";

        var mappings = new[]
        {
            ("Assets/Art/Nature/Ultimate Stylized Nature - May 2022/FBX/BirchTree_1.fbx",
             new[] { ("BirchTree_Bark", $"{matBase}/BirchTree_Bark.mat"),
                     ("BirchTree_Leaves", $"{matBase}/BirchTree_Leaves.mat") }),

            ("Assets/Art/Nature/Ultimate Stylized Nature - May 2022/FBX/Rock_1.fbx",
             new[] { ("Rock", $"{matBase}/Rock.mat") }),

            ("Assets/Art/Nature/Ultimate Stylized Nature - May 2022/FBX/Plant_1.fbx",
             new[] { ("Flowers", $"{matBase}/Flowers.mat") }),
        };

        foreach (var (fbxPath, mats) in mappings)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[NatureMaterialRemapper] Could not find ModelImporter at '{fbxPath}'.");
                continue;
            }

            foreach (var (matName, matPath) in mats)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    Debug.LogError($"[NatureMaterialRemapper] Material not found: '{matPath}'.");
                    continue;
                }

                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), matName),
                    mat);
            }

            importer.SaveAndReimport();
            Debug.Log($"[NatureMaterialRemapper] Remapped and reimported: {fbxPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[NatureMaterialRemapper] Done. Regenerate the Forrest scene to see the result.");
    }
}
