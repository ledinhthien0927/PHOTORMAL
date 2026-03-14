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

    public enum SpawnEntryType { Normal, TwinsEvent, ClownEvent, FootstepEvent, FlickerEvent }

    [System.Serializable]
    public class SpawnEntry
    {
        public SpawnEntryType Type;

        public SpawnEntry(SpawnEntryType type)
        {
            Type = type;
        }

        public override string ToString() => Type.ToString();
    }

    private Queue<SpawnEntry> spawnQueue = new Queue<SpawnEntry>();
    private bool isProcessing = false;
    private int currentCustomersAlive = 0;

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
        GameEventAPI.OnClownDisappeared += OnEventEntityCompleted;
        GameEventAPI.OnClownJumpscare += OnEventEntityCompleted;
        GameEventAPI.OnStudioLightFlicker += HandleFlickerEnd;
        GameEventAPI.OnFootstepToggled += HandleFootstepEnd;
    }

    private void OnDisable()
    {
        GameEventAPI.OnCustomerCompleted -= OnCustomerCompleted;

        GameEventAPI.OnTwinsPresenceChanged -= HandleTwinsEnd;
        GameEventAPI.OnClownDisappeared -= OnEventEntityCompleted;
        GameEventAPI.OnClownJumpscare -= OnEventEntityCompleted;
        GameEventAPI.OnStudioLightFlicker -= HandleFlickerEnd;
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
    public void ForceSpawnNormal()
    {
        if (customerSpawner == null)
        {
            Debug.LogWarning("[CustomerQueueManager] CustomerSpawner chưa được gán!");
            return;
        }
        customerSpawner.SpawnNormalCustomer();
        currentCustomersAlive++;
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

        // Bắt đầu toàn bộ là Normal
        List<SpawnEntry> entries = new List<SpawnEntry>();
        for (int i = 0; i < totalSlots; i++)
            entries.Add(new SpawnEntry(SpawnEntryType.Normal));

        // Đảm bảo sự kiện chỉ spawn sau vài khách đầu (vd: minNormalCustomersBeforeEvent = 2)
        int startIndex = Mathf.Clamp(minNormalCustomersBeforeEvent, 0, totalSlots - 1);

        // --- TWINS ---
        bool twinsGuaranteed = false;
        for (int i = startIndex; i < entries.Count; i++)
        {
            if (entries[i].Type != SpawnEntryType.Normal) continue;
            if (Random.value < twinsChancePerSlot)
            {
                entries[i] = new SpawnEntry(SpawnEntryType.TwinsEvent);
                twinsGuaranteed = true;
            }
        }
        // Guarantee ít nhất 1 lần Twins: chèn vào nửa sau nếu chưa có (nhưng vẫn phải >= startIndex)
        if (!twinsGuaranteed && entries.Count > 0)
        {
            int guaranteedIndex = Mathf.Max(startIndex, Random.Range(entries.Count / 2, entries.Count));
            // Ưu tiên chọn slot Normal
            for (int i = guaranteedIndex; i < entries.Count; i++)
            {
                if (entries[i].Type == SpawnEntryType.Normal)
                {
                    entries[i] = new SpawnEntry(SpawnEntryType.TwinsEvent);
                    break;
                }
            }
        }

        // --- CLOWN (only Night 3+) ---
        if (night >= 3)
        {
            bool clownGuaranteed = false;
            for (int i = startIndex; i < entries.Count; i++)
            {
                if (entries[i].Type != SpawnEntryType.Normal) continue;
                if (Random.value < clownChancePerSlot)
                {
                    entries[i] = new SpawnEntry(SpawnEntryType.ClownEvent);
                    clownGuaranteed = true;
                }
            }
            // Guarantee ít nhất 1 lần Clown: chèn cuối nếu chưa có
            if (!clownGuaranteed && entries.Count > 0)
            {
                // Tìm slot Normal ở nửa sau để insert Clown
                int startSearch = entries.Count / 2;
                bool inserted = false;
                for (int i = entries.Count - 1; i >= startSearch; i--)
                {
                    if (entries[i].Type == SpawnEntryType.Normal)
                    {
                        entries[i] = new SpawnEntry(SpawnEntryType.ClownEvent);
                        inserted = true;
                        break;
                    }
                }
                // Nếu không tìm được slot Normal ở nửa sau, append thêm vào cuối
                if (!inserted)
                    entries.Add(new SpawnEntry(SpawnEntryType.ClownEvent));
            }
        }

        // --- FOOTSTEP & FLICKER ---
        for (int i = startIndex; i < entries.Count; i++)
        {
            if (entries[i].Type != SpawnEntryType.Normal) continue;

            float roll = Random.value;
            if (roll < footstepChancePerSlot)
            {
                entries[i] = new SpawnEntry(SpawnEntryType.FootstepEvent);
            }
            else if (roll < footstepChancePerSlot + flickerChancePerSlot)
            {
                entries[i] = new SpawnEntry(SpawnEntryType.FlickerEvent);
            }
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

        while (spawnQueue.Count > 0)
        {
            // Chờ đến khi không còn khách nào đang active
            yield return new WaitUntil(() => currentCustomersAlive <= 0);

            SpawnEntry entry = spawnQueue.Dequeue();
            ProcessEntry(entry);

            // Chờ một chút sau khi spawn trước khi kiểm tra tiếp
            yield return new WaitForSeconds(delayBetweenCustomers);
        }

        isProcessing = false;
        Debug.Log("[CustomerQueueManager] Hết queue đêm nay.");
    }

    private void ProcessEntry(SpawnEntry entry)
    {
        switch (entry.Type)
        {
            case SpawnEntryType.Normal:
                Debug.Log("[CustomerQueueManager] Spawn: Normal Customer");
                if (customerSpawner != null)
                {
                    customerSpawner.SpawnNormalCustomer();
                    currentCustomersAlive++;
                }
                break;

            case SpawnEntryType.TwinsEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Twins Event (Tuần tự)");
                currentCustomersAlive++; // Chặn queue cho đến khi Twins biến mất
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Twins");
                break;

            case SpawnEntryType.ClownEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Clown Event (Tuần tự)");
                currentCustomersAlive++; // Chặn queue cho đến khi Clown biến mất/Jumpscare
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Clown");
                break;

            case SpawnEntryType.FootstepEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Footstep Event (Tuần tự)");
                currentCustomersAlive++; // Chặn queue cho đến khi hết tiếng chân
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("Footstep");
                break;

            case SpawnEntryType.FlickerEvent:
                Debug.Log("[CustomerQueueManager] Spawn: Flicker Event (Tuần tự)");
                currentCustomersAlive++; // Chặn queue cho đến khi hết nháy đèn
                if (EventManager.Instance != null)
                    EventManager.Instance.TriggerEvent("FlickerLight");
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
        if (!isPresent) OnEventEntityCompleted();
    }

    private void HandleFlickerEnd(bool isFlickering)
    {
        if (!isFlickering) OnEventEntityCompleted();
    }

    private void HandleFootstepEnd(bool isPlaying)
    {
        if (!isPlaying) OnEventEntityCompleted();
    }
}
