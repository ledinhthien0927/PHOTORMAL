using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
    public int night = 1;
    public int money = 0;
    
    // Player Position
    public float pX, pY, pZ;
    public float rotY;

    // Optional: add timestamp or other meta
    public string saveTime;
}

public static class SaveSystem
{
    private static readonly string FileName = "photormal_save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData pendingLoadData; // Mới: Lưu data để chờ scene load xong thì vứt vô player

    public static void SaveGame()
    {
        SaveData data = new SaveData();

        // 1. Get Night and Money from GameProgress
        if (GameProgress.Instance != null)
        {
            data.night = GameProgress.Instance.CurrentNight;
            data.money = GameProgress.Instance.CurrentMoney;
        }

        // 2. Get Position from Player
        if (PlayerMovementMobileSmooth.Instance != null)
        {
            Transform t = PlayerMovementMobileSmooth.Instance.transform;
            data.pX = t.position.x;
            data.pY = t.position.y;
            data.pZ = t.position.z;
            data.rotY = t.eulerAngles.y;
        }

        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 3. Serialize and Write
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        // Also update standard PlayerPrefs for some basic flags (legacy support/easy check)
        PlayerPrefs.SetInt("SavedNight", data.night);
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Game Saved to {SavePath}. Night: {data.night}, Money: {data.money}");
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
}
