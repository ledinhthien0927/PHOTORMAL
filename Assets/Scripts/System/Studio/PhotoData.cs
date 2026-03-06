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

    [Header("Camera Resources")]
    [SerializeField] private int defaultMemory = 10;
    [SerializeField] private float defaultBattery = 10f;
    [SerializeField] private int currentMemory = 10;
    [SerializeField] private float currentBattery = 10f;
    [SerializeField] private int memoryCostPerShot = 1;
    [SerializeField] private float batteryCostPerShot = 1.5f;

    private readonly List<PhotoRecord> photos = new List<PhotoRecord>();
    public IReadOnlyList<PhotoRecord> Photos => photos;

    public int CurrentMemory => currentMemory;
    public int DefaultMemory => defaultMemory;

    public float CurrentBattery => currentBattery;
    public float DefaultBattery => defaultBattery;

    public int MemoryCostPerShot => memoryCostPerShot;
    public float BatteryCostPerShot => batteryCostPerShot;

    public event Action<int> OnMemoryChanged;
    public event Action<float> OnBatteryChanged;

    private void Awake()
    {
        currentMemory = Mathf.Clamp(currentMemory, 0, defaultMemory);
        currentBattery = Mathf.Clamp(currentBattery, 0f, defaultBattery);
    }

    public PhotoRecord AddPhoto(Texture2D texture)
    {
        PhotoRecord record = new PhotoRecord(texture);
        photos.Add(record);

        if (photos.Count > maxPhotos)
        {
            if (photos[0].texture != null)
                Destroy(photos[0].texture);

            photos.RemoveAt(0);
            AddMemory(1);
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

            AddMemory(1);
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

        currentMemory = defaultMemory;
        OnMemoryChanged?.Invoke(currentMemory);
    }

    public bool HasEnoughResourcesForShot()
    {
        return currentMemory >= memoryCostPerShot && currentBattery >= batteryCostPerShot;
    }

    public bool HasEnoughMemoryForShot()
    {
        return currentMemory >= memoryCostPerShot;
    }

    public bool HasEnoughBatteryForShot()
    {
        return currentBattery >= batteryCostPerShot;
    }

    public bool TryConsumeShotResources()
    {
        if (!HasEnoughResourcesForShot())
            return false;

        currentMemory -= memoryCostPerShot;
        currentBattery -= batteryCostPerShot;

        currentMemory = Mathf.Max(0, currentMemory);
        currentBattery = Mathf.Max(0f, currentBattery);

        OnMemoryChanged?.Invoke(currentMemory);
        OnBatteryChanged?.Invoke(currentBattery);

        return true;
    }

    public void AddMemory(int amount)
    {
        if (amount <= 0)
            return;

        currentMemory += amount;
        currentMemory = Mathf.Clamp(currentMemory, 0, defaultMemory);

        OnMemoryChanged?.Invoke(currentMemory);
    }

    public void RechargeBattery(float amount)
    {
        if (amount <= 0f)
            return;

        currentBattery += amount;
        currentBattery = Mathf.Clamp(currentBattery, 0f, defaultBattery);

        OnBatteryChanged?.Invoke(currentBattery);
    }

    public void RefillMemoryToMax()
    {
        currentMemory = defaultMemory;
        OnMemoryChanged?.Invoke(currentMemory);
    }

    public void RechargeBatteryToMax()
    {
        currentBattery = defaultBattery;
        OnBatteryChanged?.Invoke(currentBattery);
    }

    public void ResetResourcesToDefault()
    {
        currentMemory = defaultMemory;
        currentBattery = defaultBattery;

        OnMemoryChanged?.Invoke(currentMemory);
        OnBatteryChanged?.Invoke(currentBattery);
    }
}