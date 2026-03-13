using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PrintedPhotoPickup : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField] private Renderer previewRenderer;
    [SerializeField] private TMP_Text infoText;

    public PrintPhotoData Data { get; private set; }

    public void Setup(PrintPhotoData data)
    {
        Data = data;

        if (previewRenderer != null &&
            Data != null &&
            Data.PhotoRecord != null &&
            Data.PhotoRecord.texture != null)
        {
            Material runtimeMaterial = previewRenderer.material;
            runtimeMaterial.mainTexture = Data.PhotoRecord.texture;
        }

        if (infoText != null && Data != null)
        {
            infoText.text = $"{Data.PrintSize}  x{Data.CopyCount}";
        }
    }

    public bool IsValid()
    {
        return Data != null && Data.PhotoRecord != null;
    }

    public void Consume()
    {
        Destroy(gameObject);
    }
}