using System;
using UnityEngine;

public sealed class StudioManager : MonoBehaviour
{
    public static StudioManager Instance { get; private set; }

    public enum Mode
    {
        FreeRoam,
        PhotoMode
    }

    public enum PhotoCaptureBlockReason
    {
        None,
        LowBattery,
        LowMemory,
        LowBatteryAndLowMemory
    }

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

    public CustomerController CurrentCustomer { get; private set; }
    public PrintPhotoData CurrentPrintPhotoData { get; private set; }
    public PhotoData PhotoData => photoData;
    public bool CanPrintCurrentPhoto { get; private set; }

    private float lastShotTime;

    public static event Action OnPhotoCaptured;
    public static event Action<bool> OnPrintAvailabilityChanged;
    public static event Action<PhotoCaptureBlockReason> OnPhotoCaptureBlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void NotifyCustomerReady(bool ready)
    {
        if (aimValidator != null)
            aimValidator.SetCustomerReady(ready);
    }

    public void SetCurrentCustomer(CustomerController customer)
    {
        CurrentCustomer = customer;
        SetPrintAvailability(false);
    }

    public void ClearCurrentCustomer()
    {
        CurrentCustomer = null;
        SetPrintAvailability(false);
    }

    public void ClearCurrentPrintPhotoData()
    {
        CurrentPrintPhotoData = null;
        SetPrintAvailability(false);
    }

    public void UnlockPrinting()
    {
        SetPrintAvailability(true);
    }

    public void LockPrinting()
    {
        SetPrintAvailability(false);
    }

    private void SetPrintAvailability(bool value)
    {
        if (CanPrintCurrentPhoto == value)
            return;

        CanPrintCurrentPhoto = value;
        OnPrintAvailabilityChanged?.Invoke(CanPrintCurrentPhoto);
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
        if (!CanTakePhoto(interactor, out PhotoCaptureBlockReason blockReason))
        {
            if (blockReason != PhotoCaptureBlockReason.None)
                OnPhotoCaptureBlocked?.Invoke(blockReason);

            return false;
        }

        lastShotTime = Time.time;

        cameraController.CapturePhoto(texture =>
        {
            if (texture == null)
                return;

            // Consume resources only after a valid photo texture is produced.
            if (photoData != null && !photoData.TryConsumeShotResources())
            {
                OnPhotoCaptureBlocked?.Invoke(GetPhotoCaptureBlockReason());
                return;
            }

            PhotoRecord record = photoData.AddPhoto(texture);
            CurrentPrintPhotoData = new PrintPhotoData(record);

            // A newly captured photo cannot be printed until the session ends.
            SetPrintAvailability(false);

            if (photoPrinter != null)
                photoPrinter.ShowPreview(record, 1.8f);

            OnPhotoCaptured?.Invoke();
        });

        return true;
    }

    private bool CanTakePhoto(IInteractor interactor, out PhotoCaptureBlockReason blockReason)
    {
        blockReason = PhotoCaptureBlockReason.None;

        if (interactor == null)
            return false;

        if (CurrentMode != Mode.PhotoMode)
            return false;

        if (Time.time - lastShotTime < shotCooldown)
            return false;

        if (interactor.Inventory == null || !interactor.Inventory.HasItem)
            return false;

        GameObject heldObject = interactor.Inventory.CurrentObject;
        if (heldObject == null)
            return false;

        if (heldObject.GetComponent<CameraItemUsable>() == null)
            return false;

        if (aimValidator != null && !aimValidator.CanShoot)
            return false;

        if (photoData == null)
            return false;

        if (!photoData.HasEnoughResourcesForShot())
        {
            blockReason = GetPhotoCaptureBlockReason();
            return false;
        }

        return true;
    }

    private PhotoCaptureBlockReason GetPhotoCaptureBlockReason()
    {
        if (photoData == null)
            return PhotoCaptureBlockReason.None;

        bool lowBattery = !photoData.HasEnoughBatteryForShot();
        bool lowMemory = !photoData.HasEnoughMemoryForShot();

        if (lowBattery && lowMemory)
            return PhotoCaptureBlockReason.LowBatteryAndLowMemory;

        if (lowBattery)
            return PhotoCaptureBlockReason.LowBattery;

        if (lowMemory)
            return PhotoCaptureBlockReason.LowMemory;

        return PhotoCaptureBlockReason.None;
    }
}