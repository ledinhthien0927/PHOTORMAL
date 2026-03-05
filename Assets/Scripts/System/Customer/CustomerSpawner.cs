using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public CustomerController normalCustomerPrefab;

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

    [Header("Spawn")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
            SpawnNormal();
    }

    [ContextMenu("Spawn Normal")]
    public void SpawnNormal()
    {
        if (!normalCustomerPrefab || !spawnPoint || !standByPC || !photoSpot)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab/spawnPoint/standByPC/photoSpot.");
            return;
        }

        CustomerController c = Instantiate(
            normalCustomerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Initialize movement flow (door check -> go PC -> go photo spot...)
        c.Init(standByPC, mainDoorBlocker, photoSpot);

        // Inject order system + player reference (scene objects)
        c.SetOrderService(orderService);
        c.SetPlayer(player);

        // NOTE:
        // In the simplified system, we DO NOT bind spawned customer to StudioManager/AimValidator.
        // CustomerController will signal READY when it reaches the photo spot.
        // AimValidator will only check READY + aim at PoseAimZone collider.
    }
}