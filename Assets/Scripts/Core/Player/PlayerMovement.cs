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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Recommended CharacterController settings for smoother wall sliding:
        // Skin Width: 0.03 - 0.05
        // Radius: 0.35 - 0.45 (depends on player scale)
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