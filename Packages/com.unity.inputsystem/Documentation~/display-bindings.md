### Displaying Bindings

It can be useful for the user to know what an Action is currently bound to (taking any potentially active rebindings into account) while rebinding UIs, and for on-screen hints while the app is running. You can use [`InputBinding.effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) to get the currently active path for a Binding (which returns [`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath) if set, or otherwise returns [`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)).

The easiest way to retrieve a display string for an action is to call [`InputActionRebindingExtensions.GetBindingDisplayString`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetBindingDisplayString_) which is an extension method for [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html).

```CSharp
    // Get a binding string for the action as a whole. This takes into account which
    // bindings are currently active and the actual controls bound to the action.
    m_RebindButton.GetComponentInChildren<Text>().text = action.GetBindingDisplayString();

    // Get a binding string for a specific binding on an action by index.
    m_RebindButton.GetComponentInChildren<Text>().text = action.GetBindingDisplayString(1);

    // Look up binding indices with GetBindingIndex.
    var bindingIndex = action.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
    m_RebindButton.GetComponentInChildren<Text>().text =
        action.GetBindingDisplayString(bindingIndex);
```

You can also use this method to replace the text string with images.

```CSharp
    // Call GetBindingDisplayString() such that it also returns information about the
    // name of the device layout and path of the control on the device. This information
    // is useful for reliably associating imagery with individual controls.
    // NOTE: The first argument is the index of the binding within InputAction.bindings.
    var bindingString = action.GetBindingDisplayString(0, out deviceLayout, out controlPath);

    // If it's a gamepad, look up an icon for the control.
    Sprite icon = null;
    if (!string.IsNullOrEmpty(deviceLayout)
        && !string.IsNullOrEmpty(controlPath)
        && InputSystem.IsFirstLayoutBasedOnSecond(deviceLayout, "Gamepad"))
    {
        switch (controlPath)
        {
            case "buttonSouth": icon = aButtonIcon; break;
            case "dpad/up": icon = dpadUpIcon; break;
            //...
        }
    }

    // If you have an icon, display it instead of the text.
    var text = m_RebindButton.GetComponentInChildren<Text>();
    var image = m_RebindButton.GetComponentInChildren<Image>();
    if (icon != null)
    {
        // Display icon.
        text.gameObject.SetActive(false);
        image.gameObject.SetActive(true);
        image.sprite = icon;
    }
    else
    {
        // Display text.
        text.gameObject.SetActive(true);
        image.gameObject.SetActive(false);
        text.text = bindingString;
    }
```

Additionally, each Binding has a [`ToDisplayString`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_ToDisplayString_UnityEngine_InputSystem_InputBinding_DisplayStringOptions_UnityEngine_InputSystem_InputControl_) method, which you can use to turn individual Bindings into display strings. There is also a generic formatting method for Control paths, [`InputControlPath.ToHumanReadableString`](../api/UnityEngine.InputSystem.InputControlPath.html#UnityEngine_InputSystem_InputControlPath_ToHumanReadableString_System_String_UnityEngine_InputSystem_InputControlPath_HumanReadableStringOptions_UnityEngine_InputSystem_InputControl_), which you can use with arbitrary Control path strings.

Note that the Controls a Binding resolves to can change at any time, and the display strings for controls might change dynamically. For example, if the user switches the currently active keyboard layout, the display string for each individual key on the [`Keyboard`](../api/UnityEngine.InputSystem.Keyboard.html) might change.