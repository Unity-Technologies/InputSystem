using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// TODO Seems like we actually assign reference to null which is wrong, could we provide custom setters
//      and getters?

public class Dummy : MonoBehaviour
{
    public InputAction myAction;
    public InputActionReference myReference;
    //public InputActionReference mySecondReferenceWithAVeryLongName;
    public InputActionProperty myProperty;
    public InputActionAsset myAsset;
    public InputControl myControl;
    public InputActionMap myMap;
    public InputActionReference[] myReferenceContainer;

    // Start is called before the first frame update
    void OnEnable()
    {
        if (myReference != null && myReference.action != null)
            myReference.action.performed += OnPerformed;
    }

    void OnDisable()
    {
        if (myReference != null && myReference.action != null)
            myReference.action.performed -= OnPerformed;
    }

    void OnPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Performed: " + context.action.name);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
