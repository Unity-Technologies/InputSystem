using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class OnMouseEventDragSample : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;

    private Vector3 startPosition;
    private bool wasDragging => Vector3.Distance(startPosition, transform.position) > 0.01f;
    private bool selected;

    // This logic starts the drag operation and safes the offset of the object position to the cursor position, to preserve the relative position.
    void OnMouseDown()
    {
        if (Pointer.current == null)
            return;

        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        float x = Pointer.current.position.x.value;
        float y = Pointer.current.position.y.value;
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(x, y, screenPoint.z));
        startPosition = transform.position;
    }

    // This logic allows dragging the object.
    void OnMouseDrag()
    {
        if (Pointer.current == null)
            return;

        Vector3 curScreenPoint = new Vector3(Pointer.current.position.x.value, Pointer.current.position.y.value, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }

    // This can be used to select objects. This logic allows dragging without selection.
    private void OnMouseUp()
    {
        if (wasDragging)
            return;

        selected = !selected;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", !selected ? Color.blue : Color.white);
    }
}
