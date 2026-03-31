using UnityEngine;
using System.Collections;

public class FootstepEvent : MonoBehaviour, IGameEvent
{
    private Coroutine footstepCoroutine;
    
    public void Execute()
    {
        if (footstepCoroutine != null) StopCoroutine(footstepCoroutine);
        footstepCoroutine = StartCoroutine(FootstepRoutine());
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

        EndFootstep();
    }

    private void EndFootstep()
    {
        RuleContext.Instance.IsFootstepActive = false;
        
        // Gọi API để tắt tiếng bước chân
        GameEventAPI.OnFootstepToggled?.Invoke(false);
        Debug.Log("Sự kiện: Tiếng bước chân kết thúc.");
        footstepCoroutine = null;
    }

    private void ForceStop()
    {
        if (RuleContext.Instance.IsFootstepActive)
        {
            if (footstepCoroutine != null) StopCoroutine(footstepCoroutine);
            EndFootstep();
            Debug.Log("[FootstepEvent] Forced Stop by Call Support.");
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Footstep", this);
    }

    private void OnEnable()
    {
        GameEventAPI.OnForceStopFootstep += ForceStop;
    }

    private void OnDisable()
    {
        GameEventAPI.OnForceStopFootstep -= ForceStop;
    }
}
