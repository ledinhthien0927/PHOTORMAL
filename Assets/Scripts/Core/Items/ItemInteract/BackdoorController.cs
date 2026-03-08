using UnityEngine;

[RequireComponent(typeof(DoorInteractable))]
public class BackdoorController : MonoBehaviour
{
    private DoorInteractable doorInteractable;

    private void Awake()
    {
        doorInteractable = GetComponent<DoorInteractable>();
    }

    private void OnEnable()
    {
        doorInteractable.OnDoorStateChanged += HandleDoorStateChanged;
    }

    private void OnDisable()
    {
        doorInteractable.OnDoorStateChanged -= HandleDoorStateChanged;
    }

    private void HandleDoorStateChanged(bool isOpen)
    {
        // Yêu cầu Đêm 1: Cửa sau phải khóa (lock/đóng). 
        // Trong hệ thống hiện tại, mở cửa (isOpen = true) đồng nghĩa với việc không khóa (BackDoorLocked = false).
        // Đóng cửa (isOpen = false) đồng nghĩa với việc đã khóa (BackDoorLocked = true).

        bool isLocked = !isOpen; 
        
        GameEventAPI.OnBackDoorStateChanged?.Invoke(isLocked);
    }
}
