using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour, IInteractor
{
    [Header("Detection Settings")]
    [SerializeField] private float distance = 3f;              // Maximum interaction distance
    [SerializeField] private float sphereRadius = 0.08f;       // SphereCast radius (more stable than Raycast)
    [SerializeField] private float graceTime = 0.12f;          // Time to keep target after temporary loss
    [SerializeField] private float enterRange = 2.9f;          // Distance required to show prompt
    [SerializeField] private float exitRange = 3.1f;           // Distance required to hide prompt
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Inventory / Hold")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform holdPoint;

    private IInteractable currentTarget;
    private float lastSeenTime;
    private float lastSeenDistance;

    public Transform Transform => transform;
    public Transform HoldPoint => holdPoint;
    public IInventory Inventory => inventory;

    // Cached check to avoid repeated null checks
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
        // JDD: when in PhotoMode, you usually don't want world interaction prompts flickering on screen
        // (You can still allow it if you want, but default JDD flow is shoot-only in the fixed spot.)
        if (!IsPhotoMode)
            UpdateTarget();
        else
            ClearTargetAndUI();
    }

    private void ClearTargetAndUI()
    {
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
            currentTarget = newTarget;

            if (currentTarget != null)
                UIManager.Instance.ShowTextItem(currentTarget.Prompt);
            else
                UIManager.Instance.HideTextItem();
        }
        else if (currentTarget != null && currentTarget.CanInteract(this))
        {
            // Update the prompt text continuously so changes (like toggling a switch) reflect instantly
            UIManager.Instance.ShowTextItem(currentTarget.Prompt);
        }
        else if (currentTarget != null && !currentTarget.CanInteract(this))
        {
            UIManager.Instance.HideTextItem();
        }
    }

    // UI button: Pickup / Use (mobile shared button)
    public void OnPickupButton()
    {
        // Only lock the button to SHOOT when inside PhotoMode
        if (IsPhotoMode && IsHoldingCameraItem)
        {
            var usable = inventory.CurrentObject.GetComponent<IUsable>();
            usable?.Use(this);
            return;
        }

        // Outside PhotoMode: allow normal world interaction even while holding camera
        if (currentTarget != null && currentTarget.CanInteract(this))
        {
            currentTarget.Interact(this);
            return;
        }

        // If no world target, then try using held item (optional)
        if (inventory.HasItem)
        {
            var usable = inventory.CurrentObject.GetComponent<IUsable>();
            usable?.Use(this);
        }
    }

    // UI button: Drop held item
    public void OnDropButton()
    {
        // Optional JDD rule: prevent dropping the camera while in PhotoMode (feels better)
        if (IsPhotoMode) return;

        if (!inventory.TryDrop(out GameObject obj)) return;

        Vector3 dropPos = transform.position + transform.forward * 1.5f;

        var holdable = obj.GetComponent<IHoldable>();
        holdable?.OnDrop(dropPos);

        UIManager.Instance.HideDropButton();
    }
}