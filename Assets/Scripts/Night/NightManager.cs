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

        if (CustomerQueueManager.Instance != null)
            CustomerQueueManager.Instance.BuildQueueForNight(night);
    }

    public void CheckTarget()
    {
        int night = GameProgress.Instance.CurrentNight;

        int index = Mathf.Clamp(night - 1, 0, targets.Length - 1);
        int target = targets[index];

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