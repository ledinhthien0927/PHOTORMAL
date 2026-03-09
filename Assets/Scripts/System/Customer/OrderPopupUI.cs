using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OrderPopupUI : MonoBehaviour
{
    [Header("UI Refs")]
    public GameObject root;
    public TextMeshProUGUI orderText;
    public Button pickupButton;

    [Header("Follow Rotation")]
    [SerializeField] private bool followParentRotation = true;

    private Action onPickup;

    private void Awake()
    {
        // Avoid stacking multiple listeners.
        if (pickupButton != null)
            pickupButton.onClick.AddListener(HandlePickupClicked);

        // Default hidden.
        if (root != null)
            root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!followParentRotation)
            return;

        if (transform.parent == null)
            return;

        // Match the popup rotation with its parent anchor rotation
        // so the popup always faces the same direction as the customer.
        transform.rotation = transform.parent.rotation * Quaternion.Euler(0f, 180f, 0f);
    }

    public void Show(PhotoOrder order, Action onPickupClicked)
    {
        onPickup = onPickupClicked;

        if (root != null)
            root.SetActive(true);

        if (orderText != null)
        {
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
        onPickup?.Invoke();
        Hide();
    }
}