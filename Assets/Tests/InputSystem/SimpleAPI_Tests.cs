using NUnit.Framework;
using UnityEngine.InputSystem.HighLevelAPI;

internal partial class CoreTests
{
    [Test]
    public void SimpleAPI_Test()
    {
        var a = Input.IsPressed(KeyboardButton.Digit1);

        Input.WasUp(GamepadButton.Start);

        Input.GetAxis(GamepadTwoWayAxis.LeftStickHorizontal);

        var b = Input.GetAxis(GamepadStick.Left);

        Input.WasDown(MouseButton.ScrollUp);

        Input.GetAxis(MouseTwoWayAxis.PositionHorizontal);
    }
}