using UnityEngine;

public class DeliveryEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        RuleContext.Instance.IsDeliveryWaiting = true;
        Debug.Log("Sự kiện: Hàng giao tới gõ cửa nhà!");

        // Phát ra Event để báo hiệu có người gõ cửa giao hàng
        GameEventAPI.OnDeliveryKnock?.Invoke();
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Delivery", this);
    }
}
