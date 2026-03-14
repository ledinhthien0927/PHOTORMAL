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
    public IEnumerable<RuleType> ActiveRules => activeRules;

    [Header("Rule Settings")]
    [SerializeField] private float customerServiceTimeLimit = 15f;
    [SerializeField] private float ruleViolationGracePeriod = 5f;

    [Header("Debug Status (Read Only)")]
    [SerializeField] private float serviceCountdown;
    [SerializeField] private float backDoorCountdown;
    [SerializeField] private float footstepCountdown;
    [SerializeField] private float twinsCountdown;
    [SerializeField] private float clownCountdown;
    
    // Lưu tạm thời gian Clown xuất hiện để đếm ngược 5s Instant GameOver
    private float clownPatienceTimer = 0f;
    
    // Lưu tạm thời gian mở cửa giao hàng để đếm ngược bắt lỗi
    private float backDoorOpenTimer = 0f;

    // Lưu tạm thời gian vi phạm tiếng bước chân (người chơi có 5s để vào WC)
    private float footstepViolationTimer = 0f;

    // Lưu tạm thời gian vi phạm sinh đôi (người chơi có 5s để tắt đèn)
    private float twinsViolationTimer = 0f;

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
        GameEventAPI.OnPlayerPickUpDelivery += HandlePlayerPickUpDelivery;
        GameEventAPI.OnCustomerInvitedToStudio += HandleCustomerInvited;
        GameEventAPI.OnCustomerReceivedCorrectPhoto += HandleCustomerReceivedPhoto;
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
        GameEventAPI.OnPlayerPickUpDelivery -= HandlePlayerPickUpDelivery;
        GameEventAPI.OnCustomerInvitedToStudio -= HandleCustomerInvited;
        GameEventAPI.OnCustomerReceivedCorrectPhoto -= HandleCustomerReceivedPhoto;
    }

    public void SetupRules(int night)
    {
        activeRules.Clear();
        clownPatienceTimer = 0f;
        backDoorOpenTimer = 0f;
        footstepViolationTimer = 0f;
        twinsViolationTimer = 0f;

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

        if (!isLocked)
        {
            backDoorOpenTimer = 0f; // Bắt đầu đếm thời gian mở cửa

            // Lưu ý: Không còn gọi BreakRule ngay lập tức ở đây.
            // CheckBackDoorTimeout sẽ xử lý việc đếm đủ 5 giây mới phạt.
        }
    }

    private void HandlePlayerPickUpDelivery()
    {
        if (RuleContext.Instance.IsDeliveryWaiting)
        {
            RuleContext.Instance.IsDeliveryWaiting = false;
            Debug.Log("Player đã nhận hàng thành công. Hãy nhớ khóa cửa!");
            // Coder B có thể thêm logic lấy hàng thành công ở đây
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
             
             // Kích hoạt API hệ thống cho Coder B xử lý hiệu ứng biến mất
             GameEventAPI.OnTwinsPresenceChanged?.Invoke(false);
             
             // EventManager.Instance.TriggerEvent("DespawnTwins");
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
            
            // Gọi Event cho hề biến đi để Coder B gỡ model
            GameEventAPI.OnClownDisappeared?.Invoke();
            // EventManager.Instance.TriggerEvent("ClownDisappears");
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
        Debug.Log("[RuleManager] Received Toilet State: " + isInside);
        RuleContext.Instance.IsPlayerInToilet = isInside;
    }

    private void HandleCustomerInvited()
    {
        RuleContext.Instance.CustomerServiceStartTime = Time.time;
        Debug.Log("[RuleManager] Customer Invited. Service timer started.");
    }

    private void HandleCustomerReceivedPhoto()
    {
        RuleContext.Instance.CustomerServiceStartTime = 0f;
        Debug.Log("[RuleManager] Customer received photo. Service timer stopped.");
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
        CheckBackDoorTimeout();
    }

    /// <summary> Quy tắc: Cửa sau luôn khóa. Nếu có hàng đợi, chỉ được mở vài giây. </summary>
    private void CheckBackDoorTimeout()
    {
        if (!activeRules.Contains(RuleType.BackDoorLocked)) return;

        // Nếu người chơi đang mở cửa mà không phải do Hề đang đứng (vì Hề có case riêng)
        if (!RuleContext.Instance.IsBackDoorLocked && !RuleContext.Instance.IsClownAppeared)
        {
            backDoorOpenTimer += Time.deltaTime;
            backDoorCountdown = ruleViolationGracePeriod - backDoorOpenTimer;
            
            // Nếu người chơi đã mở quá 5 giây mà chưa đóng (kể cả trong sự kiện lấy hàng)
            if (backDoorOpenTimer >= ruleViolationGracePeriod)
            {
                BreakRule(RuleType.BackDoorLocked);
                backDoorOpenTimer = 0f; // Tránh nổ lỗi liên tục
                backDoorCountdown = 0f;
            }
        }
        else
        {
            backDoorOpenTimer = 0f;
            backDoorCountdown = 0f;
        }
    }

    /// <summary> Quy tắc: Vào WC khi có tiếng chân </summary>
    private void CheckFootstep()
    {
        if (!activeRules.Contains(RuleType.HideWhenFootstep)) return;

        // Nếu tiếng chân đang kêu MÀ người chơi KHÔNG ở trong wc
        if (RuleContext.Instance.IsFootstepActive && !RuleContext.Instance.IsPlayerInToilet)
        {
            footstepViolationTimer += Time.deltaTime;
            footstepCountdown = ruleViolationGracePeriod - footstepViolationTimer;

            // Nếu đứng ngoài quá 5 giây thì mới phạt
            if (footstepViolationTimer >= ruleViolationGracePeriod)
            {
                BreakRule(RuleType.HideWhenFootstep);
                RuleContext.Instance.IsFootstepActive = false; // Tắt trạng thái để không nổ lỗi liên tục
                footstepViolationTimer = 0f;
                footstepCountdown = 0f;
            }
        }
        else
        {
            // Nếu người chơi đã vào WC hoặc hết tiếng chân -> Reset timer
            footstepViolationTimer = 0f;
            footstepCountdown = 0f;
        }
    }

    private void CheckTwinsLight()
    {
        if (!activeRules.Contains(RuleType.TwinsTurnOffLight)) return;

        // Nếu sinh đôi đang ở đó mà đèn VẪN bật
        if (RuleContext.Instance.HasTwinsAppeared && RuleContext.Instance.IsLivingRoomLightOn)
        {
            twinsViolationTimer += Time.deltaTime;
            twinsCountdown = ruleViolationGracePeriod - twinsViolationTimer;

            if (twinsViolationTimer >= ruleViolationGracePeriod)
            {
                BreakRule(RuleType.TwinsTurnOffLight);
                
                // Sau khi phạt, ép cặp sinh đôi biến mất để không phạt tiếp
                RuleContext.Instance.HasTwinsAppeared = false;
                GameEventAPI.OnTwinsPresenceChanged?.Invoke(false);
                
                twinsViolationTimer = 0f;
                twinsCountdown = 0f;
            }
        }
        else
        {
            // Nếu đã tắt đèn hoặc sinh đôi tự biến mất (do hết event) -> Reset timer
            twinsViolationTimer = 0f;
            twinsCountdown = 0f;
        }
    }

    /// <summary> Quy tắc: Khách vào studio xong phải hoàn thành trong 15s (Đêm 2) </summary>
    void CheckStudioTimeLimit()
    {
        if (!activeRules.Contains(RuleType.StudioTimeLimit)) 
        {
            serviceCountdown = 0f;
            return;
        }

        if (RuleContext.Instance.CustomerServiceStartTime <= 0) 
        {
            serviceCountdown = 0f;
            return;
        }

        float elapsed = Time.time - RuleContext.Instance.CustomerServiceStartTime;
        serviceCountdown = customerServiceTimeLimit - elapsed;

        if (elapsed > customerServiceTimeLimit)
        {
            BreakRule(RuleType.StudioTimeLimit);
            RuleContext.Instance.CustomerServiceStartTime = 0f; // Tránh nổ lỗi nhiều lần
            serviceCountdown = 0f;
        }
    }

    /// <summary> Quy tắc: Tên hề ngoài cửa chờ 5s. (Đêm 3) </summary>
    void CheckClownDoor()
    {
        if (!activeRules.Contains(RuleType.ClownDoorOpen)) return;

        if (!RuleContext.Instance.IsClownAppeared) 
        {
            clownCountdown = 0f;
            return;
        }

        // Tên hề đang chờ ở cửa
        clownPatienceTimer += Time.deltaTime;
        clownCountdown = ruleViolationGracePeriod - clownPatienceTimer;

        if (clownPatienceTimer >= ruleViolationGracePeriod)
        {
            // Quá 5s k mở -> Jumpscare Instant GameOver (theo design document: "chớp tắt đèn, jumpscare")
            GameEventAPI.OnClownJumpscare?.Invoke();
            RuleContext.Instance.IsClownAppeared = false; // Ngừng lặp
            clownCountdown = 0f;
        }
    }

    void BreakRule(RuleType rule)
    {
        Debug.Log("Rule Broken: " + rule);
        GameProgress.Instance.AddError();
        
        // Gọi API hệ thống để Coder B phát hiệu ứng nhiễu màn hình
        GameEventAPI.OnRuleBroken?.Invoke();
        
        // EventManager.Instance.TriggerEvent("RuleBrokenEffect"); 
    }
}