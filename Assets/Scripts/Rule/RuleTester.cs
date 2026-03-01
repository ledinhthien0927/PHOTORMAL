using UnityEngine;

public class RuleTester : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("press F, P, C, T");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            RuleContext.Instance.IsFlickering = true;
            Debug.Log("Flicker ON");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            RuleManager.Instance.CheckShoot();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            RuleContext.Instance.IsClownAppeared = true;
            RuleContext.Instance.ClownAppearTime = Time.time;
            Debug.Log("Clown Appeared");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            RuleContext.Instance.StudioEnterTime = Time.time;
            Debug.Log("Entered Studio");
        }
    }
}