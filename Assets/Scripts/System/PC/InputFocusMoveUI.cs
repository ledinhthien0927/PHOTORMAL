using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InputFocusMoveUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private RectTransform panelToMove;
    [SerializeField] private float moveUpY = 250f;

    private Vector2 originalPos;

    private void Awake()
    {
        if (panelToMove != null)
            originalPos = panelToMove.anchoredPosition;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (panelToMove != null)
            panelToMove.anchoredPosition = originalPos + new Vector2(0f, moveUpY);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (panelToMove != null)
            panelToMove.anchoredPosition = originalPos;
    }
}