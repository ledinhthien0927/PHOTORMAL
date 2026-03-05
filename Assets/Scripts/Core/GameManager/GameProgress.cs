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

    // =============================
    // MONEY SYSTEM
    // =============================

    private void AddMoney(int amount)
    {
        if (amount <= 0) return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);

        NightManager.Instance.CheckTarget();
    }

    private void SpendMoney(int amount)
    {
        if (amount <= 0) return;

        if (CurrentMoney < amount)
        {
            EventManager.Instance.TriggerEvent("NotEnoughMoney");
            return;
        }

        CurrentMoney -= amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    // =============================
    // ERROR SYSTEM
    // =============================
    public void AddError()
    {
        CurrentError++;
        OnErrorChanged?.Invoke(CurrentError);

        if (CurrentError > MaxError)
        {
            // Sai lần thứ 4 (3 lần là tối đa), thua ngay lập tức
            EventManager.Instance.TriggerEvent("InstantGameOver");
        }
        else
        {
            // Sai từ lần 1-3, hiện Warning và đếm ngược
            EventManager.Instance.TriggerEvent("ShowWarningUI");
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