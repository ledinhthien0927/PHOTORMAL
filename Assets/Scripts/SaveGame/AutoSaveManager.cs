using UnityEngine;

public class AutoSaveManager : MonoBehaviour
{
    [SerializeField] private float saveInterval = 30f;
    private float timer;

    private void Start()
    {
        timer = saveInterval;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SaveSystem.SaveGame();
            timer = saveInterval;
        }
    }
}
