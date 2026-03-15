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
        StartCoroutine(DelayedFadeSequence());
    }

    private IEnumerator DelayedFadeSequence()
    {
        // Initial reset
        canvasGroup.alpha = 0f;

        // 1. Wait for delay
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

        // 3. Wait while visible
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
    }
}
