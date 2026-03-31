using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour, IInteractor
{
    [Header("Detection Settings")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float sphereRadius = 0.08f;
    [SerializeField] private float graceTime = 0.12f;
    [SerializeField] private float enterRange = 2.9f;
    [SerializeField] private float exitRange = 3.1f;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Inventory / Hold")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform holdPoint;

    private IInteractable currentTarget;
    private float lastSeenTime;
    private float lastSeenDistance;
    private CustomerController currentCustomerForPopup;

    public Transform Transform => transform;
    public Transform HoldPoint => holdPoint;
    public IInventory Inventory => inventory;

    private bool IsPhotoMode =>
        StudioManager.Instance != null &&
        StudioManager.Instance.CurrentMode == StudioManager.Mode.PhotoMode;

    private bool IsHoldingCameraItem =>
        inventory != null &&
        inventory.HasItem &&
        inventory.CurrentObject != null &&
        inventory.CurrentObject.GetComponent<CameraItemUsable>() != null;

    private void Update()
    {
        if (!IsPhotoMode)
            UpdateTarget();
        else
            ClearTargetAndUI();
    }

    private void ClearTargetAndUI()
    {
        if (currentCustomerForPopup != null)
        {
            currentCustomerForPopup.SetInvitePromptVisible(false);
            currentCustomerForPopup = null;
        }

        if (currentTarget != null)
        {
            currentTarget = null;
            UIManager.Instance.HideTextItem();
        }
    }

    private void UpdateTarget()
    {
        IInteractable hitTarget = null;
        float hitDistance = float.PositiveInfinity;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Ignore))
        {
            hitTarget = hit.collider.GetComponentInParent<IInteractable>();
            hitDistance = hit.distance;
        }

        if (hitTarget != null)
        {
            lastSeenTime = Time.time;
            lastSeenDistance = hitDistance;
        }

        bool lostTooLong = (Time.time - lastSeenTime) > graceTime;

        bool withinEnterRange = (hitTarget != null && hitDistance <= enterRange);
        bool beyondExitRange = (currentTarget != null && lastSeenDistance >= exitRange);

        IInteractable newTarget = currentTarget;

        if (currentTarget == null)
        {
            if (withinEnterRange)
                newTarget = hitTarget;
        }
        else
        {
            if (hitTarget == currentTarget)
            {
                newTarget = currentTarget;
            }
            else
            {
                if (lostTooLong || beyondExitRange)
                    newTarget = null;

                if (withinEnterRange)
                    newTarget = hitTarget;
            }
        }

        if (newTarget != currentTarget)
        {
            if (currentCustomerForPopup != null)
            {
                currentCustomerForPopup.SetInvitePromptVisible(false);
                currentCustomerForPopup = null;
            }

            currentTarget = newTarget;

            if (currentTarget != null)
            {
                UIManager.Instance.ShowTextItem(currentTarget.Prompt);

                CustomerController customer = currentTarget as CustomerController;
                if (customer != null && customer.Prompt == "Invite to studio")
                {
                    currentCustomerForPopup = customer;
                    currentCustomerForPopup.SetInvitePromptVisible(true);
                }
            }
            else
            {
                UIManager.Instance.HideTextItem();
            }
        }
        else if (currentTarget != null && currentTarget.CanInteract(this))
        {
            UIManager.Instance.ShowTextItem(currentTarget.Prompt);

            CustomerController customer = currentTarget as CustomerController;
            if (customer != null && customer.Prompt == "Invite to studio")
            {
                if (currentCustomerForPopup != customer)
                {
                    if (currentCustomerForPopup != null)
                        currentCustomerForPopup.SetInvitePromptVisible(false);

                    currentCustomerForPopup = customer;
                    currentCustomerForPopup.SetInvitePromptVisible(true);
                }
            }
            else
            {
                if (currentCustomerForPopup != null)
                {
                    currentCustomerForPopup.SetInvitePromptVisible(false);
                    currentCustomerForPopup = null;
                }
            }
        }
        else if (currentTarget != null && !currentTarget.CanInteract(this))
        {
            UIManager.Instance.HideTextItem();

            if (currentCustomerForPopup != null)
            {
                currentCustomerForPopup.SetInvitePromptVisible(false);
                currentCustomerForPopup = null;
            }
        }
    }

    public void OnPickupButton()
    {
        if (IsPhotoMode && IsHoldingCameraItem)
        {
            var usable = inventory.CurrentObject.GetComponent<IUsable>();
            usable?.Use(this);
            return;
        }

        if (currentTarget != null && currentTarget.CanInteract(this))
        {
            currentTarget.Interact(this);
            return;
        }

        if (inventory.HasItem)
        {
            var usable = inventory.CurrentObject.GetComponent<IUsable>();
            usable?.Use(this);
        }
    }

    public void OnDropButton()
    {
        if (IsPhotoMode) return;

        if (!inventory.TryDrop(out GameObject obj)) return;

        Vector3 dropPos = transform.position + transform.forward * 1.5f;

        var holdable = obj.GetComponent<IHoldable>();
        holdable?.OnDrop(dropPos);

        UIManager.Instance.HideDropButton();
    }
}