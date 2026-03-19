using UnityEngine;
using TMPro;
using System.Collections;

public class NightStartUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text nightText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private string textPrefix = "Night ";

    private void Start()
    {
        if (nightText == null) nightText = GetComponent<TMP_Text>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (nightText != null && GameProgress.Instance != null)
        {
            nightText.text = textPrefix + GameProgress.Instance.CurrentNight;
        }

        if (canvasGroup != null)
        {
            StartCoroutine(FadeSequence());
        }
        else
        {
            Debug.LogWarning("[NightStartUI] CanvasGroup is missing! Animation won't play.");
        }
    }

    private IEnumerator FadeSequence()
    {
        // 1. Ensure initial state is hidden
        canvasGroup.alpha = 0f;

        // 2. Fade In
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 3. Wait
        yield return new WaitForSecondsRealtime(displayDuration);

        // 4. Fade Out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Optionally disable the object or parent after finishing
        gameObject.SetActive(false);
    }
}
