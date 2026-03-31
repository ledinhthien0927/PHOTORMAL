using UnityEngine;
using UnityEngine.SceneManagement;

public class PopupManager : MonoBehaviour, IGameEvent
{
    public static PopupManager Instance;

    [Header("Canvases & Popups")]
    [SerializeField] private GameObject gameplayCanvas;
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject winNightPopup;
    [SerializeField] private GameObject pausePopup;
    [SerializeField] private GameObject clownGameOverPopup; // Thêm popup Game Over riêng cho hề

    [Header("Settings")]
    [SerializeField] private float popupFadeDuration = 0.5f;

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
        if (pausePopup != null) pausePopup.SetActive(false);

        // Ensure game is unpaused and UI is ready
        Time.timeScale = 1f;
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
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

    public void ShowClownGameOver()
    {
        Debug.Log("[PopupManager] Clown Game Over Triggered!");
        DisplayPopup(clownGameOverPopup);
    }

    public void PauseGame()
    {
        Debug.Log("[PopupManager] Game Paused!");
        DisplayPopup(pausePopup);
    }

    public void ResumeGame()
    {
        Debug.Log("[PopupManager] Game Resumed!");
        ClosePopups();
    }

    private void DisplayPopup(GameObject popup)
    {
        if (popup != null)
        {
            // Stop game logic
            Time.timeScale = 0f;
            if (gameplayCanvas != null) gameplayCanvas.SetActive(false);
            
            // Start fade animation
            StartCoroutine(FadeInCoroutine(popup));
        }
        else
        {
            Debug.LogError("[PopupManager] Target popup is NULL! Check Inspector assignments.");
        }
    }

    private System.Collections.IEnumerator FadeInCoroutine(GameObject popup)
    {
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) cg = popup.AddComponent<CanvasGroup>();

        // Initialize state
        cg.alpha = 0f;
        popup.SetActive(true);

        float elapsed = 0f;
        while (elapsed < popupFadeDuration)
        {
            // Use unscaledDeltaTime because Time.timeScale is 0
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / popupFadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // --- Button Handlers ---

    public void RestartGame()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[PopupManager] Restarting Level: {currentSceneName}");

        // Mới: Đảm bảo không load lại bối cảnh cũ khi restart
        SaveSystem.pendingLoadData = null;

        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetNightData();
        }
        
        ResumeTime();
        SceneManager.LoadScene(currentSceneName);
    }

    public void GoToMainMenu()
    {
        Debug.Log($"[PopupManager] Loading Main Menu: {mainMenuSceneName}");
        
        // Kiểm tra xem có đang ở màn hình Game Over không
        bool hasLost = (gameOverPopup != null && gameOverPopup.activeSelf) || 
                       (clownGameOverPopup != null && clownGameOverPopup.activeSelf);

        // Mới: Lưu lại tiến trình trước khi thoát ra ngoài menu
        // Nếu đã thua, ép bộ lưu quay về trạng thái đầu đêm (forceReset = true)
        SaveSystem.SaveGame(forceReset: hasLost);

        ResumeTime();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void StartNextNight()
    {
        Debug.Log("[PopupManager] Advance Button Clicked. Advancing Night and Loading Scene...");
        
        ResumeTime();
        
        if (GameProgress.Instance != null)
        {
            Debug.Log($"[PopupManager] Current Night before advance: {GameProgress.Instance.CurrentNight}");
            
            // Tăng số đêm trước khi load scene mới
            GameProgress.Instance.NextNight();
            
            int nextNightNum = GameProgress.Instance.CurrentNight;
            Debug.Log($"[PopupManager] Advanced to Night: {nextNightNum}");
            
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Xử lý khi hoàn thành Level 3 (Win cả game)
            // Khắc phục: Dùng thêm check tên Scene để đảm bảo khi test riêng Night_03 trên Editor vẫn nhận diện đúng Game Complete
            if (nextNightNum > 3 || currentSceneName == "Night_03")
            {
                Debug.Log("[PopupManager] Finished Level 3! Returning to Main Menu...");
                PlayerPrefs.SetInt("GameComplete", 1); // Đánh dấu đã qua màn 3 để MainMenu biết
                PlayerPrefs.Save(); // Lưu ngay vào bộ nhớ
                SaveSystem.ClearSave();
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            // Save progress when transitioning to next night
            SaveSystem.SaveGame();
            Debug.Log("[PopupManager] SaveSystem.SaveGame() called.");
            
            // Tên scene theo định dạng Night_01, Night_02...
            string nextSceneName = "Night_0" + nextNightNum;
            
            Debug.Log($"[PopupManager] Attempting to load: {nextSceneName}");
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
        if (clownGameOverPopup != null) clownGameOverPopup.SetActive(false);
        //if (winNightPopup != null) winNightPopup.SetActive(false);
        if (pausePopup != null) pausePopup.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
    }
}