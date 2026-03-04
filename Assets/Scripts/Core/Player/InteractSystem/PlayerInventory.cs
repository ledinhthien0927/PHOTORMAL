using UnityEngine;

public sealed class PlayerInventory : MonoBehaviour, IInventory
{
    public GameObject CurrentObject { get; private set; }
    public bool HasItem => CurrentObject != null;

    public bool TryPick(GameObject obj)
    {
        // Prevent picking if already holding something
        if (HasItem || obj == null) return false;

        CurrentObject = obj;
        return true;
    }

    public bool TryDrop(out GameObject obj)
    {
        if (!HasItem)
        {
            obj = null;
            return false;
        }

        obj = CurrentObject;
        CurrentObject = null;
        return true;
    }
}