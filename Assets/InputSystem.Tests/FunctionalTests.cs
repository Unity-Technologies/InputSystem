using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ISX;
using NUnit.Framework;
using UnityEngine;

////TODO: make work in player (ATM we rely on the domain reload logic; probably want to include that in debug players, too)

// These tests rely on the default template setup present in the code
// of the system (e.g. they make assumptions about Gamepad is set up).
public class FunctionalTests
{
    [SetUp]
    public void Setup()
    {
        InputSystem.Save();
        InputSystem.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.Restore();
    }

    // The test categories give the feature area associated with the test:
    //
    //     a) Controls
    //     b) Templates
    //     c) Devices
    //     d) State
    //     e) Events
    //     f) Actions
    //     g) Bindings
    //     h) Other

    [Test]
    [Category("Templates")]
    public void Templates_CanCreatePrimitiveControlsFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");

        // The default ButtonControl template has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetControl("start").children, Is.Empty);
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanCreateCompoundControlsFromTemplate()
    {
        const int kNumControlsInAStick = 6;

        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.GetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.GetControl("leftStick").children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetControl("leftStick").children, Has.Exactly(1).With.Property("name").EqualTo("x"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUpDeviceFromJsonTemplate()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2""
            }
        ";
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""myThing"",
                        ""template"" : ""MyControl"",
                        ""usage"" : ""LeftStick""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);
        InputSystem.RegisterTemplate(controlJson);

        var setup = new InputControlSetup("MyDevice");

