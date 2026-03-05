using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public CustomerController normalCustomerPrefab;

    [Header("Points")]
    public Transform spawnPoint;
    public Transform standByPC;

    [Header("Door Blocker (Collider)")]
    public Collider mainDoorBlocker;

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
        if (!normalCustomerPrefab || !spawnPoint || !standByPC)
        {
            Debug.LogError("[CustomerSpawner] Missing prefab/spawnPoint/standByPC.");
            return;
        }

        CustomerController c = Instantiate(
            normalCustomerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Initialize flow: wait 5s, check door, then go to PC
        c.Init(standByPC, mainDoorBlocker);
    }
}