using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool: Tools > Biomes > Generate Scene From Selected Biome.
/// Requires a <see cref="BiomeDefinition"/> asset selected in the Project window.
/// Builds a complete scene with ground, lighting, fog, camera, vegetation, and animals,
/// then saves it to Assets/Game/Scenes/.
/// </summary>
public static class BiomeSceneGenerator
{
    const int RandomSeed = 42;
    const float SpawnExclusionRadius = 8f;
    const int MaxPlacementAttempts = 30;
    const float VegetationMinSpacing = 1.5f;

    [MenuItem("Tools/Biomes/Generate Scene From Selected Biome")]
    static void Generate()
    {
        var biome = Selection.activeObject as BiomeDefinition;
        if (biome == null)
        {
            EditorUtility.DisplayDialog(
                "No Biome Selected",
                "Please select a BiomeDefinition asset in the Project window, then try again.",
                "OK");
            return;
        }

        biome.Validate();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Biome Scene");

        var rng = new System.Random(RandomSeed);

        var world = CreateGameObject("World", null);
        var treesParent = CreateGameObject("Trees", world.transform);
        var rocksParent = CreateGameObject("Rocks", world.transform);
        var plantsParent = CreateGameObject("Plants", world.transform);
        var animalsParent = CreateGameObject("Animals", world.transform);

        CreateGround(biome, world.transform);
        SetupLighting(biome);
        SetupFogAndAmbient(biome);
        CreateCamera(biome);

        ScatterPrefabs(biome.treePrefabs, biome.treeCount, treesParent.transform,
            biome.terrainSize, rng, VegetationMinSpacing, scaleVariation: 0.3f);
        ScatterPrefabs(biome.rockPrefabs, biome.rockCount, rocksParent.transform,
            biome.terrainSize, rng, VegetationMinSpacing, scaleVariation: 0.4f);
        ScatterPrefabs(biome.plantPrefabs, biome.plantCount, plantsParent.transform,
            biome.terrainSize, rng, VegetationMinSpacing * 0.5f, scaleVariation: 0.25f);

        SpawnAnimals(biome, animalsParent.transform, rng);

        Undo.CollapseUndoOperations(undoGroup);

        string scenePath = SaveScene(scene, biome.biomeName);
        Debug.Log($"[BiomeSceneGenerator] Scene generated and saved: {scenePath}");
    }

    [MenuItem("Tools/Biomes/Generate Scene From Selected Biome", true)]
    static bool GenerateValidate()
    {
        return Selection.activeObject is BiomeDefinition;
    }

    // ------------------------------------------------------------------
    // Hierarchy helpers
    // ------------------------------------------------------------------

    static GameObject CreateGameObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    // ------------------------------------------------------------------
    // Ground
    // ------------------------------------------------------------------

    static void CreateGround(BiomeDefinition biome, Transform parent)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (biome.terrainSize / 10f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        if (biome.groundMaterial != null)
        {
            ground.GetComponent<Renderer>().sharedMaterial = biome.groundMaterial;
        }
        else
        {
            Debug.LogWarning("[BiomeSceneGenerator] No ground material assigned — using default.");
        }
    }

    // ------------------------------------------------------------------
    // Lighting
    // ------------------------------------------------------------------

    static void SetupLighting(BiomeDefinition biome)
    {
        var sunGO = new GameObject("Directional Light");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.color = biome.directionalLightColor;
        sun.intensity = biome.directionalLightIntensity;
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Undo.RegisterCreatedObjectUndo(sunGO, "Create Directional Light");
    }

    // ------------------------------------------------------------------
    // Fog & ambient
    // ------------------------------------------------------------------

