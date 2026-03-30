using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    
    [SerializeField] private GameObject pickupButton;
    [SerializeField] private GameObject dropButton;


    [SerializeField] private GameObject interactPanel;
    [SerializeField] private TMP_Text interactText;

    private void Awake()
    {
        Instance = this;
        HideTextItem();
        ShowPickupButton();
        HideDropButton();
    }

    //public void ShowInteract(string text)
    //{
    //    interactPanel.SetActive(true);
    //    interactText.text = text;
    //}
    //public void HideInteract()
    //{
    //    interactPanel.SetActive(false);
    //}
    //==============================
    public void ShowPickupButton()
    {
        pickupButton.SetActive(true);
    }
    public void HidePickupButton()
    {
        pickupButton.SetActive(false);
    }
    //==============================
    public void ShowDropButton()
    {
        dropButton.SetActive(true);
    }

    public void HideDropButton()
    {
        dropButton.SetActive(false);
    }
    //==============================
    public void ShowTextItem(string text)
    {
        if (interactText.text != text)
            interactText.text = text;

        if (!interactPanel.activeSelf)
            interactPanel.SetActive(true);
    }

    public void HideTextItem()
    {
        if (interactPanel.activeSelf)
            interactPanel.SetActive(false);
    }
}