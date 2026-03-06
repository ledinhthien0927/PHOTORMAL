using System.Collections;
using TMPro;
using UnityEngine;

public sealed class PhotoResourceWarningUI : MonoBehaviour
{
    [Header("Warning Text")]
    [SerializeField] private TMP_Text warningText;

    [Header("Display Time")]
    [SerializeField] private float displayDuration = 1.5f;

    private Coroutine displayRoutine;

    private void OnEnable()
    {
        StudioManager.OnPhotoCaptureBlocked += OnPhotoCaptureBlocked;
    }

    private void OnDisable()
    {
        StudioManager.OnPhotoCaptureBlocked -= OnPhotoCaptureBlocked;
    }

    private void Start()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    private void OnPhotoCaptureBlocked(StudioManager.PhotoCaptureBlockReason reason)
    {
        if (warningText == null)
            return;

        // Decide which message to show
        string message = GetMessage(reason);

        ShowMessage(message);
    }

    private string GetMessage(StudioManager.PhotoCaptureBlockReason reason)
    {
        switch (reason)
        {
            case StudioManager.PhotoCaptureBlockReason.LowBattery:
                return "Battery too low. Buy more battery.";

            case StudioManager.PhotoCaptureBlockReason.LowMemory:
                return "Storage full. Buy more memory.";

            case StudioManager.PhotoCaptureBlockReason.LowBatteryAndLowMemory:
                return "Battery and storage are too low. Buy more.";

            default:
                return "Cannot take photo.";
        }
    }

    private void ShowMessage(string message)
    {
        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        displayRoutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(displayDuration);

        warningText.gameObject.SetActive(false);
        displayRoutine = null;
    }
}