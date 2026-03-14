using UnityEngine;

public class ClownEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        // Sự kiện thằng hề chỉ xuất hiện ở đêm 3
        if (GameProgress.Instance.CurrentNight >= 3)
        {
            RuleContext.Instance.IsClownAppeared = true;
            
            // Reset timer in RuleManager when clown appears
            if (RuleManager.Instance != null)
                RuleManager.Instance.ResetClownTimer();

            Debug.Log("Sự kiện: Tên hề đứng ở cửa sau! (Mở cửa trong x giây)");
            
            // Gọi ra API hệ thống để báo cho Coder B Spawn model Thằng hề ngoài cửa, phát âm thanh
            GameEventAPI.OnClownAppeared?.Invoke();
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Clown", this);
    }
}
