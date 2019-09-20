using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_MAGIC_LEAP
using UnityEngine.XR.MagicLeap;
#endif

public class ControlMLEyes : MonoBehaviour
{
    public Toggle uiToggle;

#if UNITY_MAGIC_LEAP
    void Update()
    {
        if (uiToggle == null)
            return;

        bool desiredState = uiToggle.isOn;

        MagicLeapLightwear device = InputSystem.GetDevice<MagicLeapLightwear>();
        if (device != null)
        {
            if (desiredState != device.EyesEnabled)
            {
                device.EyesEnabled = desiredState;
            }
        }
    }

#endif
}
