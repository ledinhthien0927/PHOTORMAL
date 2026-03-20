using TMPro;
using UnityEngine;

public class CountTimeout : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Recommended: The object containing the timer panel. If null, will toggle only the Timer Text.")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 5f;

    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Try to get CanvasGroup for smoother/safer visibility control
        if (root != null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
        }
        else if (timerText != null)
        {
            root = timerText.gameObject;
            canvasGroup = root.GetComponent<CanvasGroup>();
        }

        if (root == gameObject)
        {
             // If we are about to hide ourselves, it will stop Update()
             Debug.LogWarning("[CountTimeout] Script is on the same object as root. If it hides itself, it won't be able to show itself again!");
        }
    }

    private void Update()
    {
        if (RuleManager.Instance == null || RuleContext.Instance == null)
        {
            SetVisible(false);
            return;
        }

        // The clock only appears when a customer is invited to the studio (Night 2+)
        bool isServing = RuleContext.Instance.CustomerServiceStartTime > 0;
        
        // Ensure RuleManager has an active rule for this to make sense
        bool isRuleActive = RuleManager.Instance.serviceCountdown > 0;
        
        SetVisible(isServing);

        if (isServing)
        {
            UpdateTimerDisplay();
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        float remainingTime = RuleManager.Instance.serviceCountdown;
        
        // Format as "60s" or "30s"
        timerText.text = $"{Mathf.Max(0, Mathf.CeilToInt(remainingTime))}s";

        // Color feedback
        timerText.color = (remainingTime <= warningThreshold) ? warningColor : normalColor;
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
        }
        else if (root != null && root.activeSelf != visible)
        {
            // WARNING: If root is a parent of this script, SetActive(false) will disable this script!
            if (visible == false && IsParentOfThis(root.transform))
            {
                Debug.LogError($"[CountTimeout] Aggressive hiding: Root '{root.name}' is a parent of current script. This will stop the timer forever! Set TimerText as root instead.");
                return;
            }
            root.SetActive(visible);
        }
    }

    private bool IsParentOfThis(Transform t)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current == t) return true;
            current = current.parent;
        }
        return false;
    }
}
