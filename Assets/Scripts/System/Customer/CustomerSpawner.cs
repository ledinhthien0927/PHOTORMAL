using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public CustomerController normalCustomerPrefab;

    [Header("Points")]
    public Transform spawnPoint;
    public Transform standByPC;

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
            Debug.LogError("[CustomerSpawner] Thi?u prefab/spawnPoint/standByPC.");
            return;
        }

        CustomerController c = Instantiate(
            normalCustomerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        c.SetTarget(standByPC);
    }
}