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
        GoingToOutside,
        WaitingOutsideDoorOpen,
        GoingToPC,
        WaitingPickupAtPC,
        GoingToPhotoSpot,
        WaitingShootDone,
        ReturningToPC,
        WaitingPrintAtPC,
        GoingToInside,
        WaitingInsideDoorOpen,
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

    [Header("Special Identity")]
    public bool isClown = false;
    public bool willTriggerFlicker = false;

    private NavMeshAgent agent;
    private CustomerAnimator customerAnimator;

    private Transform standByPC;
    private Transform photoSpot;
    private Transform exitPoint;
    private Transform outsidePoint;
    private Transform insidePoint;
    private Collider mainDoorBlocker;

    private CustomerState state;
    private OrderPopupUI popupInstance;

    private PhotoOrderService orderService;
    private Transform player;
    private PhotoOrder currentOrder;

    private bool notifiedReady;
    private bool hasPhotoTaken;
    private bool listeningPhotoEvent;
    private bool isInvitePromptVisible;
    
    [Header("Save/Restore")]
    public string prefabName = ""; // Tên prefab gốc, dùng để khôi phục đúng model

    private Coroutine enterFlowRoutine;
    private Coroutine exitFlowRoutine;

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
        Transform insidePointTransform
    )
    {
        Debug.Log($"[CustomerController] {name} Init called. isClown: {isClown}");

        standByPC = standByPcPoint;
        mainDoorBlocker = mainDoorBlockerCollider;
        photoSpot = photoSpotPoint;
        exitPoint = exitPointTransform;
        outsidePoint = outsidePointTransform;
        insidePoint = insidePointTransform;

        state = CustomerState.WaitingDoorCheck;

        if (isClown)
        {
            RuleContext.Instance.IsClownAppeared = true;
            if (RuleManager.Instance != null) RuleManager.Instance.ResetClownTimer();
            GameEventAPI.OnClownAppeared?.Invoke();
            Debug.Log($"[CustomerController] {name} is a Clown! Unified Event triggered on spawn.");
        }

        if (enterFlowRoutine != null)
            StopCoroutine(enterFlowRoutine);

        enterFlowRoutine = StartCoroutine(EnterFlowRoutine());
    }

    public void SetOrderService(PhotoOrderService service)
    {
        orderService = service;
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void RestoreState(PhotoOrder order, CustomerState savedState, bool flick, bool photoTaken, Vector3 pos, float rotY)
    {
        // Quan trọng: Dừng tất cả coroutine đang chạy (do Init() đã khởi động EnterFlowRoutine)
        StopAllCoroutines();
        enterFlowRoutine = null;
        exitFlowRoutine = null;

        this.currentOrder = order;
        this.state = savedState;
        this.willTriggerFlicker = flick;
        this.hasPhotoTaken = photoTaken;

        EnsureAgentOnNavMesh();
        agent.Warp(pos);
        transform.rotation = Quaternion.Euler(0, rotY, 0);

        Debug.Log($"[CustomerController] Restored to state: {state} at {pos}");

        // Resume routines
        if (state >= CustomerState.GoingToInside)
        {
            exitFlowRoutine = StartCoroutine(ExitFlowRoutine());
        }
        else if (state < CustomerState.WaitingPickupAtPC)
        {
            enterFlowRoutine = StartCoroutine(EnterFlowRoutine());
        }

        // Re-setup interaction logic
        if (state == CustomerState.WaitingPickupAtPC)
        {
            SpawnPopupForOrder();
        }
        else if (state == CustomerState.WaitingShootDone)
        {
            if (StudioManager.Instance != null)
                StudioManager.Instance.SetCurrentCustomer(this);
            NotifyReady(true);
            StartListeningPhotoCaptured();
        }
        else if (state == CustomerState.ReturningToPC || state == CustomerState.WaitingPrintAtPC)
        {
            if (StudioManager.Instance != null)
                StudioManager.Instance.UnlockPrinting();
            
            if (state == CustomerState.WaitingPrintAtPC)
                ShowOrderPopupAgain();
        }
        
        // Resume travel destinations if applicable
        if (state == CustomerState.GoingToOutside) MoveTo(outsidePoint.position);
        if (state == CustomerState.GoingToPC) MoveTo(standByPC.position);
        if (state == CustomerState.GoingToPhotoSpot) MoveTo(photoSpot.position);
        if (state == CustomerState.ReturningToPC) MoveTo(standByPC.position);
        if (state == CustomerState.GoingToInside) MoveTo(insidePoint.position);
        if (state == CustomerState.GoingToExit) MoveTo(exitPoint.position);
    }

    private IEnumerator EnterFlowRoutine()
    {
        if (state == CustomerState.WaitingDoorCheck)
        {
            yield return new WaitForSeconds(firstCheckDelay);
            state = CustomerState.WaitingDoorOpen;
        }

        if (agent == null || standByPC == null || outsidePoint == null)
            yield break;

        EnsureAgentOnNavMesh();

        if (state == CustomerState.WaitingDoorOpen)
        {
            while (IsMainDoorClosed())
                yield return new WaitForSeconds(recheckInterval);

            MoveTo(outsidePoint.position);
            state = CustomerState.GoingToOutside;
        }

        if (state == CustomerState.GoingToOutside)
        {
            while (state == CustomerState.GoingToOutside)
            {
                if (ReachedDestination())
                {
                    StopMovement();
                    state = CustomerState.WaitingOutsideDoorOpen;
                    break;
                }
                yield return null;
            }
        }

        if (state == CustomerState.WaitingOutsideDoorOpen)
        {
            while (state == CustomerState.WaitingOutsideDoorOpen && IsMainDoorClosed())
                yield return new WaitForSeconds(recheckInterval);

            if (state != CustomerState.WaitingOutsideDoorOpen)
                yield break;

            MoveTo(standByPC.position);
            state = CustomerState.GoingToPC;
        }
    }

    private IEnumerator ExitFlowRoutine()
    {
        if (agent == null || exitPoint == null || insidePoint == null)
            yield break;

        EnsureAgentOnNavMesh();

        if (state < CustomerState.GoingToInside)
        {
            MoveTo(insidePoint.position);
            state = CustomerState.GoingToInside;
        }

        if (state == CustomerState.GoingToInside)
        {
            while (state == CustomerState.GoingToInside)
            {
                if (ReachedDestination())
                {
                    StopMovement();
                    state = CustomerState.WaitingInsideDoorOpen;
                    break;
                }
                yield return null;
            }
        }

        if (state == CustomerState.WaitingInsideDoorOpen)
        {
            while (state == CustomerState.WaitingInsideDoorOpen && IsMainDoorClosed())
                yield return new WaitForSeconds(recheckInterval);

            if (state != CustomerState.WaitingInsideDoorOpen)
                yield break;

            MoveTo(exitPoint.position);
            state = CustomerState.GoingToExit;
        }
    }

    private void Update()
    {
        if (state == CustomerState.GoingToPC && ReachedDestination())
        {
            StopMovement();
            state = CustomerState.WaitingPickupAtPC;

            GenerateOrder();
            SpawnPopupForOrder();
        }
        else if (state == CustomerState.GoingToPhotoSpot && ReachedDestination())
        {
            StopMovement();
            state = CustomerState.WaitingShootDone;

            if (StudioManager.Instance != null)
                StudioManager.Instance.SetCurrentCustomer(this);

            hasPhotoTaken = false;
            NotifyReady(true);
            StartListeningPhotoCaptured();

            if (willTriggerFlicker)
            {
                StartCoroutine(FlickerTimerRoutine());
            }
        }
        else if (state == CustomerState.ReturningToPC && ReachedDestination())
        {
            StopMovement();
            state = CustomerState.WaitingPrintAtPC;

            if (StudioManager.Instance != null)
                StudioManager.Instance.UnlockPrinting();
        }
        else if (state == CustomerState.GoingToExit && ReachedDestination())
        {
            StopMovement();
            FinishAndDestroy();
        }

        LookAtPlayer();
    }

    private void EnsureAgentOnNavMesh()
    {
        if (agent != null && !agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
        }
    }

    private void MoveTo(Vector3 target)
    {
        if (agent == null)
            return;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(target);

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

    private IEnumerator FlickerTimerRoutine()
    {
        float waitTime = Random.Range(1f, 3f);
        yield return new WaitForSeconds(waitTime);

        if (EventManager.Instance != null)
        {
            Debug.Log($"[CustomerController] {name} is triggering the FlickerLight event while waiting!");
            EventManager.Instance.TriggerEvent("FlickerLight");
        }
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

        isInvitePromptVisible = false;

        popupInstance.Show(currentOrder, () =>
        {
            Interact();
        });
    }

    public void SetInvitePromptVisible(bool visible)
    {
        if (state != CustomerState.WaitingPickupAtPC)
            return;

        if (popupInstance == null)
            return;

        if (isInvitePromptVisible == visible)
            return;

        isInvitePromptVisible = visible;

        if (visible)
        {
            popupInstance.Hide();
        }
        else
        {
            popupInstance.Show(currentOrder, () =>
            {
                Interact();
            });
        }
    }

    public void Interact()
    {
        if (state == CustomerState.WaitingPickupAtPC)
        {
            if (popupInstance != null)
                popupInstance.Hide();

            isInvitePromptVisible = false;

            state = CustomerState.GoingToPhotoSpot;
            MoveTo(photoSpot.position);

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
            MoveTo(standByPC.position);
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

            ShowOrderPopupAgain();
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
            StudioManager.Instance.ClearCurrentCustomer();

        if (popupInstance != null)
            popupInstance.Hide();

        isInvitePromptVisible = false;

        if (exitPoint == null)
        {
            FinishAndDestroy();
            return;
        }

        if (exitFlowRoutine != null)
            StopCoroutine(exitFlowRoutine);

        exitFlowRoutine = StartCoroutine(ExitFlowRoutine());
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

    public CustomerState State => state;
    public bool HasPhotoTaken => hasPhotoTaken;
    public bool WillTriggerFlicker => willTriggerFlicker;

    private void HandleClownDisappeared()
    {
        if (isClown)
        {
            Debug.Log($"[CustomerController] {name} (Clown) heard OnClownDisappeared. Vanishing!");
            FinishAndDestroy();
        }
    }

    private void ShowOrderPopupAgain()
    {
        if (popupPrefab == null || popupAnchor == null)
            return;

        if (currentOrder.quantity <= 0)
            return;

        if (popupInstance == null)
        {
            popupInstance = Instantiate(
                popupPrefab,
                popupAnchor.position,
                Quaternion.identity,
                popupAnchor
            );
        }

        popupInstance.Show(currentOrder, null);
    }
}