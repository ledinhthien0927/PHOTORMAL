using UnityEngine;

public class RuleContext : MonoBehaviour
{
    public static RuleContext Instance;

    // Environment States
    public bool IsFlickering;
    public bool IsFootstepActive;
    public bool IsClownAppeared;
    public bool HasTwinsAppeared; // Báo hiệu đã thấy con nít song sinh
    public bool IsDeliveryWaiting; // Có hàng đang đợi ở cửa sau không
    
    // Player & Object States
    public bool IsLivingRoomLightOn = true; // Phòng khách có bật đèn không
    public bool IsBackDoorLocked = true;
    public bool IsPlayerInToilet;

    // Timers
    public float StudioEnterTime;
    public float ClownAppearTime;
    public float CustomerServiceStartTime; // Thời gian bắt đầu phục vụ (từ khi mời vào)

    private void Awake()
    {
        Instance = this;
    }
}