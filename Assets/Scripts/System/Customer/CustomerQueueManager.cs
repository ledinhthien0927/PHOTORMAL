using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý hàng đợi spawn khách và sự kiện đặc biệt (Twins, Clown) cho mỗi đêm.
/// - Twins: xuất hiện ít nhất 1 lần, từ Đêm 1 trở lên.
/// - Clown: xuất hiện ít nhất 1 lần, CHỈ từ Đêm 3 trở lên.
/// </summary>
public class CustomerQueueManager : MonoBehaviour
{
    public static CustomerQueueManager Instance;

    // ==========================================
    // INSPECTOR SETTINGS
    // ==========================================

    [Header("References")]
    [SerializeField] private CustomerSpawner customerSpawner;

    [Header("Night Settings")]
    [Tooltip("Số khách bình thường tối thiểu trong đêm")]
    [SerializeField] private int baseCustomerCountPerNight = 5;
    [Tooltip("Số khách cộng thêm mỗi đêm")]
    [SerializeField] private int customerCountIncreasePerNight = 2;

    [Header("Spawn Timing")]
    [Tooltip("Thời gian chờ (giây) trước khi spawn khách tiếp theo sau khi khách trước hoàn thành")]
    [SerializeField] private float delayBetweenCustomers = 3f;
    [Tooltip("Thời gian chờ trước khi spawn khách đầu tiên của đêm")]
    [SerializeField] private float initialSpawnDelay = 2f;

    [Header("Special Event Probabilities")]
    [Tooltip("Số khách bình thường tối thiểu đi qua trước khi có thể xuất hiện sự kiện (Twins/Clown)")]
    [SerializeField] private int minNormalCustomersBeforeEvent = 2;

