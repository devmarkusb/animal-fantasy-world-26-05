using UnityEngine;

/// <summary>
/// Picks a random point within <see cref="movementRadius"/> on a timer
/// and smoothly walks toward it. Rotates to face movement direction.
/// Values are injected from <see cref="AnimalDefinition"/> by the scene generator.
/// </summary>
public class AnimalWander : MonoBehaviour
{
    [HideInInspector] public float moveSpeed = 2f;
    [HideInInspector] public float movementRadius = 8f;

    Vector3 _origin;
    Vector3 _target;
    float _nextPickTime;
    float _interval = 4f;

    void Start()
    {
        _origin = transform.position;
        _interval = Mathf.Max(1f, movementRadius / Mathf.Max(moveSpeed, 0.1f));
        PickNewTarget();
    }

    void Update()
    {
        if (Time.time >= _nextPickTime)
            PickNewTarget();

        Vector3 direction = _target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.25f)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _target,
            moveSpeed * Time.deltaTime);

        Quaternion desired = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, 5f * Time.deltaTime);
    }

    void PickNewTarget()
    {
        Vector2 offset = Random.insideUnitCircle * movementRadius;
        _target = _origin + new Vector3(offset.x, 0f, offset.y);
        _nextPickTime = Time.time + _interval + Random.Range(-0.5f, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 centre = Application.isPlaying ? _origin : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.25f);
        Gizmos.DrawWireSphere(centre, movementRadius);
    }
}
