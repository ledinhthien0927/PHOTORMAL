using UnityEngine;

public sealed class PhotoAimValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;                 // Player POV camera
    [SerializeField] private Transform poseTarget;                // Customer (prefer head/face transform)
    [SerializeField] private StudioPoseZone poseZone;             // Zone where customer must stand
    [SerializeField] private StudioLightFlicker studioLightFlicker; // Optional: flicker rule provider
    [SerializeField] private CrosshairUI crosshairUI;             // Optional: UI that turns green/red

    [Header("Facing Rule")]
    [Range(-1f, 1f)]
    [SerializeField] private float faceDotThreshold = 0.7f;       // Higher = stricter (0.7 = must face camera clearly)

    [Header("Aim Rule")]
    [SerializeField] private float aimDistance = 6f;              // Max raycast distance from camera
    [SerializeField] private LayerMask aimMask = ~0;              // Recommended: set to Customer layer only

    public bool CanShoot { get; private set; }

    private bool _enabled;
    private IInteractor _currentInteractor;

    /// <summary>
    /// Enables/disables validation and crosshair visibility.
    /// Called by StudioManager when entering/leaving PhotoMode.
    /// </summary>
    public void SetEnabled(bool enabled, IInteractor interactor)
    {
        _enabled = enabled;
        _currentInteractor = interactor;

        if (crosshairUI != null)
            crosshairUI.SetVisible(enabled);

        SetCanShoot(false);
    }

    private void Update()
    {
        if (!_enabled)
        {
            SetCanShoot(false);
            return;
        }

        bool okZone = poseZone != null && poseZone.IsTargetInside;
        bool okFlicker = studioLightFlicker == null || !studioLightFlicker.IsFlickering;
        bool okFacing = CheckFacingCamera();
        bool okAim = CheckAimingAtTarget();

        bool can = okZone && okFlicker && okFacing && okAim;
        SetCanShoot(can);
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
    /// This ensures the player is actually pointing the camera at the customer.
    /// </summary>
    private bool CheckAimingAtTarget()
    {
        if (playerCamera == null || poseTarget == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            Transform hitT = hit.transform;
            return hitT == poseTarget || hitT.IsChildOf(poseTarget);
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