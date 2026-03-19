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
            
            // Save progress when transitioning to next night
            SaveSystem.SaveNight(nextNightNum);
            Debug.Log($"[PopupManager] SaveSystem.SaveNight({nextNightNum}) called.");
            
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
        if (winNightPopup != null) winNightPopup.SetActive(false);
        if (pausePopup != null) pausePopup.SetActive(false);
        if (gameplayCanvas != null) gameplayCanvas.SetActive(true);
    }
}