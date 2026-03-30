using UnityEngine;

public sealed class PrinterSupplyBox : MonoBehaviour, IInteractable
{
    public enum SupplyType
    {
        Paper,
        Ink
    }

    [Header("Supply")]
    [SerializeField] private SupplyType supplyType;
    [SerializeField] private float amount = 10f;

    [Header("Prompt")]
    [SerializeField] private string paperPrompt = "Take paper box";
    [SerializeField] private string inkPrompt = "Take ink box";

    public string Prompt
    {
        get
        {
            return supplyType == SupplyType.Paper ? paperPrompt : inkPrompt;
        }
    }

    public bool CanInteract(IInteractor interactor)
    {
        return PrinterSupplyData.Instance != null;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        if (supplyType == SupplyType.Paper)
        {
            PrinterSupplyData.Instance.AddPaper(amount);
            PlayerMessageUI.Instance?.ShowMessage("Paper restocked.");
        }
        else
        {
            PrinterSupplyData.Instance.AddInk(amount);
            PlayerMessageUI.Instance?.ShowMessage("Ink restocked.");
        }

        Destroy(gameObject);
    }
}