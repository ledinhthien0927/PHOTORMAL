using UnityEngine;

public class Test : MonoBehaviour
{
    private void Update()
    {
        // Bấm P = chụp ảnh
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Test: Shoot Photo");
            GameEventAPI.OnPlayerShootPhoto?.Invoke();
        }

        // Bấm L = bật/tắt đèn phòng khách
        if (Input.GetKeyDown(KeyCode.L))
        {
            bool newState = !RuleContext.Instance.IsLivingRoomLightOn;
            GameEventAPI.OnLivingRoomLightToggled?.Invoke(newState);
            Debug.Log("Test: Toggle Living Room Light: " + newState);
        }

        // Bấm B = mở/khóa cửa sau
        if (Input.GetKeyDown(KeyCode.B))
        {
            bool newState = !RuleContext.Instance.IsBackDoorLocked;
            GameEventAPI.OnBackDoorStateChanged?.Invoke(newState);
            Debug.Log("Test: Back Door Lock: " + newState);
        }

        // Bấm F = giả lập đèn flicker
        if (Input.GetKeyDown(KeyCode.F))
        {
            EventManager.Instance.TriggerEvent("Flicker");
        }

        // Bấm T = giả lập tiếng bước chân
        if (Input.GetKeyDown(KeyCode.T))
        {
            RuleContext.Instance.IsFootstepActive = true;
            Debug.Log("Test: Footstep Started");
        }

        // Bấm Y = vào toilet
        if (Input.GetKeyDown(KeyCode.Y))
        {
            GameEventAPI.OnPlayerToiletStateChanged?.Invoke(true);
        }

        // Bấm U = ra toilet
        if (Input.GetKeyDown(KeyCode.U))
        {
            GameEventAPI.OnPlayerToiletStateChanged?.Invoke(false);
        }
    }
}
