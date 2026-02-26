using UnityEngine;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public FixedJoystick joystick;
    
    private void Update()
    {
        Vector2 input = joystick.Direction;

        // Lấy hướng theo player
        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        move.y = 0f; // tránh bị bay lên nếu camera nghiêng

        transform.position += move * speed * Time.deltaTime;
    }
}