using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DelayedFadeUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float delaySeconds = 2f;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float visibleDuration = 3f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private string messageText = "Click the button to turn on the light";

    private CanvasGroup canvasGroup;
    private TMP_Text textMesh;
    private bool hasShownInThisScene = false; // Biến kiểm tra chỉ hiện 1 lần trong Scene này

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        textMesh = GetComponent<TMP_Text>();

        // Set initial state
        canvasGroup.alpha = 0f;
        if (textMesh != null && !string.IsNullOrEmpty(messageText))
        {
            textMesh.text = messageText;
        }
    }

    private void OnEnable()
    {
        // Kiểm tra nếu đã hiện rồi thì không chạy lại (kể cả khi tắt/mở lại GameObject)
        if (hasShownInThisScene)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        StartCoroutine(DelayedFadeSequence());
    }

    private IEnumerator DelayedFadeSequence()
    {
        hasShownInThisScene = true; // Đánh dấu đã bắt đầu hiện
        canvasGroup.alpha = 0f;

        // 1. Chờ delay sau khi load scene
        yield return new WaitForSeconds(delaySeconds);

        // 2. Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 3. Giữ hiển thị
        yield return new WaitForSeconds(visibleDuration);

        // 4. Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
            yield return null;
        }
        canvasGroup.alpha = 0f;

        Debug.Log("[DelayedFadeUI] Hiệu ứng kết thúc và sẽ không lặp lại trong Scene này.");
    }
}
