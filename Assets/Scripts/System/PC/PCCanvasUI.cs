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

    [Header("Audio")]
    [SerializeField] private AudioClip printSound;

    [Header("Printed Photo Spawn")]
    [SerializeField] private PrintedPhotoPickup printedPhotoPrefab;
    [SerializeField] private Transform printedPhotoSpawnPoint;

    [Header("Printer Supplies UI")]
    [SerializeField] private TMP_Text paperValueText;
    [SerializeField] private TMP_Text inkValueText;
    [SerializeField] private TMP_Text printerStatusText;

    [Header("Supply Store")]
    [SerializeField] private Button buyPaperButton;
    [SerializeField] private Button buyInkButton;
    [SerializeField] private PrinterSupplyStore printerSupplyStore;

    private IInteractor currentInteractor;

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

        if (buyPaperButton != null)
            buyPaperButton.onClick.AddListener(OnBuyPaperClicked);

        if (buyInkButton != null)
            buyInkButton.onClick.AddListener(OnBuyInkClicked);

        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (PrinterSupplyData.Instance != null)
        {
            PrinterSupplyData.Instance.OnPaperChanged += OnPaperChanged;
            PrinterSupplyData.Instance.OnInkChanged += OnInkChanged;
        }
    }

    private void OnDisable()
    {
        if (PrinterSupplyData.Instance != null)
        {
            PrinterSupplyData.Instance.OnPaperChanged -= OnPaperChanged;
            PrinterSupplyData.Instance.OnInkChanged -= OnInkChanged;
        }
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
        RefreshSupplyUI();
        SetPlayerInteractorEnabled(false);
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

    private void RefreshSupplyUI()
    {
        PrinterSupplyData supplyData = PrinterSupplyData.Instance;

        if (supplyData == null)
        {
            if (paperValueText != null)
                paperValueText.text = "PAPER: -";

            if (inkValueText != null)
                inkValueText.text = "INK: -";

            if (printerStatusText != null)
                printerStatusText.text = string.Empty;

            return;
        }

        if (paperValueText != null)
            paperValueText.text = $"PAPER: {supplyData.CurrentPaper:0.0}";

        if (inkValueText != null)
            inkValueText.text = $"INK: {supplyData.CurrentInk:0.0}";

        if (printerStatusText != null)
        {
            printerStatusText.text = supplyData.GetBlockingMessage();
        }
    }

    private void OnPaperChanged(float value)
    {
        RefreshSupplyUI();
    }

    private void OnInkChanged(float value)
    {
        RefreshSupplyUI();
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

        if (string.IsNullOrEmpty(value))
            return;

        if (!int.TryParse(value, out int copyCount))
            return;

        copyCount = Mathf.Clamp(copyCount, 1, 99);
        printData.CopyCount = copyCount;
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
            if (printerStatusText != null)
                printerStatusText.text = "Printing is not ready yet.";
            return;
        }

        PrinterSupplyData supplyData = PrinterSupplyData.Instance;
        if (supplyData == null)
        {
            Debug.LogWarning("PrinterSupplyData is missing in the scene.");
            return;
        }

        if (!supplyData.CanPrint())
        {
            RefreshSupplyUI();
            return;
        }

        if (printedPhotoPrefab == null || printedPhotoSpawnPoint == null)
        {
            Debug.LogWarning("Printed photo prefab or spawn point is missing.");
            return;
        }

        if (!supplyData.TryConsumeForPrint())
        {
            RefreshSupplyUI();
            return;
        }

        if (printButton != null)
            printButton.interactable = false;

        StartCoroutine(PrintRoutine(printData.Clone()));
    }

    private IEnumerator PrintRoutine(PrintPhotoData printDataClone)
    {
        if (printSound != null)
        {
            AudioManager.Instance?.PlaySFX(printSound);
            yield return new WaitForSeconds(printSound.length);
        }

        PrintedPhotoPickup printedItem = Instantiate(
            printedPhotoPrefab,
            printedPhotoSpawnPoint.position,
            printedPhotoSpawnPoint.rotation
        );

        printedItem.Setup(printDataClone);

        RefreshSupplyUI();

        if (printButton != null)
            printButton.interactable = true;

        CloseUI();
        PlayerMessageUI.Instance?.ShowMessage("Printed photo is ready.");
    }

    private void OnBuyPaperClicked()
    {
        if (printerSupplyStore == null)
            return;

        printerSupplyStore.BuyPaperBox();
        RefreshSupplyUI();
    }

    private void OnBuyInkClicked()
    {
        if (printerSupplyStore == null)
            return;

        printerSupplyStore.BuyInkBox();
        RefreshSupplyUI();
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
            case "3x4": return 0;
            case "4x6": return 1;
            case "5x7": return 2;
            case "6x8": return 3;
            default: return 1;
        }
    }

    private string GetSizeLabel(int index)
    {
        switch (index)
        {
            case 0: return "3x4";
            case 1: return "4x6";
            case 2: return "5x7";
            case 3: return "6x8";
            default: return "4x6";
        }
    }
}