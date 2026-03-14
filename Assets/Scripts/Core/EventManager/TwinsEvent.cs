using UnityEngine;
using System.Collections;

public class TwinsEvent : MonoBehaviour, IGameEvent
{

    public void Execute()
    {
        // Chỉ kích hoạt nếu đang ở đêm 1 trở lên
        if (GameProgress.Instance.CurrentNight >= 1)
        {
            StartCoroutine(TwinsRoutine());
        }
    }

    IEnumerator TwinsRoutine()
    {
        RuleContext.Instance.HasTwinsAppeared = true;
        Debug.Log("Sự kiện: Cặp song sinh xuất hiện (Tắt đèn ngay!)");

        // Gọi ra event để Coder B spawn model cặp sinh đôi
        GameEventAPI.OnTwinsPresenceChanged?.Invoke(true);

        float elapsed = 0f;

        while (RuleContext.Instance.HasTwinsAppeared)
        {
            // Trạng thái đèn được cập nhật liên tục qua Event API vào RuleContext
            // RuleManager sẽ bắt sự kiện tắt đèn để set HasTwinsAppeared = false
            // Hoặc RuleManager sẽ bắt timeout và set HasTwinsAppeared = false
            yield return null;
        }

        // Đảm bảo ẩn model khi kết thúc event (dù là do tắt đèn hay vi phạm)
        GameEventAPI.OnTwinsPresenceChanged?.Invoke(false);
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Twins", this);
    }
}
