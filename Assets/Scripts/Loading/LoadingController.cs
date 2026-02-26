using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image barFill;       // The red/orange fill image (Image Type = Filled)
    [SerializeField] private Image fadeOverlay;   // Full screen black overlay image

    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "SampleScene";

    [Header("Timing Settings")]
    [SerializeField] private float fillSpeed = 0.6f;      // Speed of percentage movement
    [SerializeField] private float fadeOutDuration = 1.2f;

    // Loading checkpoints (percentage, pause duration)
    private readonly (float percent, float pauseTime)[] stages = new (float, float)[]
    {
        (0.20f, 1.5f),
        (0.50f, 1.5f),
        (0.80f, 1.5f),
        (0.95f, 0.7f)
    };

    private void Start()
    {
        if (barFill != null)
            barFill.fillAmount = 0f;

        SetFadeAlpha(0f);
        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        float currentProgress = 0f;

        // Move through each staged checkpoint
        foreach (var stage in stages)
        {
            yield return MoveFill(currentProgress, stage.percent);
            currentProgress = stage.percent;
            yield return new WaitForSeconds(stage.pauseTime);
        }

        // Final fill to 100%
        yield return MoveFill(currentProgress, 1f);

        // Fade to black before switching scene
        yield return FadeToBlack(1f, fadeOutDuration);

        // Load next scene
        SceneManager.LoadScene(sceneToLoad);
    }

    private IEnumerator MoveFill(float from, float to)
    {
        float duration = Mathf.Abs(to - from) / fillSpeed;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float value = Mathf.Lerp(from, to, timer / duration);

            if (barFill != null)
                barFill.fillAmount = value;

            yield return null;
        }

        if (barFill != null)
            barFill.fillAmount = to;
    }

    private IEnumerator FadeToBlack(float targetAlpha, float duration)
    {
        float startAlpha = GetFadeAlpha();
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private float GetFadeAlpha()
    {
        if (fadeOverlay == null)
            return 0f;

        return fadeOverlay.color.a;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color color = fadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = color;
    }
}