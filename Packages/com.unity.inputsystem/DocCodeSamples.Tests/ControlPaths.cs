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
}