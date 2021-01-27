#if ENABLE_VR || ENABLE_AR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.InputSystem.XR;

public class Handedness : MonoBehaviour
{
    public Text statusText;

    public void Update()
    {
        var handedness = "";

        var leftHand = InputSystem.GetDevice<XRController>(CommonUsages.LeftHand);
        var rightHand = InputSystem.GetDevice<XRController>(CommonUsages.RightHand);

        if (leftHand != null)
            handedness += "Left ";
        if (rightHand != null)
            handedness += "Right";
        if (leftHand == null && rightHand == null)
            handedness = "None";

        statusText.text = handedness;
    }
}
#endif
