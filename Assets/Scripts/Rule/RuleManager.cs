using System.Collections.Generic;
using UnityEngine;

public enum RuleType
{
    NoFlickerShoot,
    TwinsTurnOffLight,
    BackDoorLocked,
    HideWhenFootstep,
    StudioTimeLimit,
    ClownDoorOpen
}

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance;

    private HashSet<RuleType> activeRules = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetupRules(int night)
    {
        activeRules.Clear();

        activeRules.Add(RuleType.NoFlickerShoot);
        activeRules.Add(RuleType.TwinsTurnOffLight);
        activeRules.Add(RuleType.BackDoorLocked);
        activeRules.Add(RuleType.HideWhenFootstep);

        if (night >= 2)
            activeRules.Add(RuleType.StudioTimeLimit);

        if (night >= 3)
            activeRules.Add(RuleType.ClownDoorOpen);
    }

    // ===============================
    // CHECK FUNCTIONS
    // ===============================

    public void CheckShoot()
    {
        if (!activeRules.Contains(RuleType.NoFlickerShoot)) return;

        if (RuleContext.Instance.IsFlickering)
            BreakRule(RuleType.NoFlickerShoot);
    }

    public void CheckBackDoor()
    {
        if (!activeRules.Contains(RuleType.BackDoorLocked)) return;

        if (!RuleContext.Instance.IsBackDoorLocked)
            BreakRule(RuleType.BackDoorLocked);
    }

    public void CheckFootstep()
    {
        if (!activeRules.Contains(RuleType.HideWhenFootstep)) return;

        if (RuleContext.Instance.IsFootstepActive &&
            !RuleContext.Instance.IsPlayerInToilet)
        {
            BreakRule(RuleType.HideWhenFootstep);
        }
    }

    private void Update()
    {
        CheckStudioTimeLimit();
        CheckClownDoor();
    }

    void CheckStudioTimeLimit()
    {
        if (!activeRules.Contains(RuleType.StudioTimeLimit)) return;

        if (RuleContext.Instance.StudioEnterTime <= 0) return;

        if (Time.time - RuleContext.Instance.StudioEnterTime > 15f)
        {
            BreakRule(RuleType.StudioTimeLimit);
            RuleContext.Instance.StudioEnterTime = 0;
        }
    }

    void CheckClownDoor()
    {
        if (!activeRules.Contains(RuleType.ClownDoorOpen)) return;

        if (!RuleContext.Instance.IsClownAppeared) return;

        if (Time.time - RuleContext.Instance.ClownAppearTime > 5f)
        {
            EventManager.Instance.TriggerEvent("InstantGameOver");
        }
    }

    void BreakRule(RuleType rule)
    {
        Debug.Log("Rule Broken: " + rule);
        GameProgress.Instance.AddError();
        EventManager.Instance.TriggerEvent("RuleBrokenEffect");
    }
}