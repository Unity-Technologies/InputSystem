using System;
using System.Runtime.InteropServices;
using ISX;
using NUnit.Framework;
using UnityEngine;

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
    
    // The test categories give the feature area associated with the test:
    // a) Controls
    // b) Templates
    // c) Devices
    // d) State
    // e) Events
    // f) Actions
    // g) Bindings
    // h) Other
    
    [Test]
    [Category("Templates")]
    public void Templates_CanCreatePrimitiveControlsFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        
        // The default ButtonControl template has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetChildren("start"), Is.Empty);

        TearDown();
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanCreateCompoundControlsFromTemplate()
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
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();
        
        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceWithNestedState()
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
    [Category("Controls")]
    public void Controls_CanFindControlsInSetupByPath()
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
    [Category("Controls")]
    public void Controls_DeviceAndControlsRememberTheirTemplates()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad) setup.Finish();
            
        Assert.That(gamepad.template, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.template, Is.EqualTo("Stick"));
        
        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirParent()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad) setup.Finish();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
        
        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirDevices()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(leftStick.device, Is.SameAs(device));
        
        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_AskingValueOfControlBeforeDeviceAddedToSystemIsInvalidOperation()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.Throws<InvalidOperationException>(() =>
        {
             var value = device.leftStick.value;
        });
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_DevicesGetNameFromTemplate()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();
        
        Assert.That(device.name, Contains.Substring("Gamepad"));
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(Marshal.SizeOf<GamepadState>()*8));
        Assert.That(leftStick.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf<GamepadState>("leftStick").ToInt32()));
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var rightMotor = setup.GetControl("rightMotor");
        var device = setup.Finish();

        var outputOffset = Marshal.OffsetOf<GamepadState>("motors").ToInt32();
        var rightMotorOffset = outputOffset + Marshal.OffsetOf<GamepadOutputState>("rightMotor").ToInt32();

        Assert.That(rightMotor.stateBlock.byteOffset, Is.EqualTo(rightMotorOffset));
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad) setup.Finish();

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(leftStickXOffset));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(leftStickYOffset));
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_CanSpecifyBitOffsetsOnControlProperties()
    {
        Setup();
        
        //examine control setup on dpad
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_AppendsControlsWithoutForcedOffsetToEndOfState()
    {
        Setup();
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_SupportsBitAddressingControlsWithFixedOffsets()
    {
        Setup();
        
        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        Setup();
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromTemplate()
    {
        Setup();

        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTwiceIsIgnored()
    {
        Setup();
        
        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice(device);
        
        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_EnsuresDeviceNamesAreUnique()
    {
        Setup();

        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AssignsUniqueNumericIdToDevices()
    {
        Setup();
        
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        
        Assert.That(gamepad1.deviceId, Is.Not.EqualTo(gamepad2.deviceId));
        
        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControlsWhenAddingDevice()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        
        Assert.That(leftStick.path, Is.EqualTo("leftStick"));
        
        var device = setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));
        
        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_AfterAddingDeviceCanQueryValueOfControls()
    {
        Setup();
        
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad) setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));
        
        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingANewDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
    }

    [Test]
    [Category("State")]
    public void State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
    }

    [Test]
    [Category("State")]
    public void State_CanDisableFixedUpdates()
    {
        //make sure it reduces memory usage
    }

    [Test]
    [Category("Templates")]
    public void Templates_ReplacingTemplateAffectsAllDevicesUsingTemplate()
    {
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanFindTemplateFromDeviceDescriptor()
    {
    }
}

