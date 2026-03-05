using UnityEngine;

public class NightManager : MonoBehaviour
{
    public static NightManager Instance;

    private int[] targets = { 600, 900, 1200 };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartNight(GameProgress.Instance.CurrentNight);
    }

    public void StartNight(int night)
    {
        Debug.Log("Start Night: " + night);
        RuleManager.Instance.SetupRules(night);
    }

    public void CheckTarget()
    {
        int night = GameProgress.Instance.CurrentNight;
        int target = targets[night - 1];

        if (GameProgress.Instance.CurrentMoney >= target)
        {
            EndNight();
        }
    }

    void EndNight()
    {
        Debug.Log("Night Complete");
        GameProgress.Instance.NextNight();
        StartNight(GameProgress.Instance.CurrentNight);
    }
}