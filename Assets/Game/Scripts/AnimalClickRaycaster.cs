using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Touchscreen = UnityEngine.InputSystem.Touchscreen;

/// <summary>
/// Attached to the main camera. Detects left-click or tap via the new Input System,
/// raycasts into the scene, and forwards clicks to <see cref="ClickableAnimal"/>.
/// </summary>
public class AnimalClickRaycaster : MonoBehaviour
{
    Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
                Debug.LogWarning("[AnimalClickRaycaster] No Camera found on this GameObject or tagged MainCamera.", this);
        }
    }

    void Update()
    {
        if (_camera == null) return;

        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = mouse.position.ReadValue();
        }

        if (!clicked)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
                screenPos = touch.primaryTouch.position.ReadValue();
            }
        }

        if (!clicked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var clickable = hit.collider.GetComponent<ClickableAnimal>();
            if (clickable != null)
                clickable.HandleClick();
        }
    }
}
