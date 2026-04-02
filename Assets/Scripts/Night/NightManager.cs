using UnityEngine;

public class NightManager : MonoBehaviour
{
    public static NightManager Instance;

    [SerializeField] private int[] targets = { 600, 900, 1200 };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (SaveSystem.pendingLoadData != null)
        {
            PerformSaveRestoration(SaveSystem.pendingLoadData);
            SaveSystem.pendingLoadData = null; // Quan trọng: Clear sau khi đã dùng xong
        }
        else
        {
            StartNight(GameProgress.Instance.CurrentNight);
        }
    }

    private void PerformSaveRestoration(SaveData data)
    {
        Debug.Log($"[NightManager] CONTINUING Night: {data.night} (Money: {data.money})");
        
        // 1. Setup Rules cho đêm (vẫn cần thiết khi load giữa chừng)
        if (RuleManager.Instance != null)
            RuleManager.Instance.SetupRules(data.night);

        // 2. Restore Stats và Queue thông qua GameProgress (DontDestroyOnLoad)
        if (GameProgress.Instance != null)
            GameProgress.Instance.LoadFromSaveData(data);
        
        // Mới: Khôi phục Customer đang active (nếu có)
        if (CustomerQueueManager.Instance != null)
            CustomerQueueManager.Instance.RestoreActiveCustomer(data.activeCustomer);

        // 3. Khôi phục môi trường (Cửa, Đèn...)
        SaveSystem.RestoreEnvironment(data);

        // 4. Teleport Player (Đảm bảo instance đã sẵn sàng từ Awake)
        if (PlayerMovementMobileSmooth.Instance != null && data.hasPosition)
        {
            Vector3 targetPos = new Vector3(data.pX, data.pY, data.pZ);
            PlayerMovementMobileSmooth.Instance.Teleport(targetPos, data.rotY);
        }
        
        Debug.Log("[NightManager] Save Restoration process COMPLETED.");
    }

    public void StartNight(int night)
    {
        Debug.Log("Start Night: " + night);
        RuleManager.Instance.SetupRules(night);

        if (CustomerQueueManager.Instance != null)
            CustomerQueueManager.Instance.BuildQueueForNight(night);
    }

    public bool IsTargetMet()
    {
        int night = GameProgress.Instance.CurrentNight;

        int index = Mathf.Clamp(night - 1, 0, targets.Length - 1);
        int target = targets[index];

        return GameProgress.Instance.CurrentMoney >= target;
    }

    public void CheckTarget()
    {
        if (IsTargetMet())
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.ShowWinNight();
            else
                AdvanceToNextNight();
        }
    }

    public void AdvanceToNextNight()
    {
        Debug.Log("Advancing to Next Night");
        GameProgress.Instance.NextNight();

        // Save progress when successfully transitioning to next night
        SaveSystem.SaveGame();

        StartNight(GameProgress.Instance.CurrentNight);
    }
}