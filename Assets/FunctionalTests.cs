using ISX;
using NUnit.Framework;
using UnityEngine.TestTools;

public class FunctionalTests : IPrebuildSetup
{
    public void Setup()
    {
        // The domain reload survival logic can get in the way badly during
        // development so for tests, we force resets to get the system into
        // a known state. This means we are guaranteed to have an unmodified
        // set of stock templates, usages, and processors.
        InputSystem.Reset();
    }
    
    [Test]
    public void CanCreateSimpleDeviceWithOneButton()
    {
        var setup = new InputControlSetup();
        var button = setup.AddControl("Button", "simpleButton");
        var device = setup.Finish();
        
        Assert.That(button.name, Is.EqualTo("simpleButton"));
        Assert.That(button.template.name, Is.EqualTo("Button"));
        Assert.That(button, Is.TypeOf<ButtonControl>());

        Assert.That(device.children, Has.Exactly(1).SameAs(button));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("simpleButton"));
    }

    [Test]
    public void CanAddNewDevice()
    {
    }
}

