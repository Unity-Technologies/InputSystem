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
        stick.SetupInputControl("/<Gamepad>/leftStick");
        var device = InputSystem.devices.FirstOrDefault(x => x is Gamepad);
        Assert.That(device, Is.Not.Null);
        Assert.That(device["dpad"], Is.Not.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenButton()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.SetupInputControl("/<Keyboard>/a");
        var device = InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(device, Is.Not.Null);
        Assert.That(device["space"], Is.Not.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenButtonSendsButtonPushToDevice()
    {
        var gameObject = new GameObject();
        var button = gameObject.AddComponent<OnScreenButton>();
        button.SetupInputControl("/<Keyboard>/a");
        var keyboard = (Keyboard)InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(keyboard.aKey.isPressed, Is.False);
        button.OnPointerDown(null);
        Assert.That(keyboard.aKey.isPressed, Is.True);
        button.OnPointerUp(null);
        Assert.That(keyboard.aKey.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_OnScreenStickSendsMovementToDevice()
    {
        var gameObject = new GameObject();
        var stick = gameObject.AddComponent<OnScreenStick>();
        stick.SetupInputControl("/<Gamepad>/leftStick");
        var gamepad = (Gamepad)InputSystem.devices.FirstOrDefault(x => x is Gamepad);
        stick.OnDrag(null);
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.5).Within(0.00001));
        stick.OnPointerUp(null);
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.0).Within(0.00001));
    }
}
