using UnityEngine;

[System.Serializable]
public sealed class PrintPhotoData
{
    public PhotoRecord PhotoRecord;
    public string PrintSize;
    public int CopyCount;

    public PrintPhotoData(PhotoRecord photoRecord)
    {
        PhotoRecord = photoRecord;
        PrintSize = "4x6";
        CopyCount = 1;
    }
}