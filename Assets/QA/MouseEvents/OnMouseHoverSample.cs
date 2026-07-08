using System;
using UnityEngine;

public class OnMouseHoverSample : MonoBehaviour
{
    private bool selected;

    // This can be used to select objects.
    private void OnMouseUpAsButton()
    {
        selected = !selected;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", selected ? Color.blue : Color.white);
    }

    // Highlight the object when the cursor is over it.
    private void OnMouseEnter()
    {
        if (selected)
            return;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
    }

    // Set back to a normal state.
    private void OnMouseExit()
    {
        if (selected)
            return;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
    }

    // This allows highlighting the object right after deselection, where the cursor is over the object.
    private void OnMouseOver()
    {
        if (selected)
            return;
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
    }
}