    static void SetupFogAndAmbient(BiomeDefinition biome)
    {
        RenderSettings.ambientLight = biome.ambientColor;
        RenderSettings.ambientSkyColor = biome.ambientColor;

        bool useFog = biome.fogDensity > 0f;
        RenderSettings.fog = useFog;
        if (useFog)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = biome.fogColor;
            RenderSettings.fogDensity = biome.fogDensity;
        }
    }

    // ------------------------------------------------------------------
    // Camera
    // ------------------------------------------------------------------

    static void CreateCamera(BiomeDefinition biome)
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = biome.skyColor;
        camGO.AddComponent<AudioListener>();

        camGO.transform.position = new Vector3(0f, 12f, -20f);
        camGO.transform.LookAt(Vector3.zero);

        var orbit = camGO.AddComponent<SimpleOrbitCamera>();
        orbit.maxDistance = biome.terrainSize * 0.5f;
        Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
    }

    // ------------------------------------------------------------------
    // Vegetation / prop scattering
    // ------------------------------------------------------------------

    static void ScatterPrefabs(
        GameObject[] prefabs, int count, Transform parent,
        float terrainSize, System.Random rng, float minSpacing, float scaleVariation)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return;

        float halfSize = terrainSize * 0.45f;
        var placed = new System.Collections.Generic.List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            var prefab = prefabs[rng.Next(0, prefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Null prefab entry in '{parent.name}' at index {i} — skipped.");
                continue;
            }

            if (!TryFindPosition(rng, halfSize, minSpacing, placed, out Vector3 pos))
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Could not place '{parent.name}' item {i} after {MaxPlacementAttempts} attempts — skipped.");
                continue;
            }

            placed.Add(pos);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

            float scale = 1f + ((float)rng.NextDouble() * 2f - 1f) * scaleVariation;
            instance.transform.localScale = Vector3.one * scale;

            Undo.RegisterCreatedObjectUndo(instance, $"Scatter {prefab.name}");
        }
    }

    static bool TryFindPosition(
        System.Random rng, float halfSize, float minSpacing,
        System.Collections.Generic.List<Vector3> existing, out Vector3 result)
    {
        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * halfSize;
            float z = (float)(rng.NextDouble() * 2.0 - 1.0) * halfSize;
            var candidate = new Vector3(x, 0f, z);

            if (candidate.sqrMagnitude < SpawnExclusionRadius * SpawnExclusionRadius)
                continue;

            bool tooClose = false;
            for (int j = 0; j < existing.Count; j++)
            {
                if ((existing[j] - candidate).sqrMagnitude < minSpacing * minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    // ------------------------------------------------------------------
    // Animals
    // ------------------------------------------------------------------

    static void SpawnAnimals(BiomeDefinition biome, Transform parent, System.Random rng)
    {
        if (biome.animalDefinitions == null || biome.animalDefinitions.Length == 0)
        {
            Debug.LogWarning("[BiomeSceneGenerator] No animal definitions — scene will have no animals.");
            return;
        }

        float spawnRadius = biome.terrainSize * 0.35f;

        for (int g = 0; g < biome.animalGroupCount; g++)
        {
            var animal = biome.animalDefinitions[g % biome.animalDefinitions.Length];

            if (animal == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Null AnimalDefinition at index {g % biome.animalDefinitions.Length} — skipped.");
                continue;
            }

            animal.Validate();

            if (animal.animalPrefab == null)
            {
                Debug.LogError($"[BiomeSceneGenerator] Animal '{animal.animalName}' has no prefab assigned — cannot spawn group {g + 1}.");
                continue;
            }

            Vector3 groupCenter = RandomPointOnDisc(rng, SpawnExclusionRadius, spawnRadius);

            int maxSize = Mathf.Max(animal.groupSizeMin, animal.groupSizeMax);
            int groupSize = rng.Next(animal.groupSizeMin, maxSize + 1);

            for (int i = 0; i < groupSize; i++)
            {
                float offsetR = (float)rng.NextDouble() * animal.movementRadius * 0.5f;
                float offsetA = (float)(rng.NextDouble() * 2.0 * System.Math.PI);
                Vector3 pos = groupCenter + new Vector3(
                    offsetR * Mathf.Cos(offsetA), 0f, offsetR * Mathf.Sin(offsetA));

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(animal.animalPrefab);
                instance.name = $"{animal.animalName} ({g + 1}-{i + 1})";
                instance.transform.SetParent(parent);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

                var wander = instance.GetComponent<AnimalWander>();
                if (wander == null)
                    wander = instance.AddComponent<AnimalWander>();
                wander.moveSpeed = animal.moveSpeed;
                wander.movementRadius = animal.movementRadius;
                wander.idleTimeMin = animal.idleTimeMin;
                wander.idleTimeMax = animal.idleTimeMax;

                EnsureCollider(instance);

                var clickable = instance.GetComponent<ClickableAnimal>();
                if (clickable == null)
                    clickable = instance.AddComponent<ClickableAnimal>();
                clickable.definition = animal;

                Undo.RegisterCreatedObjectUndo(instance, $"Spawn {animal.animalName}");
            }
        }
    }

    static Vector3 RandomPointOnDisc(System.Random rng, float minRadius, float maxRadius)
    {
        float angle = (float)(rng.NextDouble() * 2.0 * System.Math.PI);
        float r = Mathf.Sqrt((float)rng.NextDouble()) * (maxRadius - minRadius) + minRadius;
        return new Vector3(r * Mathf.Cos(angle), 0f, r * Mathf.Sin(angle));
    }

    // ------------------------------------------------------------------
    // Collider utility
    // ------------------------------------------------------------------

    static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null)
            return;

        var col = go.AddComponent<BoxCollider>();
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            col.size = Vector3.one;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        col.center = go.transform.InverseTransformPoint(bounds.center);
        col.size = bounds.size;
    }

    // ------------------------------------------------------------------
    // Scene saving
    // ------------------------------------------------------------------

    static string SaveScene(UnityEngine.SceneManagement.Scene scene, string biomeName)
    {
        const string folder = "Assets/Game/Scenes";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Game", "Scenes");
        }

        string safeName = SanitizeFilename(biomeName);
        string path = $"{folder}/Generated_{safeName}.unity";

        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        return path;
    }

    static string SanitizeFilename(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (System.Array.IndexOf(invalid, c) >= 0 || c == ' ')
                sb.Append('_');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }
}
