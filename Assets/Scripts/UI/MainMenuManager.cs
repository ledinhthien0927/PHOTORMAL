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

        // If played before, show quest panel. If first time, skip directly to restart.
        if (PlayerPrefs.GetInt("PlayedBefore", 0) == 1)
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
            Debug.Log("[MainMenuManager] First time playing, skipping Quest Panel.");
            PlayerPrefs.SetInt("PlayedBefore", 1);
            PlayerPrefs.Save();
            OnRestartClicked();
        }
    }

    public void OnContinueClicked()
    {
        Debug.Log("[MainMenuManager] Continue Clicked!");
        if (SaveSystem.HasSave())
        {
            int savedNight = SaveSystem.LoadNight();
            LoadNight(savedNight);
        }
        else
        {
            Debug.Log("[MainMenuManager] No save found, restarting from Night 1.");
            OnRestartClicked();
        }
    }

    public void OnRestartClicked()
    {
        Debug.Log("[MainMenuManager] Restart Clicked!");
        SaveSystem.ClearSave();
        LoadNight(1);
    }

    // --- Helper Logic ---

    private void LoadNight(int night)
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.SetNight(night);
        }

        string nightSceneName = $"Night_{night:D2}";
        Debug.Log($"[MainMenuManager] Loading scene: {nightSceneName}");
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
