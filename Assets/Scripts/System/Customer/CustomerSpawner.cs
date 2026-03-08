using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Normal Customer Prefabs (Random)")]
    public List<CustomerController> normalCustomerPrefabs = new List<CustomerController>();

    [Header("Points")]
    public Transform spawnPoint;
    public Transform standByPC;
    public Transform photoSpot;

    [Header("Door Blocker (Collider)")]
    public Collider mainDoorBlocker;

    [Header("Order System")]
    public PhotoOrderService orderService;

    [Header("Player")]
    public Transform player;

    [Header("Special Entities (Spectral)")]
    public GameObject twinsPrefab;
    public Transform twinsSpawnPoint;
    public GameObject clownPrefab;
    public Transform clownSpawnPoint;

    [Header("References")]
    public EnvironmentEffectController effectController;

    void Start()
    {
        // Spawn model đặc biệt (Twins / Clown) ở vị trí ẩn, sẵn sàng khi event kích hoạt.
        // CustomerQueueManager sẽ điều phối việc spawn khách thông qua BuildQueueForNight().
        SpawnSpecialModels();
    }

    private void SpawnSpecialModels()
    {
        if (effectController == null)
        {
            effectController = FindFirstObjectByType<EnvironmentEffectController>();
        }

        if (effectController == null)
        {
            Debug.LogWarning("[CustomerSpawner] EnvironmentEffectController not found in scene.");
            return;
        }

        // Spawn Twins
        if (twinsPrefab != null && twinsSpawnPoint != null)
        {
            GameObject twins = Instantiate(twinsPrefab, twinsSpawnPoint.position, twinsSpawnPoint.rotation);
            effectController.SetTwinsModel(twins);
        }

        // Spawn Clown
        if (clownPrefab != null && clownSpawnPoint != null)
        {
            GameObject clown = Instantiate(clownPrefab, clownSpawnPoint.position, clownSpawnPoint.rotation);
            effectController.SetClownModel(clown);
        }
    }

    /// <summary>
    /// Spawn 1 khách hàng bình thường. Được gọi bởi CustomerQueueManager.
    /// </summary>
    [ContextMenu("Spawn Normal Customer")]
    public void SpawnNormalCustomer()
    {
        if (normalCustomerPrefabs.Count == 0 || !spawnPoint || !standByPC || !photoSpot)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab list/spawnPoint/standByPC/photoSpot.");
            return;
        }

        // Chọn ngẫu nhiên 1 prefab từ danh sách
        CustomerController prefab = normalCustomerPrefabs[Random.Range(0, normalCustomerPrefabs.Count)];

        CustomerController c = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Initialize movement flow (door check -> go PC -> go photo spot...)
        c.Init(standByPC, mainDoorBlocker, photoSpot);

        // Inject order system + player reference (scene objects)
        c.SetOrderService(orderService);
        c.SetPlayer(player);
    }
}