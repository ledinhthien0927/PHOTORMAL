using UnityEngine;

public class ClownEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        // Sự kiện thằng hề chỉ xuất hiện ở đêm 3
        if (GameProgress.Instance.CurrentNight >= 3)
        {
            RuleContext.Instance.IsClownAppeared = true;
            Debug.Log("Sự kiện: Tên hề đứng ở cửa sau! (Mở cửa trong 5s)");
            
            // Render model thằng hề hoặc kích hoạt âm thanh ở cửa sau
            // EventManager.Instance.TriggerEvent("SpawnClown");
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Clown", this);
    }
}
