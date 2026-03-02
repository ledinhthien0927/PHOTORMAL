using UnityEngine;

public interface IInteractable
{
    // Text shown in UI when player looks at this object
    string Prompt { get; }

    // Whether the interactor is allowed to interact right now
    bool CanInteract(IInteractor interactor);

    // Execute interaction logic
    void Interact(IInteractor interactor);
}

public interface IInteractor
{
    // Interactor transform (player transform)
    Transform Transform { get; }

    // Where held objects should be attached (hand/hold point)
    Transform HoldPoint { get; }

    // Inventory reference
    IInventory Inventory { get; }
}

public interface IInventory
{
    // Whether inventory currently holds an object
    bool HasItem { get; }

    // Currently held object
    GameObject CurrentObject { get; }

    // Try to pick an object into inventory (returns false if already holding something)
    bool TryPick(GameObject obj);

    // Try to drop the currently held object
    bool TryDrop(out GameObject obj);
}

public interface IHoldable
{
    // Called when the object is picked up
    void OnPick(Transform holdPoint);

    // Called when the object is dropped
    void OnDrop(Vector3 worldPosition);
}

public interface IUsable
{
    // Called when player uses the held object
    void Use(IInteractor interactor);
}