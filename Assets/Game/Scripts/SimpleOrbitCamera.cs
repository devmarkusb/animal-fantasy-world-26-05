using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Third-person orbit camera for children: right-click drag (desktop) or
/// one-finger drag (mobile) to rotate, scroll wheel to zoom.
/// Smoothly follows the target and keeps the camera above ground.
/// </summary>
public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to orbit around. An invisible pivot is created at the origin if left empty.")]
    public Transform target;

    [Header("Distance")]
    [Tooltip("Starting orbit distance. Auto-derived from the camera's initial offset when placed away from the target.")]
    [Min(1f)]
    public float distance = 25f;

    [Tooltip("Closest allowed zoom.")]
    [Min(1f)]
    public float minDistance = 5f;

    [Tooltip("Farthest allowed zoom.")]
    [Min(1f)]
    public float maxDistance = 60f;

    [Header("Rotation")]
    [Tooltip("Mouse / touch drag sensitivity.")]
    [Range(0.5f, 10f)]
    public float rotationSpeed = 3f;

    [Header("Zoom")]
    [Tooltip("Scroll-wheel zoom sensitivity.")]
    [Range(1f, 30f)]
    public float zoomSpeed = 5f;

    [Header("Pitch Limits")]
    [Tooltip("Minimum pitch angle (degrees from horizontal).")]
    [Range(5f, 89f)]
    public float minPitch = 10f;

    [Tooltip("Maximum pitch angle (degrees from horizontal).")]
    [Range(5f, 89f)]
    public float maxPitch = 80f;

    [Header("Smoothing")]
    [Tooltip("Responsiveness of camera follow. Higher values feel snappier.")]
    [Range(1f, 50f)]
    public float smoothing = 10f;

    [Header("Ground Avoidance")]
    [Tooltip("Camera will not descend below this world-space height.")]
    [Min(0f)]
    [SerializeField] float _minimumHeight = 0.5f;

    float _yaw;
    float _pitch;
    float _currentDistance;
    float _targetDistance;

    void Start()
    {
        if (target == null)
        {
            var go = new GameObject("CameraPivot");
            go.transform.position = Vector3.zero;
            target = go.transform;
        }

        InitOrbitFromPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleRotation();
        HandleZoom();
        ApplyOrbit();
    }

    void InitOrbitFromPosition()
    {
        Vector3 offset = transform.position - target.position;

        if (offset.sqrMagnitude > 1f)
        {
            _yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            float ny = Mathf.Clamp(offset.normalized.y, -1f, 1f);
            _pitch = Mathf.Asin(ny) * Mathf.Rad2Deg;
            distance = offset.magnitude;
        }
        else
        {
            _pitch = (minPitch + maxPitch) * 0.5f;
        }

        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        _currentDistance = distance;
        _targetDistance = distance;
    }

    void HandleRotation()
    {
        bool rotating = Input.GetMouseButton(1);

        if (!rotating && Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                bool overUI = EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                if (!overUI)
                    rotating = true;
            }
        }

        if (!rotating) return;

        _yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        _pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _targetDistance -= scroll * zoomSpeed;
        _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
    }

    void ApplyOrbit()
    {
        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, t);

        Quaternion desiredRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition =
            target.position - desiredRotation * Vector3.forward * _currentDistance;

        if (desiredPosition.y < _minimumHeight)
            desiredPosition.y = _minimumHeight;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
    }
}
