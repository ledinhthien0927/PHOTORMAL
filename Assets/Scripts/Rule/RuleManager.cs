using System.Collections.Generic;
using UnityEngine;

public enum RuleType
{
    NoFlickerShoot,       // Đêm 1
    TwinsTurnOffLight,    // Đêm 1
    BackDoorLocked,       // Đêm 1
    HideWhenFootstep,     // Đêm 1
    StudioTimeLimit,      // Đêm 2 (15s)
    ClownDoorOpen         // Đêm 3 (5s)
}

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance;

    private HashSet<RuleType> activeRules = new();
    
    // Lưu tạm thời gian Clown xuất hiện để đếm ngược 5s Instant GameOver
    private float clownPatienceTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        // ==== SUBSCRIBING TO PLAYER INTERACTION EVENTS ====
        GameEventAPI.OnPlayerShootPhoto += HandlePlayerShoot;
        GameEventAPI.OnBackDoorStateChanged += HandleBackDoorState;
        GameEventAPI.OnLivingRoomLightToggled += HandleLivingRoomLight;
        GameEventAPI.OnPlayerOpenedDoorForClown += HandleClownDoorOpened;
        GameEventAPI.OnPlayerStudioStateChanged += HandleStudioState;
        GameEventAPI.OnPlayerToiletStateChanged += HandleToiletState;
    }

    private void OnDisable()
    {
        // ==== UNSUBSCRIBING TO PREVENT MEMORY LEAKS ====
        GameEventAPI.OnPlayerShootPhoto -= HandlePlayerShoot;
        GameEventAPI.OnBackDoorStateChanged -= HandleBackDoorState;
        GameEventAPI.OnLivingRoomLightToggled -= HandleLivingRoomLight;
        GameEventAPI.OnPlayerOpenedDoorForClown -= HandleClownDoorOpened;
        GameEventAPI.OnPlayerStudioStateChanged -= HandleStudioState;
        GameEventAPI.OnPlayerToiletStateChanged -= HandleToiletState;
    }

    public void SetupRules(int night)
    {
        activeRules.Clear();

        // Đêm 1
        activeRules.Add(RuleType.NoFlickerShoot);
        activeRules.Add(RuleType.TwinsTurnOffLight);
        activeRules.Add(RuleType.BackDoorLocked);
        activeRules.Add(RuleType.HideWhenFootstep);

        // Đêm 2
        if (night >= 2)
            activeRules.Add(RuleType.StudioTimeLimit);

        // Đêm 3
        if (night >= 3)
            activeRules.Add(RuleType.ClownDoorOpen);
    }

    // ===============================
    // CHECK FUNCTIONS (EVENT-DRIVEN)
    // ===============================

    /// <summary> Quy tắc: Không chụp khi đèn nhấp nháy </summary>
    private void HandlePlayerShoot()
    {
        if (!activeRules.Contains(RuleType.NoFlickerShoot)) return;

        if (RuleContext.Instance.IsFlickering)
        {
            BreakRule(RuleType.NoFlickerShoot);
        }
    }

    /// <summary> Quy tắc: Cửa sau phải luôn khóa </summary>
    private void HandleBackDoorState(bool isLocked)
    {
        RuleContext.Instance.IsBackDoorLocked = isLocked;

        if (!activeRules.Contains(RuleType.BackDoorLocked)) return;

        // Nếu vừa mở khóa (thùng hàng) mà k khóa lại ngay (hoặc mở ngoài ý muốn)
        // Check có thể đợi vài giây, nhưng document kêu "luôn được khóa".
        if (!isLocked && !RuleContext.Instance.IsClownAppeared)
        {
            BreakRule(RuleType.BackDoorLocked);
        }
    }

    /// <summary> Quy tắc: Tắt đèn nếu sinh đôi tới </summary>
    private void HandleLivingRoomLight(bool isOn)
    {
        RuleContext.Instance.IsLivingRoomLightOn = isOn;

        if (!activeRules.Contains(RuleType.TwinsTurnOffLight)) return;

        if (RuleContext.Instance.HasTwinsAppeared && !isOn)
        {
             // Đúng rule: đã tắt đèn -> Cặp song sinh biến mất
             RuleContext.Instance.HasTwinsAppeared = false;
             // Gọi event xóa NPC song sinh trên Scene (nếu có spawner)
             EventManager.Instance.TriggerEvent("DespawnTwins");
             Debug.Log("Đã tắt đèn, cặp song sinh biến mất.");
        }
    }

    /// <summary> Quy tắc: Mở cửa cho hề (Chỉ có ở đêm 3) </summary>
    private void HandleClownDoorOpened()
    {
        if (!activeRules.Contains(RuleType.ClownDoorOpen)) return;

        if (RuleContext.Instance.IsClownAppeared)
        {
            // Player successfully opened the door for clown, they survive.
            RuleContext.Instance.IsClownAppeared = false; 
            clownPatienceTimer = 0f;
            Debug.Log("Đã mở cửa cho hề kịp thời.");
            // Gọi Event cho hề biến đi
            EventManager.Instance.TriggerEvent("ClownDisappears");
        }
    }

    private void HandleStudioState(bool hasEntered)
    {
        if (hasEntered)
            RuleContext.Instance.StudioEnterTime = Time.time;
        else
            RuleContext.Instance.StudioEnterTime = 0f;
    }

    private void HandleToiletState(bool isInside)
    {
        RuleContext.Instance.IsPlayerInToilet = isInside;
    }

    // ===============================
    // CONTINUOUS CHECKS (UPDATE)
    // ===============================

    private void Update()
    {
        CheckFootstep(); 
        CheckStudioTimeLimit();
        CheckTwinsLight();
        CheckClownDoor();
    }

    /// <summary> Quy tắc: Vào WC khi có tiếng chân </summary>
    private void CheckFootstep()
    {
        if (!activeRules.Contains(RuleType.HideWhenFootstep)) return;

        // Lỗi nếu tiếng chân đang kêu MÀ người chơi KHÔNG ở trong wc
        // Có thể cần thêm vài giây delay grace-period cho player chạy vào.
        if (RuleContext.Instance.IsFootstepActive &&
            !RuleContext.Instance.IsPlayerInToilet)
        {
            BreakRule(RuleType.HideWhenFootstep);
            // Tắt tiếng tránh gọi hàm BreakRule liên tục
            RuleContext.Instance.IsFootstepActive = false; 
        }
    }

    private void CheckTwinsLight()
    {
        if (!activeRules.Contains(RuleType.TwinsTurnOffLight)) return;

        // Nếu sinh đôi đang ở đó mà đèn VẪN bật -> Không sao nếu event vừa nổ,
        // Nhưng nếu để quá lâu (ví dụ 5 giây) mà chưa tắt đèn -> Lỗi
        // (Sẽ triển khai timer trong script TwinEvent hoặc ở đây).
    }

    /// <summary> Quy tắc: Khách vào studio xong phải hoàn thành trong 15s (Đêm 2) </summary>
    void CheckStudioTimeLimit()
    {
        if (!activeRules.Contains(RuleType.StudioTimeLimit)) return;

        if (RuleContext.Instance.StudioEnterTime <= 0) return;

        if (Time.time - RuleContext.Instance.StudioEnterTime > 15f)
        {
            BreakRule(RuleType.StudioTimeLimit);
            RuleContext.Instance.StudioEnterTime = 0f; // Tránh nổ lỗi nhiều lần
        }
    }

    /// <summary> Quy tắc: Tên hề ngoài cửa chờ 5s. (Đêm 3) </summary>
    void CheckClownDoor()
    {
        if (!activeRules.Contains(RuleType.ClownDoorOpen)) return;

        if (!RuleContext.Instance.IsClownAppeared) return;

        // Tên hề đang chờ ở cửa
        clownPatienceTimer += Time.deltaTime;

        if (clownPatienceTimer >= 5f)
        {
            // Quá 5s k mở -> Jumpscare Instant GameOver (theo design document)
            EventManager.Instance.TriggerEvent("InstantGameOver");
            RuleContext.Instance.IsClownAppeared = false; // Ngừng lặp
        }
    }

    void BreakRule(RuleType rule)
    {
        Debug.Log("Rule Broken: " + rule);
        GameProgress.Instance.AddError();
        EventManager.Instance.TriggerEvent("RuleBrokenEffect"); 
    }
}