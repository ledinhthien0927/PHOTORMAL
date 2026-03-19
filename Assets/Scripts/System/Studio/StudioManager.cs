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
        if (!CanTakePhoto(interactor))
            return false;

        if (cameraController == null || photoData == null)
            return false;

        // Báo cho RuleManager biết người chơi vừa mới bấm nút chụp ảnh
        // (để kiểm tra xem có đang nháy đèn hay không)
        GameEventAPI.OnPlayerShootPhoto?.Invoke();

        // Mới: Nếu đang nháy đèn, chúng ta không cho phép chụp ảnh thực tế
        if (RuleContext.Instance != null && RuleContext.Instance.IsFlickering)
        {
            Debug.Log("[StudioManager] Photo blocked due to flicker glitch!");
            return false;
        }

        lastShotTime = Time.time;
        
        cameraController.CapturePhoto(texture =>
        {
            if (texture == null)
                return;

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

    private bool CanTakePhoto(IInteractor interactor)
    {
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

        return true;
    }
}