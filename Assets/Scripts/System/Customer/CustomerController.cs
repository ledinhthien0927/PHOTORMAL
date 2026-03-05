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
    public Transform popupAnchor;
    public OrderPopupUI popupPrefab;

    [Header("Look At Player")]
    public bool lookAtPlayerWhenWaiting = true;
    public float lookRotateSpeed = 360f;

    [Header("Prompt Text")]
    [SerializeField] private string inviteText = "Invite to studio";
    [SerializeField] private string finishText = "Finish session";

    private NavMeshAgent agent;

    private Transform standByPC;
    private Transform photoSpot;
    private Collider mainDoorBlocker;

    private CustomerState state;

    private OrderPopupUI popupInstance;

    private PhotoOrderService orderService;
    private Transform player;

    private PhotoOrder currentOrder;

    private bool _notifiedReady;

    // ===== NEW: block finish session until at least one photo is captured =====
    private bool hasPhotoTaken;
    private bool listeningPhotoEvent;

    public string Prompt
    {
        get
        {
            if (state == CustomerState.WaitingPickupAtPC) return inviteText;
            if (state == CustomerState.WaitingShootDone) return finishText;
            return string.Empty;
        }
    }

    public bool CanInteract(IInteractor interactor)
    {
        return state == CustomerState.WaitingPickupAtPC
            || state == CustomerState.WaitingShootDone;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor)) return;
        Interact();
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnDestroy()
    {
        StopListeningPhotoCaptured();
        NotifyReady(false);
    }

    public void Init(Transform standByPcPoint, Collider mainDoorBlockerCollider, Transform photoSpotPoint)
    {
        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;
        photoSpot = photoSpotPoint;

        state = CustomerState.WaitingDoorCheck;
        StartCoroutine(FlowRoutine());
    }

    public void SetOrderService(PhotoOrderService service)
    {
        orderService = service;
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private IEnumerator FlowRoutine()
    {
        yield return new WaitForSeconds(firstCheckDelay);

        state = CustomerState.WaitingDoorOpen;

        while (IsMainDoorClosed())
            yield return new WaitForSeconds(recheckInterval);

        if (standByPC != null)
        {
            state = CustomerState.GoingToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
        }
    }

    void Update()
    {
        if (state == CustomerState.GoingToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPickupAtPC;

            GenerateOrder();
            SpawnPopupForOrder();
        }
        else if (state == CustomerState.GoingToPhotoSpot && ReachedDestination())
        {
            state = CustomerState.WaitingShootDone;

            // Reset photo flag for this session attempt
            hasPhotoTaken = false;

            // Customer is READY for photographing
            NotifyReady(true);

            // Listen for "photo captured" signal
            StartListeningPhotoCaptured();
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPrintAtPC;
        }

        LookAtPlayer();
    }

    private void NotifyReady(bool ready)
    {
        if (_notifiedReady == ready) return;
        _notifiedReady = ready;

        if (StudioManager.Instance != null)
            StudioManager.Instance.NotifyCustomerReady(ready);
    }

    private void StartListeningPhotoCaptured()
    {
        if (listeningPhotoEvent) return;

        StudioManager.OnPhotoCaptured += OnPhotoCaptured;
        listeningPhotoEvent = true;
    }

    private void StopListeningPhotoCaptured()
    {
        if (!listeningPhotoEvent) return;

        StudioManager.OnPhotoCaptured -= OnPhotoCaptured;
        listeningPhotoEvent = false;
    }

    private void OnPhotoCaptured()
    {
        // Only accept capture signal while waiting at photo spot
        if (state != CustomerState.WaitingShootDone) return;

        hasPhotoTaken = true;
    }

    private void GenerateOrder()
    {
        if (orderService == null)
        {
            currentOrder = new PhotoOrder { quantity = 1, size = PhotoSize.Size3x4 };
            return;
        }

        currentOrder = orderService.Generate();
    }

    private void SpawnPopupForOrder()
    {
        if (popupPrefab == null || popupAnchor == null) return;

        if (popupInstance != null)
            Destroy(popupInstance.gameObject);

        popupInstance = Instantiate(popupPrefab, popupAnchor.position, Quaternion.identity, popupAnchor);

        popupInstance.Show(currentOrder, () =>
        {
            Interact();
        });
    }

    public void Interact()
    {
        if (state == CustomerState.WaitingPickupAtPC)
        {
            if (popupInstance != null)
                popupInstance.Hide();

            state = CustomerState.GoingToPhotoSpot;
            agent.isStopped = false;
            agent.SetDestination(photoSpot.position);
            return;
        }

        if (state == CustomerState.WaitingShootDone)
        {
            // ===== IMPORTANT: block ending session until at least one photo was taken =====
            if (!hasPhotoTaken)
                return;

            // Leaving photo spot: stop being READY + stop listening
            NotifyReady(false);
            StopListeningPhotoCaptured();

            state = CustomerState.ReturningToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
        }
    }

    public void OnPhotosDelivered()
    {
        if (state != CustomerState.WaitingPrintAtPC) return;

        state = CustomerState.Completed;

        StopListeningPhotoCaptured();
        NotifyReady(false);

        Destroy(gameObject);
    }

    public PhotoOrder GetCurrentOrder()
    {
        return currentOrder;
    }

    private bool IsMainDoorClosed()
    {
        if (mainDoorBlocker == null) return false;
        return mainDoorBlocker.enabled;
    }

    private bool ReachedDestination()
    {
        if (agent == null) return false;
        if (agent.pathPending) return false;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return false;

        return agent.remainingDistance <= agent.stoppingDistance;
    }

    private void LookAtPlayer()
    {
        if (!lookAtPlayerWhenWaiting) return;
        if (player == null) return;

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
}