using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 12f;

    private Dictionary<string, List<IGameEvent>> events =
        new Dictionary<string, List<IGameEvent>>();

    private bool isRunning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Tắt tạm EventLoop tự động gọi để dễ bề test bằng phím tay
        // StartCoroutine(EventLoop());
    }

    public void RegisterEvent(string id, IGameEvent gameEvent)
    {
        if (!events.ContainsKey(id))
            events.Add(id, new List<IGameEvent>());

        if (!events[id].Contains(gameEvent))
            events[id].Add(gameEvent);
    }

    public void TriggerEvent(string id)
    {
        if (events.ContainsKey(id))
        {
            // Tạo list tạm để duyệt tránh lỗi "Collection was modified" nếu Execute gọi ngược lại Register/Unregister
            List<IGameEvent> currentEvents = new List<IGameEvent>(events[id]);
            foreach (var gameEvent in currentEvents)
            {
                if (gameEvent != null) gameEvent.Execute();
            }
        }
        else
        {
            Debug.LogWarning("Event not found: " + id);
        }
    }

    IEnumerator EventLoop()
    {
        isRunning = true;

        while (isRunning)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            TriggerRandomEvent();
        }
    }

    void TriggerRandomEvent()
    {
        if (events.Count == 0) return;

        List<string> keys = new List<string>(events.Keys);
        int index = Random.Range(0, keys.Count);

        TriggerEvent(keys[index]);
    }

    public void StopEvents()
    {
        isRunning = false;
    }
}