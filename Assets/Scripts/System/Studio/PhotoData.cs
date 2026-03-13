using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PhotoRecord
{
    public string id;
    public Texture2D texture;
    public DateTime timeUtc;

    public PhotoRecord(Texture2D texture)
    {
        this.id = Guid.NewGuid().ToString("N");
        this.texture = texture;
        this.timeUtc = DateTime.UtcNow;
    }
}

public sealed class PhotoData : MonoBehaviour
{
    [Header("Photo Storage")]
    [SerializeField] private int maxPhotos = 30;

    private readonly List<PhotoRecord> photos = new List<PhotoRecord>();
    public IReadOnlyList<PhotoRecord> Photos => photos;

    public PhotoRecord AddPhoto(Texture2D texture)
    {
        PhotoRecord record = new PhotoRecord(texture);
        photos.Add(record);

        if (photos.Count > maxPhotos)
        {
            if (photos[0] != null && photos[0].texture != null)
                Destroy(photos[0].texture);

            photos.RemoveAt(0);
        }

        return record;
    }

    public bool RemovePhoto(PhotoRecord record)
    {
        if (record == null)
            return false;

        bool removed = photos.Remove(record);

        if (removed)
        {
            if (record.texture != null)
                Destroy(record.texture);
        }

        return removed;
    }

    public void RemoveAllPhotos()
    {
        for (int i = 0; i < photos.Count; i++)
        {
            if (photos[i] != null && photos[i].texture != null)
                Destroy(photos[i].texture);
        }

        photos.Clear();
    }
}