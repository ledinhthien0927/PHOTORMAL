using UnityEngine;

public class RuleContext : MonoBehaviour
{
    public static RuleContext Instance;

    public bool IsFlickering;
    public bool IsFootstepActive;
    public bool IsClownAppeared;

    public bool IsBackDoorLocked = true;
    public bool IsPlayerInToilet;

    public float StudioEnterTime;
    public float ClownAppearTime;

    private void Awake()
    {
        Instance = this;
    }
}