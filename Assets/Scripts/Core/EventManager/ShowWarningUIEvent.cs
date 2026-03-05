using UnityEngine;
using System.Collections;

public class ShowWarningUIEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        // Chèn hiệu ứng nhiễu màn hình ở đây (gọi tới CameraFilterPack hoặc PostProcessing profile)
        Debug.Log("Sự kiện: Bật nhiễu màn hình và hiện Warning UI.");
        
        if (WarningManager.Instance != null)
        {
            WarningManager.Instance.ShowWarning();
        }
        else
        {
            Debug.LogError("ShowWarningUIEvent: WarningManager is missing in scene!");
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("ShowWarningUI", this);
    }
}
