using UnityEngine;
using System;

public class GameProgress : MonoBehaviour
{
    private static GameProgress instance;
    public static GameProgress Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameProgress>();
            }
            return instance;
        }
    }

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
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
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

    public void ResetTotalProgress()
    {
        CurrentNight = 1;
        ResetNightData();
        OnNightChanged?.Invoke(CurrentNight);
    }

    public void NextNight()
    {
        CurrentNight++;
        ResetNightData();
        OnNightChanged?.Invoke(CurrentNight);
    }

    public void SetNight(int night, bool resetData = true)
    {
        CurrentNight = night;
        if (resetData) ResetNightData();
        OnNightChanged?.Invoke(CurrentNight);
    }

    public void LoadFromSaveData(SaveData data)
    {
        CurrentNight = data.night;
        CurrentMoney = data.money;
        CurrentError = data.errors;
        
        // Restore RuleContext Flags
        if (RuleContext.Instance != null)
        {
            RuleContext.Instance.IsLivingRoomLightOn = data.isLivingRoomLightOn;
            RuleContext.Instance.IsBackDoorLocked = data.isBackDoorLocked;
            RuleContext.Instance.IsDeliveryWaiting = data.isDeliveryWaiting;
        }

        OnNightChanged?.Invoke(CurrentNight);
        OnMoneyChanged?.Invoke(CurrentMoney);
        OnErrorChanged?.Invoke(CurrentError);
    }

    public void SetMoney(int money)
    {
        CurrentMoney = money;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }
}