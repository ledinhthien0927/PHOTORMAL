using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public GameObject CurrentObject { get; private set; }

    public bool HasItem()
    {
        return CurrentObject != null;
    }

    public void Pick(GameObject obj)
    {
        CurrentObject = obj;
        //UIManager.Instance.RefreshUI();
    }

    public GameObject Drop()
    {
        GameObject temp = CurrentObject;
        CurrentObject = null;
        //UIManager.Instance.RefreshUI();
        return temp;
    }

}