        Assert.That(setup.GetControl("myThing/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.GetControl("myThing"), Has.Property("template").EqualTo("MyControl"));

        var device = setup.Finish();
        Assert.That(device, Is.TypeOf<InputDevice>());
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanExtendControlInBaseTemplateUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""format"" : ""BYTE""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var setup = new InputControlSetup("MyDevice");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.leftStick.x.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetControlParametersThroughControlAttribute()
    {
        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.up.clamp, Is.True);
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUsagesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.PrimaryStick));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetAliasesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("square"));
        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("x"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetParametersOnControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""clamp=true,clampMin=0.123,clampMax=0.456""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        Assert.That(device.rightTrigger.clamp, Is.True);
        Assert.That(device.rightTrigger.clampMin, Is.EqualTo(0.123).Within(0.00001f));
        Assert.That(device.rightTrigger.clampMax, Is.EqualTo(0.456).Within(0.00001f));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanAddProcessorsToControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        // NOTE: Unfortunately, this currently relies on an internal method (TryGetProcessor).

        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>(), Is.Not.Null);
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().min, Is.EqualTo(0.1).Within(0.00001f));
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().max, Is.EqualTo(0.9).Within(0.00001f));
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanFindTemplateFromDeviceDescriptor()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""mything.*""
                }
            }
        ";

        InputSystem.RegisterTemplate(json);

        var template = InputSystem.TryFindMatchingTemplate(new InputDeviceDescription
        {
            product = "MyThingy"
        });

        Assert.That(template, Is.EqualTo("MyDevice"));
    }

    [Test]
    [Category("Templates")]
    public void Templates_AddingTwoControlsWithSameName_WillCauseException()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""MyControl"",
                        ""template"" : ""Button""
                    },
                    {
                        ""name"" : ""MyControl"",
                        ""template"" : ""Button""
                    }
                ]
            }
        ";

        // We do minimal processing when adding a template so verification
        // only happens when we actually try to instantiate the template.
        InputSystem.RegisterTemplate(json);

        Assert.That(() => InputSystem.AddDevice("MyDevice"),
            Throws.TypeOf<Exception>().With.Property("Message").Contain("Duplicate control"));
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_ReplacingTemplateAffectsAllDevicesUsingTemplate()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceWithNestedState()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        // The gamepad's output state is nested inside GamepadState and requires the template
        // code to crawl inside the field to find the motor controls.
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftMotor"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplateMatchedByDeviceDescriptor()
    {
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""interface"" : ""AA|BB"",
                    ""manufacturer"" : ""Shtabble""
                }
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);

        var descriptor = new InputDeviceDescription
        {
            interfaceName = "BB",
            manufacturer = "Shtabble"
        };

        var device = InputSystem.AddDevice(descriptor);

        Assert.That(device.template, Is.EqualTo("MyDevice"));
        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsInSetupByPath()
    {
        var setup = new InputControlSetup("Gamepad");

        Assert.That(setup.TryGetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.TryGetControl("leftStick/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/y"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/up"), Is.TypeOf<AxisControl>());
    }

    [Test]
    [Category("Controls")]
    public void Controls_DeviceAndControlsRememberTheirTemplates()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.template, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.template, Is.EqualTo("Stick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirParent()
    {
        var setup = new InputControlSetup("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirDevices()
    {
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(leftStick.device, Is.SameAs(device));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AskingValueOfControlBeforeDeviceAddedToSystemIsInvalidOperation()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.Throws<InvalidOperationException>(() =>
            {
                var value = device.leftStick.value;
            });
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanProcessDeadzones()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        ////NOTE: Unfortunately, this relies on an internal method ATM.
        var processor = device.leftStick.TryGetProcessor<DeadzoneProcessor>();

        var firstState = new GamepadState {leftStick = new Vector2(0.05f, 0.05f)};
        var secondState = new GamepadState {leftStick = new Vector2(0.5f, 0.5f)};

        InputSystem.QueueStateEvent(device, firstState);
        InputSystem.Update();

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));

        InputSystem.QueueStateEvent(device, secondState);
        InputSystem.Update();

        Assert.That(device.leftStick.value, Is.EqualTo(processor.Process(new Vector2(0.5f, 0.5f))));
    }

    [Test]
    [Category("Devices")]
    public void Devices_DevicesGetNameFromTemplate()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device.name, Contains.Substring("Gamepad"));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(Marshal.SizeOf<GamepadState>() * 8));
        Assert.That(leftStick.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf<GamepadState>("leftStick").ToInt32()));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        var setup = new InputControlSetup("Gamepad");
        var rightMotor = setup.GetControl("rightMotor");
        setup.Finish();

        var outputOffset = Marshal.OffsetOf<GamepadState>("motors").ToInt32();
        var rightMotorOffset = outputOffset + Marshal.OffsetOf<GamepadOutputState>("rightMotor").ToInt32();

        Assert.That(rightMotor.stateBlock.byteOffset, Is.EqualTo(rightMotorOffset));
    }

    [Test]
    [Category("State")]
    public void State_BeforeAddingDevice_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(leftStickXOffset));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_AfterAddingDevice_AllControlOffsetsAreRelativeToGlobalStateBuffer()
    {
        InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        var gamepad2StartOffset = gamepad2.stateBlock.byteOffset;

        Assert.That(gamepad2.leftStick.x.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickXOffset));
        Assert.That(gamepad2.leftStick.y.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_StateOfMultipleDevicesIsLaidOutSequentially()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        var sizeofGamepadState = Marshal.SizeOf<GamepadState>();

        Assert.That(gamepad1.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(gamepad1.stateBlock.byteOffset + sizeofGamepadState));
    }

    [Test]
    [Category("State")]
    public void State_RunningUpdateSwapsCurrentAndPrevious()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var oldState = new GamepadState
        {
            leftStick = new Vector2(0.25f, 0.25f)
        };
        var newState = new GamepadState
        {
            leftStick = new Vector2(0.75f, 0.75f)
        };

        InputSystem.QueueStateEvent(gamepad, oldState);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.value, Is.EqualTo(new Vector2(0.75f, 0.75f)));
        Assert.That(gamepad.leftStick.previous, Is.EqualTo(new Vector2(0.25f, 0.25f)));
    }

    // This test makes sure that a double-buffered state scheme does not lose state. In double buffering,
    // this only works if either the entire state is refreshed each step -- which for us is not guaranteed
    // as we don't know if a state event for a device will happen on a frame -- or if state is copied forward
    // between the buffers.
    [Test]
    [Category("State")]
    public void State_UpdateWithoutStateEventDoesNotAlterStateOfDevice()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var state = new GamepadState
        {
            leftStick = new Vector2(0.25f, 0.25f)
        };

        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        InputSystem.Update();

        Assert.That(gamepad.leftStick.value, Is.EqualTo(new Vector2(0.25f, 0.25f)));
    }

    // The state layout for a given device is not fixed. Even though Gamepad, for example, specifies
    // GamepadState as its state struct, this does not necessarily mean that an actual Gamepad instance
    // will actually end up with that specific state layout. This is why Gamepad should not assume
    // that 'currentValuePtr' is a pointer to a GamepadState.
    //
    // Templates can be used to re-arrange the state layout of their base template. One case where
    // this is useful are HIDs. On OSX, for example, gamepad state data does not arrive in its own
    // distinct format but rather comes in as the same generic state data as any other HID device.
    // Yet we still want a gamepad to come out as a Gamepad and not as a generic InputDevice. If we
    // weren't able to customize the state layout of a gamepad, we'd have to have code somewhere
    // along the way that takes the incoming HID data, interprets it to determine that it is in
    // fact coming from a gamepad HID, and re-arranges it into a GamepadState-compatible format
    // (which requires knowledge of the specific layout used by the HID). By having flexibly state
    // layouts we can do this entirely through data using just templates.
    //
    // A template that customizes state layout can also "park" unused controls outside the block of
    // data that will actually be sent in via state events. Space for the unused controls will still
    // be allocated in the state buffers (since InputControls still refer to it) but InputManager
    // is okay with sending StateEvents that are shorter than the full state block of a device.
    ////REVIEW: we might want to equip InputControls with the ability to be disabled (in which case the return default values)
    [Test]
    [Category("State")]
    public void State_CanCustomizeStateLayoutOfDevice()
    {
        // Create a custom template that moves the offsets of some controls around.
        var jsonTemplate = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""format"" : ""CUST"",
                ""controls"" : [
                    {
                        ""name"" : ""buttonSouth"",
                        ""offset"" : 800
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(jsonTemplate);

        var setup = new InputControlSetup("CustomGamepad");
        Assert.That(setup.GetControl("buttonSouth").stateBlock.byteOffset, Is.EqualTo(800));

        var device = (Gamepad)setup.Finish();
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(801 * 8)); // Button bitfield adds one byte.
    }

    struct CustomGamepadState
    {
        public short rightTrigger;
    }

    [Test]
    [Category("State")]
    public void State_CanChangeRepresentationOfAxisControl()
    {
        // Make right trigger be represented as just a short and force it to different offset.
        var jsonTemplate = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""format"" : ""SHRT"",
                        ""offset"" : 0
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(jsonTemplate);

        var setup = new InputControlSetup("CustomGamepad");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.rightTrigger.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
    }

    [Test]
    [Category("State")]
    public void State_AppendsControlsWithoutForcedOffsetToEndOfState()
    {
        var json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""controlWithFixedOffset"",
                        ""template"" : ""Analog"",
                        ""offset"" : ""10"",
                        ""format"" : ""FLT""
                    },
                    {
                        ""name"" : ""controlWithAutomaticOffset"",
                        ""template"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(json);
        var setup = new InputControlSetup("MyDevice");

        Assert.That(setup.GetControl("controlWithAutomaticOffset").stateBlock.byteOffset, Is.EqualTo(14));

        var device = setup.Finish();
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(15 * 8));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromTemplate()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTwiceIsIgnored()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.onDeviceChange +=
            (d, c) => Assert.Fail("Shouldn't send notification for duplicate adding of device.");

        InputSystem.AddDevice(device);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTriggersNotification()
    {
        var receivedCallCount = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                ++receivedCallCount;
                receivedDevice = device;
                receiveDeviceChange = change;
            };

        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(receivedCallCount, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Added));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItConnected()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = setup.Finish();

        Assert.That(device.connected, Is.False);

        InputSystem.AddDevice(device);

        Assert.That(device.connected, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.TryGetDeviceById(device.id), Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByTemplate()
    {
        var device = InputSystem.AddDevice("Gamepad");
        var result = InputSystem.GetDevice("Gamepad");

        Assert.That(result, Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EnsuresDeviceNamesAreUnique()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AssignsUniqueNumericIdToDevices()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.id, Is.Not.EqualTo(gamepad2.id));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeDisconnectedAndReconnected()
    {
        var device = InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                ++receivedCalls;
                receivedDevice = d;
                receiveDeviceChange = c;
            };

        InputSystem.QueueDisconnectEvent(device);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Disconnected));
        Assert.That(device.connected, Is.False);

        receivedCalls = 0;
        receivedDevice = null;
        receiveDeviceChange = null;

        InputSystem.QueueConnectEvent(device);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Connected));
        Assert.That(device.connected, Is.True);
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gamepad")]
    [TestCase("Keyboard")]
    [TestCase("Mouse")]
    [TestCase("HMD")]
    [TestCase("XRController")]
    public void Devices_CanCreateDevice(string template)
    {
        var device = InputSystem.AddDevice(template);

        Assert.That(device, Is.InstanceOf<InputDevice>());
        Assert.That(device.template, Is.EqualTo(template));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControls()
    {
        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));

        var device = setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AfterAddingDeviceCanQueryValueOfControls()
    {
        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPath()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/Gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPathCaseInsensitive()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamePAD/LeftSTICK");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{primaryStick}");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOfControlsFoundByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{primaryStick}/x");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick.x));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTemplate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/<stick>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDevicesByTemplates()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByBaseTemplate()
    {
        const string json = @"
            {
                ""name"" : ""MyGamepad"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterTemplate(json);
        var device = InputSystem.AddDevice("MyGamepad");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(device));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsFromMultipleDevices()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var matches = InputSystem.GetControls("/*/*Stick");

        Assert.That(matches, Has.Count.EqualTo(4));

        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.rightStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.rightStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanOmitLeadingSlashWhenFindingControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void TODO_Controls_CanFindControlsByTheirAliases()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matchByName = InputSystem.GetControls("/gamepad/buttonSouth");
        var matchByAlias1 = InputSystem.GetControls("/gamepad/x");
        var matchByAlias2 = InputSystem.GetControls("/gamepad/cross");

        Assert.That(matchByName, Has.Count.EqualTo(1));
        Assert.That(matchByName, Has.Exactly(1).SameAs(gamepad.xButton));
        Assert.That(matchByAlias1, Is.EqualTo(matchByName));
        Assert.That(matchByAlias2, Is.EqualTo(matchByName));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcardsInMiddleOfNames()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/g*pad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    // This is one of the most central tests. If this one breaks, it most often
    // hints at the state layouting or state updating machinery being borked.
    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.value, Is.EqualTo(0.123f));
        Assert.That(gamepad.leftStick.y.value, Is.EqualTo(0.456f));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateEventToDeviceMakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState();

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithoutBeforeRenderEnabled_DoesNothingInBeforeRenderUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.value, Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithBeforeRenderEnabled_UpdatesDeviceInBeforeRender()
    {
        // Could use one of the tracking templates but let's do it with a
        // custom template that enables before render updates on a gamepad.
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.value, Is.EqualTo(new Vector2(0.123f, 0.456f)));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsSingleControl()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsMultipleControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/*stick");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_GoesIntoWaitingPhase()
    {
        InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionSet()
    {
        var action = new InputAction();

        Assert.That(action.actionSet, Is.Null);
    }

    ////REVIEW: not sure whether this is the best behavior
    [Test]
    [Category("Actions")]
    public void Actions_SourcePathsLeadingNowhereAreIgnored()
    {
        var action = new InputAction(binding: "nothing");

        Assert.DoesNotThrow(() => action.Enable());
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartOutInDisabledPhase()
    {
        var action = new InputAction();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionIsPerformedWhenSourceControlChangesValue()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputAction receivedAction = null;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.performed +=
            (a, c) =>
            {
                ++receivedCalls;
                receivedAction = a;
                receivedControl = c;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.Enable();

        var state = new GamepadState
        {
            leftStick = new Vector2(0.5f, 0.5f)
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanListenForStateChangeOnEntireDevice()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad");
        action.performed +=
            (a, c) =>
            {
                ++receivedCalls;
                receivedControl = c;
            };
        action.Enable();

        var state = new GamepadState
        {
            rightTrigger = 0.5f
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad)); // We do not drill down to find the actual control that changed.
    }

    /*
    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var onPerformedReceivedCalls = 0;
        InputAction onPerformedAction = null;
        InputControl onPerfomedControl = null;

        var onStartedReceivedCalls = 0;
        InputAction onStartedAction = null;
        InputControl onStartedControl = null;

        var action = new InputAction(sourcePath: "/gamepad/{primaryAction}", modifiers: "hold(0.4)");
        action.onPerformed +=
            (a, c) =>
            {
                ++onPerformedReceivedCalls;
                onPerformedAction = a;
                onPerfomedControl = c;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.onStarted +=
            (a, c) =>
            {
                ++onStartedReceivedCalls;
                onStartedAction = a;
                onStartedControl = c;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Started));
            };
        action.Enable();

        var stateEvent = StateEvent.Create(
            gamepad.id,
            0.0, // First point in time.
            new GamepadState
            {
                buttons = 1 << (int) GamepadState.Button.South
            });

        InputSystem.QueueEvent(stateEvent);

        stateEvent.baseEvent.time = 0.5; // Second point in time.
        stateEvent.state.buttons = 0;

        InputSystem.QueueEvent(stateEvent);
        InputSystem.Update();

        Assert.That(onPerformedReceivedCalls, Is.EqualTo(1));
        Assert.That(receivedOnPerformedCalls, Is.Zero);
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.aButton));

        Assert.That(receivedOnStartedCalls, Is.EqualTo(0));
        Assert.That(receivedOnPerformedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.aButton));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }
    */

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionSetFromJson()
    {
        const string json = @"
            {
                ""sets"" : [
                    {
                        ""name"" : ""default"",
                        ""actions"" : [
                            {
                                ""name"" : ""jump""
                            }
                        ]
                    }
                ]
            }
        ";

        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(1));
        Assert.That(sets[0], Has.Property("name").EqualTo("default"));
        Assert.That(sets[0].actions, Has.Count.EqualTo(1));
        Assert.That(sets[0].actions, Has.Exactly(1).With.Property("name").EqualTo("jump"));
    }

#if UNITY_EDITOR
    [Test]
    [Category("Misc")]
    public void Misc_CanSaveAndRestoreStateInEditor()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterTemplate(json);
        InputSystem.AddDevice("MyDevice");

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("template").EqualTo("MyDevice").And.TypeOf<Gamepad>());
    }

#endif

    ////TODO:-----------------------------------------------------------------
    [Test]
    [Category("State")]
    public void TODO_State_CanSpecifyBitOffsetsOnControlProperties()
    {
        //examine control setup on dpad
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithFixedOffsets()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_AddingANewDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_CanDisableFixedUpdates()
    {
        //make sure it reduces memory usage
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanSwitchTemplateOfExistingDevice()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanBeRemoved()
    {
        ////TODO
        Assert.Fail();
    }
}
