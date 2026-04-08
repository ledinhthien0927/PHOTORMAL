using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChangeButtonFade : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string sceneToLoad = "MainMenu";

    [Header("First Time Flow")]
    [SerializeField] private bool useFirstTimeOverride = true;
    [SerializeField] private string firstTimeScene = "HowToPlay";
    [SerializeField] private string firstTimeKey = "HasSeenHowToPlay";

    [Header("Fade Overlay (Full Screen Black Image)")]
    [SerializeField] private Image fadeOverlay;

    [Header("Fade Timing")]
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("Safety")]
    [SerializeField] private bool disableButtonWhileLoading = true;

    private bool isBusy;

    public void ClickLoadScene()
    {
        if (isBusy) return;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        isBusy = true;

        Button btn = null;
        if (disableButtonWhileLoading)
        {
            btn = GetComponent<Button>();
            if (btn != null)
                btn.interactable = false;
        }

        if (fadeOverlay != null)
        {
            yield return FadeToBlack(1f, fadeOutDuration);
        }

        string targetScene = GetTargetScene();

        SceneManager.LoadScene(targetScene);
    }

    private string GetTargetScene()
    {
        if (!useFirstTimeOverride)
            return sceneToLoad;

        bool hasSeenHowToPlay = PlayerPrefs.GetInt(firstTimeKey, 0) == 1;

        if (!hasSeenHowToPlay)
        {
            PlayerPrefs.SetInt(firstTimeKey, 1);
            PlayerPrefs.Save();
            return firstTimeScene;
        }

        return sceneToLoad;
    }

    private IEnumerator FadeToBlack(float targetAlpha, float duration)
    {
        float startAlpha = GetFadeAlpha();
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

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

        fadeOverlay.raycastTarget = color.a > 0.01f;
    }

    public void SetTargetScene(string sceneName)
    {
        sceneToLoad = sceneName;
    }

    public void ResetFirstTimeFlag()
    {
        PlayerPrefs.DeleteKey(firstTimeKey);
        PlayerPrefs.Save();
    }
}