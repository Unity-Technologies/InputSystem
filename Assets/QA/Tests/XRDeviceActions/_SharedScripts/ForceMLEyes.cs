using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.MagicLeap;

public class ForceMLEyes : MonoBehaviour
{
    void Update()
    {
        MagicLeapLightwear device = InputSystem.GetDevice<MagicLeapLightwear>();
        if(device != null)
        {
            if(!device.EyesEnabled)
            {
                Debug.LogError("TOMB Setting Eyes Value Via Script");
                device.EyesEnabled = true;
                Debug.LogError($"TOMB Eyes Value is now {device.EyesEnabled}");
            }
        }
    }
}
