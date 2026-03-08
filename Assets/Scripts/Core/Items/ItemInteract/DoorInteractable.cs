using UnityEngine;
using System;

[DisallowMultipleComponent]
public sealed class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Setup")]
    [SerializeField] private Transform doorPivot;        // The hinge/pivot transform
    [SerializeField] private float openAngle = 90f;      // Degrees to rotate when open
    [SerializeField] private float speed = 180f;         // Degrees per second

    [Header("Door Blocking (NavMesh)")]
    [SerializeField] private Collider doorBlocker;       // Collider that blocks the doorway when closed

    [Header("UI Text")]
    [SerializeField] private string openText = "Open door";
    [SerializeField] private string closeText = "Close door";

    [Header("Lock (Optional)")]
    [SerializeField] private bool locked;
    [SerializeField] private string lockedText = "Locked";

    private bool isOpen;
    private float currentAngle;
    private float targetAngle;

    public event Action<bool> OnDoorStateChanged;

    public string Prompt
    {
        get
        {
            if (locked) return lockedText;
            return isOpen ? closeText : openText;
        }
    }

    private void Awake()
    {
        // If no pivot assigned, use this transform
        if (doorPivot == null) doorPivot = transform;

        currentAngle = 0f;
        targetAngle = 0f;

        // When starting closed, ensure blocker is enabled
        SetBlockerState(isOpen);

        ApplyRotation();
    }

    public bool CanInteract(IInteractor interactor)
    {
        // Basic rules: must have pivot, must not be locked
        if (doorPivot == null) return false;
        if (locked) return false;
        return true;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor)) return;

        // Toggle open/close state
        isOpen = !isOpen;
        targetAngle = isOpen ? openAngle : 0f;

        // Enable/disable the blocker so NavMeshAgents can pass through
        SetBlockerState(isOpen);

        OnDoorStateChanged?.Invoke(isOpen);
    }

    private void Update()
    {
        // Smoothly rotate door toward target angle
        if (Mathf.Approximately(currentAngle, targetAngle)) return;

        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        // Rotate around local Y axis by currentAngle
        doorPivot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    private void SetBlockerState(bool open)
    {
        // If door is open, blocker should be disabled; if closed, enabled
        if (doorBlocker != null)
            doorBlocker.enabled = !open;
    }

    // Optional helper if you want to lock/unlock from other scripts
    public void SetLocked(bool value)
    {
        locked = value;

        // If locked, keep door effectively closed for AI by enabling blocker
        if (locked)
        {
            isOpen = false;
            targetAngle = 0f;
            SetBlockerState(false);
        }
    }
}