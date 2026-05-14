using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu items that create ready-to-customise sample ScriptableObject assets
/// under Assets/Game/Biomes/. Prefab and material slots are left empty so
/// the user can drag in their own project-specific assets.
/// </summary>
public static class SampleAssetFactory
{
    const string BiomesFolder = "Assets/Game/Biomes";

    // ==================================================================
    // Menu items
    // ==================================================================

    [MenuItem("Tools/Biomes/Create Sample Savanna Biome")]
    static void CreateSampleSavannaBiome()
    {
        EnsureFolder(BiomesFolder);

        var biome = ScriptableObject.CreateInstance<BiomeDefinition>();

        biome.biomeName = "Savanna";
        biome.terrainSize = 120f;

        // Vegetation counts — prefabs must be assigned manually
        biome.treeCount = 12;
        biome.rockCount = 8;
        biome.plantCount = 30;

        biome.heroTreeCount = 3;
        biome.heroTreeScale = 2.5f;
        biome.heroRockCount = 2;
        biome.heroRockScale = 2f;

        biome.animalGroupCount = 4;

        // Warm African sky
        biome.skyColor = new Color(0.55f, 0.78f, 0.95f);
        biome.fogColor = new Color(0.82f, 0.78f, 0.68f);
        biome.fogDensity = 0.004f;

        // Warm golden-hour lighting
        biome.directionalLightColor = new Color(1f, 0.94f, 0.82f);
        biome.directionalLightIntensity = 1.4f;
        biome.ambientColor = new Color(0.80f, 0.75f, 0.65f);

        // Sandy paths, dry-grass clearings
        biome.pathColor = new Color(0.82f, 0.72f, 0.48f);
        biome.pathCount = 3;
        biome.clearingColor = new Color(0.78f, 0.76f, 0.50f);
        biome.clearingCount = 2;

        // Scatter style — sparse clusters typical of savanna
        biome.clusterCount = 4;
        biome.scatterRandomness = 0.4f;

        string path = AssetDatabase.GenerateUniqueAssetPath($"{BiomesFolder}/Savanna.asset");
        AssetDatabase.CreateAsset(biome, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = biome;
        EditorGUIUtility.PingObject(biome);

        Debug.Log(
            $"[SampleAssetFactory] Created sample Savanna biome at {path}.\n" +
            "  → Assign these fields manually in the Inspector:\n" +
            "     • Ground Material  (e.g. a sandy / dry-grass material)\n" +
            "     • Tree Prefabs     (e.g. acacia or baobab tree prefabs)\n" +
            "     • Rock Prefabs     (e.g. large sandstone rock prefabs)\n" +
            "     • Plant Prefabs    (e.g. dry grass / bush prefabs)\n" +
            "     • Animal Definitions (create via Tools > Biomes > Create Sample Animal Definition)\n" +
            "     • Skybox Material  (optional — a procedural sky is generated if left empty)",
            biome);
    }

    [MenuItem("Tools/Biomes/Create Sample Animal Definition")]
    static void CreateSampleAnimalDefinition()
    {
        EnsureFolder(BiomesFolder);

        var animal = ScriptableObject.CreateInstance<AnimalDefinition>();

        animal.animalName = "Zebra";

        animal.groupSizeMin = 3;
        animal.groupSizeMax = 6;

        animal.movementRadius = 10f;
        animal.moveSpeed = 2.5f;
        animal.idleTimeMin = 2f;
        animal.idleTimeMax = 6f;

        animal.factText =
            "Zebras have unique stripe patterns — no two zebras look exactly the same! " +
            "Their stripes may help confuse flies and keep them cool.";

        string path = AssetDatabase.GenerateUniqueAssetPath($"{BiomesFolder}/Zebra.asset");
        AssetDatabase.CreateAsset(animal, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = animal;
        EditorGUIUtility.PingObject(animal);

        Debug.Log(
            $"[SampleAssetFactory] Created sample Zebra animal at {path}.\n" +
            "  → Assign these fields manually in the Inspector:\n" +
            "     • Animal Prefab  (drag in a zebra 3D model prefab with a Collider)\n" +
            "     • Animal Sound   (optional — an AudioClip played on click)",
            animal);
    }

    // ==================================================================
    // Helpers
    // ==================================================================

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(folderPath);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
