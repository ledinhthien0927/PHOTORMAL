using System;
using UnityEngine;

public sealed class PrinterSupplyData : MonoBehaviour
{
    public static PrinterSupplyData Instance { get; private set; }

    [Header("Default Amount")]
    [SerializeField] private float defaultPaper = 10f;
    [SerializeField] private float defaultInk = 10f;

    [Header("Current Amount")]
    [SerializeField] private float currentPaper = 10f;
    [SerializeField] private float currentInk = 10f;

    [Header("Print Cost Per Photo")]
    [SerializeField] private float paperCostPerPrint = 1.5f;
    [SerializeField] private float inkCostPerPrint = 1f;

    public float CurrentPaper => currentPaper;
    public float CurrentInk => currentInk;
    public float DefaultPaper => defaultPaper;
    public float DefaultInk => defaultInk;

    public float PaperCostPerPrint => paperCostPerPrint;
    public float InkCostPerPrint => inkCostPerPrint;

    public event Action<float> OnPaperChanged;
    public event Action<float> OnInkChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentPaper = Mathf.Max(0f, currentPaper);
        currentInk = Mathf.Max(0f, currentInk);
    }

    private void Start()
    {
        OnPaperChanged?.Invoke(currentPaper);
        OnInkChanged?.Invoke(currentInk);
    }

    public bool CanPrint()
    {
        return currentPaper >= paperCostPerPrint && currentInk >= inkCostPerPrint;
    }

    public bool TryConsumeForPrint()
    {
        if (!CanPrint())
            return false;

        currentPaper -= paperCostPerPrint;
        currentInk -= inkCostPerPrint;

        currentPaper = Mathf.Max(0f, currentPaper);
        currentInk = Mathf.Max(0f, currentInk);

        OnPaperChanged?.Invoke(currentPaper);
        OnInkChanged?.Invoke(currentInk);

        return true;
    }

    public void AddPaper(float amount)
    {
        if (amount <= 0f)
            return;

        currentPaper += amount;
        OnPaperChanged?.Invoke(currentPaper);
    }

    public void AddInk(float amount)
    {
        if (amount <= 0f)
            return;

        currentInk += amount;
        OnInkChanged?.Invoke(currentInk);
    }

    public string GetBlockingMessage()
    {
        bool lowPaper = currentPaper < paperCostPerPrint;
        bool lowInk = currentInk < inkCostPerPrint;

        if (lowPaper && lowInk)
            return "Not enough paper and ink.";

        if (lowPaper)
            return "Not enough paper.";

        if (lowInk)
            return "Not enough ink.";

        return string.Empty;
    }

    public void LoadFromSaveData(SaveData data)
    {
        if (data == null) return;

        currentPaper = data.currentPaper;
        currentInk = data.currentInk;

        OnPaperChanged?.Invoke(currentPaper);
        OnInkChanged?.Invoke(currentInk);

        Debug.Log($"[PrinterSupplyData] Supplies Loaded. Paper: {currentPaper}, Ink: {currentInk}");
    }
}