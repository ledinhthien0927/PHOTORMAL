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

    private void OnTriggerEnter(Collider other)
    {
        if (target == null) return;
        if (other.transform == target || other.transform.IsChildOf(target))
            IsTargetInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (target == null) return;
        if (other.transform == target || other.transform.IsChildOf(target))
            IsTargetInside = false;
    }
}