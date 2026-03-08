using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerAreaTrigger : MonoBehaviour
{
    public enum AreaType
    {
        Studio,
        Toilet
    }

    [Header("Area Configuration")]
    [Tooltip("Chọn loại khu vực để bắn sự kiện tương ứng")]
    [SerializeField] private AreaType areaType = AreaType.Studio;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[AreaTrigger] Player ENTER: {areaType}");
            if (areaType == AreaType.Studio)
            {
                GameEventAPI.OnPlayerStudioStateChanged?.Invoke(true);
            }
            else if (areaType == AreaType.Toilet)
            {
                GameEventAPI.OnPlayerToiletStateChanged?.Invoke(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[AreaTrigger] Player EXIT: {areaType}");
            if (areaType == AreaType.Studio)
            {
                GameEventAPI.OnPlayerStudioStateChanged?.Invoke(false);
            }
            else if (areaType == AreaType.Toilet)
            {
                GameEventAPI.OnPlayerToiletStateChanged?.Invoke(false);
            }
        }
    }
}
