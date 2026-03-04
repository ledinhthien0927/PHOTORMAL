using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PhotoPrinter : MonoBehaviour
{
    [SerializeField] private RawImage previewImage;
    [SerializeField] private GameObject previewRoot;

    public void ShowPreview(PhotoRecord record, float seconds)
    {
        if (record == null || record.texture == null) return;

        if (previewImage != null)
            previewImage.texture = record.texture;

        if (previewRoot != null)
            previewRoot.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(HideAfter(seconds));
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (previewRoot != null)
            previewRoot.SetActive(false);
    }
}