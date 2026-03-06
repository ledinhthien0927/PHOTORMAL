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

    private bool notifiedReady;
    private bool hasPhotoTaken;
    private bool listeningPhotoEvent;

    public string Prompt
    {
        get
        {
            if (state == CustomerState.WaitingPickupAtPC)
                return inviteText;

            if (state == CustomerState.WaitingShootDone)
                return finishText;

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
        if (!CanInteract(interactor))
            return;

        Interact();
    }

    private void Awake()
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

    private void Update()
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

            if (StudioManager.Instance != null)
                StudioManager.Instance.SetCurrentCustomer(this);

            hasPhotoTaken = false;

            NotifyReady(true);
            StartListeningPhotoCaptured();
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPrintAtPC;

            // Printing is only allowed after the session has ended
            // and the customer has returned to the PC side.
            if (StudioManager.Instance != null)
                StudioManager.Instance.UnlockPrinting();
        }

        LookAtPlayer();
    }

    private void NotifyReady(bool ready)
    {
        if (notifiedReady == ready)
            return;

        notifiedReady = ready;

        if (StudioManager.Instance != null)
            StudioManager.Instance.NotifyCustomerReady(ready);
    }

    private void StartListeningPhotoCaptured()
    {
        if (listeningPhotoEvent)
            return;

        StudioManager.OnPhotoCaptured += OnPhotoCaptured;
        listeningPhotoEvent = true;
    }

    private void StopListeningPhotoCaptured()
    {
        if (!listeningPhotoEvent)
            return;

        StudioManager.OnPhotoCaptured -= OnPhotoCaptured;
        listeningPhotoEvent = false;
    }

    private void OnPhotoCaptured()
    {
        if (state != CustomerState.WaitingShootDone)
            return;

        hasPhotoTaken = true;
    }

    private void GenerateOrder()
    {
        if (orderService == null)
        {
            currentOrder = new PhotoOrder
            {
                quantity = 1,
                size = PhotoSize.Size3x4
            };
            return;
        }

        currentOrder = orderService.Generate();
    }

    private void SpawnPopupForOrder()
    {
        if (popupPrefab == null || popupAnchor == null)
            return;

        if (popupInstance != null)
            Destroy(popupInstance.gameObject);

        popupInstance = Instantiate(
            popupPrefab,
            popupAnchor.position,
            Quaternion.identity,
            popupAnchor
        );

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
            if (!hasPhotoTaken)
                return;

            NotifyReady(false);
            StopListeningPhotoCaptured();

            // Session ended, but printing is still locked
            // until the customer reaches the PC side again.
            if (StudioManager.Instance != null)
                StudioManager.Instance.LockPrinting();

            state = CustomerState.ReturningToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
        }
    }

    public void OnPhotosDelivered()
    {
        if (state != CustomerState.WaitingPrintAtPC)
            return;

        state = CustomerState.Completed;

        StopListeningPhotoCaptured();
        NotifyReady(false);

        if (StudioManager.Instance != null)
        {
            StudioManager.Instance.ClearCurrentCustomer();
            StudioManager.Instance.ClearCurrentPrintPhotoData();
        }

        Destroy(gameObject);
    }

    public PhotoOrder GetCurrentOrder()
    {
        return currentOrder;
    }

    private bool IsMainDoorClosed()
    {
        if (mainDoorBlocker == null)
            return false;

        return mainDoorBlocker.enabled;
    }

    private bool ReachedDestination()
    {
        if (agent == null)
            return false;

        if (agent.pathPending)
            return false;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance;
    }

    private void LookAtPlayer()
    {
        if (!lookAtPlayerWhenWaiting)
            return;

        if (player == null)
            return;

        bool shouldLook =
            state == CustomerState.WaitingPickupAtPC ||
            state == CustomerState.WaitingShootDone ||
            state == CustomerState.WaitingPrintAtPC ||
            state == CustomerState.GoingToPhotoSpot;

        if (!shouldLook)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            lookRotateSpeed * Time.deltaTime
        );
    }

    public void ReceivePrintedPhoto(PrintPhotoData printData)
    {
        if (printData == null)
            return;

        string photoId = "NULL";

        if (printData.PhotoRecord != null)
            photoId = printData.PhotoRecord.id;

        Debug.Log(
            $"{name} received printed photo | " +
            $"PhotoId: {photoId} | " +
            $"Size: {printData.PrintSize} | " +
            $"Copies: {printData.CopyCount}"
        );

        OnPhotosDelivered();
    }
}