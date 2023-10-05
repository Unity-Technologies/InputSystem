using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionContainer: MonoBehaviour
{
    public InputAction myAction;
    public InputActionReference myReference;
    public InputActionReference mySecondReferenceWithAVeryLongName;
    public InputActionProperty myProperty;
    public InputActionAsset myAsset;
    public InputControl myControl;

    // Start is called before the first frame update
    void Start()
    {
        if (myReference.action != null)
            myReference.action.performed += context => { Debug.Log("Performed"); };
    }

    // Update is called once per frame
    void Update()
    {
    }
}