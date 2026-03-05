using UnityEngine;

public sealed class CameraItemUsable : MonoBehaviour, IUsable
{
    public void Use(IInteractor interactor)
    {
        if (StudioManager.Instance == null) return;
        StudioManager.Instance.TryTakePhoto(interactor);
    }
}