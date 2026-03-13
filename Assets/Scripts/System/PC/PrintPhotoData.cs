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

    public PrintPhotoData Clone()
    {
        PrintPhotoData clone = new PrintPhotoData(PhotoRecord);
        clone.PrintSize = PrintSize;
        clone.CopyCount = CopyCount;
        return clone;
    }

    public bool MatchesOrder(PhotoOrder order)
    {
        return PrintSize == PhotoSizeText.ToLabel(order.size) &&
               CopyCount == order.quantity;
    }

    public bool MatchesPhotoRecord(PhotoRecord record)
    {
        if (PhotoRecord == null || record == null)
            return false;

        return PhotoRecord.id == record.id;
    }
}