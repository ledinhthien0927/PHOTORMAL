using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class StudioPoseZone : MonoBehaviour
{
    [SerializeField] private Transform target; // customer root transform
    public bool IsTargetInside { get; private set; }

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void Awake()
    {
        // Make sure it's a trigger even if Reset didn't run (existing prefabs/scenes)
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    /// <summary>
    /// Bind the current customer root at runtime (for spawned prefab customers).
    /// Pass the CUSTOMER ROOT (recommended) or any child (we will use .root anyway).
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        IsTargetInside = false;
    }

    private bool MatchesTarget(Collider other)
    {
        if (target == null || other == null) return false;

        // Robust: any collider under the same root counts as the customer
        return other.transform.root == target.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (MatchesTarget(other))
            IsTargetInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (MatchesTarget(other))
            IsTargetInside = false;
    }
}