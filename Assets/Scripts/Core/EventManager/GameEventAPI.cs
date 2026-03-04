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
}
