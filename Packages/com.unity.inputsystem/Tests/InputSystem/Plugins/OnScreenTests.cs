using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.OnScreen;


public class OnScreenTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenButtonAssignedToKeyboard()
    {
        // Assign the real Keyboard button A to this virtual button
        var gameObject = new GameObject();
        var button1 = gameObject.AddComponent<OnScreenButton>();
        button1.controlPath = "/<Keyboard>/a";

        // Verify a keyboard input device was created and the input control
        // is the A key.
        var device = InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        var matches = InputSystem.GetControls("/<Keyboard>");
        Assert.That(device, Is.Not.Null);
        Assert.That(device["space"], Is.Not.Null);
        Assert.That(button1.inputControl.displayName, Is.EqualTo("a"));
        Assert.That(matches, Has.Count.EqualTo(1));
    }
}