    [Tooltip("Xác suất mỗi 'slot' trong queue được thay bằng sự kiện Twins (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float twinsChancePerSlot = 0.20f;
    [Tooltip("Xác suất mỗi 'slot' trong queue được thay bằng sự kiện Clown (0-1). Chỉ áp dụng Đêm 3+")]
    [Range(0f, 1f)]
    [SerializeField] private float clownChancePerSlot = 0.15f;

    [Tooltip("Xác suất mỗi 'slot' trong queue được thay bằng sự kiện Footstep (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float footstepChancePerSlot = 0.15f;

    [Tooltip("Xác suất mỗi 'slot' trong queue được thay bằng sự kiện Flicker (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerChancePerSlot = 0.15f;

    // ==========================================
    // INTERNAL STATE
    // ==========================================

    public enum SpawnEntryType { Normal, TwinsEvent, ClownEvent, FootstepEvent }

    [System.Serializable]
    public class SpawnEntry
    {
        public SpawnEntryType Type;
        public bool WillFlicker; // Mới: Cờ đánh dấu ông khách này có bị chớp đèn không

        public SpawnEntry(SpawnEntryType type, bool willFlicker = false)
        {
            Type = type;
            WillFlicker = willFlicker;
        }

        public override string ToString() => $"{Type} (Flicker: {WillFlicker})";
    }

    private Queue<SpawnEntry> spawnQueue = new Queue<SpawnEntry>();
    private bool isProcessing = false;
    private int currentCustomersAlive = 0;
    private CustomerController activeCustomer;

    // ==========================================
    // UNITY LIFECYCLE
    // ==========================================

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventAPI.OnCustomerCompleted += OnCustomerCompleted;
        
        // Theo dõi sự kiện kết thúc để giải phóng hàng đợi
        GameEventAPI.OnTwinsPresenceChanged += HandleTwinsEnd;
        GameEventAPI.OnFootstepToggled += HandleFootstepEnd;
    }

    private void OnDisable()
    {
        GameEventAPI.OnCustomerCompleted -= OnCustomerCompleted;

        GameEventAPI.OnTwinsPresenceChanged -= HandleTwinsEnd;
        GameEventAPI.OnFootstepToggled -= HandleFootstepEnd;
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// Được gọi bởi NightManager khi đêm mới bắt đầu.
    /// Build queue cho đêm và bắt đầu spawn.
    public void BuildQueueForNight(int night)
    {
        StopAllCoroutines();
        spawnQueue.Clear();
        currentCustomersAlive = 0;
        isProcessing = false;

        List<SpawnEntry> entries = BuildEntryList(night);
        foreach (var e in entries)
            spawnQueue.Enqueue(e);

        DebugDumpQueue();

        StartCoroutine(ProcessQueueRoutine());
    }

    /// Force spawn 1 khách bình thường ngay, dùng cho Test.
    public void ForceSpawnNormal(bool willFlicker = false)
    {
        if (customerSpawner == null)
        {
            Debug.LogWarning("[CustomerQueueManager] CustomerSpawner chưa được gán!");
            return;
        }
        activeCustomer = customerSpawner.SpawnNormalCustomer(willFlicker);
        currentCustomersAlive++;
    }

    /// <summary> Force spawn 1 clown customer immediately. </summary>
    public void ForceSpawnClown()
    {
        if (customerSpawner == null)
        {
            Debug.LogWarning("[CustomerQueueManager] CustomerSpawner chưa được gán!");
            return;
        }
        activeCustomer = customerSpawner.SpawnClownCustomer();
        currentCustomersAlive++;
    }

    public CustomerController GetActiveCustomer()
    {
        return activeCustomer;
    }

    /// Lấy danh sách khách còn lại trong hàng đợi để lưu game
    public List<SpawnEntry> GetRemainingQueue()
    {
        return new List<SpawnEntry>(spawnQueue);
    }

    /// Lấy số lượng thực thể đang active để chặn queue khi load
    public int GetCurrentCustomersAlive()
    {
        return currentCustomersAlive;
    }

    /// Khôi phục hàng đợi từ file Save
    public void RestoreQueueState(List<SpawnEntry> savedQueue, int activeCount)
    {
        StopAllCoroutines();
        spawnQueue.Clear();
        foreach (var entry in savedQueue)
            spawnQueue.Enqueue(entry);
        
        // Luôn đặt về 0 — RestoreActiveCustomer sẽ set lại = 1 nếu thật sự có customer được khôi phục.
        // Nếu không reset về 0, queue sẽ chờ mãi cho entity không tồn tại.
        currentCustomersAlive = 0;
        isProcessing = false;
        
        Debug.Log($"[CustomerQueueManager] Restored Queue with {spawnQueue.Count} entries. Active reset to 0.");
        
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    public void RestoreActiveCustomer(SaveData.CustomerSaveData data)
    {
        if (data == null || !data.exists || customerSpawner == null) return;

        Debug.Log($"[CustomerQueueManager] Re-spawning active customer: PrefabName={data.prefabName}, IsClown={data.isClown}");
        
        Vector3 pos = new Vector3(data.pX, data.pY, data.pZ);
        Quaternion rot = Quaternion.Euler(0, data.rotY, 0);

        if (data.isClown)
        {
            activeCustomer = customerSpawner.SpawnClownCustomerAt(pos, rot);
        }
        else
        {
            activeCustomer = customerSpawner.SpawnByPrefabName(data.prefabName, pos, rot);
        }

        if (activeCustomer != null)
        {
            activeCustomer.RestoreState(data.currentOrder, data.state, data.willTriggerFlicker, data.hasPhotoTaken, pos, data.rotY);
            
            // Đảm bảo logic currentCustomersAlive khớp (thường là 1 nếu có customer)
            currentCustomersAlive = 1;
        }
    }


    /// In danh sách queue ra Console để debug.
    public void DebugDumpQueue()
    {
        var arr = spawnQueue.ToArray();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[CustomerQueueManager] Queue ({arr.Length} entries):");
        for (int i = 0; i < arr.Length; i++)
            sb.AppendLine($"  [{i}] {arr[i]}");
        Debug.Log(sb.ToString());
    }

    // ==========================================
    // QUEUE BUILDING
    // ==========================================

    private List<SpawnEntry> BuildEntryList(int night)
    {
        int totalSlots = baseCustomerCountPerNight + (night - 1) * customerCountIncreasePerNight;
        List<SpawnEntry> entries = new List<SpawnEntry>();

        // 1. Luôn bảo đảm Customer đầu tiên không có Event kèm theo
        entries.Add(new SpawnEntry(SpawnEntryType.Normal, false));

        bool twinsAdded = false;
        bool clownAdded = false;
        
        bool lastWasEvent = false;

        // 2. Từ slot thứ 2 trở đi, Random Khách HOẶC Event
        for (int i = 1; i < totalSlots; i++)
        {
            // Tránh spawn 2 event liên tiếp (Bug 12)
            if (lastWasEvent)
            {
                bool isFlickerAttached = (Random.value < flickerChancePerSlot);
                entries.Add(new SpawnEntry(SpawnEntryType.Normal, isFlickerAttached));
                lastWasEvent = false;
                continue;
            }

            float roll = Random.value;
            float currentChance = 0f;
            
            // --- TWINS ---
            if (roll < (currentChance += twinsChancePerSlot))
            {
                entries.Add(new SpawnEntry(SpawnEntryType.TwinsEvent));
                twinsAdded = true;
                lastWasEvent = true;
                continue;
            }
            
            // --- CLOWN (Only Night 3+) ---
            if (night >= 3 && roll < (currentChance += clownChancePerSlot))
            {
                entries.Add(new SpawnEntry(SpawnEntryType.ClownEvent));
                clownAdded = true;
                lastWasEvent = true;
                continue;
            }
            
            // --- FOOTSTEP ---
            if (roll < (currentChance += footstepChancePerSlot))
            {
                entries.Add(new SpawnEntry(SpawnEntryType.FootstepEvent));
                lastWasEvent = true;
                continue;
            }

            // --- NORMAL CUSTOMER ---
            // Nếu không trúng Event chắn queue nào trên kia, thì spawn Customer.
            // Customer này có thể mang theo cờ FlickerLight.
            bool isFlickerAttachedNormal = false;
            if (roll < (currentChance += flickerChancePerSlot))
            {
                isFlickerAttachedNormal = true;
            }
            
            entries.Add(new SpawnEntry(SpawnEntryType.Normal, isFlickerAttachedNormal));
            lastWasEvent = false;
        }

        // --- BẢO ĐẢM TỐI THIỂU (Guarantees) ---
        // Phải có ít nhất 1 lần Twins (từ vị trí thứ 2 trở đi để không chèn lên ông Khách đầu tiên)
        if (!twinsAdded && totalSlots > 1)
        {
            int insertIndex = Random.Range(1, entries.Count);
            // Cố gắng không đè lên một khoảng trống giữa các Event khác (nếu cần thiết)
            entries[insertIndex] = new SpawnEntry(SpawnEntryType.TwinsEvent);
        }

        // Đêm 3+ phải có ít nhất 1 lần Clown (cũng từ vị trí thứ 2 trở đi)
        if (night >= 3 && !clownAdded && totalSlots > 1)
        {
            // Thường nhét Clown vào nửa sau đêm cho khó
            int insertIndex = Random.Range(entries.Count / 2, entries.Count);
            // Đảm bảo không ghi đè lên index 1 nếu rủi ro random
            insertIndex = Mathf.Max(1, insertIndex);
            entries[insertIndex] = new SpawnEntry(SpawnEntryType.ClownEvent);
        }

        return entries;
    }

    // ==========================================
    // QUEUE PROCESSING
    // ==========================================

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessing = true;
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            // Chờ đến khi không còn khách nào đang active
            yield return new WaitUntil(() => currentCustomersAlive <= 0);

            // Kiểm tra xem đã đủ tiền chưa? Nếu đủ rồi thì dừng spawn
            if (NightManager.Instance != null && NightManager.Instance.IsTargetMet())
            {
                Debug.Log("[CustomerQueueManager] Goal reached. Stopping spawns for tonight.");
                break;
            }

            // Nếu hết queue mà vẫn chưa đủ tiền -> Rebuild queue mới
            if (spawnQueue.Count == 0)
            {
                Debug.Log("[CustomerQueueManager] Queue empty but target not met. Rebuilding queue...");
                int night = GameProgress.Instance.CurrentNight;
                List<SpawnEntry> entries = BuildEntryList(night);
                foreach (var e in entries)
                    spawnQueue.Enqueue(e);
            }

            if (spawnQueue.Count > 0)
            {
                SpawnEntry entry = spawnQueue.Dequeue();
                ProcessEntry(entry);

                // Chờ một chút sau khi spawn trước khi kiểm tra tiếp
                yield return new WaitForSeconds(delayBetweenCustomers);
            }
        }

        isProcessing = false;
        Debug.Log("[CustomerQueueManager] Hết queue đêm nay (Mục tiêu đã đạt).");
    }

    private void ProcessEntry(SpawnEntry entry)
    {
        switch (entry.Type)
        {
            case SpawnEntryType.Normal:
                Debug.Log($"[CustomerQueueManager] Spawn: Normal Customer (WillFlicker: {entry.WillFlicker})");
                if (customerSpawner != null)
                {
                    activeCustomer = customerSpawner.SpawnNormalCustomer(entry.WillFlicker);
                    currentCustomersAlive++;
                }
                break;

            case SpawnEntryType.TwinsEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Twins Event (Tuần tự)");
                // Chặn queue thông qua listener của OnTwinsPresenceChanged
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Twins");
                break;

            case SpawnEntryType.ClownEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Clown Event (Tuần tự)");
                // ForceSpawnClown inside ClownEvent.Execute will increment currentCustomersAlive
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Clown");
                break;

            case SpawnEntryType.FootstepEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Footstep Event (Tuần tự)");
                // Chặn queue thông qua listener của OnFootstepToggled
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Footstep");
                break;
        }
    }

    private void OnCustomerCompleted()
    {
        currentCustomersAlive = Mathf.Max(0, currentCustomersAlive - 1);
        Debug.Log($"[CustomerQueueManager] Thực thể (Khách/Event) hoàn thành. Còn {currentCustomersAlive} đang xử lý.");
    }

    private void OnEventEntityCompleted()
    {
        OnCustomerCompleted();
    }

    private void HandleTwinsEnd(bool isPresent)
    {
        if (isPresent) 
            currentCustomersAlive++; // Chặn queue khi cặp sinh đôi xuất hiện
        else 
            OnEventEntityCompleted(); // Giải phóng khi cặp sinh đôi biến mất
    }

    private void HandleFootstepEnd(bool isPlaying)
    {
        if (isPlaying)
            currentCustomersAlive++; // Chặn queue khi tiếng bước chân đang phát
        else
            OnEventEntityCompleted(); // Giải phóng khi hết tiếng bước chân
    }
}
