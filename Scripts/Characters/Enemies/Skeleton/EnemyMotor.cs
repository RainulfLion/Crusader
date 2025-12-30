using UnityEngine;

public class EnemyMotor : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 720f;
    public float stoppingDistance = 1.8f;

    public CharacterController controller;

    private Health _health;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        _health = GetComponentInParent<Health>();
    }

    public bool CanMove()
    {
        return controller != null && (_health == null || !_health.isDead);
    }

    public void FaceTowards(Vector3 worldPosition)
    {
        Vector3 toTarget = worldPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public float DistanceTo(Vector3 worldPosition)
    {
        Vector3 to = worldPosition - transform.position;
        to.y = 0f;
        return to.magnitude;
    }

    public void Stop()
    {
        if (!CanMove()) return;
        controller.SimpleMove(Vector3.zero);
    }

    public void MoveTowards(Vector3 worldPosition)
    {
        if (!CanMove()) return;

        Vector3 toTarget = worldPosition - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stoppingDistance)
        {
            Stop();
            return;
        }

        Vector3 dir = dist > 0.0001f ? toTarget / dist : Vector3.zero;
        FaceTowards(worldPosition);
        controller.SimpleMove(dir * moveSpeed);
    }
}
