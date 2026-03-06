using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class StorePurchaseUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buyBatteryButton;
    [SerializeField] private Button buyMemoryButton;

    [Header("Costs")]
    [SerializeField] private int batteryPackageCost = 30;
    [SerializeField] private int memoryPackageCost = 30;

    [Header("Reward Amounts")]
    [SerializeField] private float batteryRewardAmount = 5f;
    [SerializeField] private int memoryRewardAmount = 5;

    [Header("Shared Package Prefab")]
    [SerializeField] private SupplyPackageInteractable sharedPackagePrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform packageSpawnPoint;

    private SupplyPackageInteractable currentSpawnedPackage;

    private void Start()
    {
        if (buyBatteryButton != null)
            buyBatteryButton.onClick.AddListener(BuyBatteryPackage);

        if (buyMemoryButton != null)
            buyMemoryButton.onClick.AddListener(BuyMemoryPackage);
    }

    public void BuyBatteryPackage()
    {
        TryPurchaseBatteryPackage();
    }

    public void BuyMemoryPackage()
    {
        TryPurchaseMemoryPackage();
    }

    private void TryPurchaseBatteryPackage()
    {
        if (!CanSpawnPackage())
            return;

        if (GameProgress.Instance == null)
        {
            Debug.LogWarning("GameProgress instance is missing.");
            return;
        }

        bool spent = GameProgress.Instance.TrySpendMoney(batteryPackageCost);
        if (!spent)
        {
            Debug.LogWarning("Not enough money to buy battery package.");
            return;
        }

        SupplyPackageInteractable package = SpawnSharedPackage();
        if (package == null)
            return;

        package.SetupBatteryPackage(batteryRewardAmount);
        currentSpawnedPackage = package;

        StartCoroutine(ClearReferenceWhenDestroyed(currentSpawnedPackage));
    }

    private void TryPurchaseMemoryPackage()
    {
        if (!CanSpawnPackage())
            return;

        if (GameProgress.Instance == null)
        {
            Debug.LogWarning("GameProgress instance is missing.");
            return;
        }

        bool spent = GameProgress.Instance.TrySpendMoney(memoryPackageCost);
        if (!spent)
        {
            Debug.LogWarning("Not enough money to buy memory package.");
            return;
        }

        SupplyPackageInteractable package = SpawnSharedPackage();
        if (package == null)
            return;

        package.SetupMemoryPackage(memoryRewardAmount);
        currentSpawnedPackage = package;

        StartCoroutine(ClearReferenceWhenDestroyed(currentSpawnedPackage));
    }

    private bool CanSpawnPackage()
    {
        if (packageSpawnPoint == null)
        {
            Debug.LogWarning("Package spawn point is not assigned.");
            return false;
        }

        if (sharedPackagePrefab == null)
        {
            Debug.LogWarning("Shared package prefab is not assigned.");
            return false;
        }

        // Only allow one package to exist at a time.
        if (currentSpawnedPackage != null)
        {
            Debug.LogWarning("A package is already waiting at the pickup point.");
            return false;
        }

        return true;
    }

    private SupplyPackageInteractable SpawnSharedPackage()
    {
        return Instantiate(
            sharedPackagePrefab,
            packageSpawnPoint.position,
            packageSpawnPoint.rotation
        );
    }

    private IEnumerator ClearReferenceWhenDestroyed(SupplyPackageInteractable package)
    {
        while (package != null)
            yield return null;

        currentSpawnedPackage = null;
    }
}