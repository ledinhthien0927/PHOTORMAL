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
    public void SpawnNormalCustomer()
    {
        if (normalCustomerPrefabs.Count == 0 || !spawnPoint || !standByPC || !photoSpot || !exitPoint || !outsidePoint || !insidePoint)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab list/spawnPoint/standByPC/photoSpot/exitPoint/outsidePoint/insidePoint.");
            return;
        }

        CustomerController prefab = normalCustomerPrefabs[currentIndex];
        currentIndex = (currentIndex + 1) % normalCustomerPrefabs.Count;

        CustomerController customer = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);
    }

    public void SpawnClownCustomer()
    {
        if (clownCustomerPrefab == null)
        {
            Debug.LogWarning("[CustomerSpawner] ClownCustomerPrefab is null, spawning normal customer instead.");
            SpawnNormalCustomer();
            return;
        }

        if (!spawnPoint || !standByPC || !photoSpot || !exitPoint || !outsidePoint || !insidePoint)
        {
            Debug.LogError("[CustomerSpawner] Missing spawnPoint/standByPC/photoSpot/exitPoint/outsidePoint/insidePoint.");
            return;
        }

        CustomerController customer = Instantiate(
            clownCustomerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        customer.isClown = true;
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint, outsidePoint, insidePoint);
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);
    }
}