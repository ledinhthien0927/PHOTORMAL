using UnityEngine;

/// <summary>
/// Simple photo validation:
/// - Only runs when enabled by StudioManager (PhotoMode)
/// - Customer must be READY first (signaled by CustomerController via StudioManager)
/// - Then player must aim at a Pose Aim Zone collider (center screen ray hits this collider)
/// </summary>
public sealed class PhotoAimValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider aimZoneCollider;              // Drag PoseAimZone(BoxCollider) here
    [SerializeField] private StudioLightFlicker studioLightFlicker; // Optional
    [SerializeField] private CrosshairUI crosshairUI;               // Dot UI

    [Header("Rules")]
    [SerializeField] private bool requireCustomerReady = true;
    [SerializeField] private bool requireAimAtZone = true;
    [SerializeField] private bool requireNoFlicker = false;

    [Header("Aim Settings")]
    [SerializeField] private float aimDistance = 8f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    public bool CanShoot { get; private set; }

    private bool _enabled;
    private bool _customerReady;

    public void SetEnabled(bool enabled, IInteractor interactor)
    {
        _enabled = enabled;

        if (playerCamera == null) playerCamera = Camera.main;

        if (crosshairUI != null)
            crosshairUI.SetVisible(enabled);

        // Default to red whenever (re)enabled
        SetCanShoot(false);
    }

    /// <summary>
    /// CustomerController -> StudioManager -> here.
    /// READY must be true before checking aim-at-zone.
    /// </summary>
    public void SetCustomerReady(bool ready)
    {
        _customerReady = ready;

        if (!ready)
            SetCanShoot(false);
    }

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (crosshairUI != null) crosshairUI.SetVisible(false);
    }

    private void Update()
    {
        if (!_enabled)
        {
            SetCanShoot(false);
            return;
        }

        if (playerCamera == null) playerCamera = Camera.main;

        bool okReady = !requireCustomerReady || _customerReady;
        if (!okReady)
        {
            SetCanShoot(false);
            if (logDebug) Debug.Log("[AimValidator] Waiting for customer READY...");
            return;
        }

        bool okFlicker = !requireNoFlicker || (studioLightFlicker == null || !studioLightFlicker.IsFlickering);
        bool okAim = !requireAimAtZone || CheckAimAtZone();

        bool can = okFlicker && okAim;
        SetCanShoot(can);

        if (logDebug)
            Debug.Log($"[AimValidator] Ready={okReady} Flicker={okFlicker} AimZone={okAim} Can={can}");
    }

    private bool CheckAimAtZone()
    {
        if (aimZoneCollider == null || playerCamera == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Works even if aimZoneCollider is Trigger
        return aimZoneCollider.Raycast(ray, out _, aimDistance);
    }

    private void SetCanShoot(bool can)
    {
        if (CanShoot == can) return;
        CanShoot = can;

        if (crosshairUI != null)
            crosshairUI.SetGreen(can);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (aimDistance < 0.1f) aimDistance = 0.1f;
    }
#endif
}