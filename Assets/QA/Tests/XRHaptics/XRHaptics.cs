using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XR;

using UnityEngine.UI;

public class XRHaptics : MonoBehaviour
{
    public Image leftHapticDetected;
    public Image leftTryingToRumble;
    public Image rightHapticDetected;
    public Image rightTryingToRumble;

    bool m_LeftHandRumbling = false;
    float m_RumblePeriod = 1f;
    float m_Timer = 0f;

    void Update()
    {
        XRControllerWithRumble leftHandController = XRController.leftHand as XRControllerWithRumble;
        leftHapticDetected.color = (leftHandController != null) ? Color.red : Color.white;
        if (leftHandController != null)
        {
            leftHandController.SetIntensity(1f);

            leftTryingToRumble.color = (m_LeftHandRumbling ? Color.red : Color.white);
        }

        XRControllerWithRumble rightHandController = XRController.rightHand as XRControllerWithRumble;
        rightHapticDetected.color = (rightHandController != null) ? Color.red : Color.white;
        if (rightHandController != null)
        {
            rightHandController.SetIntensity(1f);

            rightTryingToRumble.color = !m_LeftHandRumbling ? Color.red : Color.white;
        }

        UpdateTimer();
    }

    // Swap rumble between left and right hand controllers about once per period.
    void UpdateTimer()
    {
        m_Timer += Time.deltaTime;

        if (m_Timer >= m_RumblePeriod)
        {
            m_Timer = 0f;
            m_LeftHandRumbling = !m_LeftHandRumbling;
        }
    }
}
