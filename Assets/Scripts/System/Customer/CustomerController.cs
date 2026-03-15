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

    private enum GateMovePhase
    {
        None,
        EnteringDirectToPC,
        MovingToOutside,
        WaitingOutsideDoorOpen,
        EnteringFromOutsideToPC,
        ExitingDirectToExit,
        MovingToInside,
        WaitingInsideDoorOpen,
        ExitingFromInsideToExit
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
    private Transform outsidePoint;
    private Transform insidePoint;
    private Collider mainDoorBlocker;

    private CustomerState state;
    private GateMovePhase gateMovePhase;
    private OrderPopupUI popupInstance;

    private PhotoOrderService orderService;
    private Transform player;
    private PhotoOrder currentOrder;

    private bool notifiedReady;
    private bool hasPhotoTaken;
    private bool listeningPhotoEvent;

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
        Transform exitPointTransform,
        Transform outsidePointTransform,
        Transform insidePointTransform)
    {
        Debug.Log($"[CustomerController] {name} Init called. isClown: {isClown}");

        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;
        photoSpot = photoSpotPoint;
        exitPoint = exitPointTransform;
        outsidePoint = outsidePointTransform;
        insidePoint = insidePointTransform;

        state = CustomerState.WaitingDoorCheck;
        gateMovePhase = GateMovePhase.None;

        if (isClown)
        {
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

        if (standByPC != null && agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning($"[CustomerController] {name} is not on NavMesh! Attempting to warp.");
                agent.Warp(transform.position);
            }

            BeginEnterFlow();
        }
        else
        {
            if (standByPC == null) Debug.LogError($"[CustomerController] {name} standByPC is NULL!");
            if (agent == null) Debug.LogError($"[CustomerController] {name} NavMeshAgent is NULL!");
        }
    }

    private void Update()
    {
        UpdateGateMovement();

        if (state == CustomerState.GoingToPhotoSpot && ReachedDestination())
        {
            state = CustomerState.WaitingShootDone;
            StopMovement();

            if (StudioManager.Instance != null)
                StudioManager.Instance.SetCurrentCustomer(this);

            hasPhotoTaken = false;
            NotifyReady(true);
            StartListeningPhotoCaptured();
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            state = CustomerState.WaitingPrintAtPC;
            StopMovement();

            if (StudioManager.Instance != null)
                StudioManager.Instance.UnlockPrinting();
        }

        LookAtPlayer();
    }

    private void UpdateGateMovement()
    {
        if (state == CustomerState.GoingToPC)
        {
            UpdateEnterFlow();
            return;
        }

        if (state == CustomerState.GoingToExit)
        {
            UpdateExitFlow();
        }
    }

    private void BeginEnterFlow()
    {
        state = CustomerState.GoingToPC;

        if (IsMainDoorClosed() && outsidePoint != null)
        {
            MoveToOutsidePoint();
            return;
        }

        gateMovePhase = GateMovePhase.EnteringDirectToPC;
        MoveAgentTo(standByPC.position);
    }

    private void UpdateEnterFlow()
    {
        if (gateMovePhase == GateMovePhase.EnteringDirectToPC)
        {
            if (IsMainDoorClosed() && outsidePoint != null)
            {
                MoveToOutsidePoint();
                return;
            }

            if (ReachedDestination())
            {
                ArriveAtPC();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.MovingToOutside)
        {
            if (!ReachedDestination())
                return;

            StopMovement();

            if (IsMainDoorClosed())
            {
                gateMovePhase = GateMovePhase.WaitingOutsideDoorOpen;
                state = CustomerState.WaitingDoorOpen;
            }
            else
            {
                StartMoveFromOutsideToPC();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.WaitingOutsideDoorOpen)
        {
            if (!IsMainDoorClosed())
            {
                StartMoveFromOutsideToPC();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.EnteringFromOutsideToPC)
        {
            if (ReachedDestination())
            {
                ArriveAtPC();
            }
        }
    }

    private void BeginExitFlow()
    {
        StopListeningPhotoCaptured();
        NotifyReady(false);

        if (StudioManager.Instance != null)
            StudioManager.Instance.ClearCurrentCustomer();

        if (popupInstance != null)
            popupInstance.Hide();

        if (exitPoint == null)
        {
            FinishAndDestroy();
            return;
        }

        state = CustomerState.GoingToExit;

        if (IsMainDoorClosed() && insidePoint != null)
        {
            MoveToInsidePoint();
            return;
        }

        gateMovePhase = GateMovePhase.ExitingDirectToExit;
        MoveAgentTo(exitPoint.position);
    }

    private void UpdateExitFlow()
    {
        if (gateMovePhase == GateMovePhase.ExitingDirectToExit)
        {
            if (IsMainDoorClosed() && insidePoint != null)
            {
                MoveToInsidePoint();
                return;
            }

            if (ReachedDestination())
            {
                StopMovement();
                FinishAndDestroy();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.MovingToInside)
        {
            if (!ReachedDestination())
                return;

            StopMovement();

            if (IsMainDoorClosed())
            {
                gateMovePhase = GateMovePhase.WaitingInsideDoorOpen;
                state = CustomerState.WaitingExitDoorOpen;
            }
            else
            {
                StartMoveFromInsideToExit();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.WaitingInsideDoorOpen)
        {
            if (!IsMainDoorClosed())
            {
                StartMoveFromInsideToExit();
            }

            return;
        }

        if (gateMovePhase == GateMovePhase.ExitingFromInsideToExit)
        {
            if (ReachedDestination())
            {
                StopMovement();
                FinishAndDestroy();
            }
        }
    }

    private void MoveToOutsidePoint()
    {
        if (outsidePoint == null)
        {
            gateMovePhase = GateMovePhase.EnteringDirectToPC;
            MoveAgentTo(standByPC.position);
            return;
        }

        gateMovePhase = GateMovePhase.MovingToOutside;
        state = CustomerState.GoingToPC;
        MoveAgentTo(outsidePoint.position);
    }

    private void StartMoveFromOutsideToPC()
    {
        gateMovePhase = GateMovePhase.EnteringFromOutsideToPC;
        state = CustomerState.GoingToPC;
        MoveAgentTo(standByPC.position);
    }

    private void MoveToInsidePoint()
    {
        if (insidePoint == null)
        {
            gateMovePhase = GateMovePhase.ExitingDirectToExit;
            MoveAgentTo(exitPoint.position);
            return;
        }

        gateMovePhase = GateMovePhase.MovingToInside;
        state = CustomerState.GoingToExit;
        MoveAgentTo(insidePoint.position);
    }

    private void StartMoveFromInsideToExit()
    {
        gateMovePhase = GateMovePhase.ExitingFromInsideToExit;
        state = CustomerState.GoingToExit;
        MoveAgentTo(exitPoint.position);
    }

    private void ArriveAtPC()
    {
        state = CustomerState.WaitingPickupAtPC;
        gateMovePhase = GateMovePhase.None;
        StopMovement();

        GenerateOrder();
        SpawnPopupForOrder();
    }

    private void MoveAgentTo(Vector3 destination)
    {
        if (agent == null)
            return;

        agent.isStopped = false;
        agent.SetDestination(destination);

        if (customerAnimator != null)
            customerAnimator.SetWalking(true);
    }

    private void StopMovement()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (customerAnimator != null)
            customerAnimator.SetWalking(false);
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
            gateMovePhase = GateMovePhase.None;
            MoveAgentTo(photoSpot.position);

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
            gateMovePhase = GateMovePhase.None;
            MoveAgentTo(standByPC.position);
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

    private void FinishAndDestroy(bool triggerEvent = true)
    {
        if (state == CustomerState.Completed)
            return;

        state = CustomerState.Completed;
        gateMovePhase = GateMovePhase.None;

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

        if (!agent.isOnNavMesh)
            return false;

        if (agent.pathPending)
            return false;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return false;

        if (agent.isStopped)
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
