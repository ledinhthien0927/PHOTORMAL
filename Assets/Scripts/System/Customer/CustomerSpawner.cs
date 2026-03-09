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

    private void Start()
    {
        // Spawn special models (Twins / Clown) at hidden points,
        // ready for event activation.
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
    /// Spawn one normal customer. Called by CustomerQueueManager.
    /// </summary>
    [ContextMenu("Spawn Normal Customer")]
    public void SpawnNormalCustomer()
    {
        if (normalCustomerPrefabs.Count == 0 || !spawnPoint || !standByPC || !photoSpot || !exitPoint)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab list/spawnPoint/standByPC/photoSpot/exitPoint.");
            return;
        }

        // Pick a random prefab from the normal customer list.
        CustomerController prefab = normalCustomerPrefabs[Random.Range(0, normalCustomerPrefabs.Count)];

        CustomerController customer = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Initialize movement flow.
        customer.Init(standByPC, mainDoorBlocker, photoSpot, exitPoint);

        // Inject scene references.
        customer.SetOrderService(orderService);
        customer.SetPlayer(player);
    }
}