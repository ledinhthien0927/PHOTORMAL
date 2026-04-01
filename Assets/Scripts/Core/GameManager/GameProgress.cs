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

            // Mới: Khôi phục cờ Event
            RuleContext.Instance.IsFootstepActive = data.isFootstepActive;
            RuleContext.Instance.IsClownAppeared = data.isClownAppeared;
            RuleContext.Instance.HasTwinsAppeared = data.hasTwinsAppeared;
            RuleContext.Instance.IsFlickering = data.isFlickering;
        }

        // Restore Printer Supplies
        if (PrinterSupplyData.Instance != null)
        {
            PrinterSupplyData.Instance.LoadFromSaveData(data);
        }

        // Restore Customer Queue
        if (CustomerQueueManager.Instance != null)
        {
            CustomerQueueManager.Instance.RestoreQueueState(data.remainingQueue, data.currentCustomersAlive);
        }

        // Re-trigger active events
        if (EventManager.Instance != null)
        {
            if (data.isFootstepActive) EventManager.Instance.TriggerEvent("Footstep");
            if (data.hasTwinsAppeared) EventManager.Instance.TriggerEvent("Twins");
            // Clown event handling might need care if it involves models, 
            // but TriggerEvent("Clown") should handle it.
            if (data.isClownAppeared) EventManager.Instance.TriggerEvent("Clown");
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