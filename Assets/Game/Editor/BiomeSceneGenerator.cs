using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool: Tools > Biomes > Generate Scene From Selected Biome.
/// Requires a <see cref="BiomeDefinition"/> asset selected in the Project window.
/// Builds a complete scene with ground, lighting, skybox, fog, camera, paths,
/// clearings, clustered vegetation, hero landmarks, and animals,
/// then saves it to Assets/Game/Scenes/.
/// </summary>
public static class BiomeSceneGenerator
{
    const int RandomSeed = 42;
    const float SpawnExclusionRadius = 8f;
    const int MaxPlacementAttempts = 40;
    const float VegetationMinSpacing = 1.5f;
    const float PathSegmentLength = 3.5f;
    const float PathWidthMin = 1.5f;
    const float PathWidthMax = 2.5f;
    const int PathSegmentsMin = 5;
    const int PathSegmentsMax = 9;
    const float ClearingRadiusMin = 4f;
    const float ClearingRadiusMax = 7f;
    const float GroundOffset = 0.02f;
    const float HeroDistanceMin = 0.25f;
    const float HeroDistanceMax = 0.45f;

    struct ExclusionZone
    {
        public Vector3 Center;
        public float Radius;
    }

    // ==================================================================
    // Menu items
    // ==================================================================

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
        var heroParent = CreateGameObject("Hero Objects", world.transform);
        var pathsParent = CreateGameObject("Paths", world.transform);
        var clearingsParent = CreateGameObject("Clearings", world.transform);
        var animalsParent = CreateGameObject("Animals", world.transform);

        var exclusions = new List<ExclusionZone>
        {
            new ExclusionZone { Center = Vector3.zero, Radius = SpawnExclusionRadius }
        };
        var allPlaced = new List<Vector3>();

        CreateGround(biome, world.transform);
        SetupLighting(biome);
        SetupSkybox(biome);
        SetupFogAndAmbient(biome);
        CreateCamera(biome);

        CreatePaths(biome, pathsParent.transform, rng, exclusions);
        CreateClearings(biome, clearingsParent.transform, rng, exclusions);

        var clusterCenters = GenerateClusterCenters(biome, rng);

        ScatterClustered(biome.treePrefabs, biome.treeCount, treesParent.transform,
            biome, rng, VegetationMinSpacing, 0.7f, 1.3f, clusterCenters, allPlaced, exclusions);
        ScatterClustered(biome.rockPrefabs, biome.rockCount, rocksParent.transform,
            biome, rng, VegetationMinSpacing, 0.6f, 1.4f, clusterCenters, allPlaced, exclusions);
        ScatterClustered(biome.plantPrefabs, biome.plantCount, plantsParent.transform,
            biome, rng, VegetationMinSpacing * 0.5f, 0.75f, 1.25f, clusterCenters, allPlaced, exclusions);

        PlaceHeroObjects(biome.treePrefabs, biome.heroTreeCount, biome.heroTreeScale,
            heroParent.transform, biome, rng, allPlaced, exclusions);
        PlaceHeroObjects(biome.rockPrefabs, biome.heroRockCount, biome.heroRockScale,
            heroParent.transform, biome, rng, allPlaced, exclusions);

        SpawnAnimals(biome, animalsParent.transform, rng);

        Undo.CollapseUndoOperations(undoGroup);

