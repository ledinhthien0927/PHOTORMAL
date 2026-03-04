using UnityEngine;

[DisallowMultipleComponent]
public sealed class Holdable : MonoBehaviour, IHoldable
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void OnPick(Transform holdPoint)
    {
        // Disable physics while holding
        if (rb) rb.isKinematic = true;

        // Disable collider to avoid clipping with player
        if (col) col.enabled = false;

        // Attach to hold point
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void OnDrop(Vector3 worldPosition)
    {
        // Detach from player
        transform.SetParent(null);
        transform.position = worldPosition;

        // Re-enable physics
        if (rb) rb.isKinematic = false;

        // Re-enable collider
        if (col) col.enabled = true;
    }
}