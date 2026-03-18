using System.Collections;
using TMPro;
using UnityEngine;

public sealed class PlayerMessageUI : MonoBehaviour
{
    public static PlayerMessageUI Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float defaultDuration = 2f;

    [Header("Optional UI To Hide While Showing Message")]
    [SerializeField] private GameObject interactPromptRoot;

    private Coroutine routine;
    private bool wasInteractPromptActiveBeforeMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ForceHideImmediate();
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("PlayerMessageUI is inactive, cannot show message.");
            return;
        }

        if (messageText == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        if (interactPromptRoot != null)
        {
            wasInteractPromptActiveBeforeMessage = interactPromptRoot.activeSelf;
            interactPromptRoot.SetActive(false);
        }

        messageText.text = message;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(duration);

        HideMessageInternal();
    }

    private void ForceHideImmediate()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        HideMessageInternal();
    }

    private void HideMessageInternal()
    {
        if (messageText != null)
            messageText.gameObject.SetActive(false);

        if (interactPromptRoot != null && wasInteractPromptActiveBeforeMessage)
            interactPromptRoot.SetActive(true);

        routine = null;
    }
}