using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickOnDropdown : MonoBehaviour {

    public delegate void OnClickDropdownDelegate();
    public static OnClickDropdownDelegate clickDropdown;

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (eventData.button == PointerEventData.InputButton.Left)
    //        clickDropdown();
    //}

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
            clickDropdown();
    }
}
