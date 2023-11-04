// This example showcases how to use on-screen controls and showcases how to use
// OnScreenStick and OnScreenButton which are two types of on-screen controls included with the Input System.
// In this example, multiple OnScreenButton component instances and OnScreenStick component instances are
// attached to a Canvas to build an on-screen gamepad.
//
// The OnScreenControl game objects LeftStick, RightStick, A, B, X, Y have all been configured to associated
// with a gamepad layout. This implies that they generate input similar to a real hardware gamepad.
// Due to this, InputActions bound to gamepad controls such as the controls mentioned above will hence be
// triggered when the user interacts with the on-screen controls.
// This example uses actions defined in OnScreenControlsSampleActions.inputactions which are preconfigured to
// bind to gamepad controls. Hence this example will work the same way when using either the on-screen controls
// or a physical gamepad.
//
// Note that on-screen controls 1 and 2 on the other hand are bound to a keyboard control layout and therefore
// generates input events similar to a physical hardware keyboard. 
//
// When actions defined for this sample are triggered, a log message is generated to show when the actions
// are performed.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OnScreenControlsSample : MonoBehaviour
{
    public InputActionAsset inputActions;

    List<InputAction> m_EnabledActions;

    void OnEnable()
    {
        // Enable all actions in associated inputActions and add an event handler to log when action is performed.
        m_EnabledActions = new List<InputAction>();
        foreach (var actionMap in inputActions.actionMaps)
        {
            foreach (var action in actionMap)
            {
                action.Enable();
                action.performed += Log;
                m_EnabledActions.Add(action);
            }
        }
    }

    void OnDisable()
    {
        // Revert action handler and disable the action to clean-up from what we did in OnEnable().
        foreach (var action in m_EnabledActions)
        {
            action.performed -= Log;
            action.Disable();
        }
    }
    
    void Log(InputAction.CallbackContext context)
    {
        Debug.Log($"Performed {context.action.name} in frame:{Time.frameCount}");
    }
}
