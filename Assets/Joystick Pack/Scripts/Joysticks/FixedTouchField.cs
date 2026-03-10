using UnityEngine;
using UnityEngine.EventSystems;

public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [HideInInspector]
    public Vector2 TouchDist;
    [HideInInspector]
    public Vector2 PointerOld;
    [HideInInspector]
    protected int PointerId;
    [HideInInspector]
    public bool Pressed;

    private Vector2 _accumulatedDelta;

    public void OnPointerDown(PointerEventData eventData)
    {
        Pressed = true;
        PointerId = eventData.pointerId;
        PointerOld = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Pressed = false;
        _accumulatedDelta = Vector2.zero;
        TouchDist = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Accumulate delta from Unity's Event System, which correctly handles multi-touch pointer IDs
        _accumulatedDelta += eventData.delta;
        PointerOld = eventData.position;
    }

    void Update()
    {
        if (Pressed)
        {
            // Sync the accumulated delta to TouchDist for other scripts to read
            TouchDist = _accumulatedDelta;

            // Reset accumulator for the next frame
            _accumulatedDelta = Vector2.zero;
            
            // Fallback for Editor mouse if not dragging but pressed (unlikely to be needed but safe)
            if (TouchDist == Vector2.zero && Application.isEditor)
            {
                // TouchDist = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - PointerOld;
                // PointerOld = Input.mousePosition;
            }
        }
        else
        {
            TouchDist = Vector2.zero;
        }
    }
}