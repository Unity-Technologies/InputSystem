using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XR;

public class Handedness : MonoBehaviour
{
    public Text statusText;

    // Update is called once per frame
    void Update()
    {
        string handedness = "";

        if (XRController.leftHand != null)
            handedness += "Left ";
        if (XRController.rightHand != null)
            handedness += "Right";
        if ((XRController.leftHand == null) && (XRController.rightHand == null))
            handedness = "None";

        statusText.text = handedness;
    }
}
