using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.OnScreen;

public class OnScreenTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenStick()
    {
        var gameObject = new GameObject();
        var stick = gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "/<Gamepad>/leftStick";
        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenButton()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "/<Keyboard>/a";
        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Keyboard>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenButtonSendsButtonPushToDeviceWithNoExistingDeviceYet()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "/<Keyboard>/a";
        var keyboard = (Keyboard)InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(keyboard.aKey.isPressed, Is.False);
        button.OnPointerDown(null);
        Assert.That(keyboard.aKey.isPressed, Is.True);
        button.OnPointerUp(null);
        Assert.That(keyboard.aKey.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenButtonSendsButtonPushToDeviceWithAlreadyExistingDevice()
    {
        InputSystem.AddDevice<Keyboard>();
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.controlPath = "/<Keyboard>/a";
        var keyboard = (Keyboard)InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(keyboard.aKey.isPressed, Is.False);
        button.OnPointerDown(null);
        Assert.That(keyboard.aKey.isPressed, Is.True);
        button.OnPointerUp(null);
        Assert.That(keyboard.aKey.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_MultipleOnScreenButtonInstancesPushToSameDevice()
    {
        var gameObject = new GameObject();
        var buttonMappedToKeyA = gameObject.AddComponent<OnScreenButton>();
        var buttonMappedToKeyB = gameObject.AddComponent<OnScreenButton>();

        buttonMappedToKeyA.controlPath = "/<Keyboard>/a";
        buttonMappedToKeyB.controlPath = "/<Keyboard>/b";

        var keyboard = (Keyboard)InputSystem.devices.FirstOrDefault(x => x is Keyboard);

        buttonMappedToKeyA.OnPointerDown(null);
        buttonMappedToKeyB.OnPointerDown(null);

        Assert.That(keyboard.aKey.isPressed, Is.True);
        Assert.That(keyboard.bKey.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenStickSendsMovementToDevice()
    {
        var gameObject = new GameObject();
        var stick = gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "/<Gamepad>/leftStick";
        var gamepad = (Gamepad)InputSystem.devices.FirstOrDefault(x => x is Gamepad);
        stick.OnDrag(null);
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.5).Within(0.00001));
        stick.OnPointerUp(null);
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.0).Within(0.00001));
    }
}
