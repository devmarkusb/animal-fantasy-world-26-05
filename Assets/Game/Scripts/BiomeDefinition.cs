using UnityEngine;

/// <summary>
/// Describes a single biome: its ground look, lighting mood, and which animals may appear.
/// Designers fill these out in the Inspector, then use Tools > Generate Biome Scene.
/// </summary>
[CreateAssetMenu(fileName = "New Biome", menuName = "Game/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string biomeName = "Unnamed Biome";

    [TextArea(2, 4)]
    public string description;

    [Header("Terrain")]
    [Tooltip("Material applied to the generated ground plane.")]
    public Material groundMaterial;

    [Tooltip("World-space size of the ground plane in units.")]
    [Min(10f)]
    public float groundSize = 100f;

    [Header("Lighting & Sky")]
    [Tooltip("Optional skybox material. Leave empty for the default URP sky.")]
    public Material skyboxMaterial;

    public Color ambientColor = new Color(0.7f, 0.85f, 1f);

    [Range(0f, 2f)]
    public float sunIntensity = 1.2f;

    public Color sunColor = Color.white;

    [Header("Animals")]
    public AnimalDefinition[] animals;

    [Header("Decoration")]
    [Tooltip("Optional prefabs scattered randomly (trees, rocks, flowers…).")]
    public GameObject[] decorationPrefabs;

    [Min(0)]
    public int decorationCount = 20;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(biomeName))
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no biome name set.", this);

        if (groundMaterial == null)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no ground material assigned.", this);

        if (animals == null || animals.Length == 0)
            Debug.LogWarning($"[BiomeDefinition] '{name}' has no animals. The scene will be empty.", this);
    }
}
