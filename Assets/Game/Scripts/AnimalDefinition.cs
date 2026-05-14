using UnityEngine;

/// <summary>
/// Data asset for one animal species: visuals, behaviour tuning, and fun-fact text
/// shown when a child taps the animal.
/// </summary>
[CreateAssetMenu(fileName = "New Animal", menuName = "Game/Animal Definition")]
public class AnimalDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Unnamed Animal";

    [Tooltip("Icon shown in UI popups.")]
    public Sprite icon;

    [TextArea(2, 5)]
    public string funFact = "This animal is really cool!";

    [Header("Prefab")]
    [Tooltip("The 3D model prefab instantiated in the scene. Must have a Collider for click detection.")]
    public GameObject prefab;

    [Header("Spawning")]
    [Min(1)]
    public int spawnCount = 3;

    [Tooltip("How far from the biome centre this animal may spawn.")]
    [Min(1f)]
    public float spawnRadius = 30f;

    [Header("Wandering")]
    [Min(0.1f)]
    public float moveSpeed = 2f;

    [Min(1f)]
    public float wanderRadius = 8f;

    [Tooltip("Seconds between picking a new wander target.")]
    [Min(0.5f)]
    public float wanderInterval = 4f;

    public void Validate()
    {
        if (prefab == null)
            Debug.LogWarning($"[AnimalDefinition] '{name}' has no prefab assigned.", this);

        if (string.IsNullOrWhiteSpace(displayName))
            Debug.LogWarning($"[AnimalDefinition] '{name}' has no display name.", this);
    }
}
