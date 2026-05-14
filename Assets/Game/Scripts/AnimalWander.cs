using UnityEngine;

/// <summary>
/// Gentle wander behaviour: picks random destinations within a radius,
/// walks there at a calm pace, idles briefly, then picks a new spot.
/// No NavMesh required. Values are injected from <see cref="AnimalDefinition"/>
/// by the scene generator.
/// </summary>
public class AnimalWander : MonoBehaviour
{
    [HideInInspector] public float movementRadius = 8f;
    [HideInInspector] public float moveSpeed = 2f;
    [HideInInspector] public float idleTimeMin = 2f;
    [HideInInspector] public float idleTimeMax = 5f;

    [Header("Tuning")]
    [Tooltip("How quickly the animal rotates toward its destination (degrees/sec).")]
    [Min(30f)]
    [SerializeField] float _turnSpeed = 180f;

    [Tooltip("Distance at which the animal considers itself arrived.")]
    [Min(0.05f)]
    [SerializeField] float _arrivalThreshold = 0.3f;

    Vector3 _origin;
    float _originY;
    Vector3 _destination;
    float _idleTimer;
    bool _isIdle;

    void Start()
    {
        _origin = transform.position;
        _originY = _origin.y;

        if (idleTimeMax < idleTimeMin)
        {
            Debug.LogWarning($"[AnimalWander] '{gameObject.name}' has idleTimeMax ({idleTimeMax}) < idleTimeMin ({idleTimeMin}) — swapping.", this);
            (idleTimeMin, idleTimeMax) = (idleTimeMax, idleTimeMin);
        }

        if (moveSpeed <= 0f)
        {
            Debug.LogWarning($"[AnimalWander] '{gameObject.name}' has moveSpeed <= 0 — defaulting to 1.", this);
            moveSpeed = 1f;
        }

        if (movementRadius <= 0f)
            Debug.LogWarning($"[AnimalWander] '{gameObject.name}' has movementRadius <= 0 — animal will not wander.", this);

        BeginIdle();
    }

    void Update()
    {
        if (_isIdle)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
                BeginWalking();
            return;
        }

        Vector3 toTarget = _destination - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= _arrivalThreshold * _arrivalThreshold)
        {
            BeginIdle();
            return;
        }

        if (toTarget == Vector3.zero)
            return;

        Quaternion desired = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, desired, _turnSpeed * Time.deltaTime);

        transform.position = Vector3.MoveTowards(
            transform.position, _destination, moveSpeed * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.y = _originY;
        transform.position = pos;
    }

    void BeginIdle()
    {
        _isIdle = true;
        _idleTimer = Random.Range(idleTimeMin, idleTimeMax);
    }

    void BeginWalking()
    {
        _isIdle = false;
        PickDestination();
    }

    void PickDestination()
    {
        Vector2 offset = Random.insideUnitCircle * movementRadius;
        _destination = _origin + new Vector3(offset.x, 0f, offset.y);
        _destination.y = _originY;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 centre = Application.isPlaying ? _origin : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.4f);
        DrawWireCircle(centre, movementRadius, 48);
    }

    static void DrawWireCircle(Vector3 centre, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prev = centre + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            Vector3 next = centre + new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
