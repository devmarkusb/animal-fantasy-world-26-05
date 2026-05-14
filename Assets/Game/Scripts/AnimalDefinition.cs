using UnityEngine;

[CreateAssetMenu(fileName = "New Animal", menuName = "Game/Animal Definition")]
public class AnimalDefinition : ScriptableObject
{
    [Header("Identity")]
    public string animalName = "Unnamed Animal";

    [Header("Prefab")]
    [Tooltip("The 3D model prefab instantiated in the scene. Should have a Collider for click detection.")]
    public GameObject animalPrefab;

    [Header("Group Spawning")]
    [Tooltip("Minimum number of animals in a spawned group.")]
    [Min(1)]
    public int groupSizeMin = 2;

    [Tooltip("Maximum number of animals in a spawned group.")]
    [Min(1)]
    public int groupSizeMax = 5;

    [Header("Movement")]
    [Tooltip("Radius around the spawn point within which the animal wanders.")]
    [Min(1f)]
    public float movementRadius = 8f;

    [Tooltip("Walking speed in units per second.")]
    [Min(0.1f)]
    public float moveSpeed = 2f;

    [Header("Fun Fact")]
    [TextArea(2, 5)]
    [Tooltip("Educational text shown when a child taps the animal.")]
    public string factText = "This animal is really cool!";

    [Header("Audio")]
    [Tooltip("Optional sound played when the animal is clicked.")]
    public AudioClip animalSound;

    public void Validate()
    {
        if (animalPrefab == null)
            Debug.LogWarning($"[AnimalDefinition] '{name}' has no prefab assigned.", this);

        if (string.IsNullOrWhiteSpace(animalName))
            Debug.LogWarning($"[AnimalDefinition] '{name}' has no animal name set.", this);

        if (groupSizeMax < groupSizeMin)
            Debug.LogWarning($"[AnimalDefinition] '{name}' has groupSizeMax ({groupSizeMax}) < groupSizeMin ({groupSizeMin}). Will clamp.", this);
    }
}
