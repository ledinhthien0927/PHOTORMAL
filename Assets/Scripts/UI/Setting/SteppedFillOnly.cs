using UnityEngine;
using UnityEngine.UI;

public class SteppedFillOnly : MonoBehaviour
{
    [System.Serializable]
    public class Bar
    {
        [Header("Assign")]
        public Slider slider;      // Slider to read value from
        public Image fillImage;    // Image that will display the fill sprite

        [Header("Fill sprites (MUST be 6): index 0..5")]
        public Sprite[] fillSteps; // 0=empty ... 5=full
    }

    [Header("Bars")]
    [SerializeField] private Bar sound; // SOUND bar
    [SerializeField] private Bar music; // MUSIC bar

    void Awake()
    {
        // Setup both bars (Sound + Music)
        SetupBar(sound);
        SetupBar(music);
    }

    /// <summary>
    /// Configures a slider to be stepped (0..5) and wires up the callback.
    /// </summary>
    private void SetupBar(Bar bar)
    {
        if (bar == null || bar.slider == null || bar.fillImage == null) return;

        // Ensure slider snaps to 6 discrete values: 0,1,2,3,4,5
        bar.slider.minValue = 0;
        bar.slider.maxValue = 5;
        bar.slider.wholeNumbers = true;

        // When slider changes, refresh only this bar
        bar.slider.onValueChanged.AddListener(_ => RefreshBar(bar));

        // Refresh once at start
        RefreshBar(bar);
    }

    /// <summary>
    /// Updates the fill sprite based on slider value (0..5).
    /// </summary>
    private void RefreshBar(Bar bar)
    {
        if (bar.fillSteps == null || bar.fillSteps.Length < 6) return;

        int step = Mathf.Clamp(Mathf.RoundToInt(bar.slider.value), 0, 5);
        bar.fillImage.sprite = bar.fillSteps[step];
    }
}