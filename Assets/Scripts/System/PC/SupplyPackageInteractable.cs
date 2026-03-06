using UnityEngine;

public sealed class SupplyPackageInteractable : MonoBehaviour, IInteractable
{
    public enum SupplyType
    {
        Battery,
        Memory
    }

    [Header("Prompt")]
    [SerializeField] private string pickupPrompt = "Pick up package";

    private SupplyType supplyType;
    private int memoryAmount;
    private float batteryAmount;

    public string Prompt => pickupPrompt;

    public bool CanInteract(IInteractor interactor)
    {
        return StudioManager.Instance != null && StudioManager.Instance.PhotoData != null;
    }

    public void SetupBatteryPackage(float amount)
    {
        supplyType = SupplyType.Battery;
        batteryAmount = amount;
        memoryAmount = 0;
    }

    public void SetupMemoryPackage(int amount)
    {
        supplyType = SupplyType.Memory;
        memoryAmount = amount;
        batteryAmount = 0f;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        PhotoData photoData = StudioManager.Instance.PhotoData;
        if (photoData == null)
            return;

        // Apply the purchased resource based on the package type.
        if (supplyType == SupplyType.Battery)
        {
            photoData.RechargeBattery(batteryAmount);
            Debug.Log($"Battery package collected. +{batteryAmount} battery.");
        }
        else
        {
            photoData.AddMemory(memoryAmount);
            Debug.Log($"Memory package collected. +{memoryAmount} memory.");
        }

        Destroy(gameObject);
    }
}