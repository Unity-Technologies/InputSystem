using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.OnScreen;


public class OnScreenTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateOnScreenDevice()
    {
        var gameObject = new GameObject();
        var button1 = gameObject.AddComponent<OnScreenButton>();
        button1.SetControlPath("/<Keyboard>/a");

        var device = InputSystem.devices.FirstOrDefault(x => x is Keyboard);
        Assert.That(device, Is.Not.Null);

        Assert.That(device["space"], Is.Not.Null);

        Assert.That(button1.m_Control.displayName, Is.EqualTo("a"));

        var matches = InputSystem.GetControls("/<Keyboard>");

        Assert.That(matches, Has.Count.EqualTo(1));
    }
}
