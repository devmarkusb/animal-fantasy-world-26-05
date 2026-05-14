using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool: Tools > Generate Biome Scene.
/// Asks the designer to pick a <see cref="BiomeDefinition"/> asset, then builds a
/// ready-to-play test scene with ground, lighting, camera, animals, and decorations.
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
        CreateGround(biome);
        SpawnAnimals(biome);
        SpawnDecorations(biome);
        CreateCamera(biome);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[BiomeSceneGenerator] Scene generated for biome '{biome.biomeName}'.");
    }

    static void SetupLighting(BiomeDefinition biome)
    {
        RenderSettings.ambientLight = biome.ambientColor;

        if (biome.skyboxMaterial != null)
            RenderSettings.skybox = biome.skyboxMaterial;

        var sun = FindOrCreateDirectionalLight();
        sun.color = biome.sunColor;
        sun.intensity = biome.sunIntensity;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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
        ground.transform.localScale = Vector3.one * (biome.groundSize / 10f);

        if (biome.groundMaterial != null)
            ground.GetComponent<Renderer>().sharedMaterial = biome.groundMaterial;
    }

    static void SpawnAnimals(BiomeDefinition biome)
    {
        if (biome.animals == null) return;

        var parent = new GameObject("Animals");

        foreach (var animal in biome.animals)
        {
            if (animal == null)
            {
                Debug.LogWarning("[BiomeSceneGenerator] Null entry in animals array — skipped.");
                continue;
            }

            animal.Validate();

            if (animal.prefab == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Animal '{animal.displayName}' has no prefab — skipped.");
                continue;
            }

            for (int i = 0; i < animal.spawnCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * animal.spawnRadius;
                Vector3 pos = new Vector3(offset.x, 0f, offset.y);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(animal.prefab);
                instance.name = $"{animal.displayName} ({i + 1})";
                instance.transform.SetParent(parent.transform);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                var wander = instance.GetComponent<AnimalWander>();
                if (wander == null)
                    wander = instance.AddComponent<AnimalWander>();
                wander.moveSpeed = animal.moveSpeed;
                wander.wanderRadius = animal.wanderRadius;
                wander.wanderInterval = animal.wanderInterval;

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

    static void SpawnDecorations(BiomeDefinition biome)
    {
        if (biome.decorationPrefabs == null || biome.decorationPrefabs.Length == 0)
            return;

        var parent = new GameObject("Decorations");
        float halfSize = biome.groundSize * 0.45f;

        for (int i = 0; i < biome.decorationCount; i++)
        {
            var prefab = biome.decorationPrefabs[Random.Range(0, biome.decorationPrefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning("[BiomeSceneGenerator] Null entry in decoration prefabs — skipped.");
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

        var orbit = cam.GetComponent<SimpleOrbitCamera>();
        if (orbit == null)
            orbit = cam.gameObject.AddComponent<SimpleOrbitCamera>();

        orbit.maxDistance = biome.groundSize * 0.5f;
    }
}
