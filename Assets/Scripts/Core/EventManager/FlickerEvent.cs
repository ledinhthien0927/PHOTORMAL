using UnityEngine;
using System.Collections;

public class FlickerEvent : MonoBehaviour, IGameEvent
{
    public void Execute()
    {
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        RuleContext.Instance.IsFlickering = true;

        Debug.Log("Studio Flickering");

        yield return new WaitForSeconds(4f);

        RuleContext.Instance.IsFlickering = false;
    }

    private void Start()
    {
        EventManager.Instance.RegisterEvent("Flicker", this);
    }
}