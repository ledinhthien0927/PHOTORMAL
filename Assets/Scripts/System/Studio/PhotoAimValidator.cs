using UnityEngine;

public sealed class PhotoAimValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform photoCameraTransform; // usually player camera transform
    [SerializeField] private Transform poseTarget;           // the customer (or head target)
    [SerializeField] private StudioPoseZone poseZone;        // zone customer must be inside
    [SerializeField] private StudioLightFlicker studioLightFlicker; // provides IsFlickering
    [SerializeField] private CrosshairUI crosshairUI;        // optional UI color control

    [Header("Facing Rule")]
    [SerializeField] private float faceDotThreshold = 0.7f;  // higher = must face camera more precisely

    public bool CanShoot { get; private set; }

    private bool _enabled;
    private IInteractor _currentInteractor;

    public void SetEnabled(bool enabled, IInteractor interactor)
    {
        _enabled = enabled;
        _currentInteractor = interactor;

        if (crosshairUI != null)
            crosshairUI.SetVisible(enabled);

        CanShoot = false;
    }

    private void Update()
    {
        if (!_enabled)
        {
            SetCanShoot(false);
            return;
        }

        bool okZone = (poseZone != null && poseZone.IsTargetInside);
        bool okFlicker = (studioLightFlicker == null) || !studioLightFlicker.IsFlickering;
        bool okFacing = CheckFacingCamera();

        bool can = okZone && okFlicker && okFacing;

        SetCanShoot(can);
    }

    private bool CheckFacingCamera()
    {
        if (poseTarget == null || photoCameraTransform == null) return false;

        // Target should face toward camera: compare target forward with direction to camera
        Vector3 toCam = (photoCameraTransform.position - poseTarget.position).normalized;
        float dot = Vector3.Dot(poseTarget.forward, toCam);

        return dot >= faceDotThreshold;
    }

    private void SetCanShoot(bool can)
    {
        if (CanShoot == can) return;

        CanShoot = can;

        if (crosshairUI != null)
            crosshairUI.SetGreen(can);
    }
}