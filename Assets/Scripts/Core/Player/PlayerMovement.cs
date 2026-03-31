using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementMobileSmooth : MonoBehaviour
{
    [Header("Input")]
    public FixedJoystick joystick; // Keep using the existing FixedJoystick system

    [Header("Movement")]
    public float moveSpeed = 4.5f;

    [Header("Gravity")]
    public float gravity = -25f;        // Stronger than -9.8 for more responsive falling
    public float groundStickForce = -2f; // Keeps player grounded to avoid small jitter

    [Header("Wall Sliding / Smoothing")]
    [Range(0f, 0.2f)]
    public float inputDeadZone = 0.08f; // Ignore small joystick noise to prevent jitter
    public bool normalizeDiagonal = true; // Prevent faster diagonal movement

    private CharacterController controller;
    private Vector3 verticalVelocity;

    public static PlayerMovementMobileSmooth Instance; // Added for SaveSystem

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
    }

    public void Teleport(Vector3 targetPos, float rotationY)
    {
        if (controller != null)
            controller.enabled = false; // Disable to allow manual transform set

        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(0, rotationY, 0);

        if (controller != null)
            controller.enabled = true;
        
        Debug.Log($"[PlayerMovement] Teleported to {targetPos}");
    }

    private void Start()
    {
        // Kiểm tra xem có data chờ load (từ Main Menu) không
        if (SaveSystem.pendingLoadData != null)
        {
            SaveData data = SaveSystem.pendingLoadData;

            // 1. Phục hồi vị trí
            Vector3 targetPos = new Vector3(data.pX, data.pY, data.pZ);
            Teleport(targetPos, data.rotY);

            // 2. Phục hồi tiền
            if (GameProgress.Instance != null)
            {
                GameProgress.Instance.SetMoney(data.money);
            }

            // Xóa data chờ sau khi đã apply xong
            SaveSystem.pendingLoadData = null;
            Debug.Log("[PlayerMovement] Restore current progress from pendingLoadData.");
        }
    }

    private void Update()
    {
        // 1. Read joystick input
        float inputX = joystick != null ? joystick.Horizontal : 0f;
        float inputZ = joystick != null ? joystick.Vertical : 0f;

        // 2. Apply dead zone to reduce jitter near walls
        if (Mathf.Abs(inputX) < inputDeadZone) inputX = 0f;
        if (Mathf.Abs(inputZ) < inputDeadZone) inputZ = 0f;

        // 3. Calculate movement direction based on player orientation
        Vector3 moveDirection = transform.right * inputX + transform.forward * inputZ;

        // 4. Normalize to prevent faster diagonal movement
        if (normalizeDiagonal && moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // 5. Apply gravity and ground sticking
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = groundStickForce;

        verticalVelocity.y += gravity * Time.deltaTime;

        // 6. Final movement vector
        Vector3 finalMove = moveDirection * moveSpeed + verticalVelocity;

        // CharacterController.Move handles collision and smooth wall sliding
        controller.Move(finalMove * Time.deltaTime);
    }
}