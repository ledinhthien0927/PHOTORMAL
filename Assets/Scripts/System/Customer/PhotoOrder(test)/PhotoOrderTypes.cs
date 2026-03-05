using System;

public enum PhotoSize
{
    Size3x4,
    Size4x6,
    Size5x7,
    Size6x8
}

[Serializable]
public struct PhotoOrder
{
    public int quantity;
    public PhotoSize size;
}

public static class PhotoSizeText
{
    // Human-readable labels for UI
    public static string ToLabel(PhotoSize size)
    {
        switch (size)
        {
            case PhotoSize.Size3x4: return "3x4";
            case PhotoSize.Size4x6: return "4x6";
            case PhotoSize.Size5x7: return "5x7";
            case PhotoSize.Size6x8: return "6x8";
            default: return size.ToString();
        }
    }
}