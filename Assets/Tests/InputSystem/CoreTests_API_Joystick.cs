using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

partial class CoreTests
{
    [Test]
    [Category("API")]
    public void API_CanListJoysticksAndGamepadsThroughGetJoystickNamesAPI()
    {
        Assert.That(Input.GetJoystickNames(), Is.Not.Null);
        Assert.That(Input.GetJoystickNames(), Is.Empty);

        var joystick1 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Input.GetJoystickNames(), Is.EqualTo(new[]
        {
            joystick1.displayName,
        }));

        var joystick2 = InputSystem.AddDevice<Joystick>();

        Assert.That(Input.GetJoystickNames(), Is.EqualTo(new[]
        {
            joystick1.displayName,
            joystick2.displayName,
        }));

        InputSystem.RemoveDevice(joystick1);

        Assert.That(Input.GetJoystickNames(), Is.EqualTo(new[]
        {
            string.Empty,
            joystick2.displayName,
        }));

        InputSystem.AddDevice(joystick1);

        Assert.That(Input.GetJoystickNames(), Is.EqualTo(new[]
        {
            joystick1.displayName,
            joystick2.displayName,
        }));

        InputSystem.RemoveDevice(joystick1);
        var joystick3 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Input.GetJoystickNames(), Is.EqualTo(new[]
        {
            joystick3.displayName,
            joystick2.displayName,
        }));
    }
}
