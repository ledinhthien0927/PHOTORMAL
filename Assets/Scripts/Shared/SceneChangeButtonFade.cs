using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChangeButtonFade : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string sceneToLoad = "SampleScene";
    // Name of the scene that will be loaded when the button is pressed.

    [Header("Fade Overlay (Full Screen Black Image)")]
    [SerializeField] private Image fadeOverlay;
    // Reference to a full screen black Image used as fade overlay.
    // This should be placed on top of the UI Canvas.

    [Header("Fade Timing")]
    [SerializeField] private float fadeOutDuration = 0.6f;
    // Duration (in seconds) for fading to black.

    [Header("Safety")]
    [SerializeField] private bool disableButtonWhileLoading = true;
    // Prevents multiple clicks while loading.

    private bool isBusy;
    // Prevents triggering the transition multiple times.

    /// <summary>
    /// Call this method from the Button OnClick().
    /// Starts fade and scene loading process.
    /// </summary>
    public void ClickLoadScene()
    {
        if (isBusy) return;
        StartCoroutine(LoadRoutine());
    }

    /// <summary>
    /// Main transition routine:
    /// 1. Disable button (optional)
    /// 2. Fade to black
    /// 3. Load the target scene
    /// </summary>
    private IEnumerator LoadRoutine()
    {
        isBusy = true;

        // Disable button to prevent double clicking
        Button btn = null;
        if (disableButtonWhileLoading)
        {
            btn = GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
        }

        // Fade screen to black before loading scene
        if (fadeOverlay != null)
        {
            yield return FadeToBlack(1f, fadeOutDuration);
        }

        // Load the next scene
        SceneManager.LoadScene(sceneToLoad);
    }

    /// <summary>
    /// Smoothly fades the overlay image alpha to the target value.
    /// </summary>
    /// <param name="targetAlpha">Final alpha value (0 = transparent, 1 = fully black)</param>
    /// <param name="duration">Fade duration in seconds</param>
    private IEnumerator FadeToBlack(float targetAlpha, float duration)
    {
        float startAlpha = GetFadeAlpha();
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            // Using unscaledDeltaTime ensures fade works even if Time.timeScale = 0

            float alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            SetFadeAlpha(alpha);

            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    /// <summary>
    /// Returns the current alpha of the fade overlay.
    /// </summary>
    private float GetFadeAlpha()
    {
        if (fadeOverlay == null)
            return 0f;

        return fadeOverlay.color.a;
    }

    /// <summary>
    /// Sets the overlay alpha safely.
    /// Also blocks raycasts while visible to prevent UI interaction.
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color color = fadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = color;

        // Block clicks when overlay is visible
        fadeOverlay.raycastTarget = color.a > 0.01f;
    }

    /// <summary>
    /// Allows changing the target scene dynamically via code.
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        sceneToLoad = sceneName;
    }
}