using UnityEngine;

/// <summary>
/// Shows a <see cref="FactCardUI"/> with the animal's name and fun fact,
/// and plays its sound. Click detection is handled by
/// <see cref="AnimalClickRaycaster"/> on the camera, which calls
/// <see cref="HandleClick"/> when this animal is hit.
/// A Collider on this root GameObject is required for raycasting.
/// If missing, one is auto-added at runtime as a safety net.
/// </summary>
public class ClickableAnimal : MonoBehaviour
{
    [HideInInspector] public AnimalDefinition definition;

    AudioSource _audioSource;

    void Awake()
    {
        EnsureRootCollider();

        if (definition == null)
        {
            Debug.LogWarning($"[ClickableAnimal] '{gameObject.name}' has no AnimalDefinition assigned — clicks will be ignored.", this);
            return;
        }

        if (definition.animalSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = definition.animalSound;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }
    }

    /// <summary>
    /// Called by <see cref="AnimalClickRaycaster"/> when the player
    /// clicks or taps this animal.
    /// </summary>
    public void HandleClick()
    {
        if (definition == null)
        {
            Debug.LogWarning($"[ClickableAnimal] '{gameObject.name}' has no AnimalDefinition — cannot show fact card.", this);
            return;
        }

        var card = FactCardUI.Instance;
        if (card == null)
        {
            Debug.LogWarning("[ClickableAnimal] FactCardUI could not be created.", this);
            return;
        }

        string displayName = string.IsNullOrWhiteSpace(definition.animalName)
            ? "Unknown Animal" : definition.animalName;
        string displayFact = string.IsNullOrWhiteSpace(definition.factText)
            ? "No fact available." : definition.factText;

        card.Show(displayName, displayFact);

        if (_audioSource != null && definition.animalSound != null)
            _audioSource.PlayOneShot(definition.animalSound);
    }

    void EnsureRootCollider()
    {
        if (GetComponent<Collider>() != null)
            return;

        var col = gameObject.AddComponent<BoxCollider>();
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            col.size = Vector3.one;
            Debug.LogWarning($"[ClickableAnimal] '{gameObject.name}' has no Renderers — added a default 1x1x1 BoxCollider for click detection.", this);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        col.center = transform.InverseTransformPoint(bounds.center);
        col.size = bounds.size;
        Debug.LogWarning($"[ClickableAnimal] '{gameObject.name}' had no root Collider — auto-added a BoxCollider for click detection.", this);
    }
}
