using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour, IInteractable
{
    public enum CustomerState
    {
        WaitingDoorCheck,
        WaitingDoorOpen,
        GoingToPC,
        WaitingPickupAtPC,
        GoingToPhotoSpot,
        WaitingShootDone,
        ReturningToPC,
        WaitingPrintAtPC,
        Completed
    }

    [Header("Flow Timers")]
    public float firstCheckDelay = 5f;
    public float recheckInterval = 0.5f;

    [Header("Popup (World Space)")]
    public Transform popupAnchor;     // Place this above customer's head
    public OrderPopupUI popupPrefab;  // Your world-space popup prefab

    [Header("Look At Player")]
    public bool lookAtPlayerWhenWaiting = true;
    public float lookRotateSpeed = 360f;

    [Header("Prompt Text")]
    [SerializeField] private string inviteText = "Invite to studio";
    [SerializeField] private string finishText = "Finish session";

    [Header("Debug")]
    public bool logFlow = true;

    private NavMeshAgent agent;

    private Transform standByPC;
    private Transform photoSpot;
    private Collider mainDoorBlocker;

    private CustomerState state;

    private OrderPopupUI popupInstance;

    private PhotoOrderService orderService; // injected by spawner
    private Transform player;               // injected by spawner

    private PhotoOrder currentOrder;

    // -------- IInteractable --------
    public string Prompt
    {
        get
        {
            // Return UI prompt when player aims at customer (like Door/LightSwitch)
            if (state == CustomerState.WaitingPickupAtPC) return inviteText;
            if (state == CustomerState.WaitingShootDone) return finishText;

            // Empty means PlayerInteractor will hide prompt
            return string.Empty;
        }
    }

    public bool CanInteract(IInteractor interactor)
    {
        // Allow interaction only when it makes sense
        return state == CustomerState.WaitingPickupAtPC
            || state == CustomerState.WaitingShootDone;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor)) return;

        // Use the same internal interaction logic
        Interact();
    }
    // -------------------------------

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Called by spawner right after Instantiate
    public void Init(Transform standByPcPoint, Collider mainDoorBlockerCollider, Transform photoSpotPoint)
    {
        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;
        photoSpot = photoSpotPoint;

        state = CustomerState.WaitingDoorCheck;
        StartCoroutine(FlowRoutine());
    }

    // Inject order system from scene
    public void SetOrderService(PhotoOrderService service)
    {
        orderService = service;
    }

    // Inject player transform from scene
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private IEnumerator FlowRoutine()
    {
        if (logFlow) Debug.Log("[Customer] Spawned. Waiting before first door check...");
        yield return new WaitForSeconds(firstCheckDelay);

        state = CustomerState.WaitingDoorOpen;

        // Keep waiting until door is open
        while (IsMainDoorClosed())
        {
            if (logFlow) Debug.Log("[Customer] Main door is closed. Waiting...");
            yield return new WaitForSeconds(recheckInterval);
        }

        // Door open => go to PC point
        if (standByPC != null)
        {
            state = CustomerState.GoingToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);

            if (logFlow) Debug.Log("[Customer] Main door open. Moving to PC...");
        }
    }

    void Update()
    {
        // Arrival handling
        if (state == CustomerState.GoingToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPickupAtPC;

            GenerateOrder();
            SpawnPopupForOrder();

            if (logFlow) Debug.Log("[Customer] Arrived at PC. Waiting for pickup.");
        }
        else if (state == CustomerState.GoingToPhotoSpot && ReachedDestination())
        {
            state = CustomerState.WaitingShootDone;

            if (logFlow) Debug.Log("[Customer] Arrived at PhotoSpot. Waiting for shooting done.");
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPrintAtPC;

            if (logFlow) Debug.Log("[Customer] Back at PC. Waiting for printed photos.");
        }

        // Rotate to face player while waiting
        LookAtPlayer();
    }

    private void GenerateOrder()
    {
        if (orderService == null)
        {
            Debug.LogError("[Customer] OrderService missing (inject it from CustomerSpawner).");
            // Fallback so popup still shows something
            currentOrder = new PhotoOrder { quantity = 1, size = PhotoSize.Size3x4 };
            return;
        }

        currentOrder = orderService.Generate();
    }

    private void SpawnPopupForOrder()
    {
        if (popupPrefab == null || popupAnchor == null)
        {
            Debug.LogWarning("[Customer] Popup prefab/anchor missing.");
            return;
        }

        // Avoid duplicate popups
        if (popupInstance != null)
            Destroy(popupInstance.gameObject);

        popupInstance = Instantiate(popupPrefab, popupAnchor.position, Quaternion.identity, popupAnchor);

        // If you want pickup only via PlayerInteractor button, set callback = null
        // popupInstance.Show(currentOrder, null);

        // If you also want clicking popup button to work, keep this:
        popupInstance.Show(currentOrder, () =>
        {
            Interact();
        });
    }

    // Internal interaction entry point (used by popup button OR IInteractable)
    public void Interact()
    {
        if (state == CustomerState.WaitingPickupAtPC)
        {
            // Hide popup after pickup
            if (popupInstance != null)
                popupInstance.Hide();

            if (photoSpot == null)
            {
                Debug.LogError("[Customer] photoSpot is null.");
                return;
            }

            state = CustomerState.GoingToPhotoSpot;
            agent.isStopped = false;
            agent.SetDestination(photoSpot.position);

            if (logFlow) Debug.Log("[Customer] Invite confirmed. Going to PhotoSpot.");
            return;
        }

        if (state == CustomerState.WaitingShootDone)
        {
            if (standByPC == null)
            {
                Debug.LogError("[Customer] standByPC is null.");
                return;
            }

            state = CustomerState.ReturningToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);

            if (logFlow) Debug.Log("[Customer] Session finished. Returning to PC.");
            return;
        }

        if (logFlow) Debug.Log($"[Customer] Interact ignored in state: {state}");
    }

    // PC system should call this when printing is correct (quantity & size validated by GameManager later)
    public void OnPhotosDelivered()
    {
        if (state != CustomerState.WaitingPrintAtPC)
        {
            if (logFlow) Debug.Log($"[Customer] OnPhotosDelivered ignored. Current state: {state}");
            return;
        }

        state = CustomerState.Completed;

        // TODO: reward money here later (or via GameManager)
        if (logFlow) Debug.Log("[Customer] Photos received. Completing order.");

        Destroy(gameObject);
    }

    // Expose order for your PC system
    public PhotoOrder GetCurrentOrder()
    {
        return currentOrder;
    }

    private bool IsMainDoorClosed()
    {
        // enabled = closed, disabled = open
        if (mainDoorBlocker == null) return false;
        return mainDoorBlocker.enabled;
    }

    private bool ReachedDestination()
    {
        if (agent == null) return false;
        if (agent.pathPending) return false;

        // If path is invalid, do not treat as arrived
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;

        return agent.remainingDistance <= agent.stoppingDistance;
    }

    private void LookAtPlayer()
    {
        if (!lookAtPlayerWhenWaiting) return;
        if (player == null) return;

        // Customer should face player in these states
        bool shouldLook =
            state == CustomerState.WaitingPickupAtPC ||
            state == CustomerState.WaitingShootDone ||
            state == CustomerState.WaitingPrintAtPC ||
            state == CustomerState.GoingToPhotoSpot;

        if (!shouldLook) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            lookRotateSpeed * Time.deltaTime
        );
    }
    private void SetAgentMovement(bool canMove)
    {
        // When customer is waiting (standing still), we control rotation manually.
        // When moving, let NavMeshAgent handle rotation.
        if (agent == null) return;

        agent.isStopped = !canMove;
        agent.updateRotation = canMove;
    }
}