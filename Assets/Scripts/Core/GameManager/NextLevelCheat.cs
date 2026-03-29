using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NextLevelCheat : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(SkipLevel);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(SkipLevel);
        }
    }

    /// <summary>
    /// Qua màn nhanh: gọi popup chiến thắng hoặc nhảy thẳng sang đêm tiếp theo.
    /// </summary>
    public void SkipLevel()
    {
        Debug.Log("[NextLevelCheat] Skip Level Triggered!");

        if (PopupManager.Instance != null)
        {
            // Hiển thị phần UI Hoàn thành nhiệm vụ ngày (Mission Day X Completed)
            PopupManager.Instance.ShowWinNight();
        }
        else if (NightManager.Instance != null)
        {
            // Nếu không có bảng thông báo Win, thì nhảy thẳng qua ngày tiếp theo luôn
            NightManager.Instance.AdvanceToNextNight();
        }
        else
        {
            Debug.LogWarning("[NextLevelCheat] Không tìm thấy PopupManager hay NightManager trong scene này để bỏ qua màn!");
        }
    }
}
