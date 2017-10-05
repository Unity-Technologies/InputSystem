using System;
using System.Runtime.InteropServices;
using ISX;
using NUnit.Framework;
using UnityEngine;

////TODO: make work in player (ATM we rely on the domain reload logic; probably want to include that in debug players, too)

// These tests rely on the default template setup present in the code
// of the system (e.g. they make assumptions about Gamepad is set up).
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
        Setup();

        var setup = new InputControlSetup("Gamepad");

        // The default ButtonControl template has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetControl("start").children, Is.Empty);

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
        Assert.That(setup.GetControl("leftStick").children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetControl("leftStick").children, Has.Exactly(1).With.Property("name").EqualTo("x"));

        TearDown();
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUpDeviceFromJsonTemplate()
    {
        Setup();

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

        TearDown();
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetControlParametersThroughControlAttribute()
    {
        Setup();

        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.up.clamp, Is.True);
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));

        TearDown();
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetUsagesThroughControlAttribute()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.PrimaryStick));

        TearDown();
    }

    [Test]
    [Category("Templates")]
    public void Templates_CanSetAliasesThroughControlAttribute()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("square"));
        Assert.That(gamepad.xButton.aliases, Has.Exactly(1).EqualTo("x"));

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
    [Category("Devices")]
    public void Devices_CanCreateDeviceFromTemplateMatchedByDeviceDescriptor()
    {
        Setup();

        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : [
                    {
                        ""interface"" : ""AA|BB"",
                        ""manufacturer"" : ""Shtabble""
                    }
                ]
            }
        ";

        InputSystem.RegisterTemplate(deviceJson);

        var descriptor = new InputDeviceDescriptor
        {
            interfaceName = "BB",
            manufacturer = "Shtabble"
        };

        var device = InputSystem.AddDevice(descriptor);

        Assert.That(device.template, Is.EqualTo("MyDevice"));
        Assert.That(device, Is.TypeOf<Gamepad>());

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
        var gamepad = (Gamepad)setup.Finish();

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
        var gamepad = (Gamepad)setup.Finish();

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

        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(Marshal.SizeOf<GamepadState>() * 8));
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
        setup.Finish();

        var outputOffset = Marshal.OffsetOf<GamepadState>("motors").ToInt32();
        var rightMotorOffset = outputOffset + Marshal.OffsetOf<GamepadOutputState>("rightMotor").ToInt32();

        Assert.That(rightMotor.stateBlock.byteOffset, Is.EqualTo(rightMotorOffset));

        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_BeforeAddingDevice_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        Setup();

        var setup = new InputControlSetup("Gamepad");
        var device = (Gamepad)setup.Finish();

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(leftStickXOffset));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(leftStickYOffset));

        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_AfterAddingDevice_AllControlOffsetsAreRelativeToGlobalStateBuffer()
    {
        Setup();

        InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var leftStickOffset = Marshal.OffsetOf<GamepadState>("leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        var gamepad2StartOffset = gamepad2.stateBlock.byteOffset;

        Assert.That(gamepad2.leftStick.x.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickXOffset));
        Assert.That(gamepad2.leftStick.y.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickYOffset));

        TearDown();
    }

    [Test]
    [Category("State")]
    public void State_StateOfMultipleDevicesIsLaidOutSequentially()
    {
        Setup();

        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        var sizeofGamepadState = Marshal.SizeOf<GamepadState>();

        Assert.That(gamepad1.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(gamepad1.stateBlock.byteOffset + sizeofGamepadState));

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

        InputSystem.onDeviceChange +=
            (d, c) => Assert.Fail("Shouldn't send notification for duplicate adding of device.");

        InputSystem.AddDevice(device);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTriggersNotification()
    {
        Setup();

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

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItCurrent()
    {
        Setup();

        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(Gamepad.current, Is.SameAs(gamepad));

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        Setup();

        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.TryGetDeviceById(device.id), Is.SameAs(device));

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByTemplate()
    {
        Setup();

        var device = InputSystem.AddDevice("Gamepad");
        var result = InputSystem.GetDevice("Gamepad");

        Assert.That(result, Is.SameAs(device));

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

        Assert.That(gamepad1.id, Is.Not.EqualTo(gamepad2.id));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControls()
    {
        Setup();

        var setup = new InputControlSetup("Gamepad");
        var leftStick = setup.GetControl("leftStick");

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));

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
        var device = (Gamepad)setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(device.leftStick.value, Is.EqualTo(default(Vector2)));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPath()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/Gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPathCaseInsensitive()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamePAD/LeftSTICK");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByUsage()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{primaryStick}");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOfControlsFoundByUsage()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{primaryStick}/x");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick.x));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTemplate()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/<stick>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDevicesByTemplates()
    {
        Setup();

        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsFromMultipleDevices()
    {
        Setup();

        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var matches = InputSystem.GetControls("/*/*Stick");

        Assert.That(matches, Has.Count.EqualTo(4));

        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.rightStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.rightStick));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanOmitLeadingSlashWhenFindingControls()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTheirAliases()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matchByName = InputSystem.GetControls("/gamepad/buttonSouth");
        var matchByAlias1 = InputSystem.GetControls("/gamepad/x");
        var matchByAlias2 = InputSystem.GetControls("/gamepad/cross");

        Assert.That(matchByName, Has.Count.EqualTo(1));
        Assert.That(matchByName, Has.Exactly(1).SameAs(gamepad.xButton));
        Assert.That(matchByAlias1, Is.EqualTo(matchByName));
        Assert.That(matchByAlias2, Is.EqualTo(matchByName));

        TearDown();
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcardsInMiddleOfNames()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/g*pad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };
        var inputEvent = StateEvent.Create(gamepad.id, 0, newState);

        InputSystem.QueueEvent(inputEvent);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.value, Is.EqualTo(0.123f));
        Assert.That(gamepad.leftStick.y.value, Is.EqualTo(0.456f));

        TearDown();
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateEventToDeviceMakesItCurrent()
    {
        Setup();

        var gamepad = InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState();
        var inputEvent = StateEvent.Create(gamepad.id, 0, newState);

        InputSystem.QueueEvent(inputEvent);
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad));

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsSingleControl()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(sourcePath: "/gamepad/leftStick");

        Assert.That(action.sourceControls, Has.Count.EqualTo(1));
        Assert.That(action.sourceControls, Has.Exactly(1).SameAs(gamepad.leftStick));

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionThatTargetsMultipleControls()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(sourcePath: "/gamepad/*stick");

        Assert.That(action.sourceControls, Has.Count.EqualTo(2));
        Assert.That(action.sourceControls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.sourceControls, Has.Exactly(1).SameAs(gamepad.rightStick));

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_GoesIntoWaitingPhase()
    {
        Setup();

        InputSystem.AddDevice("Gamepad");

        var action = new InputAction(sourcePath: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionSet()
    {
        Setup();

        var action = new InputAction();

        Assert.That(action.actionSet, Is.Null);

        TearDown();
    }

    ////REVIEW: not sure whether this is the best behavior
    [Test]
    [Category("Actions")]
    public void Actions_SourcePathsLeadingNowhereAreIgnored()
    {
        Setup();

        var action = new InputAction(sourcePath: "nothing");

        Assert.DoesNotThrow(() => action.Enable());

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartOutInDisabledPhase()
    {
        Setup();

        var action = new InputAction();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));

        TearDown();
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionIsPerformedWhenSourceControlChangesValue()
    {
        Setup();

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputAction receivedAction = null;
        InputControl receivedControl = null;

        var action = new InputAction(sourcePath: "/gamepad/leftStick");
        action.onPerformed +=
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

        TearDown();
    }

    /*
    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldAction()
    {
        Setup();

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

        TearDown();
    }
    */

    ////TODO:-----------------------------------------------------------------
    [Test]
    [Category("State")]
    public void TODO_State_CanSpecifyBitOffsetsOnControlProperties()
    {
        Setup();

        //examine control setup on dpad

        TearDown();
    }

    [Test]
    [Category("State")]
    public void TODO_State_AppendsControlsWithoutForcedOffsetToEndOfState()
    {
        Setup();

        TearDown();
    }

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithFixedOffsets()
    {
        Setup();

        TearDown();
    }

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        Setup();

        TearDown();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_AddingANewDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
    }

    [Test]
    [Category("State")]
    public void TODO_State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
    }

    [Test]
    [Category("State")]
    public void TODO_State_CanDisableFixedUpdates()
    {
        //make sure it reduces memory usage
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_ReplacingTemplateAffectsAllDevicesUsingTemplate()
    {
    }

    [Test]
    [Category("Templates")]
    public void TODO_Templates_CanFindTemplateFromDeviceDescriptor()
    {
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanSwitchTemplateOfExistingDevice()
    {
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanLoadActionSetFromJson()
    {
        Setup();

        var json = @"
            {
                """"////TODO
            }
        ";

        TearDown();
    }
}
