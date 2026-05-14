using UnityEngine;

/// <summary>
/// Moves the camera pivot on the XZ plane using WASD / arrow keys,
/// oriented relative to the camera's current yaw. Lets children
/// explore the biome without needing a character controller.
/// </summary>
public class CameraTargetMover : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed in units per second.")]
    [Min(0.5f)]
    public float moveSpeed = 10f;

    [Tooltip("Half-size of the square movement boundary. Zero means unlimited.")]
    [Min(0f)]
    public float boundaryRadius;

    bool _warnedNoCamera;

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
            return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            if (!_warnedNoCamera)
            {
                Debug.LogWarning("[CameraTargetMover] No camera tagged 'MainCamera' found — WASD movement disabled.", this);
                _warnedNoCamera = true;
            }
            return;
        }

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 input = Vector3.ClampMagnitude(forward * v + right * h, 1f);
        Vector3 newPos = transform.position + input * (moveSpeed * Time.deltaTime);
        newPos.y = transform.position.y;

        if (boundaryRadius > 0f)
        {
            newPos.x = Mathf.Clamp(newPos.x, -boundaryRadius, boundaryRadius);
            newPos.z = Mathf.Clamp(newPos.z, -boundaryRadius, boundaryRadius);
        }

        transform.position = newPos;
    }
}
