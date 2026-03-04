using UnityEngine;

public sealed class StudioLightFlicker : MonoBehaviour
{
    public bool IsFlickering { get; private set; }

    // You will drive this from your event system
    public void SetFlickering(bool flicker) => IsFlickering = flicker;
}