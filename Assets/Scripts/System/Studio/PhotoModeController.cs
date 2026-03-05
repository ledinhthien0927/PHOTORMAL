using UnityEngine;

public sealed class PhotoModeController : MonoBehaviour
{
    [SerializeField] private Transform targetYawTransform; // usually player root / body
    [SerializeField] private float yawLimitDegrees = 20f;  // small left/right rotation in photo mode

    private bool _enabled;
    private float _baseYaw;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;

        if (_enabled && targetYawTransform != null)
            _baseYaw = targetYawTransform.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (!_enabled || targetYawTransform == null) return;

        // Clamp yaw around the base yaw
        float currentYaw = NormalizeAngle(targetYawTransform.eulerAngles.y);
        float baseYaw = NormalizeAngle(_baseYaw);

        float delta = Mathf.DeltaAngle(baseYaw, currentYaw);
        delta = Mathf.Clamp(delta, -yawLimitDegrees, yawLimitDegrees);

        float clampedYaw = baseYaw + delta;
        targetYawTransform.rotation = Quaternion.Euler(0f, clampedYaw, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}