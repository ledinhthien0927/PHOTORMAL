using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OrderPopupUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject root;          // Panel root (SetActive on/off)
    public TextMeshProUGUI orderText; // Text for showing order info
    public Button pickupButton;       // Button that player clicks to pick up order

    private Action onPickup;

    void Awake()
    {
        // Avoid stacking multiple listeners
        if (pickupButton != null)
            pickupButton.onClick.AddListener(HandlePickupClicked);

        // Default hidden
        if (root != null)
            root.SetActive(false);
    }

    public void Show(PhotoOrder order, Action onPickupClicked)
    {
        onPickup = onPickupClicked;

        if (root != null)
            root.SetActive(true);

        if (orderText != null)
        {
            // English UI text formatting
            orderText.text =
                "ORDER\n" +
                $"Quantity: {order.quantity}\n" +
                $"Size: {PhotoSizeText.ToLabel(order.size)}";
        }
    }

    public void Hide()
    {
        onPickup = null;

        if (root != null)
            root.SetActive(false);
    }

    private void HandlePickupClicked()
    {
        // Invoke callback and hide popup
        onPickup?.Invoke();
        Hide();
    }
}