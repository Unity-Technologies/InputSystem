using ISX;
using NUnit.Framework;

////TODO: make work in player (ATM we rely on the domain reload logic; probably want to include that in debug players, too)

public class FunctionalTests
{
    // Unity test tools don't seem to have proper setup/teardown support.
    // Seems like there's nothing for teardown and for setup, we'd either only
    // get a single setup call for all tests or would have to put an attribute
    // on every single test. So... we do it manually. Meh.
    // NUnits SetUp and TearDown attributes don't appear to work with the Unity
    // test runner either.
    void Setup()
    {
        // Put the system in a known state but save the current state first
        // so that we can restore it after we're done testing.
        InputSystem.Save();
        InputSystem.Reset();
    }

    void TearDown()
    {
        InputSystem.Restore();
    }
    
    [Test]
    public void CanCreatePrimitiveControlsFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        
        // The default ButtonControl template has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetChildren("start"), Is.Empty);

        TearDown();
    }

    [Test]
    public void CanCreateCompoundControlsFromTemplate()
    {
        Setup();
        
        const int kNumControlsInAStick = 6;

        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.GetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.GetChildren("leftStick"), Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetChildren("leftStick"), Has.Exactly(1).With.Property("name").EqualTo("x"));

        TearDown();
    }

    [Test]
    public void CanCreateDeviceFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();
        
        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
        
        TearDown();
    }

    [Test]
    public void CanCreateDeviceWithNestedState()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        // The gamepad's output state is nested inside GamepadState and requires the template
        // code to crawl inside the field to find the motor controls.
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftMotor"));
        
        TearDown();
    }

    [Test]
    public void CanFindControlsInSetupByPath()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.TryGetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.TryGetControl("leftStick/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/y"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/up"), Is.TypeOf<AxisControl>());
            
        TearDown();
    }

    [Test]
    public void DeviceAndControlsRememberTheirTemplates()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad) setup.Finish();
            
        Assert.That(gamepad.template.name, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.template.name, Is.EqualTo("Stick"));
        
        TearDown();
    }

    [Test]
    public void DevicesGetNameFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();
        
        Assert.That(device.name, Contains.Substring("Gamepad"));
        
        TearDown();
    }
    
    [Test]
    public void CanAddDeviceFromTemplate()
    {
        Setup();

        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
        
        TearDown();
    }

    [Test]
    public void AddingDeviceTwiceIsIgnored()
    {
        Setup();
        
        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice(device);
        
        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
        
        TearDown();
    }

    [Test]
    public void EnsuresDeviceNamesAreUnique()
    {
        Setup();

        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
        
        TearDown();
    }

    [Test]
    public void AssignsUniqueNumericIdToDevices()
    {
    }

    [Test]
    public void ReplacingTemplateAffectsAllDevicesUsingTemplate()
    {
    }
}

