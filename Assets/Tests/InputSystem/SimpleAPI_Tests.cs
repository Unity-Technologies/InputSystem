using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem.HighLevelAPI;

internal partial class CoreTests
{
    [Test]
    public void SimpleAPI_Test()
    {
        Input.IsPressed(GamepadButton.DpadLeft);

        var a = Input.IsPressed(KeyboardButton.Digit1);

        Input.WasUp(GamepadButton.Start);

        Input.GetAxis(GamepadTwoWayAxis.LeftStickHorizontal);

        var b = Input.GetAxis(GamepadStick.Left);

        Input.WasDown(MouseButton.ScrollUp);

        Input.GetAxis(MouseTwoWayAxis.ScrollHorizontalTimeNormalized);

        //Input.IsPressed2(KeyboardButton.Digit1, DeviceSlot.Slot2);

        var foo = new InputControlReference[]
        {
            MouseButton.Left,
            MouseButton.Right,
            KeyboardButton.Space
        };
        
        var anyPressed = foo.Select(x => Input.ByReference.IsButtonPressed(x, DeviceSlot.Any)).Any();
    }
}