using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights controlled by this switch")]
    [SerializeField] private Light[] targetLights;

    private bool isOn = false;

    private void Start()
    {
        // Auto-find lights in children if none assigned
        if (targetLights == null || targetLights.Length == 0)
            targetLights = GetComponentsInChildren<Light>(true);

        // Sync initial state from the actual light component instead of defaulting to false
        if (targetLights != null && targetLights.Length > 0 && targetLights[0] != null)
        {
            isOn = targetLights[0].enabled;
        }
        else
        {
            SetLights(isOn);
        }

        // Initialize RuleContext if this is the living room switch
        if (isLivingRoomSwitch && RuleContext.Instance != null)
        {
            RuleContext.Instance.IsLivingRoomLightOn = isOn;
        }
    }

    // =============================
    // IInteractable Implementation
    // =============================

    public string Prompt => isOn ? "Turn off lights" : "Turn on lights";

    public bool CanInteract(IInteractor interactor)
    {
        return targetLights != null && targetLights.Length > 0;
    }

    public void Interact(IInteractor interactor)
    {
        Toggle();
    }

    [Header("Events (Night 1 Rule)")]
    [Tooltip("Tick vào ô này nếu đây là công tắc đèn ngoài phòng khách (để báo sự kiện cho RuleManager)")]
    [SerializeField] private bool isLivingRoomSwitch = false;

    // =============================
    // Existing Logic
    // =============================

    public void Toggle()
    {
        isOn = !isOn;
        SetLights(isOn);

        if (isLivingRoomSwitch)
        {
            GameEventAPI.OnLivingRoomLightToggled?.Invoke(isOn);
        }
    }

    private void SetLights(bool state)
    {
        if (targetLights == null) return;

        for (int i = 0; i < targetLights.Length; i++)
        {
            if (targetLights[i] != null)
                targetLights[i].enabled = state;
        }
    }
}