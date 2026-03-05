using UnityEngine;

public class PhotoOrderService : MonoBehaviour
{
    [Header("Quantity Range")]
    public int minQuantity = 1;
    public int maxQuantity = 6;

    [Header("Size Weights (higher = more common)")]
    [Range(0f, 1f)] public float weight3x4 = 0.45f;
    [Range(0f, 1f)] public float weight4x6 = 0.35f;
    [Range(0f, 1f)] public float weight5x7 = 0.15f;
    [Range(0f, 1f)] public float weight6x8 = 0.05f;

    // Generate a random photo order (independent from NightManager for now)
    public PhotoOrder Generate()
    {
        PhotoOrder order = new PhotoOrder
        {
            quantity = Random.Range(minQuantity, maxQuantity + 1),
            size = PickSizeWeighted()
        };

        return order;
    }

    private PhotoSize PickSizeWeighted()
    {
        // Normalize weights
        float total = Mathf.Max(0.0001f, weight3x4 + weight4x6 + weight5x7 + weight6x8);
        float r = Random.value * total;

        if (r < weight3x4) return PhotoSize.Size3x4;
        r -= weight3x4;

        if (r < weight4x6) return PhotoSize.Size4x6;
        r -= weight4x6;

        if (r < weight5x7) return PhotoSize.Size5x7;
        return PhotoSize.Size6x8;
    }
}