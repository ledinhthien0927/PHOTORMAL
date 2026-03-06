using TMPro;
using UnityEngine;

public sealed class MoneyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private string prefix = "$";

    private void OnEnable()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnMoneyChanged += RefreshMoneyText;

        RefreshCurrentValue();
    }

    private void OnDisable()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnMoneyChanged -= RefreshMoneyText;
    }

    private void Start()
    {
        RefreshCurrentValue();
    }

    private void RefreshCurrentValue()
    {
        if (GameProgress.Instance == null)
            return;

        RefreshMoneyText(GameProgress.Instance.CurrentMoney);
    }

    private void RefreshMoneyText(int value)
    {
        if (moneyText == null)
            return;

        moneyText.text = $"{prefix}{value}";
    }
}