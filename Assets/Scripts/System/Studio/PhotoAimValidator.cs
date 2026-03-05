using UnityEngine;

/// <summary>
/// Validates whether the player can shoot a photo:
/// - Enabled only in PhotoMode (driven by StudioManager)
/// - Optional rules: pose zone, flicker, facing, aim raycast
/// - Supports runtime binding for spawned customers (SetPoseTarget / auto-find by tag)
/// </summary>
public sealed class PhotoAimValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;                   // Player POV camera
    [SerializeField] private Transform poseTarget;                  // Customer (prefer head/face transform)
    [SerializeField] private StudioPoseZone poseZone;               // Optional: customer-in-zone rule
    [SerializeField] private StudioLightFlicker studioLightFlicker; // Optional: flicker rule provider
    [SerializeField] private CrosshairUI crosshairUI;               // UI dot turns green/red

    [Header("Optional Rules")]
    [SerializeField] private bool requirePoseZone = false;          // OFF by default (your photo spot already enables PhotoMode)
    [SerializeField] private bool requireFacing = true;             // If true: customer must face camera
    [SerializeField] private bool requireAim = true;                // If true: raycast must hit customer
    [SerializeField] private bool requireNoFlicker = true;          // If true: cannot shoot while flickering

    [Header("Facing Rule")]
    [Range(-1f, 1f)]
    [SerializeField] private float faceDotThreshold = 0.7f;         // Higher = stricter

    [Header("Aim Rule")]
    [SerializeField] private float aimDistance = 6f;                // Max raycast distance
    [SerializeField] private LayerMask aimMask = ~0;                // Recommended: Customer layer only
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Runtime Target Binding")]
    [SerializeField] private bool autoFindTargetIfNull = true;      // If true: try to find customer when poseTarget is null
    [SerializeField] private string customerTag = "Customer";       // Tag used for auto-find fallback

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    public bool CanShoot { get; private set; }

    // Expose rule results if you want to display them elsewhere
    public bool OkZone { get; private set; }
    public bool OkFlicker { get; private set; }
    public bool OkFacing { get; private set; }
    public bool OkAim { get; private set; }

    private bool _enabled;
    private IInteractor _currentInteractor;

    /// <summary>
    /// Bind the current customer target at runtime (for spawned prefab customers).
    /// Pass head/face transform if available; otherwise pass the root.
    /// </summary>
    public void SetPoseTarget(Transform newTarget)
    {
        poseTarget = newTarget;
    }

    /// <summary>
    /// Clear target when customer leaves / despawns.
    /// </summary>
    public void ClearPoseTarget()
    {
        poseTarget = null;
        SetCanShoot(false);
    }

    /// <summary>
    /// Enables/disables validation and crosshair visibility.
    /// Called by StudioManager when entering/leaving PhotoMode.
    /// </summary>
    public void SetEnabled(bool enabled, IInteractor interactor)
    {
        _enabled = enabled;
        _currentInteractor = interactor;

        // Fallback if not assigned
        if (playerCamera == null) playerCamera = Camera.main;

        if (crosshairUI != null)
            crosshairUI.SetVisible(enabled);

        // When entering PhotoMode, dot should be red by default
        SetCanShoot(false);
    }

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        // Keep crosshair hidden unless enabled
        if (crosshairUI != null)
            crosshairUI.SetVisible(false);
    }

    private void Update()
    {
        if (!_enabled)
        {
            SetCanShoot(false);
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        // If customer is spawned at runtime, poseTarget might be null.
        // This is a simple fallback when you haven't called SetPoseTarget().
        if (poseTarget == null && autoFindTargetIfNull)
        {
            GameObject go = GameObject.FindGameObjectWithTag(customerTag);
            if (go != null) poseTarget = go.transform;
        }

        // If still no target, you can't aim/shoot.
        if (poseTarget == null)
        {
            OkZone = !requirePoseZone; // zone irrelevant if not required
            OkFlicker = !requireNoFlicker || (studioLightFlicker == null || !studioLightFlicker.IsFlickering);
            OkFacing = !requireFacing; // can't validate without target
            OkAim = false;

            SetCanShoot(false);

            if (logDebug)
                Debug.Log("[AimValidator] poseTarget is NULL -> CanShoot=false (bind target via SetPoseTarget)");
            return;
        }

        // IMPORTANT:
        // Your "stand in photo spot" rule is already guaranteed by StudioSpotTrigger + StudioManager.SetPhotoMode().
        // So poseZone should NOT block shooting unless you explicitly want it.
        OkZone = !requirePoseZone || (poseZone != null && poseZone.IsTargetInside);

        OkFlicker = !requireNoFlicker || (studioLightFlicker == null || !studioLightFlicker.IsFlickering);

        OkFacing = !requireFacing || CheckFacingCamera();
        OkAim = !requireAim || CheckAimingAtTarget();

        bool can = OkZone && OkFlicker && OkFacing && OkAim;
        SetCanShoot(can);

        if (logDebug)
            Debug.Log($"[AimValidator] Zone={OkZone} Flicker={OkFlicker} Facing={OkFacing} Aim={OkAim} Can={can}");
    }

    /// <summary>
    /// Checks if the customer is facing toward the camera.
    /// Uses dot product between target forward and direction to camera.
    /// </summary>
    private bool CheckFacingCamera()
    {
        if (poseTarget == null || playerCamera == null)
            return false;

        Vector3 toCamera = (playerCamera.transform.position - poseTarget.position).normalized;
        float dot = Vector3.Dot(poseTarget.forward, toCamera);

        return dot >= faceDotThreshold;
    }

    /// <summary>
    /// Checks if the center of the screen is aiming at the customer (raycast hit).
    /// Robust: accepts any collider under the same root as poseTarget.
    /// </summary>
    private bool CheckAimingAtTarget()
    {
        if (playerCamera == null || poseTarget == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, triggerInteraction))
        {
            Transform hitT = hit.transform;
            if (hitT == null) return false;

            // Robust check: hit can be on body/root/any child collider, not only poseTarget itself
            return hitT.root == poseTarget.root;
        }

        return false;
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