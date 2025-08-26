using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class OnMouseEventSample : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;

    void OnMouseDown()
    {
        if (Pointer.current == null)
            return;

        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        float x = Pointer.current.position.x.value;
        float y = Pointer.current.position.y.value;
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(x, y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        if (Pointer.current == null)
            return;

        Vector3 curScreenPoint = new Vector3(Pointer.current.position.x.value, Pointer.current.position.y.value, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }
}
