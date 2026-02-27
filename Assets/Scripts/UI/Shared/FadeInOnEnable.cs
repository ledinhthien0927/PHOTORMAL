using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeInOnEnable : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delay = 0f;          // Delay before fading starts
    [SerializeField] private float fadeDuration = 0.5f; // Fade-in duration

    [Header("Behavior")]
    [SerializeField] private bool startHidden = true;   // Set alpha to 0 on enable
    [SerializeField] private bool disableRaycastWhileHidden = true; // Prevent clicks while invisible
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Fade curve

    private CanvasGroup cg;
    private Coroutine routine;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (startHidden)
        {
            cg.alpha = 0f;
            if (disableRaycastWhileHidden)
            {
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / dur);
            cg.alpha = curve.Evaluate(n);
            yield return null;
        }

        cg.alpha = 1f;

        if (disableRaycastWhileHidden)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        routine = null;
    }
}