using UnityEngine;

public sealed class PCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private PCCanvasUI pcCanvasUI;
     
    [SerializeField] private GameObject gameplayCanvas;

    public string Prompt => "Use PC";

    public bool CanInteract(IInteractor interactor)
    {
        return pcCanvasUI != null;
    }

    public void Interact(IInteractor interactor)
    {
        Debug.Log("PC Interact called");

        if (!CanInteract(interactor))
            return;

       
        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(false);

     
        pcCanvasUI.OpenUI(interactor);
    }
}