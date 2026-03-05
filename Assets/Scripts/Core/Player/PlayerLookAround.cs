using UnityEngine;

public class PlayerLookAround : MonoBehaviour
{
    public float sensitivity = 0.1f;

    public FixedTouchField touchField;   // vùng look UI
    public Transform cameraPivot;        // camera hoặc empty object chứa camera

    private float xRotation = 0f;

    void Update()
    {
        Look();
    }

    void Look()
    {
        Vector2 touchDelta = touchField.TouchDist;

        float lookX = touchDelta.x * sensitivity;
        float lookY = touchDelta.y * sensitivity;

        // Xoay trái phải (Player body)
        transform.Rotate(Vector3.up * lookX);

        // Xoay lên xuống (Camera)
        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}