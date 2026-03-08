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
            Debug.Log("Test: Mở cửa cho Tên Hề (Open door for clown)");
            GameEventAPI.OnPlayerOpenedDoorForClown?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("Test: Nhận hàng giao đến (Pick up delivery)");
            GameEventAPI.OnPlayerPickUpDelivery?.Invoke();
        }
        
        if (Input.GetKeyDown(KeyCode.LeftBracket)) // '['
        {
            Debug.Log("Test: Enter Studio");
            GameEventAPI.OnPlayerStudioStateChanged?.Invoke(true);
        }

        if (Input.GetKeyDown(KeyCode.RightBracket)) // ']'
        {
            Debug.Log("Test: Exit Studio");
            GameEventAPI.OnPlayerStudioStateChanged?.Invoke(false);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Test: Call Support");
            GameEventAPI.OnCallSupportClicked?.Invoke();
        }

        // ================= RESOURCE & PROGRESS =================
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Test: Add Money 100");
            GameEventAPI.OnAddMoney?.Invoke(100);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("Test: Spend Money 100");
            GameEventAPI.OnSpendMoney?.Invoke(100);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Test: Force Add Error");
            if (GameProgress.Instance != null)
                GameProgress.Instance.AddError();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Test: Skip to Next Night");
            if (GameProgress.Instance != null && NightManager.Instance != null)
            {
                GameProgress.Instance.NextNight();
                NightManager.Instance.StartNight(GameProgress.Instance.CurrentNight);
            }
        }

        // ================= SYSTEM EVENTS =================
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Test: Kích hoạt nháy đèn (FlickerLight)");
            EventManager.Instance.TriggerEvent("FlickerLight");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Test: Kích hoạt sự kiện Sinh Đôi (Twins)");
            EventManager.Instance.TriggerEvent("Twins");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Test: Kích hoạt tiếng bước chân (Footstep - Loop 15s, 5s Grace)");
            EventManager.Instance.TriggerEvent("Footstep");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Test: Kích hoạt sự kiện Tên Hề (Clown)");
            EventManager.Instance.TriggerEvent("Clown");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Test: Kích hoạt sự kiện Giao Hàng (Delivery)");
            EventManager.Instance.TriggerEvent("Delivery");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("Test: Not Enough Money Event");
            EventManager.Instance.TriggerEvent("NotEnoughMoney");
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Debug.Log("Test: Instance Game Over Event (7)");
            EventManager.Instance.TriggerEvent("InstantGameOver");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.Log("Test: Show Warning UI Event (8)");
            EventManager.Instance.TriggerEvent("ShowWarningUI");
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Debug.Log("Test: Rule Broken Effect Event (9)");
            EventManager.Instance.TriggerEvent("RuleBrokenEffect");
        }

        // ================= CUSTOMER QUEUE =================
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Test: Force Spawn Normal Customer (0)");
            if (CustomerQueueManager.Instance != null)
                CustomerQueueManager.Instance.ForceSpawnNormal();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Test: Debug Dump Customer Queue (T)");
            if (CustomerQueueManager.Instance != null)
                CustomerQueueManager.Instance.DebugDumpQueue();
        }
    }
}
