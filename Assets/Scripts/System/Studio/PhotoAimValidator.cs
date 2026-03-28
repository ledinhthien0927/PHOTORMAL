using UnityEngine;

public sealed class PhotoAimValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider aimZoneCollider;
    [SerializeField] private StudioLightFlicker studioLightFlicker;
    [SerializeField] private CrosshairUI crosshairUI;

    [Header("Rules")]
    [SerializeField] private bool requireCustomerReady = true;
    [SerializeField] private bool requireAimAtZone = true;
    [SerializeField] private bool requireNoFlicker = false;

    [Header("Aim Settings")]
    [SerializeField] private float aimDistance = 8f;

    [Header("Viewport Check")]
    [SerializeField] private float centerBoxWidth = 0.2f;
    [SerializeField] private float centerBoxHeight = 0.2f;

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

        SetCanShoot(false);
    }

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
            return;
        }

        bool okFlicker = !requireNoFlicker || (studioLightFlicker == null || !studioLightFlicker.IsFlickering);
        bool okAim = !requireAimAtZone || CheckAimAtZoneViewport();

        bool can = okFlicker && okAim;
        SetCanShoot(can);

        if (logDebug)
            Debug.Log($"[AimValidator] Ready={okReady} Flicker={okFlicker} AimZone={okAim} Can={can}");
    }

    private bool CheckAimAtZoneViewport()
    {
        if (aimZoneCollider == null || playerCamera == null)
            return false;

        Bounds bounds = aimZoneCollider.bounds;
        Vector3 center = bounds.center;

        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(center);

        if (viewportPoint.z <= 0f)
            return false;

        float distanceToCamera = Vector3.Distance(playerCamera.transform.position, center);
        if (distanceToCamera > aimDistance)
            return false;

        float minX = 0.5f - centerBoxWidth * 0.5f;
        float maxX = 0.5f + centerBoxWidth * 0.5f;
        float minY = 0.5f - centerBoxHeight * 0.5f;
        float maxY = 0.5f + centerBoxHeight * 0.5f;

        bool insideBox =
            viewportPoint.x >= minX && viewportPoint.x <= maxX &&
            viewportPoint.y >= minY && viewportPoint.y <= maxY;

        return insideBox;
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
        centerBoxWidth = Mathf.Clamp(centerBoxWidth, 0.01f, 1f);
        centerBoxHeight = Mathf.Clamp(centerBoxHeight, 0.01f, 1f);
    }
#endif
}