using UnityEngine;

[CreateAssetMenu(fileName = "New Biome", menuName = "Game/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string biomeName = "Unnamed Biome";

    [Header("Terrain")]
    [Tooltip("Material applied to the generated ground plane.")]
    public Material groundMaterial;

    [Tooltip("World-space size of the terrain in units.")]
    [Min(10f)]
    public float terrainSize = 100f;

    [Header("Vegetation")]
    [Tooltip("Tree prefabs placed randomly across the biome.")]
    public GameObject[] treePrefabs;

    [Min(0)]
    public int treeCount = 15;

    [Tooltip("Rock prefabs placed randomly across the biome.")]
    public GameObject[] rockPrefabs;

    [Min(0)]
    public int rockCount = 10;

    [Tooltip("Small plant / flower prefabs scattered across the biome.")]
    public GameObject[] plantPrefabs;

    [Min(0)]
    public int plantCount = 25;

    [Header("Hero Objects")]
    [Tooltip("Number of larger feature trees placed at middle distance as visual landmarks.")]
    [Min(0)]
    public int heroTreeCount = 3;

    [Tooltip("Scale multiplier applied to hero trees relative to normal trees.")]
    [Range(1.5f, 4f)]
    public float heroTreeScale = 2f;

    [Tooltip("Number of larger feature rocks placed at middle distance.")]
    [Min(0)]
    public int heroRockCount = 2;

    [Tooltip("Scale multiplier applied to hero rocks relative to normal rocks.")]
    [Range(1.5f, 4f)]
    public float heroRockScale = 2.5f;

    [Header("Animals")]
    [Tooltip("Animal species that can appear in this biome.")]
    public AnimalDefinition[] animalDefinitions;

    [Tooltip("Number of animal groups spawned (one group per definition entry, cycled).")]
    [Min(1)]
    public int animalGroupCount = 3;

    [Header("Sky & Fog")]
    public Color skyColor = new Color(0.53f, 0.81f, 0.92f);

    public Color fogColor = new Color(0.75f, 0.85f, 0.90f);

    [Range(0f, 0.05f)]
    [Tooltip("Exponential fog density. 0 = no fog.")]
    public float fogDensity = 0.005f;

    [Tooltip("Optional skybox material. A simple procedural sky is generated when unassigned.")]
    public Material skyboxMaterial;

    [Header("Lighting")]
    public Color directionalLightColor = Color.white;

    [Range(0f, 3f)]
    public float directionalLightIntensity = 1.2f;

    public Color ambientColor = new Color(0.7f, 0.85f, 1f);

    [Header("Paths & Clearings")]
    [Tooltip("Color tint for simple ground paths through the biome.")]
    public Color pathColor = new Color(0.76f, 0.70f, 0.50f);

    [Tooltip("Number of gently curved paths generated across the biome.")]
    [Min(0)]
    public int pathCount = 2;

    [Tooltip("Color tint for circular landmark clearings.")]
    public Color clearingColor = new Color(0.65f, 0.78f, 0.42f);

    [Tooltip("Number of open clearings placed in the biome.")]
    [Min(0)]
    public int clearingCount = 2;

    [Header("Scatter Style")]
    [Tooltip("Number of organic vegetation clusters. Objects group around these centres.")]
    [Range(3, 15)]
    public int clusterCount = 5;

    [Tooltip("Blend between fully clustered (0) and fully random (1) object placement.")]
    [Range(0f, 1f)]
    public float scatterRandomness = 0.3f;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(biomeName))
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no biome name set.", this);

        if (groundMaterial == null)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no ground material assigned.", this);

        if (animalDefinitions == null || animalDefinitions.Length == 0)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no animal definitions. The scene will have no animals.", this);

        if (treePrefabs != null && treePrefabs.Length > 0 && treeCount == 0)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has tree prefabs but treeCount is 0.", this);

        if (rockPrefabs != null && rockPrefabs.Length > 0 && rockCount == 0)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has rock prefabs but rockCount is 0.", this);

        if (plantPrefabs != null && plantPrefabs.Length > 0 && plantCount == 0)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has plant prefabs but plantCount is 0.", this);

        if (heroTreeCount > 0 && (treePrefabs == null || treePrefabs.Length == 0))
            Debug.LogWarning($"[BiomeDefinition] '{name}' requests hero trees but has no tree prefabs.", this);

        if (heroRockCount > 0 && (rockPrefabs == null || rockPrefabs.Length == 0))
            Debug.LogWarning($"[BiomeDefinition] '{name}' requests hero rocks but has no rock prefabs.", this);
    }
}
