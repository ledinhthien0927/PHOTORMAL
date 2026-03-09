using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PCCanvasUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Other UI")]
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Preview")]
    [SerializeField] private RawImage previewImage;

    [Header("Controls")]
    [SerializeField] private TMP_Dropdown sizeDropdown;
    [SerializeField] private TMP_InputField copyInput;
    [SerializeField] private Button printButton;
    [SerializeField] private Button closeButton;

    [Header("Notification Images")]
    [SerializeField] private Image notReadyImage;
    [SerializeField] private Image wrongOrderImage;
    [SerializeField] private Image threeWrongAttemptsImage;
    [SerializeField] private float notificationDuration = 1.5f;
    [SerializeField] private int maxWrongAttempts = 3;

    private IInteractor currentInteractor;
    private Coroutine notificationRoutine;
    private int wrongPrintAttempts;

    private void Start()
    {
        if (printButton != null)
            printButton.onClick.AddListener(OnPrintClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);

        if (sizeDropdown != null)
            sizeDropdown.onValueChanged.AddListener(OnSizeChanged);

        if (copyInput != null)
            copyInput.onValueChanged.AddListener(OnCopyChanged);

        if (rootPanel != null)
            rootPanel.SetActive(false);

        HideAllNotificationImages();
    }

    public void OpenUI(IInteractor interactor)
    {
        if (rootPanel == null)
        {
            Debug.LogWarning("PC root panel is not assigned.");
            return;
        }

        currentInteractor = interactor;

        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(false);

        rootPanel.SetActive(true);
        RefreshUI();
        SetPlayerInteractorEnabled(false);
        HideAllNotificationImages();
    }

    public void CloseUI()
    {
        if (rootPanel == null)
            return;

        rootPanel.SetActive(false);

        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(true);

        SetPlayerInteractorEnabled(true);
        currentInteractor = null;
        HideAllNotificationImages();
    }

    private void RefreshUI()
    {
        if (StudioManager.Instance == null)
            return;

        PrintPhotoData printData = StudioManager.Instance.CurrentPrintPhotoData;

        if (printData == null)
        {
            if (previewImage != null)
                previewImage.texture = null;

            if (sizeDropdown != null)
                sizeDropdown.SetValueWithoutNotify(0);

            if (copyInput != null)
                copyInput.SetTextWithoutNotify("1");

            return;
        }

        if (previewImage != null && printData.PhotoRecord != null)
            previewImage.texture = printData.PhotoRecord.texture;

        if (sizeDropdown != null)
            sizeDropdown.SetValueWithoutNotify(GetSizeIndex(printData.PrintSize));

        if (copyInput != null)
            copyInput.SetTextWithoutNotify(printData.CopyCount.ToString());
    }

    private void OnSizeChanged(int index)
    {
        if (StudioManager.Instance == null)
            return;

        PrintPhotoData printData = StudioManager.Instance.CurrentPrintPhotoData;
        if (printData == null)
            return;

        printData.PrintSize = GetSizeLabel(index);
    }

    private void OnCopyChanged(string value)
    {
        if (StudioManager.Instance == null)
            return;

        PrintPhotoData printData = StudioManager.Instance.CurrentPrintPhotoData;
        if (printData == null)
            return;

        if (!int.TryParse(value, out int copyCount))
            copyCount = 1;

        copyCount = Mathf.Clamp(copyCount, 1, 99);
        printData.CopyCount = copyCount;

        if (copyInput != null && copyInput.text != copyCount.ToString())
            copyInput.SetTextWithoutNotify(copyCount.ToString());
    }

    private void OnPrintClicked()
    {
        if (StudioManager.Instance == null)
            return;

        CustomerController customer = StudioManager.Instance.CurrentCustomer;
        PrintPhotoData printData = StudioManager.Instance.CurrentPrintPhotoData;

        if (customer == null || printData == null)
            return;

        if (!StudioManager.Instance.CanPrintCurrentPhoto)
        {
            ShowNotificationImage(notReadyImage);
            Debug.LogWarning("Printing is locked because the session has not ended yet.");
            return;
        }

        PhotoOrder order = customer.GetCurrentOrder();

        if (!IsPrintSizeCorrect(order, printData) || !IsCopyCountCorrect(order, printData))
        {
            HandleWrongPrintAttempt(customer);
            return;
        }

        int reward = 15 * order.quantity;

        if (GameProgress.Instance != null)
            GameProgress.Instance.AddMoney(reward);

        customer.ReceivePrintedPhoto(printData);

        if (StudioManager.Instance.PhotoData != null && printData.PhotoRecord != null)
            StudioManager.Instance.PhotoData.RemovePhoto(printData.PhotoRecord);

        StudioManager.Instance.ClearCurrentPrintPhotoData();
        wrongPrintAttempts = 0;

        CloseUI();
    }

    private void HandleWrongPrintAttempt(CustomerController customer)
    {
        wrongPrintAttempts++;

        Debug.LogWarning($"Wrong print request for this customer. Attempt {wrongPrintAttempts}/{maxWrongAttempts}");

        if (wrongPrintAttempts >= maxWrongAttempts)
        {
            ShowNotificationImage(threeWrongAttemptsImage);
            StartCoroutine(HandleCustomerLeaveAfterWrongAttempts(customer));
            return;
        }

        ShowNotificationImage(wrongOrderImage);
    }

    private IEnumerator HandleCustomerLeaveAfterWrongAttempts(CustomerController customer)
    {
        yield return new WaitForSecondsRealtime(notificationDuration);

        if (customer != null)
            customer.LeaveBecauseOfPrintMistakes();

        wrongPrintAttempts = 0;
        CloseUI();
    }

    private bool IsPrintSizeCorrect(PhotoOrder order, PrintPhotoData printData)
    {
        if (printData == null)
            return false;

        string requiredSize = ConvertPhotoSizeToLabel(order.size);
        return printData.PrintSize == requiredSize;
    }

    private bool IsCopyCountCorrect(PhotoOrder order, PrintPhotoData printData)
    {
        if (printData == null)
            return false;

        return printData.CopyCount == order.quantity;
    }

    private string ConvertPhotoSizeToLabel(PhotoSize size)
    {
        switch (size)
        {
            case PhotoSize.Size3x4:
                return "3x4";
            case PhotoSize.Size4x6:
                return "4x6";
            case PhotoSize.Size5x7:
                return "5x7";
            case PhotoSize.Size6x8:
                return "6x8";
            default:
                return "4x6";
        }
    }

    private void SetPlayerInteractorEnabled(bool enabled)
    {
        if (currentInteractor == null)
            return;

        MonoBehaviour interactorBehaviour = currentInteractor as MonoBehaviour;
        if (interactorBehaviour == null)
            return;

        interactorBehaviour.enabled = enabled;
    }

    private int GetSizeIndex(string size)
    {
        switch (size)
        {
            case "3x4":
                return 0;
            case "4x6":
                return 1;
            case "5x7":
                return 2;
            case "6x8":
                return 3;
            default:
                return 1;
        }
    }

    private string GetSizeLabel(int index)
    {
        switch (index)
        {
            case 0:
                return "3x4";
            case 1:
                return "4x6";
            case 2:
                return "5x7";
            case 3:
                return "6x8";
            default:
                return "4x6";
        }
    }

    private void ShowNotificationImage(Image targetImage)
    {
        if (targetImage == null)
            return;

        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);

        HideAllNotificationImages();
        notificationRoutine = StartCoroutine(ShowNotificationRoutine(targetImage));
    }

    private IEnumerator ShowNotificationRoutine(Image targetImage)
    {
        targetImage.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(notificationDuration);
        targetImage.gameObject.SetActive(false);
        notificationRoutine = null;
    }

    private void HideAllNotificationImages()
    {
        if (notReadyImage != null)
            notReadyImage.gameObject.SetActive(false);

        if (wrongOrderImage != null)
            wrongOrderImage.gameObject.SetActive(false);

        if (threeWrongAttemptsImage != null)
            threeWrongAttemptsImage.gameObject.SetActive(false);
    }
}