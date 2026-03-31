using UnityEngine;
using System.Collections;

public class FootstepEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        StartCoroutine(FootstepRoutine());
    }

    IEnumerator FootstepRoutine()
    {
        RuleContext.Instance.IsFootstepActive = true;
        Debug.Log("Sự kiện: Tiếng bước chân bắt đầu (Nấp vào WC!)");

        // Gọi API để Coder B phát âm thanh tiếng bước chân
        GameEventAPI.OnFootstepToggled?.Invoke(true);

        // Lấy thời gian duration trực tiếp từ RuleManager
        float duration = RuleManager.Instance != null ? RuleManager.Instance.footstepGracePeriod : 15f;
        yield return new WaitForSeconds(duration);

        RuleContext.Instance.IsFootstepActive = false;
        
        // Gọi API để tắt tiếng bước chân
        GameEventAPI.OnFootstepToggled?.Invoke(false);
        Debug.Log("Sự kiện: Tiếng bước chân kết thúc.");
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Footstep", this);
    }
}
