using UnityEngine;
using System.Collections;

public class FootstepEvent : MonoBehaviour, IGameEvent
{
    [SerializeField] private float duration = 8f;

    public void Execute()
    {
        StartCoroutine(FootstepRoutine());
    }

    IEnumerator FootstepRoutine()
    {
        RuleContext.Instance.IsFootstepActive = true;
        Debug.Log("Sự kiện: Tiếng bước chân bắt đầu (Nấp vào WC!)");

        // Có thể gọi AudioManager.PlayFootstep() tại đây

        yield return new WaitForSeconds(duration);

        RuleContext.Instance.IsFootstepActive = false;
        Debug.Log("Sự kiện: Tiếng bước chân kết thúc.");
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Footstep", this);
    }
}
