using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("Flow Timers")]
    public float firstCheckDelay = 5f;     // Wait after spawn before checking main door
    public float recheckInterval = 0.5f;   // Recheck interval when door is closed

    [Header("Debug")]
    public bool logFlow = false;

    private NavMeshAgent agent;

    private Transform standByPC;
    private Collider mainDoorBlocker; // enabled = closed, disabled = open

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Called by spawner right after Instantiate
    public void Init(Transform standByPcPoint, Collider mainDoorBlockerCollider)
    {
        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;

        StartCoroutine(FlowRoutine());
    }

    private IEnumerator FlowRoutine()
    {
        // 1) Wait 5 seconds after spawn (per your design)
        if (logFlow) Debug.Log("[Customer] Spawned. Waiting before first door check...");
        yield return new WaitForSeconds(firstCheckDelay);

        // 2) Check main door; if closed, keep waiting
        while (IsMainDoorClosed())
        {
            if (logFlow) Debug.Log("[Customer] Main door is closed. Waiting...");
            yield return new WaitForSeconds(recheckInterval);
        }

        // 3) Door open => go to PC standby point
        if (standByPC != null)
        {
            if (logFlow) Debug.Log("[Customer] Main door open. Moving to StandBy_PC...");
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
        }
    }

    private bool IsMainDoorClosed()
    {
        // If blocker collider is enabled, we consider the door closed
        if (mainDoorBlocker == null) return false;
        return mainDoorBlocker.enabled;
    }
}