        string scenePath = SaveScene(scene, biome.biomeName);
        Debug.Log($"[BiomeSceneGenerator] Scene generated and saved: {scenePath}");
    }

    [MenuItem("Tools/Biomes/Generate Scene From Selected Biome", true)]
    static bool GenerateValidate() => Selection.activeObject is BiomeDefinition;

    // ==================================================================
    // Hierarchy helpers
    // ==================================================================

    static GameObject CreateGameObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    // ==================================================================
    // Ground
    // ==================================================================

    static void CreateGround(BiomeDefinition biome, Transform parent)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(parent);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = Vector3.one * (biome.terrainSize / 10f);
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        if (biome.groundMaterial != null)
            ground.GetComponent<Renderer>().sharedMaterial = biome.groundMaterial;
        else
            Debug.LogWarning("[BiomeSceneGenerator] No ground material assigned — using default.");
    }

    // ==================================================================
    // Lighting — warm afternoon sun angled at ~40 degrees elevation
    // ==================================================================

    static void SetupLighting(BiomeDefinition biome)
    {
        var sunGO = new GameObject("Directional Light");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.color = biome.directionalLightColor;
        sun.intensity = biome.directionalLightIntensity;
        sunGO.transform.rotation = Quaternion.Euler(40f, -50f, 0f);
        Undo.RegisterCreatedObjectUndo(sunGO, "Create Directional Light");
    }

    // ==================================================================
    // Skybox — procedural fallback when no material is configured
    // ==================================================================

    static void SetupSkybox(BiomeDefinition biome)
    {
        if (biome.skyboxMaterial != null)
        {
            RenderSettings.skybox = biome.skyboxMaterial;
            return;
        }

        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            Debug.LogWarning("[BiomeSceneGenerator] Skybox/Procedural shader not found — skipping skybox.");
            return;
        }

        var mat = new Material(shader) { name = $"Sky_{biome.biomeName}" };
        mat.SetFloat("_SunDisk", 2f);
        mat.SetFloat("_SunSize", 0.04f);
        mat.SetFloat("_SunSizeConvergence", 5f);
        mat.SetFloat("_AtmosphereThickness", 1.05f);
        mat.SetColor("_SkyTint", biome.skyColor);
        mat.SetColor("_GroundColor", biome.fogColor * 0.8f);
        mat.SetFloat("_Exposure", 1.3f);
        RenderSettings.skybox = mat;
    }

    // ==================================================================
    // Fog & ambient colour
    // ==================================================================

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

    // ==================================================================
    // Camera — uses skybox clear flags when a skybox is present
    // ==================================================================

    static void CreateCamera(BiomeDefinition biome)
    {
        var pivotGO = new GameObject("CameraPivot");
        pivotGO.transform.position = Vector3.zero;
        var mover = pivotGO.AddComponent<CameraTargetMover>();
        mover.boundaryRadius = biome.terrainSize * 0.4f;
        Undo.RegisterCreatedObjectUndo(pivotGO, "Create Camera Pivot");

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();

        bool hasSkybox = RenderSettings.skybox != null;
        cam.clearFlags = hasSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.backgroundColor = biome.skyColor;
        camGO.AddComponent<AudioListener>();

        camGO.transform.position = new Vector3(0f, 12f, -20f);
        camGO.transform.LookAt(Vector3.zero);

        var orbit = camGO.AddComponent<SimpleOrbitCamera>();
        orbit.target = pivotGO.transform;
        orbit.maxDistance = biome.terrainSize * 0.5f;
        Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
    }

    // ==================================================================
    // Paths — gently curved ground strips leading outward from the start
    // ==================================================================

    static void CreatePaths(BiomeDefinition biome, Transform parent, System.Random rng,
        List<ExclusionZone> exclusions)
    {
        if (biome.pathCount <= 0) return;

        var mat = CreateFlatMaterial($"Path_{biome.biomeName}", biome.pathColor);
        float angleStep = Mathf.PI * 2f / biome.pathCount;

        for (int p = 0; p < biome.pathCount; p++)
        {
            float baseAngle = angleStep * p + (float)(rng.NextDouble() - 0.5) * angleStep * 0.4f;
            CreateSinglePath(biome.terrainSize, baseAngle, mat, parent, rng, exclusions);
        }
    }

    static void CreateSinglePath(float terrainSize, float startAngle, Material mat,
        Transform parent, System.Random rng, List<ExclusionZone> exclusions)
    {
        float heading = startAngle;
        float startDist = SpawnExclusionRadius + 1f;
        var pos = new Vector3(
            Mathf.Cos(heading) * startDist,
            GroundOffset,
            Mathf.Sin(heading) * startDist);

        float pathWidth = PathWidthMin + (float)rng.NextDouble() * (PathWidthMax - PathWidthMin);
        int segmentCount = PathSegmentsMin + rng.Next(PathSegmentsMax - PathSegmentsMin + 1);
        float halfSize = terrainSize * 0.45f;

        for (int i = 0; i < segmentCount; i++)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            seg.name = "PathSeg";
            seg.transform.SetParent(parent);
            seg.transform.position = pos;
            seg.transform.rotation = Quaternion.Euler(90f, heading * Mathf.Rad2Deg, 0f);
            seg.transform.localScale = new Vector3(pathWidth, PathSegmentLength, 1f);

            Undo.DestroyObjectImmediate(seg.GetComponent<Collider>());
            seg.GetComponent<Renderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(seg, "Create Path Segment");

            exclusions.Add(new ExclusionZone
            {
                Center = new Vector3(pos.x, 0f, pos.z),
                Radius = pathWidth
            });

            heading += (float)(rng.NextDouble() - 0.5) * 0.5f;
            pos += new Vector3(
                Mathf.Cos(heading) * PathSegmentLength * 0.9f,
                0f,
                Mathf.Sin(heading) * PathSegmentLength * 0.9f);

            if (Mathf.Abs(pos.x) > halfSize || Mathf.Abs(pos.z) > halfSize)
                break;
        }
    }

    // ==================================================================
    // Clearings — flat discs marking open landmark areas
    // ==================================================================

    static void CreateClearings(BiomeDefinition biome, Transform parent, System.Random rng,
        List<ExclusionZone> exclusions)
    {
        if (biome.clearingCount <= 0) return;

        var mat = CreateFlatMaterial($"Clearing_{biome.biomeName}", biome.clearingColor);
        var discMesh = CreateDiscMesh(20);
        float halfSize = biome.terrainSize * 0.4f;

        for (int i = 0; i < biome.clearingCount; i++)
        {
            float radius = ClearingRadiusMin + (float)rng.NextDouble() * (ClearingRadiusMax - ClearingRadiusMin);
            Vector3 center = RandomPointOnDisc(rng, SpawnExclusionRadius + radius, halfSize - radius);

            var go = new GameObject($"Clearing {i + 1}");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(center.x, GroundOffset * 0.5f, center.z);
            go.transform.localScale = Vector3.one * (radius * 2f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = discMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(go, "Create Clearing");
            exclusions.Add(new ExclusionZone { Center = center, Radius = radius });
        }
    }

    // ==================================================================
    // Cluster centre generation — evenly spaced sectors with jitter
    // ==================================================================

    static Vector3[] GenerateClusterCenters(BiomeDefinition biome, System.Random rng)
    {
        int count = biome.clusterCount;
        float halfSize = biome.terrainSize * 0.42f;
        var centers = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float sector = ((float)i / count) * Mathf.PI * 2f;
            float jitter = (float)(rng.NextDouble() - 0.5) * (Mathf.PI * 2f / count * 0.6f);
            float angle = sector + jitter;

            float minR = SpawnExclusionRadius * 1.5f;
            float maxR = halfSize * 0.9f;
            float r = minR + (float)rng.NextDouble() * (maxR - minR);

            centers[i] = new Vector3(r * Mathf.Cos(angle), 0f, r * Mathf.Sin(angle));
        }

        return centers;
    }

    // ==================================================================
    // Clustered vegetation scattering
    // ==================================================================

    static void ScatterClustered(
        GameObject[] prefabs, int count, Transform parent,
        BiomeDefinition biome, System.Random rng,
        float minSpacing, float scaleMin, float scaleMax,
        Vector3[] clusterCenters, List<Vector3> allPlaced,
        List<ExclusionZone> exclusions)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return;

        float halfSize = biome.terrainSize * 0.45f;
        float clusterSpread = biome.terrainSize * 0.08f;

        for (int i = 0; i < count; i++)
        {
            var prefab = prefabs[rng.Next(prefabs.Length)];
            if (prefab == null)
            {
                Debug.LogWarning($"[BiomeSceneGenerator] Null prefab in '{parent.name}' — skipped.");
                continue;
            }

            if (!TryFindClusteredPosition(rng, halfSize, minSpacing, clusterCenters,
                    clusterSpread, biome.scatterRandomness, allPlaced, exclusions, out Vector3 pos))
            {
                continue;
            }

            allPlaced.Add(pos);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

            float scale = scaleMin + (float)rng.NextDouble() * (scaleMax - scaleMin);
            instance.transform.localScale = Vector3.one * scale;

            Undo.RegisterCreatedObjectUndo(instance, $"Scatter {prefab.name}");
        }
    }

    static bool TryFindClusteredPosition(
        System.Random rng, float halfSize, float minSpacing,
        Vector3[] clusterCenters, float clusterSpread, float randomness,
        List<Vector3> placed, List<ExclusionZone> exclusions,
        out Vector3 result)
    {
        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate;
            bool useCluster = clusterCenters.Length > 0 && rng.NextDouble() > randomness;

            if (useCluster)
            {
                var center = clusterCenters[rng.Next(clusterCenters.Length)];
                float ox = GaussianRandom(rng) * clusterSpread;
                float oz = GaussianRandom(rng) * clusterSpread;
                candidate = new Vector3(
                    Mathf.Clamp(center.x + ox, -halfSize, halfSize),
                    0f,
                    Mathf.Clamp(center.z + oz, -halfSize, halfSize));
            }
            else
            {
                candidate = new Vector3(
                    (float)(rng.NextDouble() * 2.0 - 1.0) * halfSize,
                    0f,
                    (float)(rng.NextDouble() * 2.0 - 1.0) * halfSize);
            }

            if (candidate.sqrMagnitude < SpawnExclusionRadius * SpawnExclusionRadius)
                continue;

            if (IsInsideExclusion(candidate, exclusions))
                continue;

            bool tooClose = false;
            for (int j = 0; j < placed.Count; j++)
            {
                if ((placed[j] - candidate).sqrMagnitude < minSpacing * minSpacing)
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

    // ==================================================================
    // Hero objects — larger landmarks at middle distance
    // ==================================================================

    static void PlaceHeroObjects(
        GameObject[] prefabs, int count, float scaleMul,
        Transform parent, BiomeDefinition biome, System.Random rng,
        List<Vector3> allPlaced, List<ExclusionZone> exclusions)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return;

        float minDist = biome.terrainSize * HeroDistanceMin;
        float maxDist = biome.terrainSize * HeroDistanceMax;
        float angleStep = Mathf.PI * 2f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i + (float)(rng.NextDouble() - 0.5) * angleStep * 0.5f;
            float dist = minDist + (float)rng.NextDouble() * (maxDist - minDist);
            var candidate = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            if (IsInsideExclusion(candidate, exclusions))
            {
                dist = maxDist;
                candidate = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            }

            allPlaced.Add(candidate);

            var prefab = prefabs[rng.Next(prefabs.Length)];
            if (prefab == null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Hero {prefab.name}";
            instance.transform.SetParent(parent);
            instance.transform.position = candidate;
            instance.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);

            float baseScale = 0.85f + (float)rng.NextDouble() * 0.3f;
            instance.transform.localScale = Vector3.one * (baseScale * scaleMul);

            Undo.RegisterCreatedObjectUndo(instance, $"Place Hero {prefab.name}");
        }
    }

    // ==================================================================
    // Animals
    // ==================================================================

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
                Debug.LogError($"[BiomeSceneGenerator] Animal '{animal.animalName}' has no prefab — cannot spawn group {g + 1}.");
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

    // ==================================================================
    // Placement utilities
    // ==================================================================

    static Vector3 RandomPointOnDisc(System.Random rng, float minRadius, float maxRadius)
    {
        float angle = (float)(rng.NextDouble() * 2.0 * System.Math.PI);
        float r = Mathf.Sqrt((float)rng.NextDouble()) * (maxRadius - minRadius) + minRadius;
        return new Vector3(r * Mathf.Cos(angle), 0f, r * Mathf.Sin(angle));
    }

    static bool IsInsideExclusion(Vector3 pos, List<ExclusionZone> exclusions)
    {
        for (int i = 0; i < exclusions.Count; i++)
        {
            float dx = pos.x - exclusions[i].Center.x;
            float dz = pos.z - exclusions[i].Center.z;
            if (dx * dx + dz * dz < exclusions[i].Radius * exclusions[i].Radius)
                return true;
        }
        return false;
    }

    /// <summary>Box-Muller transform producing a standard-normal sample.</summary>
    static float GaussianRandom(System.Random rng)
    {
        float u1 = 1f - (float)rng.NextDouble();
        float u2 = (float)rng.NextDouble();
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
    }

    // ==================================================================
    // Mesh utilities
    // ==================================================================

    static Mesh CreateDiscMesh(int segments)
    {
        var vertices = new Vector3[segments + 1];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);

            int next = (i + 1) % segments + 1;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = i + 1;
        }

        var mesh = new Mesh { name = "ClearingDisc" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ==================================================================
    // Material utilities
    // ==================================================================

    static Material CreateFlatMaterial(string matName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = matName };
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.color = color;
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.1f);
        mat.renderQueue = 2001;
        return mat;
    }

    // ==================================================================
    // Collider utility
    // ==================================================================

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

    // ==================================================================
    // Scene saving
    // ==================================================================

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
