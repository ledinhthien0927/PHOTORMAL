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

    private void OnEnable()
    {
        GameEventAPI.OnAddMoney += AddMoney;
        GameEventAPI.OnSpendMoney += SpendMoney;
    }

    private void OnDisable()
    {
        GameEventAPI.OnAddMoney -= AddMoney;
        GameEventAPI.OnSpendMoney -= SpendMoney;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);

        if (NightManager.Instance != null)
            NightManager.Instance.CheckTarget();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
            return true;

        if (CurrentMoney < amount)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.TriggerEvent("NotEnoughMoney");

            return false;
        }

        CurrentMoney -= amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        return true;
    }

    private void SpendMoney(int amount)
    {
        TrySpendMoney(amount);
    }

    public void AddError()
    {
        CurrentError++;
        OnErrorChanged?.Invoke(CurrentError);

        if (CurrentError > MaxError)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.TriggerEvent("InstantGameOver");
        }
        else
        {
            if (EventManager.Instance != null)
                EventManager.Instance.TriggerEvent("ShowWarningUI");
        }
    }

    public void ResetNightData()
    {
        CurrentMoney = 0;
        CurrentError = 0;

        OnMoneyChanged?.Invoke(CurrentMoney);
        OnErrorChanged?.Invoke(CurrentError);
    }

    public void NextNight()
    {
        CurrentNight++;
        ResetNightData();
        OnNightChanged?.Invoke(CurrentNight);
    }
}