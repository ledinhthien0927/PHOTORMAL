using UnityEngine;
using UnityEngine.SceneManagement;

public class PopupManager : MonoBehaviour, IGameEvent
{
    public static PopupManager Instance;

    [Header("Canvases & Popups")]
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject winNightPopup;

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Initialize popups to hidden state
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        if (winNightPopup != null) winNightPopup.SetActive(false);
    }

    private void Start()
    {
        // Register for standard event system
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RegisterEvent("InstantGameOver", this);
        }
    }

    // --- Interface Execution ---
    public void Execute()
    {
        ShowGameOver();
    }

    // --- Show UI Logic ---

    public void ShowWinNight()
    {
        Debug.Log("[PopupManager] User Won Night!");
        DisplayPopup(winNightPopup);
    }

    public void ShowGameOver()
    {
        Debug.Log("[PopupManager] Game Over Triggered!");
        DisplayPopup(gameOverPopup);
    }

    private void DisplayPopup(GameObject popup)
    {
        if (popup != null)
        {
            // Stop game logic and show UI
            Time.timeScale = 0f;
            if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
            popup.SetActive(true);
            
            // Unlock/Show cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Debug.LogError("[PopupManager] Target popup is NULL! Check Inspector assignments.");
        }
    }

    // --- Button Handlers ---

    public void RestartGame()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[PopupManager] Restarting Level: {currentSceneName}");
        
        ResumeTime();
        SceneManager.LoadScene(currentSceneName);
    }

    public void GoToMainMenu()
    {
        Debug.Log($"[PopupManager] Loading Main Menu: {mainMenuSceneName}");
        
        ResumeTime();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void StartNextNight()
    {
        Debug.Log("[PopupManager] Advance Button Clicked. Advancing Night and Loading Scene...");
        
        ResumeTime();
        
        if (GameProgress.Instance != null)
        {
            // Tăng số đêm trước khi load scene mới
            GameProgress.Instance.NextNight();
            
            int nextNightNum = GameProgress.Instance.CurrentNight;
            // Tên scene theo định dạng Night_01, Night_02...
            string nextSceneName = "Night_0" + nextNightNum;
            
            Debug.Log($"[PopupManager] Attempting to load: {nextSceneName}");
            
            // Chúng ta load scene theo tên. 
            // Nếu bạn không có các scene Night_02, Night_03 riêng biệt mà dùng chung 1 scene
            // thì bạn cần sửa lại đoạn này hoặc đổi tên scene trong Build Settings.
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[PopupManager] GameProgress.Instance is NULL! Cannot advance.");
        }
    }

    private void ResumeTime()
    {
        Time.timeScale = 1f;
    }

    public void ClosePopups()
    {
        ResumeTime();
        if (gameOverPopup != null) gameOverPopup.SetActive(false);
        if (winNightPopup != null) winNightPopup.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
    }
}