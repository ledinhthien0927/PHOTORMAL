using UnityEngine;

public sealed class PrinterSupplyStore : MonoBehaviour
{
    [Header("Prices")]
    [SerializeField] private int paperBoxPrice = 25;
    [SerializeField] private int inkBoxPrice = 35;

    [Header("Prefabs")]
    [SerializeField] private PrinterSupplyBox paperBoxPrefab;
    [SerializeField] private PrinterSupplyBox inkBoxPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform paperSpawnPoint;
    [SerializeField] private Transform inkSpawnPoint;

    [Header("Optional")]
    [SerializeField] private bool preventDuplicateBoxAtSpawn = true;

    private PrinterSupplyBox currentPaperBox;
    private PrinterSupplyBox currentInkBox;

    public void BuyPaperBox()
    {
        if (paperBoxPrefab == null || paperSpawnPoint == null)
        {
            Debug.LogWarning("Paper box prefab or spawn point is missing.");
            return;
        }

        if (preventDuplicateBoxAtSpawn && currentPaperBox != null)
        {
            PlayerMessageUI.Instance?.ShowMessage("Paper box is already waiting at the back door.");
            return;
        }

        if (GameProgress.Instance == null)
            return;

        if (!GameProgress.Instance.TrySpendMoney(paperBoxPrice))
            return;

        currentPaperBox = Instantiate(
            paperBoxPrefab,
            paperSpawnPoint.position,
            paperSpawnPoint.rotation
        );
    }

    public void BuyInkBox()
    {
        if (inkBoxPrefab == null || inkSpawnPoint == null)
        {
            Debug.LogWarning("Ink box prefab or spawn point is missing.");
            return;
        }

        if (preventDuplicateBoxAtSpawn && currentInkBox != null)
        {
            PlayerMessageUI.Instance?.ShowMessage("Ink box is already waiting at the back door.");
            return;
        }

        if (GameProgress.Instance == null)
            return;

        if (!GameProgress.Instance.TrySpendMoney(inkBoxPrice))
            return;

        currentInkBox = Instantiate(
            inkBoxPrefab,
            inkSpawnPoint.position,
            inkSpawnPoint.rotation
        );
    }

    private void Update()
    {
        if (currentPaperBox == null)
            currentPaperBox = null;

        if (currentInkBox == null)
            currentInkBox = null;
    }
}