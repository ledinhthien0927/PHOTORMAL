using UnityEngine;

// Ensure the object always has these components
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CustomerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.6f;     // Movement speed of the customer
    public float rotateSpeed = 360f;   // Rotation speed when turning toward target
    public float stopDistance = 0.1f;  // Distance to stop before reaching target

    private Transform target;          // Target position the customer will move toward
    private Rigidbody rb;              // Cached Rigidbody reference

    void Awake()
    {
        // Get Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Disable gravity because the customer moves manually
        rb.useGravity = false;

        // Rigidbody must NOT be kinematic to allow collision detection
        rb.isKinematic = false;

        // Prevent the character from tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Called by the spawner to assign a destination
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void FixedUpdate()
    {
        // If there is no target, do nothing
        if (target == null) return;

        // Calculate direction to target
        Vector3 targetPos = target.position;
        Vector3 dir = targetPos - transform.position;

        // Ignore vertical movement
        dir.y = 0f;

        // Stop if we are close enough to the target
        float sqrStop = stopDistance * stopDistance;
        if (dir.sqrMagnitude <= sqrStop) return;

        Vector3 moveDir = dir.normalized;

        // Rotate smoothly toward movement direction
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                look,
                rotateSpeed * Time.fixedDeltaTime
            );
        }

        // Calculate next position
        Vector3 nextPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;

        // Move using physics so collisions with walls are respected
        rb.MovePosition(nextPos);
    }
}