using System;
using UnityEngine;

public sealed class StudioManager : MonoBehaviour
{
    public static StudioManager Instance { get; private set; }

    public enum Mode { FreeRoam, PhotoMode }

    [Header("Modules")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PhotoData photoData;
    [SerializeField] private PhotoPrinter photoPrinter;

    [Header("Photo Mode")]
    [SerializeField] private PhotoModeController photoModeController;
    [SerializeField] private PhotoAimValidator aimValidator;

    [Header("Rules")]
    [SerializeField] private float shotCooldown = 0.8f;

    public Mode CurrentMode { get; private set; } = Mode.FreeRoam;

    private float _lastShotTime;

    public static event Action OnPhotoCaptured;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void NotifyCustomerReady(bool ready)
    {
        if (aimValidator != null)
            aimValidator.SetCustomerReady(ready);
    }

    public void SetPhotoMode(bool enabled, IInteractor interactor)
    {
        bool holdingCamera =
            interactor != null &&
            interactor.Inventory != null &&
            interactor.Inventory.HasItem &&
            interactor.Inventory.CurrentObject != null &&
            interactor.Inventory.CurrentObject.GetComponent<CameraItemUsable>() != null;

        bool allowPhotoMode = enabled && holdingCamera;

        CurrentMode = allowPhotoMode ? Mode.PhotoMode : Mode.FreeRoam;

        if (photoModeController != null)
            photoModeController.SetEnabled(allowPhotoMode);

        if (aimValidator != null)
            aimValidator.SetEnabled(allowPhotoMode, interactor);
    }

    public bool TryTakePhoto(IInteractor interactor)
    {
        if (!CanTakePhoto(interactor)) return false;

        _lastShotTime = Time.time;

        cameraController.CapturePhoto(tex =>
        {
            if (tex == null) return;

            var record = photoData.AddPhoto(tex);
            photoPrinter.ShowPreview(record, 1.8f);

            OnPhotoCaptured?.Invoke();
        });

        return true;
    }

    private bool CanTakePhoto(IInteractor interactor)
    {
        if (interactor == null) return false;

        if (CurrentMode != Mode.PhotoMode) return false;

        if (Time.time - _lastShotTime < shotCooldown) return false;

        if (interactor.Inventory == null || !interactor.Inventory.HasItem) return false;
        var held = interactor.Inventory.CurrentObject;
        if (held == null) return false;
        if (held.GetComponent<CameraItemUsable>() == null) return false;

        if (aimValidator != null && !aimValidator.CanShoot) return false;

        return true;
    }
}