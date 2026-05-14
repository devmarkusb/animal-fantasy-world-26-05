using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool: Tools > Generate Biome Scene.
/// Picks a <see cref="BiomeDefinition"/> asset, then builds a ready-to-play test scene
/// with ground, lighting, fog, camera, animals, trees, rocks, and plants.
/// </summary>
public static class BiomeSceneGenerator
{
    [MenuItem("Tools/Generate Biome Scene")]
    static void Generate()
    {
        string path = EditorUtility.OpenFilePanelWithFilters(
            "Select a Biome Definition",
            "Assets",
            new[] { "ScriptableObject", "asset" });

        if (string.IsNullOrEmpty(path))
            return;

        string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
        var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(relativePath);

        if (biome == null)
        {
            Debug.LogError($"[BiomeSceneGenerator] Could not load BiomeDefinition at '{relativePath}'.");
            return;
        }

        biome.Validate();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Undo.SetCurrentGroupName("Generate Biome Scene");

        SetupLighting(biome);
        SetupFog(biome);
        CreateGround(biome);
        SpawnPrefabs(biome.treePrefabs, biome.treeCount, "Trees", biome.terrainSize);
        SpawnPrefabs(biome.rockPrefabs, biome.rockCount, "Rocks", biome.terrainSize);
        SpawnPrefabs(biome.plantPrefabs, biome.plantCount, "Plants", biome.terrainSize);
        SpawnAnimals(biome);
        CreateCamera(biome);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[BiomeSceneGenerator] Scene generated for biome '{biome.biomeName}'.");
    }

    static void SetupLighting(BiomeDefinition biome)
    {
        RenderSettings.ambientLight = biome.ambientColor;

        var sun = FindOrCreateDirectionalLight();
        sun.color = biome.directionalLightColor;
        sun.intensity = biome.directionalLightIntensity;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Camera.main.backgroundColor = biome.skyColor;
    }

    static void SetupFog(BiomeDefinition biome)
    {
        bool useFog = biome.fogDensity > 0f;
        RenderSettings.fog = useFog;

        if (!useFog) return;

        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = biome.fogColor;
        RenderSettings.fogDensity = biome.fogDensity;
    }

    static Light FindOrCreateDirectionalLight()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
                return light;
        }

        var go = new GameObject("Directional Light");
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.shadows = LightShadows.Soft;
        return l;
    }

    static void CreateGround(BiomeDefinition biome)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (biome.terrainSize / 10f);

        if (biome.groundMaterial != null)
            ground.GetComponent<Renderer>().sharedMaterial = biome.groundMaterial;
    }

    static void SpawnPrefabs(GameObject[] prefabs, int count, string parentName, float terrainSize)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return;

        var parent = new GameObject(parentName);
        float halfSize = terrainSize * 0.45f;

        for (int i = 0; i < count; i++)
        {
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Null entry in {parentName} prefabs — skipped.");
                continue;
            }

            Vector3 pos = new Vector3(
                Random.Range(-halfSize, halfSize),
                0f,
                Random.Range(-halfSize, halfSize));

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent.transform);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }

    static void SpawnAnimals(BiomeDefinition biome)
    {
        if (biome.animalDefinitions == null || biome.animalDefinitions.Length == 0)
            return;

        var parent = new GameObject("Animals");
        float spawnRadius = biome.terrainSize * 0.35f;

        for (int g = 0; g < biome.animalGroupCount; g++)
        {
            var animal = biome.animalDefinitions[g % biome.animalDefinitions.Length];

            if (animal == null)
            {
                Debug.LogWarning("[BiomeSceneGenerator] Null entry in animalDefinitions — skipped.");
                continue;
            }

            animal.Validate();

            if (animal.animalPrefab == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Animal '{animal.animalName}' has no prefab — skipped.");
                continue;
            }

            Vector2 groupCenter2D = Random.insideUnitCircle * spawnRadius;
            Vector3 groupCenter = new Vector3(groupCenter2D.x, 0f, groupCenter2D.y);

            int groupSize = Random.Range(animal.groupSizeMin, Mathf.Max(animal.groupSizeMin, animal.groupSizeMax) + 1);

            for (int i = 0; i < groupSize; i++)
            {
                Vector2 offset = Random.insideUnitCircle * animal.movementRadius * 0.5f;
                Vector3 pos = groupCenter + new Vector3(offset.x, 0f, offset.y);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(animal.animalPrefab);
                instance.name = $"{animal.animalName} ({g + 1}-{i + 1})";
                instance.transform.SetParent(parent.transform);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                var wander = instance.GetComponent<AnimalWander>();
                if (wander == null)
                    wander = instance.AddComponent<AnimalWander>();
                wander.moveSpeed = animal.moveSpeed;
                wander.movementRadius = animal.movementRadius;

                EnsureCollider(instance);

                var clickable = instance.GetComponent<ClickableAnimal>();
                if (clickable == null)
                    clickable = instance.AddComponent<ClickableAnimal>();
                clickable.definition = animal;
            }
        }
    }

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

    static void CreateCamera(BiomeDefinition biome)
    {
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }

        cam.transform.position = new Vector3(0f, 12f, -20f);
        cam.transform.LookAt(Vector3.zero);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = biome.skyColor;

        var orbit = cam.GetComponent<SimpleOrbitCamera>();
        if (orbit == null)
            orbit = cam.gameObject.AddComponent<SimpleOrbitCamera>();

        orbit.maxDistance = biome.terrainSize * 0.5f;
    }
}
