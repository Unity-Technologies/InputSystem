using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Experimental.Input;

public class DeviceList : MonoBehaviour
{
    public Text deviceText;

    // Use this for initialization
    void Start()
    {
        UpdateDeviceText();

        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        UpdateDeviceText();
    }

    void UpdateDeviceText()
    {
        string deviceAccumulator = "";
        for (int i = 0; i < InputSystem.devices.Count; i++)
        {
            deviceAccumulator += "<" + i + "> " + InputSystem.devices[i].displayName + "\n";
        }

        if (deviceText != null)
        {
            deviceText.text = deviceAccumulator;
        }
    }
}
