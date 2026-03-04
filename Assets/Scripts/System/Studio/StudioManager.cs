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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by StudioSpotTrigger when player enters/leaves the fixed shooting spot
    public void SetPhotoMode(bool enabled, IInteractor interactor)
    {
        // Check if player is holding the camera item
        bool holdingCamera =
            interactor != null &&
            interactor.Inventory != null &&
            interactor.Inventory.HasItem &&
            interactor.Inventory.CurrentObject != null &&
            interactor.Inventory.CurrentObject.GetComponent<CameraItemUsable>() != null;

        // Only allow PhotoMode when inside spot AND holding the camera
        bool allowPhotoMode = enabled && holdingCamera;

        CurrentMode = allowPhotoMode ? Mode.PhotoMode : Mode.FreeRoam;

        if (photoModeController != null)
            photoModeController.SetEnabled(allowPhotoMode);

        if (aimValidator != null)
            aimValidator.SetEnabled(allowPhotoMode, interactor);
    }

    // Called by the held camera item (IUsable)
    public bool TryTakePhoto(IInteractor interactor)
    {
        if (!CanTakePhoto(interactor)) return false;

        _lastShotTime = Time.time;

        cameraController.CapturePhoto(tex =>
        {
            if (tex == null) return;

            var record = photoData.AddPhoto(tex);
            photoPrinter.ShowPreview(record, 1.8f);
        });

        return true;
    }

    private bool CanTakePhoto(IInteractor interactor)
    {
        if (interactor == null) return false;

        // Must be inside PhotoMode (fixed studio spot)
        if (CurrentMode != Mode.PhotoMode) return false;

        // Cooldown
        if (Time.time - _lastShotTime < shotCooldown) return false;

        // Must hold the camera item
        if (interactor.Inventory == null || !interactor.Inventory.HasItem) return false;
        var held = interactor.Inventory.CurrentObject;
        if (held == null) return false;
        if (held.GetComponent<CameraItemUsable>() == null) return false;

        // Must be "green dot" state
        if (aimValidator != null && !aimValidator.CanShoot) return false;

        return true;
    }
}