using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XR;

public class GearVRHandedness : MonoBehaviour {

    public Text statusText;

	// Update is called once per frame
	void Update () {
        bool hasLeft = false;
        bool hasRight = false;
        string handedness = "";

		foreach (InputDevice device in InputSystem.devices)
        {
            if ((device as XRController) == null)
                continue;
            
            foreach (string usage in (device as XRController).usages)
            {
                if (usage == "LeftHand")
                    hasLeft = true;
                if (usage == "RightHand")
                    hasRight = true;
            }
        }

        if (!hasLeft && !hasRight)
            handedness = "none";
        if (hasLeft)
            handedness += "Left ";
        if (hasRight)
            handedness += "Right";

        statusText.text = handedness;
	}
}
