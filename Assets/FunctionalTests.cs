using System.Linq;
using ISX;
using NUnit.Framework;
using UnityEngine.TestTools;

////TODO: make work in player (ATM we rely on the domain reload logic; probably want to include that in debug players, too)

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
    public void CanFindControlsInSetupByPath()
    {
        var setup = new InputControlSetup();
        setup.AddControl("Stick", "stick");

        Assert.That(setup.TryGetControl("stick"), Is.TypeOf<StickControl>());
        Assert.That(setup.TryGetControl("stick/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("stick/y"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("stick/up"), Is.TypeOf<AxisControl>());
            
        InputSystem.Restore();
    }

    [Test]
    public void CanCreateComplexDeviceWithState()
    {
        var setup = new InputControlSetup();
        setup.AddControl("Gamepad");
        var device = setup.Finish();
        
        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
        
        InputSystem.Restore();
    }

    [Test]
    public void CanCreateDeviceWithNestedState()
    {
        var setup = new InputControlSetup();
        setup.AddControl("Gamepad");
        var device = setup.Finish();

        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftMotor"));
        
        InputSystem.Restore();
    }

    [Test]
    public void CanAddNewDevice()
    {
    }

    [Test]
    public void ReplacingTemplateAffectsAllDevicesUsingTemplate()
    {
    }
}

