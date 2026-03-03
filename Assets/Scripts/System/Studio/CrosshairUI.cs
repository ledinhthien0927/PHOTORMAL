using UnityEngine;
using UnityEngine.UI;

public sealed class CrosshairUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image dot;

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    public void SetGreen(bool green)
    {
        if (dot == null) return;
        dot.color = green ? Color.green : Color.red;
    }
}