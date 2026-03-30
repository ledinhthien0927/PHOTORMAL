using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class WarningManager : MonoBehaviour, IGameEvent
{
    public static WarningManager Instance;

    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button callSupportButton;
    [SerializeField] private float showDelay = 0.5f; // Thời gian chờ trước khi hiện Warning UI (giây)

    private float countdownTimer;
    private bool isWarningActive = false;

    private void Awake()
    {
        Instance = this;
        warningPanel.SetActive(false);
        callSupportButton.onClick.AddListener(OnCallSupportClicked);
    }

    private void Start()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RegisterEvent("ShowWarningUI", this);
        }
    }

    public void Execute()
    {
        ShowWarning();
    }


    private void OnEnable()
    {
        // Lắng nghe sự kiện Call Support Clicked từ Player B (nếu UI do Player B tương tác)
        // hoặc từ chính Canvas này nếu bạn làm
        GameEventAPI.OnCallSupportClicked += OnCallSupportClicked;
    }

    private void OnDisable()
    {
        GameEventAPI.OnCallSupportClicked -= OnCallSupportClicked;
    }

    public void ShowWarning()
    {
        StartCoroutine(ShowWarningRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        // Chờ một khoảng thời gian trước khi hiện UI (để tránh đè lên các hiệu ứng như glitch)
        yield return new WaitForSecondsRealtime(showDelay);

        isWarningActive = true;
        warningPanel.SetActive(true);
        countdownTimer = 5f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySupportCall();
        }
    }

    private void Update()
    {
        if (isWarningActive)
        {
            countdownTimer -= Time.deltaTime;
            
            // Cập nhật text số nguyên hoặc 1 chữ số thập phân
            timerText.text = "Call Support: " + Mathf.Ceil(countdownTimer).ToString() + "s";

            if (countdownTimer <= 0f)
            {
                isWarningActive = false;
                warningPanel.SetActive(false);
                
                // Stop the ringing audio before firing the Game Over event
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopSupportCall();
                }
                
                EventManager.Instance.TriggerEvent("InstantGameOver");
            }
        }
    }

    private void OnCallSupportClicked()
    {
        if (isWarningActive)
        {
            isWarningActive = false;
            warningPanel.SetActive(false);
            Debug.Log("Đã gọi Support kịp thời. Thoát nạn.");
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSupportCall();
            }
        }
    }
}
