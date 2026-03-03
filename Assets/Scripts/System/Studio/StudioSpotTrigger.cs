using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class StudioSpotTrigger : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactor = other.GetComponentInParent<IInteractor>();
        if (interactor == null) return;

        if (StudioManager.Instance != null)
            StudioManager.Instance.SetPhotoMode(true, interactor);
    }

    private void OnTriggerExit(Collider other)
    {
        var interactor = other.GetComponentInParent<IInteractor>();
        if (interactor == null) return;

        if (StudioManager.Instance != null)
            StudioManager.Instance.SetPhotoMode(false, interactor);
    }
}