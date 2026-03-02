using UnityEngine;

[DisallowMultipleComponent]
public sealed class PickupInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName = "Item";
    public string Prompt => $"Pick up {itemName}";

    public bool CanInteract(IInteractor interactor)
    {
        // Only allow picking if player is not holding another object
        return interactor != null && interactor.Inventory != null && !interactor.Inventory.HasItem;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor)) return;

        // Store object in inventory
        if (!interactor.Inventory.TryPick(gameObject)) return;

        // Let holdable handle parenting/physics
        var holdable = GetComponent<IHoldable>();
        holdable?.OnPick(interactor.HoldPoint);

        // UI hooks (optional)
        UIManager.Instance.ShowDropButton();
        UIManager.Instance.HideTextItem();
    }
}