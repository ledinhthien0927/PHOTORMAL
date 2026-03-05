using UnityEngine;
using System.Collections;

public class FlickerLightEvent : MonoBehaviour, IGameEvent
{
    [SerializeField] private float flickerDuration = 5f;

    public void Execute()
    {
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        RuleContext.Instance.IsFlickering = true;
        Debug.Log("Sự kiện: Đèn Studio bắt đầu nhấp nháy (Không được chụp ảnh!).");

        // Gọi API để Coder B xử lý hiệu ứng nhấp nháy đèn
        GameEventAPI.OnStudioLightFlicker?.Invoke(true);

        yield return new WaitForSeconds(flickerDuration);

        RuleContext.Instance.IsFlickering = false;
        
        // Gọi API dừng hiệu ứng nhấp nháy đèn
        GameEventAPI.OnStudioLightFlicker?.Invoke(false);
        Debug.Log("Sự kiện: Đèn Studio hết nhấp nháy.");
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("FlickerLight", this);
    }
}
