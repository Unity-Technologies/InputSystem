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

    public float m_RumblePeriod = 1f;
    public float m_Amplitude = 1f;

    private bool m_LeftHandRumbling = false;
    private float m_Timer = 0f;

    public void Update()
    {
        var leftHandController = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.LeftHand);
        leftHapticDetected.color = leftHandController != null ? Color.red : Color.white;

        var rightHandController = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.RightHand);
        rightHapticDetected.color = rightHandController != null ? Color.red : Color.white;

        UpdateTimer();
    }

    // Swap rumble between left and right hand controllers about once per period.
    private void UpdateTimer()
    {
        m_Timer += Time.deltaTime;

        if (m_Timer >= m_RumblePeriod)
        {
            m_Timer -= m_RumblePeriod;
            m_LeftHandRumbling = !m_LeftHandRumbling;

            var controller = InputSystem.GetDevice<XRControllerWithRumble>(m_LeftHandRumbling ? CommonUsages.LeftHand : CommonUsages.RightHand);
            Image controllerRumbleImage = m_LeftHandRumbling ? leftTryingToRumble : rightTryingToRumble;

            if (controller != null)
            {
                controller.SendImpulse(1f, m_RumblePeriod);
                controllerRumbleImage.color = Color.red;
            }
            else
            {
                controllerRumbleImage.color = Color.white;
            }

            Image otherControllerImage = m_LeftHandRumbling ? rightTryingToRumble : leftTryingToRumble;
            otherControllerImage.color = Color.white;
        }
    }
}
