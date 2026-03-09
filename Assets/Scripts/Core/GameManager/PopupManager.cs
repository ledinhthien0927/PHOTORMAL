using UnityEngine;
using UnityEngine.SceneManagement;

public class PopupManager : MonoBehaviour, IGameEvent
{
    public static PopupManager Instance;

    [Header("Game Over Popup")]
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject gameplayCanvas;

    private void Awake()
    {
        Instance = this;

        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);
    }

    private void Start()
    {
        // Đăng ký sự kiện GameOver
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RegisterEvent("InstantGameOver", this);
        }
    }

    // EventManager gọi
    public void Execute()
    {
        ShowGameOver();
    }

    public void ShowGameOver()
    {
        Debug.Log("[PopupManager] Game Over!");

        if (gameOverPopup != null)
        {
            if (gameplayCanvas != null)
                gameplayCanvas.SetActive(false);
            gameOverPopup.SetActive(true);
        }    
            
        else
            Debug.LogError("GameOverPopup chưa được gán!");

    }

    public void HideGameOver()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);

        Time.timeScale = 1f;
    }

    // Button prefab gọi
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Button prefab gọi
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}