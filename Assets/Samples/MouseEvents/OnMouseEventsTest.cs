using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class OnMouseEventsTest : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;

    void OnMouseDown()
    {
        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        float x;
        float y;
        if (Pointer.current != null)
        {
            x = Pointer.current.position.x.value;
            y = Pointer.current.position.y.value;
        }
        else  // Fallback to InputManager if InputSystem is not available
        {
            x = Input.mousePosition.x;
            y = Input.mousePosition.y;
        }
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(x, y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        Vector3 curScreenPoint;
        if (Pointer.current != null)
            curScreenPoint = new Vector3(Pointer.current.position.x.value, Pointer.current.position.y.value, screenPoint.z);
        else // Fallback to InputManager if InputSystem is not available
            curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);

        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }
}
