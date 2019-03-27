using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XR;

using UnityEngine.UI;

public class XRHaptics : MonoBehaviour
{
    public Image leftHapticDetected;
    public Image leftTryingToRumble;
    public Image leftTryingToRumbleHalf;
    public Image rightHapticDetected;
    public Image rightTryingToRumble;
    public Image rightTryingToRumbleHalf;

    public float m_RumblePeriod = 1f;

    private RumbleState state;
    private float m_Timer = 0f;

    private enum RumbleState
    {
        Left,
        LeftHalf,
        Right,
        RightHalf
    }

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

            XRControllerWithRumble controller;

            switch (state)
            {
                case RumbleState.Left:
                    controller = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.LeftHand);
                    controller.SendImpulse(1f, m_RumblePeriod);
                    leftTryingToRumble.color = Color.red;
                    leftTryingToRumbleHalf.color = Color.white;
                    rightTryingToRumble.color = Color.white;
                    rightTryingToRumbleHalf.color = Color.white;
                    state = RumbleState.LeftHalf;
                    break;
                case RumbleState.LeftHalf:
                    controller = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.LeftHand);
                    controller.SendImpulse(0.5f, m_RumblePeriod);

                    leftTryingToRumble.color = Color.white;
                    leftTryingToRumbleHalf.color = Color.red;
                    rightTryingToRumble.color = Color.white;
                    rightTryingToRumbleHalf.color = Color.white;
                    state = RumbleState.Right;
                    break;
                case RumbleState.Right:
                    controller = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.RightHand);
                    controller.SendImpulse(1f, m_RumblePeriod);
                    leftTryingToRumble.color = Color.white;
                    leftTryingToRumbleHalf.color = Color.white;
                    rightTryingToRumble.color = Color.red;
                    rightTryingToRumbleHalf.color = Color.white;
                    state = RumbleState.RightHalf;
                    break;
                case RumbleState.RightHalf:
                    controller = InputSystem.GetDevice<XRControllerWithRumble>(CommonUsages.RightHand);
                    controller.SendImpulse(0.5f, m_RumblePeriod);
                    leftTryingToRumble.color = Color.white;
                    leftTryingToRumbleHalf.color = Color.white;
                    rightTryingToRumble.color = Color.white;
                    rightTryingToRumbleHalf.color = Color.red;
                    state = RumbleState.Left;
                    break;
            }
        }
    }
}
