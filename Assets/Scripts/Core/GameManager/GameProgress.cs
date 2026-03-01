using UnityEngine;
using System;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance;

    public int CurrentNight { get; private set; } = 1;
    public int CurrentMoney { get; private set; }
    public int CurrentError { get; private set; }

    public int MaxError = 3;

    public Action<int> OnMoneyChanged;
    public Action<int> OnErrorChanged;
    public Action<int> OnNightChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void AddMoney(int amount)
    {
        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void AddError()
    {
        CurrentError++;
        OnErrorChanged?.Invoke(CurrentError);

        if (CurrentError >= MaxError)
        {
            EventManager.Instance.TriggerEvent("GameOver");
        }
    }

    public void ResetNightData()
    {
        CurrentMoney = 0;
        CurrentError = 0;
    }

    public void NextNight()
    {
        CurrentNight++;
        ResetNightData();
        OnNightChanged?.Invoke(CurrentNight);
    }
}