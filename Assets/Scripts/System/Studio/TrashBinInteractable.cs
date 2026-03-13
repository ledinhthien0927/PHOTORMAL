using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrashBinInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Throw photo away";

    public string Prompt => prompt;

    public bool CanInteract(IInteractor interactor)
    {
        if (interactor == null || interactor.Inventory == null || !interactor.Inventory.HasItem)
            return false;

        GameObject heldObject = interactor.Inventory.CurrentObject;
        if (heldObject == null)
            return false;

        return heldObject.GetComponent<PrintedPhotoPickup>() != null;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        GameObject heldObject = interactor.Inventory.CurrentObject;
        PrintedPhotoPickup printedPhoto = heldObject.GetComponent<PrintedPhotoPickup>();

        if (printedPhoto == null)
            return;

        Destroy(heldObject);

        PlayerMessageUI.Instance?.ShowMessage("Printed photo discarded.", 2f);
    }
}