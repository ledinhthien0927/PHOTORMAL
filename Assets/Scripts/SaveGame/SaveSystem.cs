using UnityEngine;

public static class SaveSystem
{
    private const string SaveKey = "SavedNight";

    public static void SaveNight(int night)
    {
        PlayerPrefs.SetInt(SaveKey, night);
        PlayerPrefs.Save();
        Debug.Log($"[SaveSystem] Game Saved. Night: {night}");
    }

    public static int LoadNight()
    {
        int night = PlayerPrefs.GetInt(SaveKey, 1);
        Debug.Log($"[SaveSystem] Game Loaded. Night: {night}");
        return night;
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Save Cleared.");
    }
}
