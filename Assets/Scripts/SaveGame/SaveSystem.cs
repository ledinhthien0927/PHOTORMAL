using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public struct ObjectState
{
    public string name;
    public bool state; // true = Open/On, false = Closed/Off
}

[System.Serializable]
public class SaveData
{
    public int night = 1;
    public int money = 0;
    public int errors = 0; 

    // Printer Supplies
    public float currentPaper = 10f;
    public float currentInk = 10f;
    
    public bool isLivingRoomLightOn;
    public bool isBackDoorLocked;
    public bool isDeliveryWaiting;

    // Event Flags
    public bool isFootstepActive;
    public bool isClownAppeared;
    public bool hasTwinsAppeared;
    public bool isFlickering;

    // Customer Queue State
    public int currentCustomersAlive;
    public List<CustomerQueueManager.SpawnEntry> remainingQueue = new List<CustomerQueueManager.SpawnEntry>();

    // Environmental States
    public List<ObjectState> envStates = new List<ObjectState>();

    // Player Position
    public bool hasPosition = false; // Mới: Để biết có cần teleport không
    public float pX, pY, pZ;
    public float rotY;

    // Meta
    public string saveTime;
}

public static class SaveSystem
{
    private static readonly string FileName = "photormal_save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData pendingLoadData;

    public static void SaveGame(bool forceReset = false)
    {
        SaveData data = new SaveData();

        // 1. Get Night and Money from GameProgress
        if (GameProgress.Instance != null)
        {
            data.night = GameProgress.Instance.CurrentNight;
            data.money = forceReset ? 0 : GameProgress.Instance.CurrentMoney;
            data.errors = forceReset ? 0 : GameProgress.Instance.CurrentError;
        }

        // 2. Get Printer Supplies
        if (PrinterSupplyData.Instance != null && !forceReset)
        {
            data.currentPaper = PrinterSupplyData.Instance.CurrentPaper;
            data.currentInk = PrinterSupplyData.Instance.CurrentInk;
        }
        else
        {
            // Mặc định hoặc reset
            data.currentPaper = 10f;
            data.currentInk = 10f;
        }

        // 3. Get Rule Context Flags
        if (RuleContext.Instance != null && !forceReset)
        {
            data.isLivingRoomLightOn = RuleContext.Instance.IsLivingRoomLightOn;
            data.isBackDoorLocked = RuleContext.Instance.IsBackDoorLocked;
            data.isDeliveryWaiting = RuleContext.Instance.IsDeliveryWaiting;

            // Mới: Thu thập trạng thái Event
            data.isFootstepActive = RuleContext.Instance.IsFootstepActive;
            data.isClownAppeared = RuleContext.Instance.IsClownAppeared;
            data.hasTwinsAppeared = RuleContext.Instance.HasTwinsAppeared;
            data.isFlickering = RuleContext.Instance.IsFlickering;
        }
        else
        {
            // Mặc định ban đầu
            data.isLivingRoomLightOn = false;
            data.isBackDoorLocked = true;
            data.isDeliveryWaiting = false;

            data.isFootstepActive = false;
            data.isClownAppeared = false;
            data.hasTwinsAppeared = false;
            data.isFlickering = false;
        }

        // 4. Get Customer Queue State
        if (CustomerQueueManager.Instance != null && !forceReset)
        {
            data.remainingQueue = CustomerQueueManager.Instance.GetRemainingQueue();
            data.currentCustomersAlive = CustomerQueueManager.Instance.GetCurrentCustomersAlive();
        }
        else
        {
            data.remainingQueue = new List<CustomerQueueManager.SpawnEntry>();
            data.currentCustomersAlive = 0;
        }

        // 5. Scan and save Environmental Objects (Doors and Lights)
        if (!forceReset)
        {
            DoorInteractable[] doors = Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
            foreach (var door in doors)
            {
                data.envStates.Add(new ObjectState { name = door.gameObject.name, state = door.IsOpen });
            }

            LightSwitch[] switches = Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None);
            foreach (var ls in switches)
            {
                data.envStates.Add(new ObjectState { name = ls.gameObject.name, state = ls.IsOn });
            }
        }

        // 4. Get Position from Player
        if (PlayerMovementMobileSmooth.Instance != null && !forceReset)
        {
            Transform t = PlayerMovementMobileSmooth.Instance.transform;
            data.pX = t.position.x;
            data.pY = t.position.y;
            data.pZ = t.position.z;
            data.rotY = t.eulerAngles.y;
            data.hasPosition = true;
        }
        else
        {
            data.hasPosition = false;
        }

        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 5. Serialize and Write
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        PlayerPrefs.SetInt("SavedNight", data.night);
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Game Saved. Environment and Flags included.");
    }

    public static SaveData LoadGame()
    {
        if (!HasSave()) return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Game Loaded. Night: {data.night}, Money: {data.money}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void ClearSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
        }
        PlayerPrefs.DeleteKey("SavedNight");
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Save Cleared.");
    }
    
    // Legacy support for scripts already calling LoadNight
    public static int LoadNight()
    {
        SaveData data = LoadGame();
        return data != null ? data.night : 1;
    }

    public static void RestoreEnvironment(SaveData data)
    {
        if (data == null || data.envStates == null) return;

        foreach (var state in data.envStates)
        {
            GameObject obj = GameObject.Find(state.name);
            if (obj == null) continue;

            DoorInteractable door = obj.GetComponent<DoorInteractable>();
            if (door != null)
            {
                door.SetState(state.state);
                continue;
            }

            LightSwitch ls = obj.GetComponent<LightSwitch>();
            if (ls != null)
            {
                ls.SetState(state.state);
                continue;
            }
        }

        Debug.Log("[SaveSystem] Environment Restored.");
    }
}
