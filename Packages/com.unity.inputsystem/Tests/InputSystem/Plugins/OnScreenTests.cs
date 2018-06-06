using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.OnScreen;


public class OnScreenTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenButton()
    {
        // Assign the real Keyboard button A to this virtual button
        var gameObject = new GameObject();
        var button1 = gameObject.AddComponent<OnScreenButton>();
        button1.SetupInputControl("/<Keyboard>/a");

        // Verify a keyboard input device was created
        var device = InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(device, Is.Not.Null);
        Assert.That(device["space"], Is.Not.Null);
    }
}
