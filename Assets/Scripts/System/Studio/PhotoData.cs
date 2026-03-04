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
    [SerializeField] private int maxPhotos = 30;

    private readonly List<PhotoRecord> _photos = new List<PhotoRecord>();
    public IReadOnlyList<PhotoRecord> Photos => _photos;

    public PhotoRecord AddPhoto(Texture2D tex)
    {
        var record = new PhotoRecord(tex);
        _photos.Add(record);

        // Keep list bounded
        if (_photos.Count > maxPhotos)
        {
            // Destroy old texture to avoid memory leak
            if (_photos[0].texture != null) Destroy(_photos[0].texture);
            _photos.RemoveAt(0);
        }

        return record;
    }
}