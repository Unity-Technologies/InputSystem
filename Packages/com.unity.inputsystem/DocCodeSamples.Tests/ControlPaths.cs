using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

class ControlPathsExample
{
    void Example()
    {
        #region parse
        var parsed = InputControlPath.Parse("<XRController>{LeftHand}/trigger").ToArray();

        Debug.Log(parsed.Length); // Prints 2.
        Debug.Log(parsed[0].layout); // Prints "XRController".
        Debug.Log(parsed[0].name); // Prints an empty string.
        Debug.Log(parsed[0].usages.First()); // Prints "LeftHand".
        Debug.Log(parsed[1].layout); // Prints null.
        Debug.Log(parsed[1].name); // Prints "trigger".
        #endregion

        #region findcontrols
        var gamepad = Gamepad.all[0];
        var leftStickX = gamepad["leftStick/x"];
        var submitButton = gamepad["{Submit}"];
        var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
        #endregion
    }

    void PathExamples()
    {
        #region pathExamples
        // Matches all gamepads (also gamepads *based* on the Gamepad layout):
        "<Gamepad>";
        // Matches the "Submit" control on all devices:
        "*/";
        // Matches the key that prints the "a" character on the current keyboard layout:
        "<Keyboard>/#(a)";
        // Matches the X axis of the left stick on a gamepad.
        "<Gamepad>/leftStick/x";
        // Matches the orientation control of the right-hand XR controller:
        "<XRController>/orientation";
        // Matches all buttons on a gamepad.
        "<Gamepad>/<Button>";
        #endregion
    }
}