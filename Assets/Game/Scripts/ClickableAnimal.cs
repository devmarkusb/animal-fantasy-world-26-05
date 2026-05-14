using UnityEngine;

/// <summary>
/// Detects mouse/touch clicks on an animal and logs its fun-fact.
/// A real project would open a child-friendly UI popup instead.
/// Requires a Collider on this GameObject or its children.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClickableAnimal : MonoBehaviour
{
    [HideInInspector] public AnimalDefinition definition;

    void OnMouseDown()
    {
        if (definition == null)
        {
            Debug.LogWarning("[ClickableAnimal] No AnimalDefinition assigned. Cannot show info.", this);
            return;
        }

        Debug.Log($"<b>{definition.displayName}</b>: {definition.funFact}");
    }
}
