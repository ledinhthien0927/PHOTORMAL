using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject questPanel;

    private void Start()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
            Debug.Log("[MainMenuManager] questPanel found and hidden at start.");
        }
        else
        {
            Debug.LogWarning("[MainMenuManager] questPanel NOT assigned in Inspector!");
        }
    }

    // --- UI Callbacks ---

    public void OnStartGameClicked()
    {
        Debug.Log("[MainMenuManager] Start Game Clicked!");

        // Khắc phục: Nếu người chơi vừa hoàn thành cả 3 level (GameComplete == 1), bỏ qua Quest Panel
        bool isGameFinished = PlayerPrefs.GetInt("GameComplete", 0) == 1;

        // If played before AND game is not just finished, show quest panel.
        if (PlayerPrefs.GetInt("PlayedBefore", 0) == 1 && !isGameFinished)
        {
            if (questPanel != null)
            {
                questPanel.SetActive(true);
                Debug.Log("[MainMenuManager] questPanel set to ACTIVE.");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Cannot show Quest Panel: reference is missing!");
            }
        }
        else
        {
            Debug.Log("[MainMenuManager] First time playing OR Game Finished, skipping Quest Panel.");
            PlayerPrefs.SetInt("PlayedBefore", 1);
            
            // Xóa cờ GameComplete để những lần mở game sau lại hiện QuestPanel bình thường
            if (isGameFinished)
                PlayerPrefs.SetInt("GameComplete", 0);

            PlayerPrefs.Save();
            OnRestartClicked();
        }
    }

    public void OnNoClicked()
    {
        Debug.Log("[MainMenuManager] No Clicked! Restarting night from baseline.");
        SaveSystem.pendingLoadData = null; // Quên bối cảnh đang đứng và doanh thu
        
        if (SaveSystem.HasSave())
        {
            int savedNight = SaveSystem.LoadNight();
            LoadNight(savedNight, true); // Load đêm gần nhất nhưng reset data
        }
        else
        {
            OnRestartClicked();
        }

        if (questPanel != null) questPanel.SetActive(false);
    }

    public void OnContinueClicked()
    {
        Debug.Log("[MainMenuManager] Continue Clicked!");
        if (SaveSystem.HasSave())
        {
            SaveData data = SaveSystem.LoadGame();
            SaveSystem.pendingLoadData = data; 
            
            // Mới: Load ngay dữ liệu vào GameProgress trước khi chuyển scene
            if (GameProgress.Instance != null)
            {
                GameProgress.Instance.LoadFromSaveData(data);
            }

            Debug.Log($"[MainMenuManager] Continue! Night: {data.night}, Money: {data.money}");
            
            // Gọi LoadNight với resetData = false để không xóa tiền vừa nạp
            LoadNight(data.night, false);
        }
        else
        {
            Debug.Log("[MainMenuManager] No save found! Restarting from Night 1.");
            OnRestartClicked();
        }
    }

    public void OnRestartClicked()
    {
        Debug.Log("[MainMenuManager] Restart Clicked!");
        SaveSystem.pendingLoadData = null; // Quên bối cảnh
        SaveSystem.ClearSave();
        LoadNight(1);
    }

    // --- Helper Logic ---

    private void LoadNight(int night, bool resetData = true)
    {
        Debug.Log($"[MainMenuManager] LoadNight called with Night: {night}, Reset: {resetData}");
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.SetNight(night, resetData);
            Debug.Log($"[MainMenuManager] GameProgress updated to Night: {GameProgress.Instance.CurrentNight}");
        }
        else
        {
            Debug.LogError("[MainMenuManager] GameProgress.Instance is NULL during LoadNight!");
        }

        string nightSceneName = $"Night_{night:D2}";
        Debug.Log($"[MainMenuManager] FINAL Loading scene: {nightSceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(nightSceneName);
    }

    [ContextMenu("Reset Played Status")]
    public void ResetPlayedStatus()
    {
        PlayerPrefs.DeleteKey("PlayedBefore");
        PlayerPrefs.Save();
        Debug.Log("[MainMenuManager] Played status reset. Next launch will skip Quest Panel.");
    }
}
