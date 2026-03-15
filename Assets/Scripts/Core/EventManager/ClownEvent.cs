using UnityEngine;

public class ClownEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        // Sự kiện thằng hề chỉ xuất hiện ở đêm 3
        if (GameProgress.Instance.CurrentNight >= 3)
        {
            Debug.Log("Sự kiện Clown: Spawning clown guest.");
            
            // Trigger guest spawn. The guest's Init handles flags and sounds.
            if (CustomerQueueManager.Instance != null)
            {
                CustomerQueueManager.Instance.ForceSpawnClown();
            }
        }
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Clown", this);
    }
}
