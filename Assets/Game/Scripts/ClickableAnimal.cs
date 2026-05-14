using UnityEngine;

/// <summary>
/// Detects mouse/touch clicks on an animal, shows its fun-fact and plays its sound.
/// Requires a Collider on this GameObject or its children.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClickableAnimal : MonoBehaviour
{
    [HideInInspector] public AnimalDefinition definition;

    AudioSource _audioSource;

    void Awake()
    {
        if (definition != null && definition.animalSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = definition.animalSound;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }
    }

    void OnMouseDown()
    {
        if (definition == null)
        {
            Debug.LogWarning("[ClickableAnimal] No AnimalDefinition assigned. Cannot show info.", this);
            return;
        }

        Debug.Log($"<b>{definition.animalName}</b>: {definition.factText}");

        if (_audioSource != null && definition.animalSound != null)
            _audioSource.PlayOneShot(definition.animalSound);
    }
}
