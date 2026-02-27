using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterText : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_Text targetText;          // TextMeshPro target

    [Header("Timing")]
    [SerializeField] private float startDelay = 1f;        // Delay before typing starts
    [SerializeField] private float charsPerSecond = 28f;   // Base typing speed

    [Header("Punctuation Pauses")]
    [SerializeField] private float commaPause = 0.12f;     // Extra pause after commas
    [SerializeField] private float periodPause = 0.25f;    // Extra pause after . ! ?

    [Header("Next Button")]
    [SerializeField] private GameObject nextButton;        // Button to show after typing

    private string fullText;
    private Coroutine typingCoroutine;
    private bool isTyping;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        fullText = targetText.text;

        // Hide button at start
        if (nextButton != null)
            nextButton.SetActive(false);

        // Prepare TMP for smooth reveal
        targetText.ForceMeshUpdate();
        targetText.maxVisibleCharacters = 0;
    }

    private void OnEnable()
    {
        typingCoroutine = StartCoroutine(TypeRoutine());
    }

    private void Update()
    {
        // Mobile tap or mouse click
        bool tapped = Input.GetMouseButtonDown(0);

        if (!tapped && Input.touchCount > 0)
            tapped = Input.GetTouch(0).phase == TouchPhase.Began;

        // One tap while typing: reveal all text instantly
        if (tapped && isTyping)
        {
            SkipToEnd();
        }
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;

        yield return new WaitForSeconds(startDelay);

        targetText.ForceMeshUpdate();
        int totalChars = targetText.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            targetText.maxVisibleCharacters = i;

            if (i >= totalChars)
                break;

            char c = fullText[Mathf.Clamp(i, 0, fullText.Length - 1)];

            float interval = 1f / Mathf.Max(1f, charsPerSecond);

            // Add dramatic pause for punctuation
            if (c == ',' || c == ';' || c == ':')
                interval += commaPause;
            else if (c == '.' || c == '!' || c == '?')
                interval += periodPause;

            yield return new WaitForSeconds(interval);
        }

        isTyping = false;
        ShowNextButton();
    }

    private void SkipToEnd()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        targetText.ForceMeshUpdate();
        targetText.maxVisibleCharacters = targetText.textInfo.characterCount;

        isTyping = false;
        ShowNextButton();
    }

    private void ShowNextButton()
    {
        if (nextButton != null && !nextButton.activeSelf)
            nextButton.SetActive(true);
    }
}