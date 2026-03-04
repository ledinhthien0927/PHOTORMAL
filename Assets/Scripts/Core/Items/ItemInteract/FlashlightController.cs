using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [SerializeField] private Light flashlight;
    private bool isOn;

    private void Start()
    {
        // Sync internal state with actual light state
        if (flashlight != null)
            isOn = flashlight.enabled;
    }

    public void ToggleFlashlight()
    {
        isOn = !isOn;
        flashlight.enabled = isOn;
    }
}