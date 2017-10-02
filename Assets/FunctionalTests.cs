using System.Linq;
using ISX;
using NUnit.Framework;
using UnityEngine.TestTools;

public class FunctionalTests : IPrebuildSetup
{
    public void Setup()
    {
        // Put the system in a known state but save the current state first
        // so that we can restore it after we're done testing.
        InputSystem.Save();
        InputSystem.Reset();
        
        // NOTE: There's no teardown support in the Unity test tools so InputSystem.Restore()
        //       has to be manually called at the end of each test.
    }
    
    [Test]
    public void CanCreateSimpleDeviceWithPrimitiveControl()
    {
        var setup = new InputControlSetup();
        var button = setup.AddControl("Button", "simpleButton");
        var device = setup.Finish();
        
        Assert.That(button.name, Is.EqualTo("simpleButton"));
        Assert.That(button.template.name, Is.EqualTo("Button"));
        Assert.That(button, Is.TypeOf<ButtonControl>());

        Assert.That(device.children, Has.Exactly(1).SameAs(button));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("simpleButton"));

        InputSystem.Restore();
    }

    [Test]
    public void CanCreateSimpleDeviceWithCompoundControl()
    {
        const int kNumControlsInAStick = 6;
        
        var setup = new InputControlSetup();
        var stick = setup.AddControl("Stick", "stick");
        var device = setup.Finish();

        Assert.That(stick, Is.TypeOf<StickControl>());
        Assert.That(stick.children, Has.Count.EqualTo(kNumControlsInAStick));

        Assert.That(device.children, Has.Count.EqualTo(1)); // Just stick itself.
        Assert.That(device.children.First(), Is.SameAs(stick));
        
        InputSystem.Restore();
    }

    [Test]
    public void CanAddNewDevice()
    {
    }
}

