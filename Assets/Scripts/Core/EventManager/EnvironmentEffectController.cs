using UnityEngine;
using System.Collections;

public class EnvironmentEffectController : MonoBehaviour, IGameEvent
{
    [Header("Studio Lights")]
    [SerializeField] private Light[] studioLights;
    [SerializeField] private float flickerSpeed = 0.1f;

    [Header("Audio")]
    // Footstep and Knock audio are now managed via AudioManager

    [Header("Models & GameObjects")]
    [SerializeField] private GameObject twinsModel;
    [SerializeField] private GameObject clownModel;

    [Header("UI / Screen Effects")]
    [SerializeField] private GameObject staticGlitchEffect; // Hiệu ứng nhiễu màn hình khi vi phạm luật

    private Coroutine flickerCoroutine;

    private void Start()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RegisterEvent("RuleBrokenEffect", this);
        }
    }

    public void Execute()
    {
        HandleRuleBroken();
    }


    private void OnEnable()
    {
        // 1. Đăng ký lắng nghe sự kiện từ GameEventAPI
        GameEventAPI.OnStudioLightFlicker += HandleLightFlicker;
        GameEventAPI.OnTwinsPresenceChanged += HandleTwinsPresence;
        GameEventAPI.OnFootstepToggled += HandleFootstep;
        GameEventAPI.OnClownAppeared += HandleClownAppears;
        GameEventAPI.OnClownDisappeared += HandleClownDisappears;
        GameEventAPI.OnClownJumpscare += HandleClownJumpscare;
        GameEventAPI.OnDeliveryKnock += HandleDeliveryKnock;
        GameEventAPI.OnRuleBroken += HandleRuleBroken;
    }

    private void OnDisable()
    {
        // 2. Hủy đăng ký khi object bị vô hiệu hóa để tránh rò rỉ bộ nhớ (Memory Leak)
        GameEventAPI.OnStudioLightFlicker -= HandleLightFlicker;
        GameEventAPI.OnTwinsPresenceChanged -= HandleTwinsPresence;
        GameEventAPI.OnFootstepToggled -= HandleFootstep;
        GameEventAPI.OnClownAppeared -= HandleClownAppears;
        GameEventAPI.OnClownDisappeared -= HandleClownDisappears;
        GameEventAPI.OnClownJumpscare -= HandleClownJumpscare;
        GameEventAPI.OnDeliveryKnock -= HandleDeliveryKnock;
        GameEventAPI.OnRuleBroken -= HandleRuleBroken;
    }

    // ==========================================
    // 3. XỬ LÝ TỪNG HIỆU ỨNG KHI SỰ KIỆN XẢY RA
    // ==========================================

    private void HandleLightFlicker(bool isFlickering)
    {
        if (isFlickering)
        {
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }
        else
        {
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
            // Đảm bảo đèn bật lại bình thường khi hết nhấp nháy
            foreach (var light in studioLights)
            {
                if (light != null) light.enabled = true;
            }
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            foreach (var light in studioLights)
            {
                if (light != null) light.enabled = !light.enabled;
            }
            yield return new WaitForSeconds(flickerSpeed);
        }
    }

    private void HandleTwinsPresence(bool isPresent)
    {
        if (twinsModel != null)
        {
            twinsModel.SetActive(isPresent);
        }
    }

    private void HandleFootstep(bool isPlaying)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep(isPlaying);
        }
    }

    private void HandleClownAppears()
    {
        if (clownModel != null)
        {
            clownModel.SetActive(true);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClownAppear();
            }
        }
    }

    private void HandleClownDisappears()
    {
        if (clownModel != null)
        {
            clownModel.SetActive(false);
        }
    }

    private void HandleClownJumpscare()
    {
        Debug.Log("PHÁT VIDEO JUMPSCARE Ở ĐÂY RỒI MỚI GỌI GAMEOVER!");
        // Chèn logic chạy cutscene/video jumpscare ở đây
    }

    private void HandleDeliveryKnock()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayKnockDoor();
        }
    }

    private void HandleRuleBroken() 
    {
        // Khi vi phạm luật: chớp màn hình nhiễu
        if (staticGlitchEffect != null)
        {
            StartCoroutine(ShowGlitchEffectRoutine());
        }
    }

    private IEnumerator ShowGlitchEffectRoutine()
    {
        if (staticGlitchEffect != null) staticGlitchEffect.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Hiện nhiễu trong 0.5s rồi tắt
        if (staticGlitchEffect != null) staticGlitchEffect.SetActive(false);
    }

    // ==========================================
    // 4. API CHO SPAWNER GÁN MODEL
    // ==========================================

    public void SetTwinsModel(GameObject model)
    {
        twinsModel = model;
        if (twinsModel != null) twinsModel.SetActive(false);
    }

    public void SetClownModel(GameObject model)
    {
        clownModel = model;
        if (clownModel != null) clownModel.SetActive(false);
    }
}
