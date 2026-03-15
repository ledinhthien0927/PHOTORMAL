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
        WaitingExitDoorOpen,
        GoingToExit,
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
    [SerializeField] private string deliverText = "Give printed photo";

    private NavMeshAgent agent;
    private CustomerAnimator customerAnimator;

    [Header("Special Identity")]
    public bool isClown = false;

    private Transform standByPC;
    private Transform photoSpot;
    private Transform exitPoint;
    private Collider mainDoorBlocker;

    private CustomerState state;
    private OrderPopupUI popupInstance;

    private PhotoOrderService orderService;
    private Transform player;
    private PhotoOrder currentOrder;

    private bool notifiedReady;
    private bool hasPhotoTaken;
    private bool listeningPhotoEvent;
    private bool waitingForExitDoorRoutine;

    private static readonly string[] paymentMessages =
{
    "Perfect! Here is your payment:",
    "Great job! Here's your payment:",
    "Looks amazing! Your payment:",
    "Excellent work! You earned:",
    "Exactly what I wanted! Here you go:",
    "Fantastic print! Your reward:",
    "That's perfect! Here's your money:",
    "Nice work! Payment received:"
};

    public string Prompt
    {
        get
        {
            if (state == CustomerState.WaitingPickupAtPC)
                return inviteText;

            if (state == CustomerState.WaitingShootDone)
                return finishText;

            if (state == CustomerState.WaitingPrintAtPC)
                return deliverText;

            return string.Empty;
        }
    }

    public bool CanInteract(IInteractor interactor)
    {
        return state == CustomerState.WaitingPickupAtPC
            || state == CustomerState.WaitingShootDone
            || state == CustomerState.WaitingPrintAtPC;
    }

    public void Interact(IInteractor interactor)
    {
        if (!CanInteract(interactor))
            return;

        if (state == CustomerState.WaitingPrintAtPC)
        {
            TryReceivePrintedPhotoFrom(interactor);
            return;
        }

        Interact();
    }

    private void OnEnable()
    {
        GameEventAPI.OnClownDisappeared += HandleClownDisappeared;
    }

    private void OnDisable()
    {
        GameEventAPI.OnClownDisappeared -= HandleClownDisappeared;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
        
        customerAnimator = GetComponent<CustomerAnimator>();
        if (customerAnimator == null) customerAnimator = GetComponentInChildren<CustomerAnimator>();
    }

    private void OnDestroy()
    {
        StopListeningPhotoCaptured();
        NotifyReady(false);
    }

    public void Init(
        Transform standByPcPoint,
        Collider mainDoorBlockerCollider,
        Transform photoSpotPoint,
        Transform exitPointTransform
    )
    {
        Debug.Log($"[CustomerController] {name} Init called. isClown: {isClown}");
        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;
        photoSpot = photoSpotPoint;
        exitPoint = exitPointTransform;

        state = CustomerState.WaitingDoorCheck;

        if (isClown)
        {
            // Unified Clown Logic: Trigger event immediately on spawn
            // This starts the countdown and sounds
            RuleContext.Instance.IsClownAppeared = true;
            if (RuleManager.Instance != null) RuleManager.Instance.ResetClownTimer();
            GameEventAPI.OnClownAppeared?.Invoke();
            Debug.Log($"[CustomerController] {name} is a Clown! Unified Event triggered on spawn.");
        }

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
        Debug.Log($"[CustomerController] {name} FlowRoutine started. Delay: {firstCheckDelay}");
        yield return new WaitForSeconds(firstCheckDelay);

        state = CustomerState.WaitingDoorOpen;
        Debug.Log($"[CustomerController] {name} state: WaitingDoorOpen. Checking door block...");

        while (IsMainDoorClosed())
            yield return new WaitForSeconds(recheckInterval);

        Debug.Log($"[CustomerController] {name} door is open! Preparing to move to PC.");

        if (standByPC != null && agent != null)
        {
            state = CustomerState.GoingToPC;
            
            // Ensure agent is on NavMesh
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"[CustomerController] {name} is not on NavMesh! Attempting to warp.");
                agent.Warp(transform.position);
            }

            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
            if (customerAnimator != null) customerAnimator.SetWalking(true);
            
            Debug.Log($"[CustomerController] {name} destination set to {standByPC.position}. Agent speed: {agent.speed}");
        }
        else
        {
            if (standByPC == null) Debug.LogError($"[CustomerController] {name} standByPC is NULL!");
            if (agent == null) Debug.LogError($"[CustomerController] {name} NavMeshAgent is NULL!");
        }
    }

    private void Update()
    {
        if (state == CustomerState.GoingToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPickupAtPC;
            if (customerAnimator != null) customerAnimator.SetWalking(false);

            GenerateOrder();
            SpawnPopupForOrder();
        }
        else if (state == CustomerState.GoingToPhotoSpot && ReachedDestination())
        {
            state = CustomerState.WaitingShootDone;
            if (customerAnimator != null) customerAnimator.SetWalking(false);

            if (StudioManager.Instance != null)
                StudioManager.Instance.SetCurrentCustomer(this);

            hasPhotoTaken = false;

            NotifyReady(true);
            StartListeningPhotoCaptured();
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPrintAtPC;
            if (customerAnimator != null) customerAnimator.SetWalking(false);

            if (StudioManager.Instance != null)
                StudioManager.Instance.UnlockPrinting();
        }
        else if (state == CustomerState.GoingToExit && ReachedDestination())
        {
            if (customerAnimator != null) customerAnimator.SetWalking(false);
            FinishAndDestroy();
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
            if (customerAnimator != null) customerAnimator.SetWalking(true);

            GameEventAPI.OnCustomerInvitedToStudio?.Invoke();
            return;
        }

        if (state == CustomerState.WaitingShootDone)
        {
            if (!hasPhotoTaken)
                return;

            NotifyReady(false);
            StopListeningPhotoCaptured();

            if (StudioManager.Instance != null)
                StudioManager.Instance.LockPrinting();

            state = CustomerState.ReturningToPC;
            agent.isStopped = false;
            agent.SetDestination(standByPC.position);
            if (customerAnimator != null) customerAnimator.SetWalking(true);
        }
    }

    private void TryReceivePrintedPhotoFrom(IInteractor interactor)
    {
        if (interactor == null || interactor.Inventory == null || !interactor.Inventory.HasItem)
        {
            PlayerMessageUI.Instance?.ShowMessage("Please bring me the printed photo.");
            return;
        }

        GameObject heldObject = interactor.Inventory.CurrentObject;
        if (heldObject == null)
        {
            PlayerMessageUI.Instance?.ShowMessage("Please bring me the printed photo.");
            return;
        }

        PrintedPhotoPickup printedPhoto = heldObject.GetComponent<PrintedPhotoPickup>();
        if (printedPhoto == null || !printedPhoto.IsValid())
        {
            PlayerMessageUI.Instance?.ShowMessage("This is not the printed photo.");
            return;
        }

        PrintPhotoData deliveredData = printedPhoto.Data;
        bool isCorrect = IsDeliveredPhotoCorrect(deliveredData);

        if (!isCorrect)
        {
            PlayerMessageUI.Instance?.ShowMessage(
                "You printed the wrong order. Please throw it in the trash.",
                3f
            );
            return;
        }

        GameEventAPI.OnCustomerReceivedCorrectPhoto?.Invoke();

        int reward = 15 * currentOrder.quantity;
        if (GameProgress.Instance != null)
            GameProgress.Instance.AddMoney(reward);

        if (StudioManager.Instance != null && StudioManager.Instance.PhotoData != null)
        {
            PhotoRecord record = GetExpectedPhotoRecord();
            if (record != null)
                StudioManager.Instance.PhotoData.RemovePhoto(record);
        }

        if (StudioManager.Instance != null)
            StudioManager.Instance.ClearCurrentPrintPhotoData();

        printedPhoto.Consume();
        string message = paymentMessages[Random.Range(0, paymentMessages.Length)];

        PlayerMessageUI.Instance?.ShowMessage(
            $"{message} +${reward}",
            3f
        );
        BeginExitFlow();
    }

    private bool IsDeliveredPhotoCorrect(PrintPhotoData deliveredData)
    {
        if (deliveredData == null)
            return false;

        if (!deliveredData.MatchesOrder(currentOrder))
            return false;

        PhotoRecord expectedRecord = GetExpectedPhotoRecord();
        if (expectedRecord == null)
            return false;

        return deliveredData.MatchesPhotoRecord(expectedRecord);
    }

    private PhotoRecord GetExpectedPhotoRecord()
    {
        if (StudioManager.Instance == null)
            return null;

        PrintPhotoData expectedData = StudioManager.Instance.CurrentPrintPhotoData;
        if (expectedData == null)
            return null;

        return expectedData.PhotoRecord;
    }

    public void OnPhotosDelivered()
    {
        if (state != CustomerState.WaitingPrintAtPC)
            return;

        BeginExitFlow();
    }

    public void LeaveBecauseOfPrintMistakes()
    {
        if (state != CustomerState.WaitingPrintAtPC)
            return;

        Debug.Log($"{name} left because of too many wrong print attempts.");
        BeginExitFlow();
    }

    private void BeginExitFlow()
    {
        StopListeningPhotoCaptured();
        NotifyReady(false);

        if (StudioManager.Instance != null)
        {
            StudioManager.Instance.ClearCurrentCustomer();
        }

        if (popupInstance != null)
            popupInstance.Hide();

        if (exitPoint == null)
        {
            FinishAndDestroy();
            return;
        }

        state = CustomerState.WaitingExitDoorOpen;
        agent.isStopped = true;

        if (!waitingForExitDoorRoutine)
            StartCoroutine(WaitForExitDoorAndLeaveRoutine());
    }

    private IEnumerator WaitForExitDoorAndLeaveRoutine()
    {
        waitingForExitDoorRoutine = true;

        while (state == CustomerState.WaitingExitDoorOpen && IsMainDoorClosed())
            yield return new WaitForSeconds(recheckInterval);

        waitingForExitDoorRoutine = false;

        if (state != CustomerState.WaitingExitDoorOpen)
            yield break;

        state = CustomerState.GoingToExit;
        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);
        if (customerAnimator != null) customerAnimator.SetWalking(true);
    }

    private void FinishAndDestroy(bool triggerEvent = true)
    {
        if (state == CustomerState.Completed)
            return;

        state = CustomerState.Completed;

        if (triggerEvent)
            GameEventAPI.OnCustomerCompleted?.Invoke();

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
            state == CustomerState.WaitingPrintAtPC;

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

        Debug.Log($"{name} received printed photo directly.");
    }

    private void HandleClownDisappeared()
    {
        if (isClown)
        {
            Debug.Log($"[CustomerController] {name} (Clown) heard OnClownDisappeared. Vanishing!");
            FinishAndDestroy();
        }
    }
}