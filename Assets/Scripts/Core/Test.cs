using UnityEngine;

public class Test : MonoBehaviour
{
    private void Update()
    {
        // ================= PLAYER ACTIONS =================
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Test: Shoot Photo");
            GameEventAPI.OnPlayerShootPhoto?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            bool newState = !RuleContext.Instance.IsLivingRoomLightOn;
            GameEventAPI.OnLivingRoomLightToggled?.Invoke(newState);
            Debug.Log("Test: Toggle Living Room Light: " + newState);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            bool newState = !RuleContext.Instance.IsBackDoorLocked;
            GameEventAPI.OnBackDoorStateChanged?.Invoke(newState);
            Debug.Log("Test: Back Door Lock: " + newState);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("Test: Enter Toilet");
            GameEventAPI.OnPlayerToiletStateChanged?.Invoke(true);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("Test: Exit Toilet");
            GameEventAPI.OnPlayerToiletStateChanged?.Invoke(false);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log("Test: Mở cửa cho Tên Hề");
            GameEventAPI.OnPlayerOpenedDoorForClown?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Test: Nhận hàng giao đến");
            GameEventAPI.OnPlayerPickUpDelivery?.Invoke();
        }

        // ================= SYSTEM EVENTS =================
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Test: Kích hoạt nháy đèn");
            EventManager.Instance.TriggerEvent("FlickerLight");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Test: Kích hoạt sự kiện Sinh Đôi");
            EventManager.Instance.TriggerEvent("Twins");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Test: Kích hoạt tiếng bước chân");
            EventManager.Instance.TriggerEvent("Footstep");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Test: Kích hoạt sự kiện Tên Hề");
            EventManager.Instance.TriggerEvent("Clown");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Test: Kích hoạt sự kiện Giao Hàng");
            EventManager.Instance.TriggerEvent("Delivery");
        }
    }
}
