using UnityEngine;
using System.Collections;

public class TwinsEvent : MonoBehaviour, IGameEvent
{
    [SerializeField] private float durationBeforeError = 10f; // Thời gian cho phép bật đèn trước khi dính lỗi

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

        while (RuleContext.Instance.HasTwinsAppeared && elapsed < durationBeforeError)
        {
            // Trạng thái đèn được cập nhật liên tục qua Event API vào RuleContext
            // RuleManager sẽ bắt sự kiện tắt đèn để set HasTwinsAppeared = false
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Nếu hết thời gian mà cặp song sinh vẫn còn (người chơi không tắt đèn)
        if (RuleContext.Instance.HasTwinsAppeared)
        {
            Debug.Log("Lỗi: Không tắt đèn khi cặp song sinh xuất hiện!");
            RuleContext.Instance.HasTwinsAppeared = false;
            
            // Xóa cặp song sinh thông qua API Coder B
            GameEventAPI.OnTwinsPresenceChanged?.Invoke(false);

            // Phạt lỗi
            GameProgress.Instance.AddError();
            EventManager.Instance.TriggerEvent("RuleBrokenEffect");
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Twins", this);
    }
}
