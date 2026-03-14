using System;

/// Giao diện API giao tiếp giữa phần Player (Developer B) và phần System/Rule (Bạn).
/// Developer B CHỈ CẦN GỌI (Invoke) các sự kiện ở đây khi Player thực hiện hành động.

public static class GameEventAPI 
{
    // ==========================================
    // CÁC HÀNH ĐỘNG CỦA PLAYER
    // ==========================================

    /// Gọi khi Player ấn nút chụp ảnh
    public static Action OnPlayerShootPhoto; 

    /// Gọi khi Player tương tác công tắc đèn phòng khách. True = Đang bật, False = Đang tắt.
    public static Action<bool> OnLivingRoomLightToggled; 

    /// Gọi khi Player mở/khóa cửa sau. True = Đang khóa, False = Đang mở
    public static Action<bool> OnBackDoorStateChanged; 

    /// Gọi khi Player mở cửa cho tên hề ở Đêm 3.
    public static Action OnPlayerOpenedDoorForClown;

    // ==========================================
    // VỊ TRÍ CỦA PLAYER
    // ==========================================

    /// Báo cáo Player vừa bước vào (hoặc ra) khỏi phòng Studio. True = Vào, False = Ra
    public static Action<bool> OnPlayerStudioStateChanged;

    /// Báo cáo Player vừa bước vào (hoặc ra) khỏi Toilet. True = Vào, False = Ra.
    public static Action<bool> OnPlayerToiletStateChanged;

    // ==========================================
    // TƯƠNG TÁC UI / SYSTEM
    // ==========================================
    
    /// Khi Player bấm nút "Call Support" trên màn hình khi bị lỗi
    public static Action OnCallSupportClicked;

    // ==========================================
    // MONEY SYSTEM
    // ==========================================

    /// Yêu cầu cộng tiền
    public static Action<int> OnAddMoney;

    /// Yêu cầu trừ tiền
    public static Action<int> OnSpendMoney;

    // ==========================================
    // CÁC SỰ KIỆN TỪ HỆ THỐNG (SYSTEM EVENTS)
    // ==========================================
    
    // Coder B đăng ký (subscribe) các sự kiện này để bật/tắt hiệu ứng, model, âm thanh

    /// Sự kiện: Đèn studio bắt đầu/ngừng nhấp nháy
    public static Action<bool> OnStudioLightFlicker;

    /// Sự kiện: Cặp song sinh xuất hiện/biến mất
    public static Action<bool> OnTwinsPresenceChanged;

    /// Sự kiện: Bắt đầu/Kết thúc tiếng bước chân
    public static Action<bool> OnFootstepToggled;

    /// Sự kiện: Tên hề xuất hiện ở cửa sau
    public static Action OnClownAppeared;

    /// Sự kiện: Tên hề biến mất (khi kịp mở cửa)
    public static Action OnClownDisappeared;

    /// Sự kiện: Tên hề Jumpscare (khi không kịp mở cửa)
    public static Action OnClownJumpscare;

    /// Sự kiện: Có người giao hàng gõ cửa
    public static Action OnDeliveryKnock;

    /// Sự kiện: Gọi khi Player nhặt cục hàng giao đến
    public static Action OnPlayerPickUpDelivery;

    /// Sự kiện: Khi Player vi phạm bất kỳ luật nào (để Coder B phát hiệu ứng nhiễu màn hình)
    public static Action OnRuleBroken;

    /// Sự kiện: Khi 1 khách hàng đã hoàn thành phiên của họ (Destroy). 
    /// CustomerQueueManager lắng nghe để biết khi nào có thể spawn khách tiếp theo.
    public static Action OnCustomerCompleted;

    /// Yêu cầu đếm ngược: Player bấm mời khách vào Studio
    public static Action OnCustomerInvitedToStudio;

    /// Yêu cầu đếm ngược kết thúc: Khách nhận đúng ảnh
    public static Action OnCustomerReceivedCorrectPhoto;
}
