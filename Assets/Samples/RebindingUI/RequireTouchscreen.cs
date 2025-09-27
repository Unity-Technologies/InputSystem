using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A simple utility script that disables/enables its associated GameObject based on the presence of a Touchscreen
/// device.
/// </summary>
public class RequireTouchscreen : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChanged;
        UpdateState();
    }

    private void OnDisable()
    {
        UpdateState();
        InputSystem.onDeviceChange -= OnDeviceChanged;
    }

    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        UpdateState();
    }

    private void UpdateState()
    {
        var touchscreenIsPresent = Touchscreen.current != null;
        var activeInHierarchy = gameObject.activeInHierarchy || gameObject.activeSelf;
        if (activeInHierarchy && !touchscreenIsPresent)
            gameObject.SetActive(false);
        else if (!activeInHierarchy && touchscreenIsPresent)
            gameObject.SetActive(true);
    }
}
