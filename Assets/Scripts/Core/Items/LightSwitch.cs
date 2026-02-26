using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [SerializeField] private Light targetLight;

    private bool isOn = true;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        SetLight(isOn);
    }

    public void Toggle()
    {
        isOn = !isOn;
        SetLight(isOn);
    }

    private void SetLight(bool state)
    {
        if (targetLight != null)
            targetLight.enabled = state;
    }
}
