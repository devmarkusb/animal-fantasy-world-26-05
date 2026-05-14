using UnityEngine;

/// <summary>
/// Child-friendly orbit camera: drag to rotate, scroll to zoom.
/// Orbits around a fixed pivot point (defaults to world origin).
/// </summary>
public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform pivot;

    [Header("Orbit")]
    [Range(0.5f, 10f)]
    public float rotateSpeed = 3f;

    [Header("Zoom")]
    [Min(2f)]
    public float minDistance = 5f;

    [Min(2f)]
    public float maxDistance = 60f;

    [Range(1f, 20f)]
    public float zoomSpeed = 5f;

    [Header("Pitch Limits (degrees)")]
    [Range(5f, 89f)]
    public float minPitch = 10f;

    [Range(5f, 89f)]
    public float maxPitch = 80f;

    float _yaw;
    float _pitch = 35f;
    float _distance = 25f;

    void Start()
    {
        if (pivot == null)
        {
            var go = new GameObject("CameraPivot");
            go.transform.position = Vector3.zero;
            pivot = go.transform;
            Debug.LogWarning("[SimpleOrbitCamera] No pivot assigned — created one at world origin.", this);
        }

        Vector3 offset = transform.position - pivot.position;
        _distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
        _yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        HandleInput();
        ApplyOrbit();
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
        {
            _yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _distance -= scroll * zoomSpeed;
            _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
        }
    }

    void ApplyOrbit()
    {
        if (pivot == null) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 position = pivot.position - rotation * Vector3.forward * _distance;

        transform.SetPositionAndRotation(position, rotation);
    }
}
