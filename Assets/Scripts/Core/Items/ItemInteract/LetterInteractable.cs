using UnityEngine;

[DisallowMultipleComponent]
public sealed class LetterInteractable : MonoBehaviour, IInteractable
{
    [Header("Letter UI")]
    [SerializeField] private GameObject letterCanvas;

    [Header("Prompt")]
    [SerializeField] private string prompt = "Read letter";

    private IInteractor currentInteractor;

    public string Prompt => prompt;

    public bool CanInteract(IInteractor interactor)
    {
        return letterCanvas != null;
    }

    public void Interact(IInteractor interactor)
    {
        if (letterCanvas == null) return;

        currentInteractor = interactor;

        letterCanvas.SetActive(true);
        SetInteractorEnabled(false);

        if (UIManager.Instance != null)
            UIManager.Instance.HideTextItem();
    }

    public void CloseLetter()
    {
        if (letterCanvas == null) return;

        letterCanvas.SetActive(false);
        SetInteractorEnabled(true);
        currentInteractor = null;
    }

    private void SetInteractorEnabled(bool enabled)
    {
        if (currentInteractor == null) return;

        MonoBehaviour interactorBehaviour = currentInteractor as MonoBehaviour;
        if (interactorBehaviour == null) return;

        interactorBehaviour.enabled = enabled;
    }
}