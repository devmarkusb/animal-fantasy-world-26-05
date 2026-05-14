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

    [Header("Lighting")]
    public Color directionalLightColor = Color.white;

    [Range(0f, 3f)]
    public float directionalLightIntensity = 1.2f;

    public Color ambientColor = new Color(0.7f, 0.85f, 1f);

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
    }
}
