using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

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

    [Test]
    [Category("API")]
    public void API_JoysticksAreTaggedByTheirIndex()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var joystick = InputSystem.AddDevice<Joystick>();

        Assert.That(gamepad.usages, Does.Contain(new InternedString("Joystick1")));
        Assert.That(joystick.usages, Does.Contain(new InternedString("Joystick2")));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(gamepad.usages, Does.Not.Contain(new InternedString("Joystick1")));
        Assert.That(joystick.usages, Does.Contain(new InternedString("Joystick2")));
    }

    [Test]
    [Category("API")]
    public void API_CanGetKeyCodeForJoystickButton()
    {
        Assert.That(KeyCode.JoystickButton0.ForJoystick(1), Is.EqualTo(KeyCode.Joystick1Button0));
        Assert.That(KeyCode.JoystickButton0.ForJoystick(5), Is.EqualTo(KeyCode.Joystick5Button0));
        Assert.That(KeyCode.JoystickButton2.ForJoystick(1), Is.EqualTo(KeyCode.Joystick1Button2));
        Assert.That(KeyCode.JoystickButton4.ForJoystick(6), Is.EqualTo(KeyCode.Joystick6Button4));
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadJoystickButtonsThroughGetKeyAPI()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        Press(gamepad1.buttonSouth);
        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        Press(gamepad2.buttonSouth);
        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        Release(gamepad1.buttonSouth);
        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.True);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        Release(gamepad2.buttonSouth);
        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.True);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.True);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);

        yield return null;

        Assert.That(Input.GetKey(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.JoystickButton0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick1Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick2Button0), Is.False);
        Assert.That(Input.GetKey(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyUp(KeyCode.Joystick3Button0), Is.False);
        Assert.That(Input.GetKeyDown(KeyCode.Joystick3Button0), Is.False);
    }

    [Test]
    [Category("API")]
    public void API_CanReadJoystickButtonsThroughGetButtonAPI()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.Fail();
    }
}
