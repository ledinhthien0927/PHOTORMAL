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
    public Transform exitPoint;
    public Transform outsidePoint;
    public Transform insidePoint;

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
    public CustomerController clownCustomerPrefab;

    [Header("References")]
    public EnvironmentEffectController effectController;

    int currentIndex = 0;

    private void Start()
    {
        SpawnSpecialModels();
        ShufflePrefabs();
    }

    private void ShufflePrefabs()
    {
        if (normalCustomerPrefabs.Count <= 1) return;
        
        for (int i = 0; i < normalCustomerPrefabs.Count; i++)
        {
            CustomerController temp = normalCustomerPrefabs[i];
            int randomIndex = Random.Range(i, normalCustomerPrefabs.Count);
            normalCustomerPrefabs[i] = normalCustomerPrefabs[randomIndex];
            normalCustomerPrefabs[randomIndex] = temp;
        }
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

        if (twinsPrefab != null && twinsSpawnPoint != null)
        {
            GameObject twins = Instantiate(twinsPrefab, twinsSpawnPoint.position, twinsSpawnPoint.rotation);
            effectController.SetTwinsModel(twins);
        }

        if (clownPrefab != null && clownSpawnPoint != null)
        {
            GameObject clown = Instantiate(clownPrefab, clownSpawnPoint.position, clownSpawnPoint.rotation);
            effectController.SetClownModel(clown);
        }
    }

    [ContextMenu("Spawn Normal Customer")]
    public CustomerController SpawnNormalCustomer(bool willFlicker = false)
    {
        if (normalCustomerPrefabs.Count == 0 || !spawnPoint || !standByPC || !photoSpot || !exitPoint || !outsidePoint || !insidePoint)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab list/spawnPoint/standByPC/photoSpot/exitPoint/outsidePoint/insidePoint.");
            return null;
        }

        if (currentIndex >= normalCustomerPrefabs.Count)
        {
            currentIndex = 0;
            ShufflePrefabs();
        }

        CustomerController prefab = normalCustomerPrefabs[currentIndex];
        currentIndex++;

        CustomerController customer = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        customer.prefabName = prefab.name; // Lưu tên prefab gốc (trước khi Unity thêm "(Clone)")
        customer.willTriggerFlicker = willFlicker;
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);

        return customer;
    }

    /// <summary>Spawn khách bình thường theo tên prefab (dùng khi restore save).</summary>
    public CustomerController SpawnByPrefabName(string targetPrefabName, Vector3 pos, Quaternion rot)
    {
        CustomerController prefab = null;
        foreach (var p in normalCustomerPrefabs)
        {
            if (p.name == targetPrefabName)
            {
                prefab = p;
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[CustomerSpawner] Prefab '{targetPrefabName}' not found! Spawning random customer.");
            return SpawnNormalCustomer();
        }

        CustomerController customer = Instantiate(prefab, pos, rot);
        customer.prefabName = prefab.name;
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);

        return customer;
    }

    public CustomerController SpawnClownCustomer()
    {
        if (clownCustomerPrefab == null)
        {
            Debug.LogWarning("[CustomerSpawner] ClownCustomerPrefab is null, spawning normal customer instead.");
            return SpawnNormalCustomer();
        }

        if (!spawnPoint || !standByPC || !photoSpot || !exitPoint || !outsidePoint || !insidePoint)
        {
            Debug.LogError("[CustomerSpawner] Missing spawnPoint/standByPC/photoSpot/exitPoint/outsidePoint/insidePoint.");
            return null;
        }

        CustomerController customer = Instantiate(
            clownCustomerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        customer.prefabName = clownCustomerPrefab.name;
        customer.isClown = true;
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);

        return customer;
    }

    public CustomerController SpawnClownCustomerAt(Vector3 pos, Quaternion rot)
    {
        if (clownCustomerPrefab == null) return null;

        CustomerController customer = Instantiate(clownCustomerPrefab, pos, rot);
        
        customer.prefabName = clownCustomerPrefab.name;
        customer.isClown = true;
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);

        return customer;
    }
}